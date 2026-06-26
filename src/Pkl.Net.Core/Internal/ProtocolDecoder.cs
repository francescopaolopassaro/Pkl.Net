// Pkl.Net - Passaro Francesco Paolo 2026
using System;
using System.IO;
using PklNet.Core.Internal.Messages;

namespace PklNet.Core.Internal;

/// <summary>
/// Decodes the pkl-server binary protocol messages from stdout.
/// Each message is encoded as a 2-element msgpack array: [code, {map}].
/// </summary>
internal static class ProtocolDecoder
{
    internal static IncomingMessage Decode(MsgPackReader r)
    {
        var arrLen = r.ReadArrayHeader();
        if (arrLen != 2) throw new InvalidDataException($"Expected 2-element protocol array, got {arrLen}");

        var code = (int)r.ReadInt();
        var mapLen = r.ReadMapHeader();

        return code switch
        {
            MessageCodes.CreateEvaluatorResponse => ReadCreateEvaluatorResponse(r, mapLen),
            MessageCodes.EvaluateResponse        => ReadEvaluateResponse(r, mapLen),
            MessageCodes.EvaluateLog             => ReadLog(r, mapLen),
            MessageCodes.EvaluateRead            => ReadReadResource(r, mapLen),
            MessageCodes.EvaluateReadModule      => ReadReadModule(r, mapLen),
            MessageCodes.ListResourcesRequest    => ReadListResources(r, mapLen),
            MessageCodes.ListModulesRequest      => ReadListModules(r, mapLen),
            MessageCodes.InitializeModuleReaderRequest   => ReadInitModuleReader(r, mapLen),
            MessageCodes.InitializeResourceReaderRequest => ReadInitResourceReader(r, mapLen),
            MessageCodes.CloseExternalProcess    => ReadCloseExternalProcess(r, mapLen),
            _ => throw new InvalidDataException($"Unknown protocol message code: 0x{code:x2}")
        };
    }

    private static CreateEvaluatorResponse ReadCreateEvaluatorResponse(MsgPackReader r, int mapLen)
    {
        var msg = new CreateEvaluatorResponse();
        for (int i = 0; i < mapLen; i++)
        {
            var key = r.ReadString()!;
            switch (key)
            {
                case "requestId":   msg.RequestId = r.ReadInt(); break;
                case "evaluatorId": msg.EvaluatorId = r.ReadInt(); break;
                case "error":       msg.Error = r.ReadString(); break;
                default: r.Skip(); break;
            }
        }
        return msg;
    }

    private static EvaluateResponse ReadEvaluateResponse(MsgPackReader r, int mapLen)
    {
        var msg = new EvaluateResponse();
        for (int i = 0; i < mapLen; i++)
        {
            var key = r.ReadString()!;
            switch (key)
            {
                case "requestId":   msg.RequestId = r.ReadInt(); break;
                case "evaluatorId": msg.EvaluatorId = r.ReadInt(); break;
                case "result":      msg.Result = r.ReadBytes(); break;
                case "error":       msg.Error = r.ReadString(); break;
                default: r.Skip(); break;
            }
        }
        return msg;
    }

    private static LogMessage ReadLog(MsgPackReader r, int mapLen)
    {
        var msg = new LogMessage();
        for (int i = 0; i < mapLen; i++)
        {
            var key = r.ReadString()!;
            switch (key)
            {
                case "evaluatorId": msg.EvaluatorId = r.ReadInt(); break;
                case "level":       msg.Level = (int)r.ReadInt(); break;
                case "message":     msg.Message = r.ReadString()!; break;
                case "frameUri":    msg.FrameUri = r.ReadString()!; break;
                default: r.Skip(); break;
            }
        }
        return msg;
    }

    private static ReadResourceRequest ReadReadResource(MsgPackReader r, int mapLen)
    {
        var msg = new ReadResourceRequest();
        for (int i = 0; i < mapLen; i++)
        {
            var key = r.ReadString()!;
            switch (key)
            {
                case "requestId":   msg.RequestId = r.ReadInt(); break;
                case "evaluatorId": msg.EvaluatorId = r.ReadInt(); break;
                case "uri":         msg.Uri = r.ReadString()!; break;
                default: r.Skip(); break;
            }
        }
        return msg;
    }

    private static ReadModuleRequest ReadReadModule(MsgPackReader r, int mapLen)
    {
        var msg = new ReadModuleRequest();
        for (int i = 0; i < mapLen; i++)
        {
            var key = r.ReadString()!;
            switch (key)
            {
                case "requestId":   msg.RequestId = r.ReadInt(); break;
                case "evaluatorId": msg.EvaluatorId = r.ReadInt(); break;
                case "uri":         msg.Uri = r.ReadString()!; break;
                default: r.Skip(); break;
            }
        }
        return msg;
    }

    private static ListResourcesRequest ReadListResources(MsgPackReader r, int mapLen)
    {
        var msg = new ListResourcesRequest();
        for (int i = 0; i < mapLen; i++)
        {
            var key = r.ReadString()!;
            switch (key)
            {
                case "requestId":   msg.RequestId = r.ReadInt(); break;
                case "evaluatorId": msg.EvaluatorId = r.ReadInt(); break;
                case "uri":         msg.Uri = r.ReadString()!; break;
                default: r.Skip(); break;
            }
        }
        return msg;
    }

    private static ListModulesRequest ReadListModules(MsgPackReader r, int mapLen)
    {
        var msg = new ListModulesRequest();
        for (int i = 0; i < mapLen; i++)
        {
            var key = r.ReadString()!;
            switch (key)
            {
                case "requestId":   msg.RequestId = r.ReadInt(); break;
                case "evaluatorId": msg.EvaluatorId = r.ReadInt(); break;
                case "uri":         msg.Uri = r.ReadString()!; break;
                default: r.Skip(); break;
            }
        }
        return msg;
    }

    private static InitializeModuleReaderRequest ReadInitModuleReader(MsgPackReader r, int mapLen)
    {
        var msg = new InitializeModuleReaderRequest();
        for (int i = 0; i < mapLen; i++)
        {
            var key = r.ReadString()!;
            switch (key)
            {
                case "requestId": msg.RequestId = r.ReadInt(); break;
                case "scheme":    msg.Scheme = r.ReadString()!; break;
                default: r.Skip(); break;
            }
        }
        return msg;
    }

    private static InitializeResourceReaderRequest ReadInitResourceReader(MsgPackReader r, int mapLen)
    {
        var msg = new InitializeResourceReaderRequest();
        for (int i = 0; i < mapLen; i++)
        {
            var key = r.ReadString()!;
            switch (key)
            {
                case "requestId": msg.RequestId = r.ReadInt(); break;
                case "scheme":    msg.Scheme = r.ReadString()!; break;
                default: r.Skip(); break;
            }
        }
        return msg;
    }

    private static CloseExternalProcess ReadCloseExternalProcess(MsgPackReader r, int mapLen)
    {
        for (int i = 0; i < mapLen; i++) { r.Skip(); r.Skip(); }
        return new CloseExternalProcess();
    }
}
