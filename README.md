# Pkl.Net

A modern, type-safe configuration management library for .NET, bringing the power of Apple's **Pkl** configuration language to the C# ecosystem.

## Why Pkl.Net?

Traditional configuration formats like **YAML** and **JSON** are static, hard to maintain, and prone to runtime failures. They lack native validation, leading to common pitfalls like indentation errors, typos, and missing required values.

**Pkl.Net** solves this by embedding Pkl's programmable configuration engine into .NET. It bridges the gap between Pkl's robust, type-safe language and your C# code.

### Key Features

* 🚫 **No More YAML/JSON Hell:** Eliminate runtime configuration crashes caused by syntax errors or missing fields.
* 🛡️ **Built-in Validation:** Leverage Pkl's native type system (e.g., `port: Int(this >= 1024)`) to catch invalid configurations before your app even starts.
* 🧊 **DRY (Don't Repeat Yourself):** Use Pkl’s classes, inheritance, and functions to manage complex, multi-environment configurations without copy-pasting.
* ⚙️ **Seamless .NET Integration:** Easily map Pkl configuration files directly into strongly-typed C# records or classes, or hook it natively into `IConfiguration`.
* 🚀 **Cross-Platform:** Works wherever .NET works (Windows, macOS, Linux).

## Quick Start

Instead of fighting with unvalidated YAML:

```yaml
# old_config.yaml
server:
  host: "localhost"
  port: "eight-zero-eight-zero" # ❌ Runtime crash!
