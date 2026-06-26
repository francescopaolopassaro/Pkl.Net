// Pkl.Net - Passaro Francesco Paolo 2026
using System;
using System.Collections.Generic;
using System.IO;
using PklNet.Core.Internal;

namespace PklNet.Core;

/// <summary>
/// Public facade for reading and writing MessagePack values.
/// You can use this independently of Pkl for general-purpose MsgPack serialization.
/// </summary>
public static class MsgPack
{
    // ─── Writer helpers ────────────────────────────────────────────────────────

    /// <summary>Serializes <paramref name="value"/> into a new <see cref="MsgPackDocument"/>.</summary>
    public static MsgPackDocument Serialize(Action<MsgPackDocument.Writer> write)
    {
        using var ms = new MemoryStream();
        var w = new MsgPackWriter(ms);
        var doc = new MsgPackDocument.Writer(w);
        write(doc);
        return new MsgPackDocument(ms.ToArray());
    }

    /// <summary>Writes a nil value to <paramref name="stream"/>.</summary>
    public static void WriteNil(Stream stream) => new MsgPackWriter(stream).WriteNil();

    /// <summary>Writes a bool value.</summary>
    public static void WriteBool(Stream stream, bool value) => new MsgPackWriter(stream).WriteBool(value);

    /// <summary>Writes a signed integer.</summary>
    public static void WriteInt(Stream stream, long value) => new MsgPackWriter(stream).WriteInt(value);

    /// <summary>Writes an unsigned integer.</summary>
    public static void WriteUInt(Stream stream, ulong value) => new MsgPackWriter(stream).WriteUInt(value);

    /// <summary>Writes a double-precision float.</summary>
    public static void WriteFloat(Stream stream, double value) => new MsgPackWriter(stream).WriteFloat(value);

    /// <summary>Writes a string.</summary>
    public static void WriteString(Stream stream, string? value) => new MsgPackWriter(stream).WriteString(value);

    /// <summary>Writes binary bytes.</summary>
    public static void WriteBytes(Stream stream, byte[]? value) => new MsgPackWriter(stream).WriteBytes(value);

    /// <summary>Writes an array header (count of following elements).</summary>
    public static void WriteArrayHeader(Stream stream, int count) => new MsgPackWriter(stream).WriteArrayHeader(count);

    /// <summary>Writes a map header (count of following key-value pairs).</summary>
    public static void WriteMapHeader(Stream stream, int count) => new MsgPackWriter(stream).WriteMapHeader(count);

    // ─── Reader helpers ────────────────────────────────────────────────────────

    /// <summary>Creates a reader over the given bytes.</summary>
    public static MsgPackDocument.Reader CreateReader(byte[] bytes)
        => new MsgPackDocument.Reader(new MsgPackReader(new MemoryStream(bytes)));

    /// <summary>Creates a reader over the given stream.</summary>
    public static MsgPackDocument.Reader CreateReader(Stream stream)
        => new MsgPackDocument.Reader(new MsgPackReader(stream));

    // ─── Convenience round-trip ────────────────────────────────────────────────

    /// <summary>Serializes a dictionary with string keys and object values.</summary>
    public static byte[] SerializeMap(IDictionary<string, object?> map)
    {
        using var ms = new MemoryStream();
        var w = new MsgPackWriter(ms);
        WriteObjectMap(w, map);
        return ms.ToArray();
    }

    /// <summary>Deserializes a flat string-keyed map from MsgPack bytes.</summary>
    public static Dictionary<string, object?> DeserializeMap(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        var r = new MsgPackReader(ms);
        return ReadObjectMap(r);
    }

    private static void WriteObjectMap(MsgPackWriter w, IDictionary<string, object?> map)
    {
        w.WriteMapHeader(map.Count);
        foreach (var kv in map)
        {
            w.WriteString(kv.Key);
            WriteObject(w, kv.Value);
        }
    }

    private static void WriteObject(MsgPackWriter w, object? value)
    {
        switch (value)
        {
            case null:           w.WriteNil(); break;
            case bool b:         w.WriteBool(b); break;
            case int i:          w.WriteInt(i); break;
            case long l:         w.WriteInt(l); break;
            case ulong ul:       w.WriteUInt(ul); break;
            case double d:       w.WriteFloat(d); break;
            case float f:        w.WriteFloat(f); break;
            case string s:       w.WriteString(s); break;
            case byte[] ba:      w.WriteBytes(ba); break;
            case IDictionary<string, object?> dict: WriteObjectMap(w, dict); break;
            case IEnumerable<object?> list:
                var arr = new System.Collections.Generic.List<object?>(list);
                w.WriteArrayHeader(arr.Count);
                foreach (var item in arr) WriteObject(w, item);
                break;
            default:             w.WriteString(value.ToString()); break;
        }
    }

    private static Dictionary<string, object?> ReadObjectMap(MsgPackReader r)
    {
        var count = r.ReadMapHeader();
        var dict = new Dictionary<string, object?>(count);
        for (int i = 0; i < count; i++)
            dict[r.ReadString()!] = ReadObject(r);
        return dict;
    }

    private static object? ReadObject(MsgPackReader r)
    {
        var code = r.PeekCode();
        if (code == 0xc0) { r.Skip(); return null; }
        if (code == 0xc2 || code == 0xc3) return r.ReadBool();
        if ((code & 0xe0) == 0xa0 || code == 0xd9 || code == 0xda || code == 0xdb) return r.ReadString();
        if (code == 0xc4 || code == 0xc5 || code == 0xc6) return r.ReadBytes();
        if ((code & 0xf0) == 0x80 || code == 0xde || code == 0xdf) return ReadObjectMap(r);
        if ((code & 0xf0) == 0x90 || code == 0xdc || code == 0xdd)
        {
            var len = r.ReadArrayHeader();
            var list = new List<object?>(len);
            for (int i = 0; i < len; i++) list.Add(ReadObject(r));
            return list;
        }
        if (code == 0xca || code == 0xcb) return r.ReadFloat();
        return r.ReadInt();
    }
}

/// <summary>Represents a serialized MsgPack payload and provides typed accessors.</summary>
public sealed class MsgPackDocument
{
    private readonly byte[] _bytes;

    internal MsgPackDocument(byte[] bytes) => _bytes = bytes;

    /// <summary>The raw MsgPack bytes.</summary>
    public byte[] ToBytes() => _bytes;

    /// <summary>A fluent writer that wraps <see cref="MsgPackWriter"/>.</summary>
    public sealed class Writer
    {
        private readonly MsgPackWriter _w;
        internal Writer(MsgPackWriter w) => _w = w;

        public Writer Nil()                   { _w.WriteNil(); return this; }
        public Writer Bool(bool v)            { _w.WriteBool(v); return this; }
        public Writer Int(long v)             { _w.WriteInt(v); return this; }
        public Writer UInt(ulong v)           { _w.WriteUInt(v); return this; }
        public Writer Float(double v)         { _w.WriteFloat(v); return this; }
        public Writer String(string? v)       { _w.WriteString(v); return this; }
        public Writer Bytes(byte[]? v)        { _w.WriteBytes(v); return this; }
        public Writer ArrayHeader(int count)  { _w.WriteArrayHeader(count); return this; }
        public Writer MapHeader(int count)    { _w.WriteMapHeader(count); return this; }
    }

    /// <summary>A fluent reader that wraps <see cref="MsgPackReader"/>.</summary>
    public sealed class Reader
    {
        private readonly MsgPackReader _r;
        internal Reader(MsgPackReader r) => _r = r;

        public bool IsNil()           => _r.IsNil();
        public void ReadNil()         => _r.ReadNil();
        public bool ReadBool()        => _r.ReadBool();
        public long ReadInt()         => _r.ReadInt();
        public ulong ReadUInt()       => _r.ReadUInt();
        public double ReadFloat()     => _r.ReadFloat();
        public string? ReadString()   => _r.ReadString();
        public byte[]? ReadBytes()    => _r.ReadBytes();
        public int ReadArrayHeader()  => _r.ReadArrayHeader();
        public int ReadMapHeader()    => _r.ReadMapHeader();
        public void Skip()            => _r.Skip();
        public byte PeekCode()        => _r.PeekCode();
    }
}
