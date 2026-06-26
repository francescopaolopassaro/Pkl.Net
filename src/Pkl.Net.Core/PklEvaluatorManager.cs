// Pkl.Net - Passaro Francesco Paolo 2026
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using PklNet.Core.Internal;
using PklNet.Core.Internal.Messages;

namespace PklNet.Core;

/// <summary>
/// Manages a single <c>pkl server</c> process and creates <see cref="PklEvaluator"/> instances on it.
/// Dispose to shut down the underlying process.
/// </summary>
public sealed class PklEvaluatorManager : IDisposable
{
    private readonly PklProcess _process;
    private readonly ConcurrentDictionary<long, TaskCompletionWrapper> _pendingEvals = new();
    private readonly ConcurrentDictionary<long, TaskCompletionWrapper> _pendingCreates = new();
    private readonly ConcurrentDictionary<long, PklEvaluator> _evaluators = new();
    private readonly List<IPklModuleReader> _moduleReaders;
    private readonly List<IPklResourceReader> _resourceReaders;
    private bool _disposed;
    private static long _nextRequestId;

    internal PklEvaluatorManager(PklProcess process, List<IPklModuleReader> moduleReaders, List<IPklResourceReader> resourceReaders)
    {
        _process = process;
        _moduleReaders = moduleReaders;
        _resourceReaders = resourceReaders;
        _process.StartReaderLoop();
        _ = System.Threading.Tasks.Task.Run(MessageLoop);
    }

    // ─── Public factory ────────────────────────────────────────────────────────

    /// <summary>Creates an <see cref="PklEvaluatorManager"/> by starting the pkl process.</summary>
    public static PklEvaluatorManager Create(EvaluatorOptions? options = null, string[]? command = null)
    {
        options ??= EvaluatorOptions.Preconfigured();
        var cmd = command ?? ResolveCommand();
        var proc = new PklProcess(cmd);
        var moduleReaders  = options.ModuleReaders  ?? [];
        var resourceReaders = options.ResourceReaders ?? [];
        var manager = new PklEvaluatorManager(proc, moduleReaders, resourceReaders);
        return manager;
    }

    private static string[] ResolveCommand()
    {
        var env = Environment.GetEnvironmentVariable("PKL_EXEC");
        if (!string.IsNullOrWhiteSpace(env))
        {
            env = env.Replace(" --server", "", StringComparison.Ordinal).Trim();
            return env.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }
        return ["pkl"];
    }

    // ─── Create evaluator ──────────────────────────────────────────────────────

    /// <summary>Creates a new evaluator with the given options.</summary>
    public PklEvaluator NewEvaluator(EvaluatorOptions? options = null, CancellationToken ct = default)
    {
        options ??= EvaluatorOptions.Preconfigured();
        var requestId = NextId();
        var wrapper = new TaskCompletionWrapper(ct);
        _pendingCreates[requestId] = wrapper;

        _process.Send(BuildCreateMessage(requestId, options));

        if (!wrapper.WaitOne(ct))
            throw new OperationCanceledException(ct);

        var resp = (CreateEvaluatorResponse)wrapper.Result!;
        if (!string.IsNullOrEmpty(resp.Error))
            throw new PklException(resp.Error);

        var eval = new PklEvaluator(this, resp.EvaluatorId, options.Logger ?? NullLogger.Instance);
        _evaluators[resp.EvaluatorId] = eval;
        return eval;
    }

    // ─── Evaluate (called by PklEvaluator) ─────────────────────────────────────

    internal byte[]? Evaluate(long evaluatorId, ModuleSource source, string? expr, IPklLogger logger, CancellationToken ct)
    {
        var requestId = NextId();
        var wrapper = new TaskCompletionWrapper(ct);
        _pendingEvals[requestId] = wrapper;

        _process.Send(new EvaluateMessage
        {
            RequestId    = requestId,
            EvaluatorId  = evaluatorId,
            ModuleUri    = source.Uri,
            ModuleText   = source.Text,
            Expr         = expr,
        });

        if (!wrapper.WaitOne(ct))
            throw new OperationCanceledException(ct);

        var resp = (EvaluateResponse)wrapper.Result!;
        if (!string.IsNullOrEmpty(resp.Error))
            throw new PklException(resp.Error);

        return resp.Result;
    }

    internal void CloseEvaluator(long evaluatorId)
    {
        _evaluators.TryRemove(evaluatorId, out _);
        try { _process.Send(new CloseEvaluatorMessage { EvaluatorId = evaluatorId }); }
        catch { /* process may already be gone */ }
    }

    // ─── Message dispatch loop ─────────────────────────────────────────────────

    private void MessageLoop()
    {
        using var cts = new CancellationTokenSource();
        while (!_disposed)
        {
            if (!_process.TryTake(out var msg, cts.Token) || msg is null)
                break;

            switch (msg)
            {
                case CreateEvaluatorResponse cer:
                    if (_pendingCreates.TryRemove(cer.RequestId, out var cw))
                        cw.SetResult(cer);
                    break;

                case EvaluateResponse er:
                    if (_pendingEvals.TryRemove(er.RequestId, out var ew))
                        ew.SetResult(er);
                    break;

                case LogMessage log:
                    if (_evaluators.TryGetValue(log.EvaluatorId, out _))
                    {
                        // Logger is stored per-evaluator; retrieve it if needed
                        // For now dispatch to all registered loggers
                    }
                    break;

                case ReadResourceRequest rrq:
                    HandleReadResource(rrq);
                    break;

                case ReadModuleRequest rmq:
                    HandleReadModule(rmq);
                    break;

                case ListResourcesRequest lrq:
                    HandleListResources(lrq);
                    break;

                case ListModulesRequest lmq:
                    HandleListModules(lmq);
                    break;
            }
        }
    }

    private void HandleReadResource(ReadResourceRequest req)
    {
        var uri = new Uri(req.Uri);
        foreach (var reader in _resourceReaders)
        {
            if (reader.Scheme == uri.Scheme)
            {
                try
                {
                    var contents = reader.Read(uri);
                    _process.Send(new ReadResourceResponse { RequestId = req.RequestId, EvaluatorId = req.EvaluatorId, Contents = contents });
                }
                catch (Exception ex)
                {
                    _process.Send(new ReadResourceResponse { RequestId = req.RequestId, EvaluatorId = req.EvaluatorId, Error = ex.Message });
                }
                return;
            }
        }
        _process.Send(new ReadResourceResponse { RequestId = req.RequestId, EvaluatorId = req.EvaluatorId, Error = $"No reader for scheme '{uri.Scheme}'" });
    }

    private void HandleReadModule(ReadModuleRequest req)
    {
        var uri = new Uri(req.Uri);
        foreach (var reader in _moduleReaders)
        {
            if (reader.Scheme == uri.Scheme)
            {
                try
                {
                    var contents = reader.Read(uri);
                    _process.Send(new ReadModuleResponse { RequestId = req.RequestId, EvaluatorId = req.EvaluatorId, Contents = contents });
                }
                catch (Exception ex)
                {
                    _process.Send(new ReadModuleResponse { RequestId = req.RequestId, EvaluatorId = req.EvaluatorId, Error = ex.Message });
                }
                return;
            }
        }
        _process.Send(new ReadModuleResponse { RequestId = req.RequestId, EvaluatorId = req.EvaluatorId, Error = $"No reader for scheme '{uri.Scheme}'" });
    }

    private void HandleListResources(ListResourcesRequest req)
    {
        var uri = new Uri(req.Uri);
        foreach (var reader in _resourceReaders)
        {
            if (reader.Scheme == uri.Scheme)
            {
                try
                {
                    var elements = reader.ListElements(uri);
                    var paths = new System.Collections.Generic.List<PathElement>(elements.Count);
                    foreach (var (name, isDir) in elements)
                        paths.Add(new PathElement { Name = name, IsDirectory = isDir });
                    _process.Send(new ListResourcesResponse { RequestId = req.RequestId, EvaluatorId = req.EvaluatorId, PathElements = paths });
                }
                catch (Exception ex)
                {
                    _process.Send(new ListResourcesResponse { RequestId = req.RequestId, EvaluatorId = req.EvaluatorId, Error = ex.Message });
                }
                return;
            }
        }
        _process.Send(new ListResourcesResponse { RequestId = req.RequestId, EvaluatorId = req.EvaluatorId, Error = $"No reader for scheme '{uri.Scheme}'" });
    }

    private void HandleListModules(ListModulesRequest req)
    {
        var uri = new Uri(req.Uri);
        foreach (var reader in _moduleReaders)
        {
            if (reader.Scheme == uri.Scheme)
            {
                try
                {
                    var elements = reader.ListElements(uri);
                    var paths = new System.Collections.Generic.List<PathElement>(elements.Count);
                    foreach (var (name, isDir) in elements)
                        paths.Add(new PathElement { Name = name, IsDirectory = isDir });
                    _process.Send(new ListModulesResponse { RequestId = req.RequestId, EvaluatorId = req.EvaluatorId, PathElements = paths });
                }
                catch (Exception ex)
                {
                    _process.Send(new ListModulesResponse { RequestId = req.RequestId, EvaluatorId = req.EvaluatorId, Error = ex.Message });
                }
                return;
            }
        }
        _process.Send(new ListModulesResponse { RequestId = req.RequestId, EvaluatorId = req.EvaluatorId, Error = $"No reader for scheme '{uri.Scheme}'" });
    }

    private static CreateEvaluatorMessage BuildCreateMessage(long requestId, EvaluatorOptions o)
    {
        var msg = new CreateEvaluatorMessage
        {
            RequestId        = requestId,
            AllowedModules   = o.AllowedModules,
            AllowedResources = o.AllowedResources,
            Env              = o.Env,
            Properties       = o.Properties,
            ModulePaths      = o.ModulePaths,
            CacheDir         = o.CacheDir,
            RootDir          = o.RootDir,
            OutputFormat     = o.OutputFormat,
            TimeoutSeconds   = o.Timeout.HasValue ? (long)o.Timeout.Value.TotalSeconds : null,
        };

        if (o.ResourceReaders?.Count > 0)
        {
            msg.ResourceReaders = [];
            foreach (var r in o.ResourceReaders)
                msg.ResourceReaders.Add(new ResourceReaderSpec { Scheme = r.Scheme, HasHierarchicalUris = r.HasHierarchicalUris, IsGlobbable = r.IsGlobbable });
        }

        if (o.ModuleReaders?.Count > 0)
        {
            msg.ModuleReaders = [];
            foreach (var r in o.ModuleReaders)
                msg.ModuleReaders.Add(new ModuleReaderSpec { Scheme = r.Scheme, HasHierarchicalUris = r.HasHierarchicalUris, IsGlobbable = r.IsGlobbable, IsLocal = r.IsLocal });
        }

        return msg;
    }

    private static long NextId() => Interlocked.Increment(ref _nextRequestId);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var ev in _evaluators.Values)
            try { ev.Close(); } catch { }
        _process.Dispose();
    }

    // ─── Inner helper ──────────────────────────────────────────────────────────

    private sealed class TaskCompletionWrapper
    {
        private IncomingMessage? _result;
        private readonly ManualResetEventSlim _event = new(false);

        internal TaskCompletionWrapper(CancellationToken ct) { }

        internal void SetResult(IncomingMessage result)
        {
            _result = result;
            _event.Set();
        }

        internal bool WaitOne(CancellationToken ct)
        {
            try { _event.Wait(ct); return true; }
            catch (OperationCanceledException) { return false; }
        }

        internal IncomingMessage? Result => _result;
    }
}

/// <summary>Thrown when the Pkl evaluator returns an error.</summary>
public sealed class PklException : Exception
{
    public PklException(string message) : base(message) { }
}
