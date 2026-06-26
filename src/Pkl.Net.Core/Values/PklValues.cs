// Pkl.Net - Passaro Francesco Paolo 2026
using System;
using System.Collections.Generic;
using System.Linq;

namespace PklNet.Core.Values;

/// <summary>Represents a Pkl Duration value.</summary>
public sealed class PklDuration
{
    public double Value { get; }
    public string Unit { get; }

    public PklDuration(double value, string unit) { Value = value; Unit = unit; }

    public TimeSpan ToTimeSpan() => Unit switch
    {
        "ns" => TimeSpan.FromTicks((long)(Value / 100)),
        "us" => TimeSpan.FromTicks((long)(Value * 10)),
        "ms" => TimeSpan.FromMilliseconds(Value),
        "s"  => TimeSpan.FromSeconds(Value),
        "min"=> TimeSpan.FromMinutes(Value),
        "h"  => TimeSpan.FromHours(Value),
        "d"  => TimeSpan.FromDays(Value),
        _    => throw new InvalidOperationException($"Unknown duration unit: {Unit}")
    };

    public override string ToString() => $"{Value} {Unit}";
}

/// <summary>Represents a Pkl DataSize value.</summary>
public sealed class PklDataSize
{
    public double Value { get; }
    public string Unit { get; }

    public PklDataSize(double value, string unit) { Value = value; Unit = unit; }

    public long ToBytes() => Unit switch
    {
        "b"  => (long)Value,
        "kb" => (long)(Value * 1_000),
        "kib"=> (long)(Value * 1_024),
        "mb" => (long)(Value * 1_000_000),
        "mib"=> (long)(Value * 1_048_576),
        "gb" => (long)(Value * 1_000_000_000),
        "gib"=> (long)(Value * 1_073_741_824L),
        "tb" => (long)(Value * 1_000_000_000_000L),
        "tib"=> (long)(Value * 1_099_511_627_776L),
        "pb" => (long)(Value * 1_000_000_000_000_000L),
        "pib"=> (long)(Value * 1_125_899_906_842_624L),
        _    => throw new InvalidOperationException($"Unknown data size unit: {Unit}")
    };

    public override string ToString() => $"{Value} {Unit}";
}

/// <summary>Represents a Pkl Pair value.</summary>
public sealed class PklPair
{
    public object? First { get; }
    public object? Second { get; }

    public PklPair(object? first, object? second) { First = first; Second = second; }

    public override string ToString() => $"Pair({First}, {Second})";
}

/// <summary>Represents a Pkl Pair with strongly-typed values.</summary>
public sealed class PklPair<TFirst, TSecond>
{
    public TFirst? First { get; }
    public TSecond? Second { get; }

    public PklPair(TFirst? first, TSecond? second) { First = first; Second = second; }

    public override string ToString() => $"Pair({First}, {Second})";
}

/// <summary>Represents a Pkl IntSeq value.</summary>
public sealed class PklIntSeq
{
    public long Start { get; }
    public long End { get; }
    public long Step { get; }

    public PklIntSeq(long start, long end, long step) { Start = start; End = end; Step = step; }

    public IEnumerable<long> ToEnumerable()
    {
        if (Step > 0)
            for (long i = Start; i <= End; i += Step) yield return i;
        else if (Step < 0)
            for (long i = Start; i >= End; i += Step) yield return i;
    }

    public override string ToString() => $"IntSeq({Start}, {End}, {Step})";
}

/// <summary>Represents a Pkl Regex value.</summary>
public sealed class PklRegex
{
    public string Pattern { get; }
    public PklRegex(string pattern) => Pattern = pattern;
    public System.Text.RegularExpressions.Regex ToRegex() => new(Pattern);
    public override string ToString() => $"Regex({Pattern})";
}
