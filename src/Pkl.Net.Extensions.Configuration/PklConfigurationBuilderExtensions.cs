// Pkl.Net - Passaro Francesco Paolo 2026
using System;
using Microsoft.Extensions.Configuration;
using PklNet.Core;

namespace PklNet.Extensions.Configuration;

/// <summary>Extension methods for <see cref="IConfigurationBuilder"/> to add Pkl sources.</summary>
public static class PklConfigurationBuilderExtensions
{
    /// <summary>Adds a Pkl configuration file.</summary>
    public static IConfigurationBuilder AddPklFile(
        this IConfigurationBuilder builder,
        string filePath,
        EvaluatorOptions? options = null,
        bool reloadOnChange = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path must not be empty.", nameof(filePath));

        return builder.Add(new PklConfigurationSource
        {
            FilePath = filePath,
            Options = options,
            ReloadOnChange = reloadOnChange,
        });
    }

    /// <summary>Adds an inline Pkl text snippet as a configuration source.</summary>
    public static IConfigurationBuilder AddPklText(
        this IConfigurationBuilder builder,
        string pklText,
        EvaluatorOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (string.IsNullOrWhiteSpace(pklText)) throw new ArgumentException("Pkl text must not be empty.", nameof(pklText));

        return builder.Add(new PklConfigurationSource
        {
            PklText = pklText,
            Options = options,
        });
    }
}
