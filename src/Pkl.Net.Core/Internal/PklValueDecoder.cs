// Pkl.Net - Passaro Francesco Paolo 2026
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using PklNet.Core.Values;

namespace PklNet.Core.Internal;

/// <summary>
/// Decodes the pkl binary value format (the bytes in EvaluateResponse.result)
/// into .NET objects using reflection.
/// </summary>
internal sealed class PklValueDecoder
{
    private readonly Dictionary<string, Type> _typeRegistry;

    internal PklValueDecoder(Dictionary<string, Type> typeRegistry)
    {
        _typeRegistry = typeRegistry;
    }

    internal T? Decode<T>(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        var r = new MsgPackReader(ms);
        var result = Decode(r, typeof(T));
        if (result is null) return default;
        if (result is T typed) return typed;
        return (T)Convert.ChangeType(result, typeof(T));
    }

    private object? Decode(MsgPackReader r, Type targetType)
    {
        var code = r.PeekCode();

        // null
        if (code == 0xc0) { r.Skip(); return null; }

        // bool
        if (code == 0xc2 || code == 0xc3)
        {
            var b = r.ReadBool();
            if (targetType == typeof(bool) || targetType == typeof(bool?)) return b;
            return b;
        }

        // fixarray -> pkl object/collection
        if ((code & 0xf0) == 0x90 || code == 0xdc || code == 0xdd)
            return DecodePklValue(r, targetType);

        // string
        if ((code & 0xe0) == 0xa0 || code == 0xd9 || code == 0xda || code == 0xdb)
        {
            var s = r.ReadString()!;
            if (targetType == typeof(string)) return s;
            if (targetType == typeof(Uri)) return new Uri(s);
            return s;
        }

        // bin -> bytes
        if (code == 0xc4 || code == 0xc5 || code == 0xc6)
        {
            var bytes = r.ReadBytes();
            if (targetType == typeof(byte[])) return bytes;
            return bytes;
        }

        // fixmap -> dynamic map
        if ((code & 0xf0) == 0x80 || code == 0xde || code == 0xdf)
        {
            var mapLen = r.ReadMapHeader();
            var dict = new Dictionary<string, object?>(mapLen);
            for (int i = 0; i < mapLen; i++)
                dict[r.ReadString()!] = Decode(r, typeof(object));
            return dict;
        }

        // numeric
        return DecodeNumeric(r, targetType);
    }

    private object? DecodeNumeric(MsgPackReader r, Type targetType)
    {
        var code = r.PeekCode();
        bool isFloat = code == 0xca || code == 0xcb;

        if (isFloat)
        {
            var d = r.ReadFloat();
            return ConvertNumber(d, targetType);
        }

        var i = r.ReadInt();
        return ConvertNumber(i, targetType);
    }

    private static object ConvertNumber(double value, Type t)
    {
        if (t == typeof(double) || t == typeof(double?)) return value;
        if (t == typeof(float) || t == typeof(float?)) return (float)value;
        if (t == typeof(int) || t == typeof(int?)) return (int)value;
        if (t == typeof(long) || t == typeof(long?)) return (long)value;
        if (t == typeof(object)) return value;
        return value;
    }

    private static object ConvertNumber(long value, Type t)
    {
        if (t == typeof(long) || t == typeof(long?)) return value;
        if (t == typeof(int) || t == typeof(int?)) return (int)value;
        if (t == typeof(double) || t == typeof(double?)) return (double)value;
        if (t == typeof(float) || t == typeof(float?)) return (float)value;
        if (t == typeof(object)) return value;
        return value;
    }

    private object? DecodePklValue(MsgPackReader r, Type targetType)
    {
        var arrLen = r.ReadArrayHeader();
        var objCode = (int)r.ReadInt();

        switch (objCode)
        {
            case MessageCodes.PklObject:
                return DecodeObject(r, targetType, arrLen);

            case MessageCodes.PklMap:
            case MessageCodes.PklMapping:
                return DecodeMap(r, targetType, arrLen - 1);

            case MessageCodes.PklList:
            case MessageCodes.PklListing:
                return DecodeList(r, targetType, arrLen - 1);

            case MessageCodes.PklSet:
                return DecodeSet(r, targetType, arrLen - 1);

            case MessageCodes.PklDuration:
                return DecodeDuration(r, targetType, arrLen - 1);

            case MessageCodes.PklDataSize:
                return DecodeDataSize(r, targetType, arrLen - 1);

            case MessageCodes.PklPair:
                return DecodePair(r, targetType, arrLen - 1);

            case MessageCodes.PklIntSeq:
                return DecodeIntSeq(r, targetType, arrLen - 1);

            case MessageCodes.PklRegex:
                return DecodeRegex(r, targetType, arrLen - 1);

            case MessageCodes.PklBytes:
                return DecodeBytes(r, targetType, arrLen - 1);

            case MessageCodes.PklClass:
            case MessageCodes.PklTypeAlias:
            case MessageCodes.PklFunction:
                for (int i = 1; i < arrLen; i++) r.Skip();
                return null;

            default:
                for (int i = 1; i < arrLen; i++) r.Skip();
                return null;
        }
    }

    private object? DecodeObject(MsgPackReader r, Type targetType, int arrLen)
    {
        var pklName = r.ReadString()!;
        var moduleUri = r.ReadString()!;
        var memberCount = r.ReadArrayHeader();

        // Resolve actual C# type from registry
        var resolvedType = ResolveType(pklName, targetType);

        if (resolvedType == typeof(object) || resolvedType == typeof(Dictionary<string, object?>))
        {
            var dict = new Dictionary<string, object?>(memberCount);
            for (int i = 0; i < memberCount; i++)
                ReadMemberIntoDictionary(r, dict);
            SkipRemaining(r, arrLen - 4); // 4 = code(already read), name, moduleUri, memberArray
            return dict;
        }

        var instance = Activator.CreateInstance(resolvedType)
            ?? throw new InvalidOperationException($"Cannot create instance of {resolvedType}");

        for (int i = 0; i < memberCount; i++)
            ReadMemberIntoObject(r, resolvedType, instance);

        SkipRemaining(r, arrLen - 4);
        return instance;
    }

    private Type ResolveType(string pklName, Type targetType)
    {
        if (_typeRegistry.TryGetValue(pklName, out var registered))
            return registered;

        if (targetType != typeof(object) && targetType.IsClass && !targetType.IsAbstract
            && targetType != typeof(string))
            return targetType;

        return typeof(Dictionary<string, object?>);
    }

    private void ReadMemberIntoObject(MsgPackReader r, Type type, object instance)
    {
        var memberArrLen = r.ReadArrayHeader();
        var memberCode = (int)r.ReadInt();

        if (memberCode == MessageCodes.MemberProperty)
        {
            var key = r.ReadString()!;
            var prop = FindProperty(type, key);
            if (prop is not null)
            {
                var value = Decode(r, prop.PropertyType);
                if (value is not null || IsNullable(prop.PropertyType))
                    prop.SetValue(instance, ConvertValue(value, prop.PropertyType));
                else
                    r.Skip(); // already consumed above, but handle null non-nullable gracefully
            }
            else
            {
                r.Skip(); // unknown property
            }
            SkipRemaining(r, memberArrLen - 3); // 3 = code, key, value
        }
        else
        {
            // Entry or Element - skip for struct targets
            for (int i = 1; i < memberArrLen; i++) r.Skip();
        }
    }

    private void ReadMemberIntoDictionary(MsgPackReader r, Dictionary<string, object?> dict)
    {
        var memberArrLen = r.ReadArrayHeader();
        var memberCode = (int)r.ReadInt();

        if (memberCode == MessageCodes.MemberProperty)
        {
            var key = r.ReadString()!;
            var value = Decode(r, typeof(object));
            dict[key] = value;
            SkipRemaining(r, memberArrLen - 3);
        }
        else if (memberCode == MessageCodes.MemberElement)
        {
            r.Skip(); // index
            r.Skip(); // value
            SkipRemaining(r, memberArrLen - 3);
        }
        else
        {
            for (int i = 1; i < memberArrLen; i++) r.Skip();
        }
    }

    private object? DecodeMap(MsgPackReader r, Type targetType, int remaining)
    {
        // map entries follow as: [0x11, key, value] pairs in the enclosing array (already counted)
        // Actually for map/mapping: remaining items are the entry members themselves as a flat array count
        // Actually looking at the Go decoder: for map/mapping it calls decodeMapImpl which reads a msgpack map
        // But wait - the outer pkl value is encoded as [code, ...members] where members are pairs in map format
        // Re-examine: in Go decoder, decodeMap calls decodeMapImpl which does dec.DecodeMap() - so it reads a raw msgpack map
        // But here after the code we already read the array header and code, remaining = arrLen - 1
        // Looking more carefully: [code, mapData] where mapData is a msgpack map
        // So remaining should be 1 (the map data)
        var keyType = typeof(object);
        var valueType = typeof(object);
        if (targetType.IsGenericType)
        {
            var args = targetType.GetGenericArguments();
            if (args.Length == 2) { keyType = args[0]; valueType = args[1]; }
        }

        // The map is next as a msgpack map
        var mapLen = r.ReadMapHeader();
        var dict = CreateDictionary(targetType, mapLen);
        for (int i = 0; i < mapLen; i++)
        {
            var k = Decode(r, keyType);
            var v = Decode(r, valueType);
            AddToDictionary(dict, k, v);
        }
        SkipRemaining(r, remaining - 1);
        return dict;
    }

    private object? DecodeList(MsgPackReader r, Type targetType, int remaining)
    {
        var elemType = typeof(object);
        if (targetType.IsGenericType)
            elemType = targetType.GetGenericArguments()[0];

        var arrLen = r.ReadArrayHeader();
        var list = CreateList(targetType, arrLen);
        for (int i = 0; i < arrLen; i++)
            AddToList(list, Decode(r, elemType));
        SkipRemaining(r, remaining - 1);
        return list;
    }

    private object? DecodeSet(MsgPackReader r, Type targetType, int remaining)
    {
        var elemType = typeof(object);
        if (targetType.IsGenericType)
            elemType = targetType.GetGenericArguments()[0];

        var arrLen = r.ReadArrayHeader();
        var set = new HashSet<object?>();
        for (int i = 0; i < arrLen; i++)
            set.Add(Decode(r, elemType));
        SkipRemaining(r, remaining - 1);
        return set;
    }

    private object? DecodeDuration(MsgPackReader r, Type targetType, int remaining)
    {
        var value = r.ReadFloat();
        var unit = r.ReadString()!;
        SkipRemaining(r, remaining - 2);
        var dur = new PklDuration(value, unit);
        if (targetType == typeof(TimeSpan)) return dur.ToTimeSpan();
        return dur;
    }

    private object? DecodeDataSize(MsgPackReader r, Type targetType, int remaining)
    {
        var value = r.ReadFloat();
        var unit = r.ReadString()!;
        SkipRemaining(r, remaining - 2);
        return new PklDataSize(value, unit);
    }

    private object? DecodePair(MsgPackReader r, Type targetType, int remaining)
    {
        var first = Decode(r, typeof(object));
        var second = Decode(r, typeof(object));
        SkipRemaining(r, remaining - 2);
        return new PklPair(first, second);
    }

    private object? DecodeIntSeq(MsgPackReader r, Type targetType, int remaining)
    {
        var start = r.ReadInt();
        var end = r.ReadInt();
        var step = r.ReadInt();
        SkipRemaining(r, remaining - 3);
        return new PklIntSeq(start, end, step);
    }

    private object? DecodeRegex(MsgPackReader r, Type targetType, int remaining)
    {
        var pattern = r.ReadString()!;
        SkipRemaining(r, remaining - 1);
        return new PklRegex(pattern);
    }

    private object? DecodeBytes(MsgPackReader r, Type targetType, int remaining)
    {
        var bytes = r.ReadBytes();
        SkipRemaining(r, remaining - 1);
        return bytes;
    }

    private static void SkipRemaining(MsgPackReader r, int count)
    {
        for (int i = 0; i < count; i++) r.Skip();
    }

    private static PropertyInfo? FindProperty(Type type, string pklKey)
    {
        // Exact match first
        var prop = type.GetProperty(pklKey, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop is not null) return prop;

        // Check [PklProperty] attribute
        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = p.GetCustomAttribute<PklPropertyAttribute>();
            if (attr?.Name == pklKey) return p;
        }
        return null;
    }

    private static bool IsNullable(Type t)
        => !t.IsValueType || Nullable.GetUnderlyingType(t) is not null;

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value is null) return null;
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (value.GetType() == underlying) return value;
        if (value is long l && underlying == typeof(int)) return (int)l;
        if (value is long l2 && underlying == typeof(double)) return (double)l2;
        if (value is double d && underlying == typeof(float)) return (float)d;
        if (value is Dictionary<string, object?> dict && underlying != typeof(Dictionary<string, object?>))
            return null; // cannot auto-convert nested dicts to typed objects at this stage
        try { return Convert.ChangeType(value, underlying); }
        catch { return value; }
    }

    private static object CreateDictionary(Type targetType, int capacity)
    {
        if (targetType.IsGenericType)
        {
            var args = targetType.GetGenericArguments();
            var dictType = typeof(Dictionary<,>).MakeGenericType(args);
            return Activator.CreateInstance(dictType)!;
        }
        return new Dictionary<object, object?>(capacity);
    }

    private static void AddToDictionary(object dict, object? key, object? value)
    {
        var method = dict.GetType().GetMethod("Add")!;
        method.Invoke(dict, [key, value]);
    }

    private static object CreateList(Type targetType, int capacity)
    {
        if (targetType.IsGenericType)
        {
            var args = targetType.GetGenericArguments();
            var listType = typeof(List<>).MakeGenericType(args);
            return Activator.CreateInstance(listType)!;
        }
        return new List<object?>(capacity);
    }

    private static void AddToList(object list, object? value)
    {
        var method = list.GetType().GetMethod("Add")!;
        method.Invoke(list, [value]);
    }
}

/// <summary>Marks a C# property as mapped to a specific pkl property name.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PklPropertyAttribute : Attribute
{
    public string Name { get; }
    public PklPropertyAttribute(string name) => Name = name;
}
