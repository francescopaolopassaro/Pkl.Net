// Pkl.Net - Passaro Francesco Paolo 2026
using System;
using System.IO;

namespace PklNet.Core;

/// <summary>Represents a source of a Pkl module to evaluate.</summary>
public abstract class ModuleSource
{
    internal abstract string Uri { get; }
    internal abstract string? Text { get; }

    /// <summary>Creates a module source from a local file path.</summary>
    public static ModuleSource FromFile(string path)
        => new FileModuleSource(Path.GetFullPath(path));

    /// <summary>Creates a module source from a URI string.</summary>
    public static ModuleSource FromUri(string uri)
        => new UriModuleSource(uri);

    /// <summary>Creates a module source from inline Pkl text.</summary>
    public static ModuleSource FromText(string pklText)
        => new TextModuleSource(pklText);
}

internal sealed class FileModuleSource : ModuleSource
{
    internal override string Uri { get; }
    internal override string? Text => null;

    internal FileModuleSource(string absolutePath)
        => Uri = new Uri(absolutePath).AbsoluteUri;
}

internal sealed class UriModuleSource : ModuleSource
{
    internal override string Uri { get; }
    internal override string? Text => null;

    internal UriModuleSource(string uri) => Uri = uri;
}

internal sealed class TextModuleSource : ModuleSource
{
    internal override string Uri => "repl:text";
    internal override string? Text { get; }

    internal TextModuleSource(string text) => Text = text;
}
