// Pkl.Net - Passaro Francesco Paolo 2026
using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace PklNet.Core.Internal;

internal sealed class MsgPackReader
{
    private readonly Stream _stream;

    internal MsgPackReader(Stream stream) => _stream = stream;

    internal byte PeekByte()
    {
        var b = _stream.ReadByte();
        if (b < 0) throw new EndOfStreamException("Unexpected end of MsgPack stream.");
        // Unread by using a PeekableStream wrapper or MemoryStream position adjustment.
        // We use a workaround: caller must rely on PeekCode() only.
        // Actually, we need a proper peek. Let us buffer one byte.
        throw new NotSupportedException("Use PeekCode()");
    }

    // One-byte lookahead buffer
    private int _peeked = -1;

    internal byte PeekCode()
    {
        if (_peeked < 0)
        {
            _peeked = _stream.ReadByte();
            if (_peeked < 0) throw new EndOfStreamException("Unexpected end of MsgPack stream.");
        }
        return (byte)_peeked;
    }

    private byte ReadByte()
    {
        if (_peeked >= 0)
        {
            var b = (byte)_peeked;
            _peeked = -1;
            return b;
        }
        var v = _stream.ReadByte();
        if (v < 0) throw new EndOfStreamException("Unexpected end of MsgPack stream.");
        return (byte)v;
    }

    private void ReadExact(byte[] buf, int offset, int count)
    {
        int read = 0;
        if (_peeked >= 0 && count > 0)
        {
            buf[offset + read++] = (byte)_peeked;
            _peeked = -1;
            count--;
        }
        while (count > 0)
        {
            var n = _stream.Read(buf, offset + read, count);
            if (n == 0) throw new EndOfStreamException("Unexpected end of MsgPack stream.");
            read += n;
            count -= n;
        }
    }

    internal bool IsNil() => PeekCode() == 0xc0;

    internal void ReadNil()
    {
        var b = ReadByte();
        if (b != 0xc0) throw new InvalidDataException($"Expected nil (0xc0), got 0x{b:x2}");
    }

    internal bool ReadBool()
    {
        var b = ReadByte();
        return b switch
        {
            0xc3 => true,
            0xc2 => false,
            _ => throw new InvalidDataException($"Expected bool, got 0x{b:x2}")
        };
    }

    internal long ReadInt()
    {
        var code = ReadByte();
        if (code <= 0x7f) return code;                          // positive fixint
        if (code >= 0xe0) return (sbyte)code;                   // negative fixint
        return code switch
        {
            0xcc => ReadByte(),                                  // uint8
            0xcd => ReadUInt16BE(),                              // uint16
            0xce => ReadUInt32BE(),                              // uint32
            0xcf => (long)ReadUInt64BE(),                        // uint64
            0xd0 => (sbyte)ReadByte(),                          // int8
            0xd1 => (short)ReadUInt16BE(),                      // int16
            0xd2 => (int)ReadUInt32BE(),                        // int32
            0xd3 => (long)ReadUInt64BE(),                        // int64
            _ => throw new InvalidDataException($"Expected integer, got 0x{code:x2}")
        };
    }

    internal ulong ReadUInt()
    {
        var code = ReadByte();
        if (code <= 0x7f) return code;
        return code switch
        {
            0xcc => ReadByte(),
            0xcd => ReadUInt16BE(),
            0xce => ReadUInt32BE(),
            0xcf => ReadUInt64BE(),
            _ => throw new InvalidDataException($"Expected uint, got 0x{code:x2}")
        };
    }

    internal double ReadFloat()
    {
        var code = ReadByte();
        if (code == 0xcb)
        {
            var buf = new byte[8];
            ReadExact(buf, 0, 8);
            return BinaryPrimitives.ReadDoubleBigEndian(buf);
        }
        if (code == 0xca)
        {
            var buf = new byte[4];
            ReadExact(buf, 0, 4);
            return BinaryPrimitives.ReadSingleBigEndian(buf);
        }
        // Allow integer codes to be read as float (pkl sometimes uses int for float fields)
        // Put back and re-read as int
        _peeked = code;
        return ReadInt();
    }

    internal string? ReadString()
    {
        var code = PeekCode();
        if (code == 0xc0) { ReadByte(); return null; }
        return Encoding.UTF8.GetString(ReadStringBytes());
    }

    internal byte[] ReadStringBytes()
    {
        var code = ReadByte();
        int len;
        if ((code & 0xe0) == 0xa0)          // fixstr
            len = code & 0x1f;
        else if (code == 0xd9)              // str8
            len = ReadByte();
        else if (code == 0xda)              // str16
            len = ReadUInt16BE();
        else if (code == 0xdb)              // str32
            len = (int)ReadUInt32BE();
        else
            throw new InvalidDataException($"Expected string, got 0x{code:x2}");

        var buf = new byte[len];
        ReadExact(buf, 0, len);
        return buf;
    }

    internal byte[]? ReadBytes()
    {
        var code = PeekCode();
        if (code == 0xc0) { ReadByte(); return null; }
        code = ReadByte();
        int len;
        if (code == 0xc4)      len = ReadByte();
        else if (code == 0xc5) len = ReadUInt16BE();
        else if (code == 0xc6) len = (int)ReadUInt32BE();
        else throw new InvalidDataException($"Expected bin, got 0x{code:x2}");
        var buf = new byte[len];
        ReadExact(buf, 0, len);
        return buf;
    }

    internal int ReadArrayHeader()
    {
        var code = ReadByte();
        if ((code & 0xf0) == 0x90) return code & 0x0f;          // fixarray
        if (code == 0xdc) return ReadUInt16BE();
        if (code == 0xdd) return (int)ReadUInt32BE();
        throw new InvalidDataException($"Expected array, got 0x{code:x2}");
    }

    internal int ReadMapHeader()
    {
        var code = ReadByte();
        if ((code & 0xf0) == 0x80) return code & 0x0f;          // fixmap
        if (code == 0xde) return ReadUInt16BE();
        if (code == 0xdf) return (int)ReadUInt32BE();
        throw new InvalidDataException($"Expected map, got 0x{code:x2}");
    }

    internal void Skip()
    {
        var code = PeekCode();

        // nil, bool
        if (code == 0xc0 || code == 0xc2 || code == 0xc3) { ReadByte(); return; }

        // fixint positive
        if (code <= 0x7f) { ReadByte(); return; }

        // fixint negative
        if (code >= 0xe0) { ReadByte(); return; }

        // fixstr
        if ((code & 0xe0) == 0xa0)
        {
            var len = code & 0x1f;
            ReadByte();
            var buf = new byte[len];
            ReadExact(buf, 0, len);
            return;
        }

        // fixarray
        if ((code & 0xf0) == 0x90)
        {
            var count = ReadArrayHeader();
            for (int i = 0; i < count; i++) Skip();
            return;
        }

        // fixmap
        if ((code & 0xf0) == 0x80)
        {
            var count = ReadMapHeader();
            for (int i = 0; i < count * 2; i++) Skip();
            return;
        }

        ReadByte(); // consume the code
        switch (code)
        {
            case 0xc4: { var l = ReadByte(); var b = new byte[l]; ReadExact(b,0,l); break; }     // bin8
            case 0xc5: { var l = ReadUInt16BE(); var b = new byte[l]; ReadExact(b,0,l); break; } // bin16
            case 0xc6: { var l = (int)ReadUInt32BE(); var b = new byte[l]; ReadExact(b,0,l); break; } // bin32
            case 0xca: { var b = new byte[4]; ReadExact(b,0,4); break; }  // float32
            case 0xcb: { var b = new byte[8]; ReadExact(b,0,8); break; }  // float64
            case 0xcc: ReadByte(); break;   // uint8
            case 0xcd: ReadUInt16BE(); break; // uint16
            case 0xce: ReadUInt32BE(); break; // uint32
            case 0xcf: ReadUInt64BE(); break; // uint64
            case 0xd0: ReadByte(); break;   // int8
            case 0xd1: ReadUInt16BE(); break; // int16
            case 0xd2: ReadUInt32BE(); break; // int32
            case 0xd3: ReadUInt64BE(); break; // int64
            case 0xd9: { var l = ReadByte(); var b = new byte[l]; ReadExact(b,0,l); break; }     // str8
            case 0xda: { var l = ReadUInt16BE(); var b = new byte[l]; ReadExact(b,0,l); break; } // str16
            case 0xdb: { var l = (int)ReadUInt32BE(); var b = new byte[l]; ReadExact(b,0,l); break; } // str32
            case 0xdc: { var n = ReadUInt16BE(); for (int i=0;i<n;i++) Skip(); break; }  // array16
            case 0xdd: { var n = (int)ReadUInt32BE(); for (int i=0;i<n;i++) Skip(); break; } // array32
            case 0xde: { var n = ReadUInt16BE(); for (int i=0;i<n*2;i++) Skip(); break; } // map16
            case 0xdf: { var n = (int)ReadUInt32BE(); for (int i=0;i<n*2;i++) Skip(); break; } // map32
            default: throw new InvalidDataException($"Cannot skip unknown MsgPack code 0x{code:x2}");
        }
    }

    private ushort ReadUInt16BE()
    {
        var buf = new byte[2];
        ReadExact(buf, 0, 2);
        return BinaryPrimitives.ReadUInt16BigEndian(buf);
    }

    private uint ReadUInt32BE()
    {
        var buf = new byte[4];
        ReadExact(buf, 0, 4);
        return BinaryPrimitives.ReadUInt32BigEndian(buf);
    }

    private ulong ReadUInt64BE()
    {
        var buf = new byte[8];
        ReadExact(buf, 0, 8);
        return BinaryPrimitives.ReadUInt64BigEndian(buf);
    }
}
