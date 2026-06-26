// Pkl.Net - Passaro Francesco Paolo 2026
using System;
using System.Collections.Generic;
using System.IO;

namespace PklNet.Core;

/// <summary>Options for configuring a Pkl evaluator.</summary>
public sealed class EvaluatorOptions
{
    /// <summary>External properties available via the <c>prop:</c> resource reader.</summary>
    public Dictionary<string, string>? Properties { get; set; }

    /// <summary>Environment variables available via the <c>env:</c> resource reader.</summary>
    public Dictionary<string, string>? Env { get; set; }

    /// <summary>Directories or archives to search when resolving <c>modulepath:</c> imports.</summary>
    public List<string>? ModulePaths { get; set; }

    /// <summary>Output format for <c>output.text</c>. Supported: json, yaml, xml, plist, properties, pcf.</summary>
    public string? OutputFormat { get; set; }

    /// <summary>URI patterns for allowed modules (Java regex dialect).</summary>
    public List<string>? AllowedModules { get; set; }

    /// <summary>URI patterns for allowed resources (Java regex dialect).</summary>
    public List<string>? AllowedResources { get; set; }

    /// <summary>Directory for caching <c>package:</c> modules.</summary>
    public string? CacheDir { get; set; }

    /// <summary>Root directory for file-based reads; reads outside this directory fail.</summary>
    public string? RootDir { get; set; }

    /// <summary>Timeout for evaluation. When null, no timeout is applied.</summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>Logger for trace/warn messages emitted by the Pkl evaluator.</summary>
    public IPklLogger? Logger { get; set; }

    /// <summary>Custom module readers keyed by scheme.</summary>
    public List<IPklModuleReader>? ModuleReaders { get; set; }

    /// <summary>Custom resource readers keyed by scheme.</summary>
    public List<IPklResourceReader>? ResourceReaders { get; set; }

    /// <summary>
    /// Creates an <see cref="EvaluatorOptions"/> pre-configured with sensible defaults:
    /// all common URI schemes allowed, OS environment variables, and the default cache dir.
    /// </summary>
    public static EvaluatorOptions Preconfigured()
    {
        var opts = new EvaluatorOptions
        {
            AllowedModules = ["pkl:", "repl:", "file:", "http:", "https:", "modulepath:", "package:", "projectpackage:"],
            AllowedResources = ["http:", "https:", "file:", "env:", "prop:", "modulepath:", "package:", "projectpackage:"],
            Logger = NullLogger.Instance,
        };

        try
        {
            opts.CacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pkl", "cache");
        }
        catch { /* ignore if home dir unavailable */ }

        opts.Env = new Dictionary<string, string>();
        foreach (var entry in Environment.GetEnvironmentVariables())
        {
            var kv = entry.ToString()!;
            var idx = kv.IndexOf('=');
            if (idx > 0) opts.Env[kv[..idx]] = kv[(idx + 1)..];
        }

        return opts;
    }
}

/// <summary>Logger interface for Pkl trace/warn messages.</summary>
public interface IPklLogger
{
    void Trace(string message, string frameUri);
    void Warn(string message, string frameUri);
}

/// <summary>A logger that discards all messages.</summary>
public sealed class NullLogger : IPklLogger
{
    public static readonly NullLogger Instance = new();
    public void Trace(string message, string frameUri) { }
    public void Warn(string message, string frameUri) { }
}

/// <summary>Interface for custom module readers.</summary>
public interface IPklModuleReader
{
    string Scheme { get; }
    bool HasHierarchicalUris { get; }
    bool IsGlobbable { get; }
    bool IsLocal { get; }
    string Read(Uri uri);
    IReadOnlyList<(string Name, bool IsDirectory)> ListElements(Uri uri);
}

/// <summary>Interface for custom resource readers.</summary>
public interface IPklResourceReader
{
    string Scheme { get; }
    bool HasHierarchicalUris { get; }
    bool IsGlobbable { get; }
    byte[] Read(Uri uri);
    IReadOnlyList<(string Name, bool IsDirectory)> ListElements(Uri uri);
}
