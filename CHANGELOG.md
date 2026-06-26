# Changelog

All notable changes to **Pkl.Net** are documented in this file.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versioning follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

![Pkl.Net Logo](assets/icon.jpg)

---

## [1.0.0] â€” 2026-06-26

### Added

#### `PklNet.Core`
- Custom **MessagePack encoder/decoder** (`MsgPackWriter`, `MsgPackReader`) with zero external dependencies â€” covers all MsgPack types: nil, bool, int, uint, float32/64, fixstr/str8/16/32, bin8/16/32, fixarray/array16/32, fixmap/map16/32, with full `Skip()` support.
- Public `MsgPack` facade for standalone MessagePack serialization/deserialization, independent of Pkl.
- `MsgPackDocument.Writer` and `MsgPackDocument.Reader` fluent API.
- `PklProcess` â€” spawns and manages the `pkl server` child process; reads/writes the binary protocol over stdin/stdout pipes.
- `ProtocolDecoder` â€” decodes `[code, {map}]` protocol frames into typed `IncomingMessage` objects (CreateEvaluatorResponse, EvaluateResponse, Log, ReadResource, ReadModule, ListResources, ListModules, InitializeModuleReader, InitializeResourceReader, CloseExternalProcess).
- `PklValueDecoder` â€” reflection-based decoder for pkl binary value format; supports Object, Map, Mapping, List, Listing, Set, Duration, DataSize, Pair, IntSeq, Regex, Bytes, Class, TypeAlias; maps to registered C# types or `Dictionary<string,object?>`.
- `PklEvaluatorManager` â€” manages a single pkl process, dispatches messages, handles custom module/resource readers.
- `PklEvaluator` â€” evaluates Pkl modules with `EvaluateModule<T>()`, `EvaluateExpression<T>()`, `EvaluateOutputText()`, `EvaluateOutputFiles()`, `EvaluateRaw()`.
- `ModuleSource` â€” `FromFile(path)`, `FromText(pklText)`, `FromUri(uri)`.
- `EvaluatorOptions` â€” full options: AllowedModules/Resources, Env, Properties, CacheDir, RootDir, OutputFormat, Timeout, Logger, ModuleReaders, ResourceReaders. Static `Preconfigured()` factory.
- `SchemaRegistry` â€” global registry mapping pkl qualified type names to C# types.
- `[PklProperty("name")]` attribute for custom property name mapping.
- `Pkl` static facade: `Load<T>()`, `LoadText<T>()`, `EvaluateOutputText()`, `EvaluateTextOutputFromText()`.
- Pkl value types in `PklNet.Core.Values`: `PklDuration`, `PklDataSize`, `PklPair`, `PklIntSeq`, `PklRegex`.
- `IPklLogger`, `NullLogger`, `IPklModuleReader`, `IPklResourceReader` interfaces.
- `PklException` for evaluation errors.
- Targets **net8.0** and **net9.0**.

#### `PklNet.Tools`
- `PklCodeGenerator` â€” evaluates a Pkl file and emits a C# class file with inferred property types.

#### `PklNet.Extensions.Configuration`
- `AddPklFile()` extension method on `IConfigurationBuilder` with optional `reloadOnChange`.
- `AddPklText()` extension method for inline Pkl text.
- `PklConfigurationProvider` â€” flattens nested pkl objects into colon-separated `IConfiguration` keys; supports live-reload via `FileSystemWatcher`.
- `PklConfigurationSource`.

#### Tests
- 48 unit tests for MsgPack (all pass without pkl installed).
- Integration test suite (`PklEvaluatorIntegrationTests`) covering primitives, collections, Duration, DataSize, typed deserialization, output text, expression evaluation, validation errors, external property injection, and multi-call reuse.
- Pkl fixture files: `primitives.pkl`, `server.pkl`, `collections.pkl`, `duration_datasize.pkl`, `config_base.pkl`.

#### Project infrastructure
- `.editorconfig` with C# style rules (naming, spacing, pattern matching, var, modifiers).
- `.gitignore` extended with `bin/`, `obj/`, `.vs/`, `.idea/`, `.vscode/`, `.claude/`, `.pkl/`, secrets files.
- `Directory.Build.props` with shared NuGet metadata (version, author, license, icon, README).
- NuGet packages: `PklNet.Core.1.0.0.nupkg`, `PklNet.Tools.1.0.0.nupkg`, `PklNet.Extensions.Configuration.1.0.0.nupkg` + matching `.snupkg` symbol packages.
- MIT License with attribution requirement.

---

## Links

- GitHub: <https://github.com/francescopaolopassaro/Pkl.Net>
- NuGet: <https://www.nuget.org/packages/PklNet.Core>
- Pkl language: <https://pkl-lang.org>
- pkl-go (inspiration): <https://github.com/apple/pkl-go>

