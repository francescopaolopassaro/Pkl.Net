// Pkl.Net - Passaro Francesco Paolo 2026
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PklNet.Core.Internal;
using PklNet.Core.Internal.Messages;

namespace PklNet.Core;

/// <summary>Evaluates Pkl modules via the pkl-server process.</summary>
public sealed class PklEvaluator : IDisposable, IAsyncDisposable
{
    private readonly PklEvaluatorManager _manager;
    private readonly long _evaluatorId;
    private readonly IPklLogger _logger;
    private bool _closed;

    internal PklEvaluator(PklEvaluatorManager manager, long evaluatorId, IPklLogger logger)
    {
        _manager = manager;
        _evaluatorId = evaluatorId;
        _logger = logger;
    }

    // ─── Evaluate module → typed C# object ────────────────────────────────────

    /// <summary>Evaluates the module and deserializes the result into <typeparamref name="T"/>.</summary>
    public T? EvaluateModule<T>(ModuleSource source, CancellationToken ct = default)
        => EvaluateExpression<T>(source, null, ct);

    /// <summary>Evaluates a Pkl expression on the module and deserializes the result.</summary>
    public T? EvaluateExpression<T>(ModuleSource source, string? expr, CancellationToken ct = default)
    {
        var raw = EvaluateRaw(source, expr, ct);
        if (raw is null || raw.Length == 0) return default;
        var decoder = new PklValueDecoder(SchemaRegistry.GetAll());
        return decoder.Decode<T>(raw);
    }

    /// <summary>Evaluates <c>output.text</c> and returns it as a string.</summary>
    public string EvaluateOutputText(ModuleSource source, CancellationToken ct = default)
    {
        var raw = EvaluateRaw(source, "output.text", ct);
        if (raw is null || raw.Length == 0) return "";
        // result is a plain msgpack string
        using var ms = new MemoryStream(raw);
        var r = new MsgPackReader(ms);
        return r.ReadString() ?? "";
    }

    /// <summary>Evaluates <c>output.files</c> and returns each file's text keyed by path.</summary>
    public Dictionary<string, string> EvaluateOutputFiles(ModuleSource source, CancellationToken ct = default)
    {
        var expr = "output.files?.toMap()?.mapValues((_, it) -> it.text) ?? Map()";
        return EvaluateExpression<Dictionary<string, string>>(source, expr, ct)
               ?? new Dictionary<string, string>();
    }

    /// <summary>Returns the raw MsgPack bytes of the evaluation result.</summary>
    public byte[]? EvaluateRaw(ModuleSource source, string? expr, CancellationToken ct = default)
    {
        EnsureOpen();
        return _manager.Evaluate(_evaluatorId, source, expr, _logger, ct);
    }

    // ─── Lifecycle ─────────────────────────────────────────────────────────────

    public void Close()
    {
        if (_closed) return;
        _closed = true;
        _manager.CloseEvaluator(_evaluatorId);
    }

    public void Dispose() => Close();

    public ValueTask DisposeAsync() { Close(); return ValueTask.CompletedTask; }

    private void EnsureOpen()
    {
        if (_closed) throw new ObjectDisposedException(nameof(PklEvaluator), "Evaluator has been closed.");
    }
}
