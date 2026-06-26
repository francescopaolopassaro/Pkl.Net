// Pkl.Net - Passaro Francesco Paolo 2026
using System.Collections.Generic;

namespace PklNet.Core.Internal.Messages;

internal abstract class OutgoingMessage
{
    internal abstract void Write(MsgPackWriter w);
}

internal sealed class ResourceReaderSpec
{
    internal string Scheme { get; set; } = "";
    internal bool HasHierarchicalUris { get; set; }
    internal bool IsGlobbable { get; set; }
}

internal sealed class ModuleReaderSpec
{
    internal string Scheme { get; set; } = "";
    internal bool HasHierarchicalUris { get; set; }
    internal bool IsGlobbable { get; set; }
    internal bool IsLocal { get; set; }
}

internal sealed class CreateEvaluatorMessage : OutgoingMessage
{
    internal long RequestId { get; set; }
    internal List<ResourceReaderSpec>? ResourceReaders { get; set; }
    internal List<ModuleReaderSpec>? ModuleReaders { get; set; }
    internal Dictionary<string, string>? Env { get; set; }
    internal Dictionary<string, string>? Properties { get; set; }
    internal List<string>? ModulePaths { get; set; }
    internal List<string>? AllowedModules { get; set; }
    internal List<string>? AllowedResources { get; set; }
    internal string? OutputFormat { get; set; }
    internal string? CacheDir { get; set; }
    internal string? RootDir { get; set; }
    internal long? TimeoutSeconds { get; set; }

    internal override void Write(MsgPackWriter w)
    {
        var fields = CountFields();
        w.WriteArrayHeader(2);
        w.WriteInt(MessageCodes.CreateEvaluator);
        w.WriteMapHeader(fields);

        w.WriteString("requestId"); w.WriteInt(RequestId);

        if (AllowedModules is { Count: > 0 })
        { w.WriteString("allowedModules"); w.WriteStringList(AllowedModules); }

        if (AllowedResources is { Count: > 0 })
        { w.WriteString("allowedResources"); w.WriteStringList(AllowedResources); }

        if (Env is { Count: > 0 })
        { w.WriteString("env"); w.WriteStringMap(Env); }

        if (Properties is { Count: > 0 })
        { w.WriteString("properties"); w.WriteStringMap(Properties); }

        if (ModulePaths is { Count: > 0 })
        { w.WriteString("modulePaths"); w.WriteStringList(ModulePaths); }

        if (CacheDir is not null)
        { w.WriteString("cacheDir"); w.WriteString(CacheDir); }

        if (RootDir is not null)
        { w.WriteString("rootDir"); w.WriteString(RootDir); }

        if (OutputFormat is not null)
        { w.WriteString("outputFormat"); w.WriteString(OutputFormat); }

        if (ResourceReaders is { Count: > 0 })
        {
            w.WriteString("clientResourceReaders");
            w.WriteArrayHeader(ResourceReaders.Count);
            foreach (var r in ResourceReaders) WriteResourceReader(w, r);
        }

        if (ModuleReaders is { Count: > 0 })
        {
            w.WriteString("clientModuleReaders");
            w.WriteArrayHeader(ModuleReaders.Count);
            foreach (var r in ModuleReaders) WriteModuleReader(w, r);
        }

        if (TimeoutSeconds.HasValue)
        { w.WriteString("timeoutSeconds"); w.WriteInt(TimeoutSeconds.Value); }
    }

    private int CountFields()
    {
        int n = 1; // requestId always present
        if (AllowedModules is { Count: > 0 }) n++;
        if (AllowedResources is { Count: > 0 }) n++;
        if (Env is { Count: > 0 }) n++;
        if (Properties is { Count: > 0 }) n++;
        if (ModulePaths is { Count: > 0 }) n++;
        if (CacheDir is not null) n++;
        if (RootDir is not null) n++;
        if (OutputFormat is not null) n++;
        if (ResourceReaders is { Count: > 0 }) n++;
        if (ModuleReaders is { Count: > 0 }) n++;
        if (TimeoutSeconds.HasValue) n++;
        return n;
    }

    private static void WriteResourceReader(MsgPackWriter w, ResourceReaderSpec r)
    {
        w.WriteMapHeader(3);
        w.WriteString("scheme"); w.WriteString(r.Scheme);
        w.WriteString("hasHierarchicalUris"); w.WriteBool(r.HasHierarchicalUris);
        w.WriteString("isGlobbable"); w.WriteBool(r.IsGlobbable);
    }

    private static void WriteModuleReader(MsgPackWriter w, ModuleReaderSpec r)
    {
        w.WriteMapHeader(4);
        w.WriteString("scheme"); w.WriteString(r.Scheme);
        w.WriteString("hasHierarchicalUris"); w.WriteBool(r.HasHierarchicalUris);
        w.WriteString("isGlobbable"); w.WriteBool(r.IsGlobbable);
        w.WriteString("isLocal"); w.WriteBool(r.IsLocal);
    }
}

internal sealed class CloseEvaluatorMessage : OutgoingMessage
{
    internal long EvaluatorId { get; set; }

    internal override void Write(MsgPackWriter w)
    {
        w.WriteArrayHeader(2);
        w.WriteInt(MessageCodes.CloseEvaluator);
        w.WriteMapHeader(1);
        w.WriteString("evaluatorId"); w.WriteInt(EvaluatorId);
    }
}

internal sealed class EvaluateMessage : OutgoingMessage
{
    internal long RequestId { get; set; }
    internal long EvaluatorId { get; set; }
    internal string ModuleUri { get; set; } = "";
    internal string? ModuleText { get; set; }
    internal string? Expr { get; set; }

    internal override void Write(MsgPackWriter w)
    {
        int fields = 3;
        if (ModuleText is not null) fields++;
        if (Expr is not null) fields++;

        w.WriteArrayHeader(2);
        w.WriteInt(MessageCodes.Evaluate);
        w.WriteMapHeader(fields);
        w.WriteString("requestId"); w.WriteInt(RequestId);
        w.WriteString("evaluatorId"); w.WriteInt(EvaluatorId);
        w.WriteString("moduleUri"); w.WriteString(ModuleUri);
        if (ModuleText is not null) { w.WriteString("moduleText"); w.WriteString(ModuleText); }
        if (Expr is not null) { w.WriteString("expr"); w.WriteString(Expr); }
    }
}

internal sealed class ReadResourceResponse : OutgoingMessage
{
    internal long RequestId { get; set; }
    internal long EvaluatorId { get; set; }
    internal byte[]? Contents { get; set; }
    internal string? Error { get; set; }

    internal override void Write(MsgPackWriter w)
    {
        int fields = 2;
        if (Contents is not null) fields++;
        if (Error is not null) fields++;

        w.WriteArrayHeader(2);
        w.WriteInt(MessageCodes.EvaluateReadResponse);
        w.WriteMapHeader(fields);
        w.WriteString("requestId"); w.WriteInt(RequestId);
        w.WriteString("evaluatorId"); w.WriteInt(EvaluatorId);
        if (Contents is not null) { w.WriteString("contents"); w.WriteBytes(Contents); }
        if (Error is not null) { w.WriteString("error"); w.WriteString(Error); }
    }
}

internal sealed class ReadModuleResponse : OutgoingMessage
{
    internal long RequestId { get; set; }
    internal long EvaluatorId { get; set; }
    internal string? Contents { get; set; }
    internal string? Error { get; set; }

    internal override void Write(MsgPackWriter w)
    {
        int fields = 2;
        if (Contents is not null) fields++;
        if (Error is not null) fields++;

        w.WriteArrayHeader(2);
        w.WriteInt(MessageCodes.EvaluateReadModuleResponse);
        w.WriteMapHeader(fields);
        w.WriteString("requestId"); w.WriteInt(RequestId);
        w.WriteString("evaluatorId"); w.WriteInt(EvaluatorId);
        if (Contents is not null) { w.WriteString("contents"); w.WriteString(Contents); }
        if (Error is not null) { w.WriteString("error"); w.WriteString(Error); }
    }
}

internal sealed class ListResourcesResponse : OutgoingMessage
{
    internal long RequestId { get; set; }
    internal long EvaluatorId { get; set; }
    internal List<PathElement>? PathElements { get; set; }
    internal string? Error { get; set; }

    internal override void Write(MsgPackWriter w)
    {
        int fields = 2;
        if (PathElements is not null) fields++;
        if (Error is not null) fields++;

        w.WriteArrayHeader(2);
        w.WriteInt(MessageCodes.ListResourcesResponse);
        w.WriteMapHeader(fields);
        w.WriteString("requestId"); w.WriteInt(RequestId);
        w.WriteString("evaluatorId"); w.WriteInt(EvaluatorId);
        if (PathElements is not null)
        {
            w.WriteString("pathElements");
            w.WriteArrayHeader(PathElements.Count);
            foreach (var pe in PathElements)
            {
                w.WriteMapHeader(2);
                w.WriteString("name"); w.WriteString(pe.Name);
                w.WriteString("isDirectory"); w.WriteBool(pe.IsDirectory);
            }
        }
        if (Error is not null) { w.WriteString("error"); w.WriteString(Error); }
    }
}

internal sealed class ListModulesResponse : OutgoingMessage
{
    internal long RequestId { get; set; }
    internal long EvaluatorId { get; set; }
    internal List<PathElement>? PathElements { get; set; }
    internal string? Error { get; set; }

    internal override void Write(MsgPackWriter w)
    {
        int fields = 2;
        if (PathElements is not null) fields++;
        if (Error is not null) fields++;

        w.WriteArrayHeader(2);
        w.WriteInt(MessageCodes.ListModulesResponse);
        w.WriteMapHeader(fields);
        w.WriteString("requestId"); w.WriteInt(RequestId);
        w.WriteString("evaluatorId"); w.WriteInt(EvaluatorId);
        if (PathElements is not null)
        {
            w.WriteString("pathElements");
            w.WriteArrayHeader(PathElements.Count);
            foreach (var pe in PathElements)
            {
                w.WriteMapHeader(2);
                w.WriteString("name"); w.WriteString(pe.Name);
                w.WriteString("isDirectory"); w.WriteBool(pe.IsDirectory);
            }
        }
        if (Error is not null) { w.WriteString("error"); w.WriteString(Error); }
    }
}

internal sealed class InitializeModuleReaderResponse : OutgoingMessage
{
    internal long RequestId { get; set; }
    internal ModuleReaderSpec? Spec { get; set; }

    internal override void Write(MsgPackWriter w)
    {
        w.WriteArrayHeader(2);
        w.WriteInt(MessageCodes.InitializeModuleReaderResponse);
        w.WriteMapHeader(Spec is null ? 1 : 2);
        w.WriteString("requestId"); w.WriteInt(RequestId);
        if (Spec is not null)
        {
            w.WriteString("spec");
            w.WriteMapHeader(4);
            w.WriteString("scheme"); w.WriteString(Spec.Scheme);
            w.WriteString("hasHierarchicalUris"); w.WriteBool(Spec.HasHierarchicalUris);
            w.WriteString("isGlobbable"); w.WriteBool(Spec.IsGlobbable);
            w.WriteString("isLocal"); w.WriteBool(Spec.IsLocal);
        }
    }
}

internal sealed class InitializeResourceReaderResponse : OutgoingMessage
{
    internal long RequestId { get; set; }
    internal ResourceReaderSpec? Spec { get; set; }

    internal override void Write(MsgPackWriter w)
    {
        w.WriteArrayHeader(2);
        w.WriteInt(MessageCodes.InitializeResourceReaderResponse);
        w.WriteMapHeader(Spec is null ? 1 : 2);
        w.WriteString("requestId"); w.WriteInt(RequestId);
        if (Spec is not null)
        {
            w.WriteString("spec");
            w.WriteMapHeader(3);
            w.WriteString("scheme"); w.WriteString(Spec.Scheme);
            w.WriteString("hasHierarchicalUris"); w.WriteBool(Spec.HasHierarchicalUris);
            w.WriteString("isGlobbable"); w.WriteBool(Spec.IsGlobbable);
        }
    }
}

internal sealed class PathElement
{
    internal string Name { get; set; } = "";
    internal bool IsDirectory { get; set; }
}
