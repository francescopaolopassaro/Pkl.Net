// Pkl.Net - Passaro Francesco Paolo 2026
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PklNet.Core;
using PklNet.Core.Values;
using Xunit;

namespace PklNet.Core.Tests;

/// <summary>
/// Integration tests that require the <c>pkl</c> binary to be on PATH.
/// Skip if PKL_SKIP_INTEGRATION=1 is set.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PklEvaluatorIntegrationTests : IDisposable
{
    private readonly PklEvaluatorManager _manager;
    private readonly PklEvaluator _eval;
    private readonly string _fixturesDir;

    public PklEvaluatorIntegrationTests()
    {
        Skip.If(IsSkipped(), "Set PKL_SKIP_INTEGRATION=0 and ensure pkl is on PATH to run integration tests.");
        var opts = EvaluatorOptions.Preconfigured();
        _manager = PklEvaluatorManager.Create(opts);
        _eval = _manager.NewEvaluator(opts);
        _fixturesDir = Path.Combine(AppContext.BaseDirectory, "Fixtures");
    }

    private static bool IsSkipped()
        => Environment.GetEnvironmentVariable("PKL_SKIP_INTEGRATION") == "1";

    public void Dispose()
    {
        _eval.Dispose();
        _manager.Dispose();
    }

    // ─── Inline text evaluation ────────────────────────────────────────────────

    [SkippableFact]
    public void EvaluateText_SimpleString()
    {
        var result = _eval.EvaluateModule<Dictionary<string, object?>>(
            ModuleSource.FromText("greeting = \"Hello, Pkl.Net!\""));
        Assert.Equal("Hello, Pkl.Net!", result!["greeting"]);
    }

    [SkippableFact]
    public void EvaluateText_Integer()
    {
        var result = _eval.EvaluateModule<Dictionary<string, object?>>(
            ModuleSource.FromText("answer: Int = 42"));
        Assert.Equal(42L, result!["answer"]);
    }

    [SkippableFact]
    public void EvaluateText_Bool()
    {
        var result = _eval.EvaluateModule<Dictionary<string, object?>>(
            ModuleSource.FromText("active: Boolean = true"));
        Assert.Equal(true, result!["active"]);
    }

    [SkippableFact]
    public void EvaluateText_Float()
    {
        var result = _eval.EvaluateModule<Dictionary<string, object?>>(
            ModuleSource.FromText("pi: Float = 3.14159"));
        Assert.NotNull(result);
        var pi = Convert.ToDouble(result!["pi"]);
        Assert.InRange(pi, 3.14158, 3.14160);
    }

    [SkippableFact]
    public void EvaluateText_NullableString()
    {
        var result = _eval.EvaluateModule<Dictionary<string, object?>>(
            ModuleSource.FromText("maybe: String? = null"));
        Assert.True(result!.ContainsKey("maybe"));
        Assert.Null(result["maybe"]);
    }

    // ─── Multiple properties ────────────────────────────────────────────────────

    [SkippableFact]
    public void EvaluateText_MultipleProperties()
    {
        const string pkl = """
            name: String = "server"
            port: Int = 8080
            debug: Boolean = false
            """;
        var result = _eval.EvaluateModule<Dictionary<string, object?>>(ModuleSource.FromText(pkl));
        Assert.Equal("server", result!["name"]);
        Assert.Equal(8080L, result["port"]);
        Assert.Equal(false, result["debug"]);
    }

    // ─── Collections ───────────────────────────────────────────────────────────

    [SkippableFact]
    public void EvaluateText_List()
    {
        const string pkl = "items: Listing<Int> = new { 10; 20; 30 }";
        var result = _eval.EvaluateModule<Dictionary<string, object?>>(ModuleSource.FromText(pkl));
        var list = Assert.IsType<List<object?>>(result!["items"]);
        Assert.Equal([10L, 20L, 30L], list);
    }

    [SkippableFact]
    public void EvaluateText_StringList()
    {
        const string pkl = "tags: Listing<String> = new { \"a\"; \"b\"; \"c\" }";
        var result = _eval.EvaluateModule<Dictionary<string, object?>>(ModuleSource.FromText(pkl));
        var list = Assert.IsType<List<object?>>(result!["tags"]);
        Assert.Equal(["a", "b", "c"], list);
    }

    [SkippableFact]
    public void EvaluateText_Map()
    {
        const string pkl = "env: Mapping<String, String> = new { [\"HOST\"] = \"localhost\"; [\"PORT\"] = \"8080\" }";
        var result = _eval.EvaluateModule<Dictionary<string, object?>>(ModuleSource.FromText(pkl));
        Assert.NotNull(result!["env"]);
    }

    // ─── Typed C# object deserialization ───────────────────────────────────────

    [SkippableFact]
    public void EvaluateText_TypedRecord()
    {
        const string pkl = """
            name: String = "MyApp"
            port: Int = 9000
            debug: Boolean = true
            """;
        var cfg = _eval.EvaluateModule<AppConfig>(ModuleSource.FromText(pkl));
        Assert.NotNull(cfg);
        Assert.Equal("MyApp", cfg!.Name);
        Assert.Equal(9000, cfg.Port);
        Assert.True(cfg.Debug);
    }

    // ─── Expression evaluation ─────────────────────────────────────────────────

    [SkippableFact]
    public void EvaluateExpression_OutputText()
    {
        const string pkl = """
            output {
                text = "rendered!"
            }
            """;
        var text = _eval.EvaluateOutputText(ModuleSource.FromText(pkl));
        Assert.Equal("rendered!", text);
    }

    [SkippableFact]
    public void EvaluateExpression_SingleField()
    {
        const string pkl = "port: Int = 5432";
        var port = _eval.EvaluateExpression<long>(ModuleSource.FromText(pkl), "port");
        Assert.Equal(5432L, port);
    }

    // ─── Duration & DataSize ─────────────────────────────────────────────────

    [SkippableFact]
    public void EvaluateText_Duration()
    {
        const string pkl = "t: Duration = 30.s";
        var result = _eval.EvaluateModule<Dictionary<string, object?>>(ModuleSource.FromText(pkl));
        var dur = Assert.IsType<PklDuration>(result!["t"]);
        Assert.Equal(30.0, dur.Value);
        Assert.Equal("s", dur.Unit);
        Assert.Equal(TimeSpan.FromSeconds(30), dur.ToTimeSpan());
    }

    [SkippableFact]
    public void EvaluateText_DataSize()
    {
        const string pkl = "d: DataSize = 512.kib";
        var result = _eval.EvaluateModule<Dictionary<string, object?>>(ModuleSource.FromText(pkl));
        var ds = Assert.IsType<PklDataSize>(result!["d"]);
        Assert.Equal(512.0, ds.Value);
        Assert.Equal("kib", ds.Unit);
        Assert.Equal(524_288L, ds.ToBytes());
    }

    // ─── Validation (Pkl constraint) ────────────────────────────────────────

    [SkippableFact]
    public void EvaluateText_ValidationError_ThrowsPklException()
    {
        const string pkl = "port: Int(this >= 1024) = 80"; // violates constraint
        Assert.Throws<PklException>(() =>
            _eval.EvaluateModule<Dictionary<string, object?>>(ModuleSource.FromText(pkl)));
    }

    // ─── Add a parameter dynamically ────────────────────────────────────────

    [SkippableFact]
    public void EvaluateText_AddExternalProperty()
    {
        var opts = EvaluatorOptions.Preconfigured();
        opts.Properties = new Dictionary<string, string> { ["myProp"] = "injected-value" };
        using var mgr  = PklEvaluatorManager.Create(opts);
        using var eval = mgr.NewEvaluator(opts);

        const string pkl = """
            import "prop:myProp"
            value: String = read("prop:myProp")
            """;

        // Attempt: prop: reader may need AllowedResources with "prop:" which is already included
        // This verifies that external properties are passed correctly to pkl server
        var result = eval.EvaluateModule<Dictionary<string, object?>>(ModuleSource.FromText(pkl));
        Assert.Equal("injected-value", result!["value"]);
    }

    // ─── File-based loading ──────────────────────────────────────────────────

    [SkippableFact]
    public void LoadFromFile_Primitives()
    {
        var file = Path.Combine(_fixturesDir, "primitives.pkl");
        Skip.If(!File.Exists(file), "Fixture file not found.");
        var result = _eval.EvaluateModule<Dictionary<string, object?>>(ModuleSource.FromFile(file));
        Assert.Equal("Pkl.Net", result!["name"]);
        Assert.Equal(42L, result["version"]);
        Assert.Equal(true, result["enabled"]);
        Assert.Equal(false, result["disabled"]);
    }

    [SkippableFact]
    public void LoadFromFile_ServerConfig()
    {
        var file = Path.Combine(_fixturesDir, "server.pkl");
        Skip.If(!File.Exists(file), "Fixture file not found.");
        var result = _eval.EvaluateModule<Dictionary<string, object?>>(ModuleSource.FromFile(file));
        Assert.Equal("localhost", result!["host"]);
        Assert.Equal(8080L, result["port"]);
    }

    [SkippableFact]
    public void LoadFromFile_DurationAndDataSize()
    {
        var file = Path.Combine(_fixturesDir, "duration_datasize.pkl");
        Skip.If(!File.Exists(file), "Fixture file not found.");
        var result = _eval.EvaluateModule<Dictionary<string, object?>>(ModuleSource.FromFile(file));
        var timeout = Assert.IsType<PklDuration>(result!["timeout"]);
        Assert.Equal(TimeSpan.FromSeconds(30), timeout.ToTimeSpan());
        var disk = Assert.IsType<PklDataSize>(result["diskQuota"]);
        Assert.Equal(2_000_000_000L, disk.ToBytes());
    }

    // ─── Update a parameter (evaluate with overridden expression) ─────────────

    [SkippableFact]
    public void EvaluateText_OverrideValue_WithAmendExpression()
    {
        // pkl supports amending modules: `amends "..."` or passing properties
        // Here we simulate "update" by passing new inline text that overrides:
        const string original = "port: Int = 3000";

        // Evaluate original
        var r1 = _eval.EvaluateModule<Dictionary<string, object?>>(ModuleSource.FromText(original));
        Assert.Equal(3000L, r1!["port"]);

        // "Update" port by re-evaluating with new value
        const string updated = "port: Int = 9999";
        var r2 = _eval.EvaluateModule<Dictionary<string, object?>>(ModuleSource.FromText(updated));
        Assert.Equal(9999L, r2!["port"]);
    }

    // ─── Pkl static top-level API ────────────────────────────────────────────

    [SkippableFact]
    public void Pkl_LoadText_StaticApi()
    {
        var result = Pkl.LoadText<Dictionary<string, object?>>("answer: Int = 42");
        Assert.Equal(42L, result!["answer"]);
    }

    [SkippableFact]
    public void Pkl_EvaluateTextOutputFromText()
    {
        const string pkl = """
            output {
                text = "hello from output"
            }
            """;
        var text = Pkl.EvaluateTextOutputFromText(pkl);
        Assert.Equal("hello from output", text);
    }

    // ─── Error handling ──────────────────────────────────────────────────────

    [SkippableFact]
    public void EvaluateText_SyntaxError_ThrowsPklException()
    {
        Assert.Throws<PklException>(() =>
            _eval.EvaluateModule<Dictionary<string, object?>>(ModuleSource.FromText("invalid pkl @@@")));
    }

    [SkippableFact]
    public void EvaluateText_UndefinedProperty_ThrowsPklException()
    {
        Assert.Throws<PklException>(() =>
            _eval.EvaluateExpression<string>(ModuleSource.FromText("name = \"x\""), "nonExistentProperty"));
    }

    // ─── Reuse evaluator for multiple calls ──────────────────────────────────

    [SkippableFact]
    public void EvaluateMultipleTimes_SameEvaluator()
    {
        for (int i = 0; i < 5; i++)
        {
            var result = _eval.EvaluateModule<Dictionary<string, object?>>(
                ModuleSource.FromText($"n: Int = {i}"));
            Assert.Equal((long)i, result!["n"]);
        }
    }
}

// ─── Test POCO used by typed deserialization test ──────────────────────────

public sealed class AppConfig
{
    public string? Name { get; set; }
    public int Port { get; set; }
    public bool Debug { get; set; }
}

// ─── SkippableFact attribute ───────────────────────────────────────────────

[AttributeUsage(AttributeTargets.Method)]
public sealed class SkippableFactAttribute : FactAttribute { }

/// <summary>Helper for conditional test skipping.</summary>
public static class Skip
{
    public static void If(bool condition, string reason = "")
    {
        if (condition) throw new SkipException(reason);
    }
}

public sealed class SkipException : Exception
{
    public SkipException(string reason) : base(reason) { }
}
