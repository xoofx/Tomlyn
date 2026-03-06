---
title: Migration (v0.x to v1.0)
---

Tomlyn v1.0 is a breaking-change release aligned with modern .NET serialization patterns (similar to `System.Text.Json`) and **TOML 1.1.0**.
This page summarizes the major breaking changes when migrating from Tomlyn v0.x.

## TOML version

- v1 targets **TOML 1.1.0 only** (Tomlyn v1 does **not** support TOML 1.0).

## Quick mapping (old → new)

| v0.x | v1.0 |
| --- | --- |
| `Toml.ToModel(...)` | [`TomlSerializer.Deserialize(...)`](xref:Tomlyn.TomlSerializer) |
| `Toml.ToModel<T>(...)` | `TomlSerializer.Deserialize<T>(...)` |
| `Toml.FromModel(...)` | [`TomlSerializer.Serialize(...)`](xref:Tomlyn.TomlSerializer) |
| `Toml.Parse(...)` | [`SyntaxParser.Parse(...)`](xref:Tomlyn.Parsing.SyntaxParser) / `ParseStrict(...)` |
| `DocumentSyntax.Tokens()` | [`TomlLexer`](xref:Tomlyn.Parsing.TomlLexer) |
| `ITomlMetadataProvider` (on your types) | [`TomlSerializerOptions.MetadataStore`](xref:Tomlyn.TomlSerializerOptions.MetadataStore) (external store) |
| `[TomlModel]` source gen roots | [`TomlSerializerContext`](xref:Tomlyn.Serialization.TomlSerializerContext) + [`TomlSerializable`](xref:Tomlyn.Serialization.TomlSerializableAttribute) |

## Main API shape (TomlSerializer)

The `Toml` facade API from v0.x (`Toml.ToModel`, `Toml.FromModel`, `Toml.Parse`, ...) is removed.
Tomlyn v1 uses a `System.Text.Json`-style entry point:

```csharp
using Tomlyn;

var toml = TomlSerializer.Serialize(value);
var model = TomlSerializer.Deserialize<MyType>(toml);
```

```csharp
var options = new TomlSerializerOptions { WriteIndented = true };
var toml = TomlSerializer.Serialize(value, options);
```

See [Serialization](serialization.md) for the full API surface and options.

## Dynamic model (TomlTable / TomlArray)

In v0.x, a common pattern was:

```csharp
var model = Toml.ToModel(toml);            // TomlTable
var tomlOut = Toml.FromModel(model);
```

In v1.0, use the DOM types through `TomlSerializer`:

```csharp
using Tomlyn;
using Tomlyn.Model;

var table = TomlSerializer.Deserialize<TomlTable>(toml)!;
var tomlOut = TomlSerializer.Serialize(table);
```

See [DOM model](dom.md).

## Source generation and NativeAOT

v0.x source generation required referencing an analyzer and marking a root model with `[TomlModel]`.

Tomlyn v1 uses a `System.Text.Json`-style source generation model based on:

- [`TomlSerializerContext`](xref:Tomlyn.Serialization.TomlSerializerContext)
- [`TomlSerializableAttribute`](xref:Tomlyn.Serialization.TomlSerializableAttribute) roots

> [!NOTE]
> The Tomlyn v1 source generator ships with the main `Tomlyn` NuGet package (as a compiler analyzer).
> You typically only need to add a `TomlSerializerContext` to your project.

```csharp
using Tomlyn.Serialization;

[TomlSerializable(typeof(MyType))]
internal partial class MyTomlContext : TomlSerializerContext
{
}
```

Use generated `TomlTypeInfo<T>` metadata:

```csharp
var context = MyTomlContext.Default;
var toml = TomlSerializer.Serialize(value, context.MyType);
var model = TomlSerializer.Deserialize(toml, context.MyType);
```

When publishing with NativeAOT, reflection-based object mapping is disabled by default (via MSBuild feature switches).
See [Source generation and NativeAOT](source-generation.md).

## JSON attribute interop

v0.x used custom attribute lists configured through `TomlModelOptions` (e.g. which attributes affect naming or ignore behavior).

Tomlyn v1 supports `System.Text.Json.Serialization` attributes (and equivalent Tomlyn attributes) directly in both reflection and source-generated mapping.
For the exact supported set and precedence rules, see [Serialization](serialization.md#attributes).

## Reflection control

Reflection-based serialization can be disabled:

```csharp
AppContext.SetSwitch("Tomlyn.TomlSerializer.IsReflectionEnabledByDefault", false);
```

When reflection is disabled, object mapping requires metadata via `TomlSerializerContext`/`TomlTypeInfo<T>` (or a custom `ITomlTypeInfoResolver`).

## Preserving comments / trivia (ITomlMetadataProvider replacement)

In v0.x, preserving comments on custom models required implementing `ITomlMetadataProvider` on your CLR types, which “polluted” your models with extra members.

Tomlyn v1 replaces this with an external metadata store:

```csharp
using Tomlyn;
using Tomlyn.Serialization;

var store = new TomlMetadataStore();
var options = new TomlSerializerOptions { MetadataStore = store };

var model = TomlSerializer.Deserialize<MyType>(toml, options)!;
var tomlOut = TomlSerializer.Serialize(model, options);
```

When `MetadataStore` is set, Tomlyn can capture trivia and source spans during deserialization and reuse them when serializing again, without requiring any interface on your models.
See [DOM model → Metadata and trivia](dom.md#metadata-and-trivia) for details.

## Parsing and syntax tree APIs

In v0.x, the main entry point for a full-fidelity parse was `Toml.Parse(...)` returning a `DocumentSyntax`.

In v1.0, the low-level pipeline is split into three public layers (see [Low-level APIs](low-level.md)):

- [`TomlLexer`](xref:Tomlyn.Parsing.TomlLexer): tokens (syntax highlighting, diagnostics, tooling)
- [`TomlParser`](xref:Tomlyn.Parsing.TomlParser): incremental semantic events (reader/serializer)
- [`SyntaxParser`](xref:Tomlyn.Parsing.SyntaxParser): full-fidelity [`DocumentSyntax`](xref:Tomlyn.Syntax.DocumentSyntax) (round-trip, comments, whitespace)

If you want strict “throw on first error” behavior, use `ParseStrict(...)`:

```csharp
using Tomlyn.Parsing;

var doc = SyntaxParser.ParseStrict(toml, sourceName: "config.toml");
```

## Exceptions include precise locations

Tomlyn v1 ensures parsing errors provide actionable location information.
When parsing fails in strict mode, Tomlyn throws [`TomlException`](xref:Tomlyn.TomlException) which exposes:

- `Span` / `SourceName`
- 1-based `Line` and `Column`
- 0-based `Offset`

For tolerant parsing, use APIs that return diagnostics (for example [`SyntaxParser.Parse`](xref:Tomlyn.Parsing.SyntaxParser)).

## Low-level vs object mapping

- Use `Tomlyn.Syntax` / `SyntaxParser` for lossless roundtrip and source span tooling.
- Use `TomlSerializer` for mapping to .NET objects (does not preserve formatting/comments).
