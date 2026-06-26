// Pkl.Net - Passaro Francesco Paolo 2026
using Microsoft.Extensions.Configuration;
using PklNet.Core;

namespace PklNet.Extensions.Configuration;

/// <summary>
/// An <see cref="IConfigurationSource"/> that loads configuration from a Pkl file.
/// </summary>
public sealed class PklConfigurationSource : IConfigurationSource
{
    /// <summary>Path to the .pkl configuration file.</summary>
    public string FilePath { get; set; } = "";

    /// <summary>Inline Pkl text to use instead of a file. Mutually exclusive with <see cref="FilePath"/>.</summary>
    public string? PklText { get; set; }

    /// <summary>Optional evaluator options. Defaults to <see cref="EvaluatorOptions.Preconfigured"/>.</summary>
    public EvaluatorOptions? Options { get; set; }

    /// <summary>Whether to reload configuration when the file changes on disk.</summary>
    public bool ReloadOnChange { get; set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new PklConfigurationProvider(this);
}
