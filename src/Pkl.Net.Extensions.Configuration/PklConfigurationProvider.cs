// Pkl.Net - Passaro Francesco Paolo 2026
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using PklNet.Core;

namespace PklNet.Extensions.Configuration;

/// <summary>
/// An <see cref="IConfigurationProvider"/> that evaluates a Pkl file and flattens
/// the resulting object graph into the colon-separated key format used by
/// <see cref="IConfiguration"/>.
/// </summary>
public sealed class PklConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly PklConfigurationSource _source;
    private FileSystemWatcher? _watcher;

    internal PklConfigurationProvider(PklConfigurationSource source)
        => _source = source;

    public override void Load()
    {
        ModuleSource moduleSource = !string.IsNullOrWhiteSpace(_source.PklText)
            ? ModuleSource.FromText(_source.PklText)
            : ModuleSource.FromFile(_source.FilePath);

        using var manager = PklEvaluatorManager.Create(_source.Options);
        using var eval    = manager.NewEvaluator(_source.Options);
        var result = eval.EvaluateModule<Dictionary<string, object?>>(moduleSource);

        var flat = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (result is not null)
            Flatten(flat, result, prefix: null);

        Data = flat;

        if (_source.ReloadOnChange && !string.IsNullOrWhiteSpace(_source.FilePath))
            StartWatcher();
    }

    private static void Flatten(Dictionary<string, string?> flat, Dictionary<string, object?> obj, string? prefix)
    {
        foreach (var (key, value) in obj)
        {
            var fullKey = prefix is null ? key : $"{prefix}:{key}";
            switch (value)
            {
                case Dictionary<string, object?> nested:
                    Flatten(flat, nested, fullKey);
                    break;
                case List<object?> list:
                    for (int i = 0; i < list.Count; i++)
                    {
                        var itemKey = $"{fullKey}:{i}";
                        if (list[i] is Dictionary<string, object?> itemDict)
                            Flatten(flat, itemDict, itemKey);
                        else
                            flat[itemKey] = list[i]?.ToString();
                    }
                    break;
                case null:
                    flat[fullKey] = null;
                    break;
                default:
                    flat[fullKey] = value.ToString();
                    break;
            }
        }
    }

    private void StartWatcher()
    {
        var dir  = Path.GetDirectoryName(Path.GetFullPath(_source.FilePath))!;
        var file = Path.GetFileName(_source.FilePath);
        _watcher = new FileSystemWatcher(dir, file)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };
        _watcher.Changed += (_, _) => { Load(); OnReload(); };
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _watcher = null;
    }
}
