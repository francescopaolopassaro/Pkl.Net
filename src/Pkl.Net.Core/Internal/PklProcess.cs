// Pkl.Net - Passaro Francesco Paolo 2026
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PklNet.Core.Internal.Messages;

namespace PklNet.Core.Internal;

/// <summary>
/// Manages the lifecycle of the <c>pkl server</c> child process and its binary streams.
/// Runs a dedicated reader loop on a background thread.
/// </summary>
internal sealed class PklProcess : IDisposable
{
    private readonly Process _process;
    private readonly MsgPackWriter _writer;
    private readonly MsgPackReader _reader;
    private readonly BlockingCollection<IncomingMessage> _inbox = new(64);
    private readonly CancellationTokenSource _cts = new();
    private readonly object _writeLock = new();
    private Thread? _readerThread;
    private bool _disposed;

    internal PklProcess(string[] command)
    {
        var exe  = command.Length > 0 ? command[0] : "pkl";
        var args = command.Length > 1 ? string.Join(" ", command[1..]) + " server" : "server";

        _process = new Process
        {
            StartInfo = new ProcessStartInfo(exe, args)
            {
                UseShellExecute        = false,
                RedirectStandardInput  = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = false,
            }
        };

        _process.Start();

        _writer = new MsgPackWriter(_process.StandardInput.BaseStream);
        _reader = new MsgPackReader(_process.StandardOutput.BaseStream);
    }

    internal void StartReaderLoop()
    {
        _readerThread = new Thread(ReaderLoop) { IsBackground = true, Name = "PklReaderLoop" };
        _readerThread.Start();
    }

    private void ReaderLoop()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                var msg = ProtocolDecoder.Decode(_reader);
                _inbox.Add(msg);
            }
        }
        catch (EndOfStreamException) { /* process exited */ }
        catch (OperationCanceledException) { /* normal shutdown */ }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PklReaderLoop] Fatal: {ex.Message}");
        }
        finally
        {
            _inbox.CompleteAdding();
        }
    }

    internal void Send(OutgoingMessage msg)
    {
        lock (_writeLock)
        {
            using var buf = new MemoryStream();
            var w = new MsgPackWriter(buf);
            msg.Write(w);
            var bytes = buf.ToArray();
            _process.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
            _process.StandardInput.BaseStream.Flush();
        }
    }

    /// <summary>Blocks until a message arrives or the token is cancelled.</summary>
    internal bool TryTake(out IncomingMessage? msg, CancellationToken ct)
    {
        try
        {
            msg = _inbox.Take(ct);
            return true;
        }
        catch (OperationCanceledException)
        {
            msg = null;
            return false;
        }
        catch (InvalidOperationException)
        {
            // collection completed
            msg = null;
            return false;
        }
    }

    internal string GetVersion()
    {
        var exe = _process.StartInfo.FileName;
        using var p = Process.Start(new ProcessStartInfo(exe, "--version")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
        })!;
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        // "Pkl 0.27.0 (something)"
        var idx = output.IndexOf("Pkl ", StringComparison.Ordinal);
        if (idx < 0) return "0.0.0";
        var rest = output[(idx + 4)..].Split(' ')[0].Trim();
        return rest;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        try
        {
            if (!_process.HasExited)
            {
                _process.StandardInput.Close();
                if (!_process.WaitForExit(5_000))
                    _process.Kill();
            }
        }
        catch { /* best effort */ }
        _process.Dispose();
        _cts.Dispose();
    }
}
