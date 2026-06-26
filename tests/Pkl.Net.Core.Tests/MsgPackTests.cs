// Pkl.Net - Passaro Francesco Paolo 2026
// InternalsVisibleTo allows direct access to MsgPackWriter/MsgPackReader for low-level tests.
using System.IO;
using PklNet.Core;
using PklNet.Core.Internal;
using Xunit;

// InternalsVisibleTo allows direct access to MsgPackWriter/MsgPackReader for low-level tests.

namespace PklNet.Core.Tests;

/// <summary>Unit tests for the custom MsgPack reader/writer — no external dependencies.</summary>
public sealed class MsgPackTests
{
    // ─── Round-trip helpers ────────────────────────────────────────────────────

    private static byte[] Write(Action<MsgPackWriter> act)
    {
        using var ms = new MemoryStream();
        act(new MsgPackWriter(ms));
        return ms.ToArray();
    }

    private static MsgPackReader Reader(byte[] data)
        => new MsgPackReader(new MemoryStream(data));

    // ─── Nil ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Nil_RoundTrip()
    {
        var bytes = Write(w => w.WriteNil());
        var r = Reader(bytes);
        Assert.True(r.IsNil());
        r.ReadNil();
    }

    // ─── Bool ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Bool_RoundTrip(bool value)
    {
        var bytes = Write(w => w.WriteBool(value));
        Assert.Equal(value, Reader(bytes).ReadBool());
    }

    // ─── Integers ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(127L)]
    [InlineData(128L)]
    [InlineData(255L)]
    [InlineData(256L)]
    [InlineData(65535L)]
    [InlineData(65536L)]
    [InlineData(int.MaxValue)]
    [InlineData(long.MaxValue)]
    [InlineData(-1L)]
    [InlineData(-32L)]
    [InlineData(-33L)]
    [InlineData(-128L)]
    [InlineData(-129L)]
    [InlineData(int.MinValue)]
    [InlineData(long.MinValue)]
    public void Int_RoundTrip(long value)
    {
        var bytes = Write(w => w.WriteInt(value));
        Assert.Equal(value, Reader(bytes).ReadInt());
    }

    // ─── Float ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0.0)]
    [InlineData(3.14159)]
    [InlineData(-1.5)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    public void Float_RoundTrip(double value)
    {
        var bytes = Write(w => w.WriteFloat(value));
        Assert.Equal(value, Reader(bytes).ReadFloat(), precision: 10);
    }

    // ─── String ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("Pkl.Net — Passaro Francesco Paolo 2026")]
    [InlineData("Unicode: à è ì ò ù")]
    public void String_RoundTrip(string value)
    {
        var bytes = Write(w => w.WriteString(value));
        Assert.Equal(value, Reader(bytes).ReadString());
    }

    [Fact]
    public void String_Null_RoundTrip()
    {
        var bytes = Write(w => w.WriteString(null));
        Assert.Null(Reader(bytes).ReadString());
    }

    // ─── LongString (> 31 bytes, > 255 bytes) ─────────────────────────────────

    [Fact]
    public void String_Medium_RoundTrip()
    {
        var s = new string('x', 200);
        var bytes = Write(w => w.WriteString(s));
        Assert.Equal(s, Reader(bytes).ReadString());
    }

    [Fact]
    public void String_Large_RoundTrip()
    {
        var s = new string('A', 70_000);
        var bytes = Write(w => w.WriteString(s));
        Assert.Equal(s, Reader(bytes).ReadString());
    }

    // ─── Bytes ────────────────────────────────────────────────────────────────

    [Fact]
    public void Bytes_RoundTrip()
    {
        byte[] data = [0x00, 0xFF, 0x42, 0xAB];
        var bytes = Write(w => w.WriteBytes(data));
        Assert.Equal(data, Reader(bytes).ReadBytes());
    }

    [Fact]
    public void Bytes_Null_RoundTrip()
    {
        var bytes = Write(w => w.WriteBytes(null));
        Assert.Null(Reader(bytes).ReadBytes());
    }

    // ─── Array ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(300)]
    public void ArrayHeader_RoundTrip(int count)
    {
        var bytes = Write(w => w.WriteArrayHeader(count));
        Assert.Equal(count, Reader(bytes).ReadArrayHeader());
    }

    // ─── Map ──────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(400)]
    public void MapHeader_RoundTrip(int count)
    {
        var bytes = Write(w => w.WriteMapHeader(count));
        Assert.Equal(count, Reader(bytes).ReadMapHeader());
    }

    // ─── Skip ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Skip_Int_ThenReadNext()
    {
        var bytes = Write(w => { w.WriteInt(999); w.WriteString("after"); });
        var r = Reader(bytes);
        r.Skip();
        Assert.Equal("after", r.ReadString());
    }

    [Fact]
    public void Skip_Array_ThenReadNext()
    {
        var bytes = Write(w =>
        {
            w.WriteArrayHeader(3);
            w.WriteInt(1); w.WriteInt(2); w.WriteInt(3);
            w.WriteString("done");
        });
        var r = Reader(bytes);
        r.Skip(); // skips [1,2,3]
        Assert.Equal("done", r.ReadString());
    }

    // ─── MsgPack public API ───────────────────────────────────────────────────

    [Fact]
    public void MsgPack_SerializeDeserializeMap()
    {
        var original = new Dictionary<string, object?>
        {
            ["name"] = "Pkl.Net",
            ["version"] = 42L,
            ["active"] = true,
            ["score"] = 3.14,
        };
        var bytes = MsgPack.SerializeMap(original);
        var result = MsgPack.DeserializeMap(bytes);

        Assert.Equal("Pkl.Net", result["name"]);
        Assert.Equal(42L, result["version"]);
        Assert.Equal(true, result["active"]);
        Assert.Equal(3.14, result["score"]);
    }

    [Fact]
    public void MsgPack_FluentWriter_And_Reader()
    {
        var doc = MsgPack.Serialize(w =>
        {
            w.ArrayHeader(2)
             .String("hello")
             .Int(99);
        });
        var r = MsgPack.CreateReader(doc.ToBytes());
        Assert.Equal(2, r.ReadArrayHeader());
        Assert.Equal("hello", r.ReadString());
        Assert.Equal(99L, r.ReadInt());
    }
}
