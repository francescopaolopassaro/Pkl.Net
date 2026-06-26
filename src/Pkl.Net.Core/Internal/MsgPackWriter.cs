// Pkl.Net - Passaro Francesco Paolo 2026
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PklNet.Core.Internal;

internal sealed class MsgPackWriter
{
    private readonly Stream _stream;

    internal MsgPackWriter(Stream stream) => _stream = stream;

    internal void WriteNil() => _stream.WriteByte(0xc0);

    internal void WriteBool(bool value) => _stream.WriteByte(value ? (byte)0xc3 : (byte)0xc2);

    internal void WriteInt(long value)
    {
        if (value >= 0)
        {
            WriteUInt((ulong)value);
            return;
        }
        if (value >= -32)
        {
            _stream.WriteByte((byte)(0xe0 | (int)(value + 32)));
        }
        else if (value >= sbyte.MinValue)
        {
            _stream.WriteByte(0xd0);
            _stream.WriteByte((byte)(sbyte)value);
        }
        else if (value >= short.MinValue)
        {
            _stream.WriteByte(0xd1);
            WriteUInt16BE((ushort)(short)value);
        }
        else if (value >= int.MinValue)
        {
            _stream.WriteByte(0xd2);
            WriteUInt32BE((uint)(int)value);
        }
        else
        {
            _stream.WriteByte(0xd3);
            WriteUInt64BE((ulong)value);
        }
    }

    internal void WriteUInt(ulong value)
    {
        if (value <= 127)
        {
            _stream.WriteByte((byte)value);
        }
        else if (value <= byte.MaxValue)
        {
            _stream.WriteByte(0xcc);
            _stream.WriteByte((byte)value);
        }
        else if (value <= ushort.MaxValue)
        {
            _stream.WriteByte(0xcd);
            WriteUInt16BE((ushort)value);
        }
        else if (value <= uint.MaxValue)
        {
            _stream.WriteByte(0xce);
            WriteUInt32BE((uint)value);
        }
        else
        {
            _stream.WriteByte(0xcf);
            WriteUInt64BE(value);
        }
    }

    internal void WriteFloat(double value)
    {
        _stream.WriteByte(0xcb);
        Span<byte> buf = stackalloc byte[8];
        BinaryPrimitives.WriteDoubleBigEndian(buf, value);
        _stream.Write(buf);
    }

    internal void WriteString(string? value)
    {
        if (value is null) { WriteNil(); return; }
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteStringBytes(bytes);
    }

    private void WriteStringBytes(byte[] bytes)
    {
        var len = bytes.Length;
        if (len <= 31)
        {
            _stream.WriteByte((byte)(0xa0 | len));
        }
        else if (len <= byte.MaxValue)
        {
            _stream.WriteByte(0xd9);
            _stream.WriteByte((byte)len);
        }
        else if (len <= ushort.MaxValue)
        {
            _stream.WriteByte(0xda);
            WriteUInt16BE((ushort)len);
        }
        else
        {
            _stream.WriteByte(0xdb);
            WriteUInt32BE((uint)len);
        }
        _stream.Write(bytes);
    }

    internal void WriteBytes(byte[]? value)
    {
        if (value is null) { WriteNil(); return; }
        var len = value.Length;
        if (len <= byte.MaxValue)
        {
            _stream.WriteByte(0xc4);
            _stream.WriteByte((byte)len);
        }
        else if (len <= ushort.MaxValue)
        {
            _stream.WriteByte(0xc5);
            WriteUInt16BE((ushort)len);
        }
        else
        {
            _stream.WriteByte(0xc6);
            WriteUInt32BE((uint)len);
        }
        _stream.Write(value);
    }

    internal void WriteArrayHeader(int count)
    {
        if (count <= 15)
        {
            _stream.WriteByte((byte)(0x90 | count));
        }
        else if (count <= ushort.MaxValue)
        {
            _stream.WriteByte(0xdc);
            WriteUInt16BE((ushort)count);
        }
        else
        {
            _stream.WriteByte(0xdd);
            WriteUInt32BE((uint)count);
        }
    }

    internal void WriteMapHeader(int count)
    {
        if (count <= 15)
        {
            _stream.WriteByte((byte)(0x80 | count));
        }
        else if (count <= ushort.MaxValue)
        {
            _stream.WriteByte(0xde);
            WriteUInt16BE((ushort)count);
        }
        else
        {
            _stream.WriteByte(0xdf);
            WriteUInt32BE((uint)count);
        }
    }

    internal void WriteStringMap(Dictionary<string, string>? map)
    {
        if (map is null) { WriteNil(); return; }
        WriteMapHeader(map.Count);
        foreach (var kv in map) { WriteString(kv.Key); WriteString(kv.Value); }
    }

    internal void WriteStringList(IReadOnlyList<string>? list)
    {
        if (list is null) { WriteNil(); return; }
        WriteArrayHeader(list.Count);
        foreach (var s in list) WriteString(s);
    }

    private void WriteUInt16BE(ushort v)
    {
        Span<byte> buf = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(buf, v);
        _stream.Write(buf);
    }

    private void WriteUInt32BE(uint v)
    {
        Span<byte> buf = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buf, v);
        _stream.Write(buf);
    }

    private void WriteUInt64BE(ulong v)
    {
        Span<byte> buf = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(buf, v);
        _stream.Write(buf);
    }
}
