// Pkl.Net - Passaro Francesco Paolo 2026
using System.Collections.Generic;

namespace PklNet.Core.Internal.Messages;

internal abstract class IncomingMessage { }

internal sealed class CreateEvaluatorResponse : IncomingMessage
{
    internal long RequestId { get; set; }
    internal long EvaluatorId { get; set; }
    internal string? Error { get; set; }
}

internal sealed class EvaluateResponse : IncomingMessage
{
    internal long RequestId { get; set; }
    internal long EvaluatorId { get; set; }
    internal byte[]? Result { get; set; }
    internal string? Error { get; set; }
}

internal sealed class ReadResourceRequest : IncomingMessage
{
    internal long RequestId { get; set; }
    internal long EvaluatorId { get; set; }
    internal string Uri { get; set; } = "";
}

internal sealed class ReadModuleRequest : IncomingMessage
{
    internal long RequestId { get; set; }
    internal long EvaluatorId { get; set; }
    internal string Uri { get; set; } = "";
}

internal sealed class LogMessage : IncomingMessage
{
    internal long EvaluatorId { get; set; }
    internal int Level { get; set; }
    internal string Message { get; set; } = "";
    internal string FrameUri { get; set; } = "";
}

internal sealed class ListResourcesRequest : IncomingMessage
{
    internal long RequestId { get; set; }
    internal long EvaluatorId { get; set; }
    internal string Uri { get; set; } = "";
}

internal sealed class ListModulesRequest : IncomingMessage
{
    internal long RequestId { get; set; }
    internal long EvaluatorId { get; set; }
    internal string Uri { get; set; } = "";
}

internal sealed class InitializeModuleReaderRequest : IncomingMessage
{
    internal long RequestId { get; set; }
    internal string Scheme { get; set; } = "";
}

internal sealed class InitializeResourceReaderRequest : IncomingMessage
{
    internal long RequestId { get; set; }
    internal string Scheme { get; set; } = "";
}

internal sealed class CloseExternalProcess : IncomingMessage { }
