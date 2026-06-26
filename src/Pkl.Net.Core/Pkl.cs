// Pkl.Net - Passaro Francesco Paolo 2026
using System;
using System.IO;
using System.Threading;

namespace PklNet.Core;

/// <summary>
/// Top-level convenience API for evaluating Pkl modules without manually managing an evaluator.
/// For advanced usage (custom readers, multiple evaluations) use <see cref="PklEvaluatorManager"/>.
/// </summary>
public static class Pkl
{
    /// <summary>
    /// Evaluates a Pkl file and deserializes the result into <typeparamref name="T"/>.
    /// </summary>
    public static T? Load<T>(string filePath, EvaluatorOptions? options = null, CancellationToken ct = default)
    {
        using var manager = PklEvaluatorManager.Create(options);
        using var eval    = manager.NewEvaluator(options, ct);
        return eval.EvaluateModule<T>(ModuleSource.FromFile(filePath), ct);
    }

    /// <summary>
    /// Evaluates inline Pkl text and deserializes the result into <typeparamref name="T"/>.
    /// </summary>
    public static T? LoadText<T>(string pklText, EvaluatorOptions? options = null, CancellationToken ct = default)
    {
        using var manager = PklEvaluatorManager.Create(options);
        using var eval    = manager.NewEvaluator(options, ct);
        return eval.EvaluateModule<T>(ModuleSource.FromText(pklText), ct);
    }

    /// <summary>
    /// Evaluates a Pkl file and returns <c>output.text</c> as a string.
    /// </summary>
    public static string EvaluateOutputText(string filePath, EvaluatorOptions? options = null, CancellationToken ct = default)
    {
        using var manager = PklEvaluatorManager.Create(options);
        using var eval    = manager.NewEvaluator(options, ct);
        return eval.EvaluateOutputText(ModuleSource.FromFile(filePath), ct);
    }

    /// <summary>
    /// Evaluates inline Pkl text and returns the rendered <c>output.text</c>.
    /// </summary>
    public static string EvaluateTextOutputFromText(string pklText, EvaluatorOptions? options = null, CancellationToken ct = default)
    {
        using var manager = PklEvaluatorManager.Create(options);
        using var eval    = manager.NewEvaluator(options, ct);
        return eval.EvaluateOutputText(ModuleSource.FromText(pklText), ct);
    }
}
