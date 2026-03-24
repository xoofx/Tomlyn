# Tomlyn [![ci](https://github.com/xoofx/Tomlyn/actions/workflows/ci.yml/badge.svg)](https://github.com/xoofx/Tomlyn/actions/workflows/ci.yml) [![Coverage Status](https://coveralls.io/repos/github/xoofx/Tomlyn/badge.svg?branch=main)](https://coveralls.io/github/xoofx/Tomlyn?branch=main) [![NuGet](https://img.shields.io/nuget/v/Tomlyn.svg)](https://www.nuget.org/packages/Tomlyn/)

<img align="right" width="256px" height="256px" src="img/Tomlyn.png">

Tomlyn is a high-performance .NET [TOML](https://toml.io/en/) 1.1 parser, round-trippable syntax tree, and `System.Text.Json`-style object serializer - NativeAOT ready.

#### <img src="https://xoofx.github.io/SharpYaml/img/SharpYaml.png" alt="SharpYaml" height="32" style="vertical-align: text-bottom; margin-right: .45rem;" /> Looking for YAML support? Check out [SharpYaml](https://xoofx.github.io/SharpYaml/).

> **Note**: Tomlyn v1 is a major redesign with breaking changes from earlier versions. It uses a **`System.Text.Json`-style API** with `TomlSerializer`, `TomlSerializerOptions`, and resolver-based metadata (`ITomlTypeInfoResolver`). See the [migration guide](https://xoofx.github.io/Tomlyn/docs/migration) for details.

## ✨ Features

- **`System.Text.Json`-style API**: familiar surface with `TomlSerializer`, `TomlSerializerOptions`, `TomlTypeInfo<T>`
- **TOML 1.1.0 only**: Tomlyn v1 targets [TOML 1.1.0](https://toml.io/en/v1.1.0) and does **not** support TOML 1.0
- **Source generation**: NativeAOT / trimming friendly via `TomlSerializerContext` and `[TomlSerializable]` roots
- **`System.Text.Json` attribute interop**: reuse `[JsonPropertyName]`, `[JsonIgnore]`, `[JsonRequired]`, `[JsonConstructor]`, `[JsonObjectCreationHandling]`, and polymorphism attributes
- **Allocation-free parsing pipeline**: incremental `TomlLexer` → `TomlParser` with precise spans for errors
- **Low-level access**: full lexer/parser API plus a lossless, trivia-preserving syntax tree (`SyntaxParser` → `DocumentSyntax`)
- **Reflection control**: reflection-based POCO mapping is available, but can be disabled for NativeAOT via a feature switch / MSBuild property

## 📐 Requirements

Tomlyn targets `net8.0`, `net10.0`, and `netstandard2.0`.

- Consuming the NuGet package works on any runtime that supports `netstandard2.0` (including .NET Framework) or modern .NET (`net8.0+`).
- Building Tomlyn from source requires the .NET 10 SDK.

## 📦 Install

```sh
dotnet add package Tomlyn
```

Tomlyn ships the source generator in-package (`analyzers/dotnet/cs`) - no extra package needed.

## 🚀 Quick Start

```csharp
using Tomlyn;

// Serialize
var toml = TomlSerializer.Serialize(new { Name = "Ada", Age = 37 });

// Deserialize
var person = TomlSerializer.Deserialize<Person>(toml);
```

### Untyped model (`TomlTable`)

```csharp
using Tomlyn;
using Tomlyn.Model;

var toml = @"global = ""this is a string""
# This is a comment of a table
[my_table]
key = 1 # Comment a key
value = true
list = [4, 5, 6]
";

var model = TomlSerializer.Deserialize<TomlTable>(toml)!;
var global = (string)model["global"]!;

Console.WriteLine(global);
Console.WriteLine(TomlSerializer.Serialize(model));
```

### Options

```csharp
using System.Text.Json;
using Tomlyn;

var options = new TomlSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PreferredObjectCreationHandling = JsonObjectCreationHandling.Replace,
    WriteIndented = true,
    IndentSize = 4,
    MaxDepth = 64,
    DefaultIgnoreCondition = TomlIgnoreCondition.WhenWritingNull,
};

var toml = TomlSerializer.Serialize(config, options);
var model = TomlSerializer.Deserialize<MyConfig>(toml, options);
```

By default, `PropertyNamingPolicy` is `null`, meaning CLR member names are used as-is for TOML mapping keys (same default as `System.Text.Json`).
`MaxDepth = 0` uses the built-in default of `64`.
`PreferredObjectCreationHandling = JsonObjectCreationHandling.Replace` matches `System.Text.Json`: read-only properties are not populated unless you opt into `Populate` via options or `[JsonObjectCreationHandling]`.

### Source Generation

```csharp
using System.Text.Json.Serialization;
using Tomlyn.Serialization;

public sealed class MyConfig
{
    public string? Global { get; set; }
}

[TomlSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PreferredObjectCreationHandling = JsonObjectCreationHandling.Replace)]
[TomlSerializable(typeof(MyConfig))]
internal partial class MyTomlContext : TomlSerializerContext
{
}

var config = TomlSerializer.Deserialize(toml, MyTomlContext.Default.MyConfig);
var tomlOut = TomlSerializer.Serialize(config, MyTomlContext.Default.MyConfig);
```

### Reflection Control

Reflection fallback can be disabled globally before first serializer use:

```csharp
AppContext.SetSwitch("Tomlyn.TomlSerializer.IsReflectionEnabledByDefault", false);
```

When publishing with NativeAOT (`PublishAot=true`), the Tomlyn NuGet package disables reflection-based serialization by default.
You can override the default by setting the following MSBuild property in your app project:

```xml
<PropertyGroup>
  <TomlynIsReflectionEnabledByDefault>true</TomlynIsReflectionEnabledByDefault>
</PropertyGroup>
```

## 📖 Documentation

- [User guide](site/readme.md)
- Website (Lunet): https://xoofx.github.io/Tomlyn

## 🪪 License

This software is released under the [BSD-Clause 2 license](https://opensource.org/licenses/BSD-2-Clause). 

## 🤗 Author

Alexandre Mutel aka [xoofx](http://xoofx.github.io).
