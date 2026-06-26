// Pkl.Net - Passaro Francesco Paolo 2026
using System;
using System.Collections.Generic;

namespace PklNet.Core;

/// <summary>
/// Maps Pkl fully-qualified type names to C# types for deserialization.
/// Register your generated types here to enable automatic mapping from pkl objects.
/// </summary>
public static class SchemaRegistry
{
    private static readonly Dictionary<string, Type> _registry = new();

    /// <summary>Registers a C# type for the given Pkl qualified name.</summary>
    public static void Register<T>(string pklQualifiedName)
        => _registry[pklQualifiedName] = typeof(T);

    /// <summary>Registers a C# type for the given Pkl qualified name.</summary>
    public static void Register(string pklQualifiedName, Type type)
        => _registry[pklQualifiedName] = type;

    /// <summary>Removes a registration.</summary>
    public static void Unregister(string pklQualifiedName)
        => _registry.Remove(pklQualifiedName);

    /// <summary>Clears all registrations.</summary>
    public static void Clear() => _registry.Clear();

    internal static Dictionary<string, Type> GetAll() => new(_registry);

    internal static bool TryGet(string pklName, out Type? type)
        => _registry.TryGetValue(pklName, out type);
}
