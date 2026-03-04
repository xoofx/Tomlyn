---
title: Documentation
---

Tomlyn is a high-performance TOML 1.1 library for .NET, shaped like [`System.Text.Json`](xref:System.Text.Json).
It provides two main layers:

- **Object serialization** - [`TomlSerializer`](xref:Tomlyn.TomlSerializer) maps TOML documents to .NET objects (POCOs, records, dictionaries) and back.
  Supports reflection and source generation. Does **not** preserve comments or formatting.
- **Low-level parsing** - [`TomlLexer`](xref:Tomlyn.Parsing.TomlLexer), [`TomlParser`](xref:Tomlyn.Parsing.TomlParser), and [`SyntaxParser`](xref:Tomlyn.Parsing.SyntaxParser) provide token streams, incremental events,
  and a full-fidelity syntax tree for tooling, analyzers, and formatters that need to preserve every character.

## Where to start

| Scenario | Start here |
| --- | --- |
| New to Tomlyn | [Getting started](getting-started.md) |
| Familiar with [`System.Text.Json`](xref:System.Text.Json) | [Serialization](serialization.md) - the API shape is intentionally similar |
| Need NativeAOT / trimming | [Source generation](source-generation.md) |
| Want a dynamic model (no POCOs) | [DOM model](dom.md) - [`TomlTable`](xref:Tomlyn.Model.TomlTable), [`TomlArray`](xref:Tomlyn.Model.TomlArray) |
| Building tooling (formatters, analyzers) | [Low-level APIs](low-level.md) - [`SyntaxParser`](xref:Tomlyn.Parsing.SyntaxParser) |
| Migrating from Tomlyn v0.x | [Migration guide](migration.md) |

## Key concepts

> [!IMPORTANT]
> Tomlyn v1 targets **TOML 1.1.0 only**. It does not support TOML 1.0 or earlier drafts.
> See the official [TOML v1.1.0 specification](https://toml.io/en/v1.1.0) for the full language reference.

- **Diagnostics** - parsing and mapping failures throw [`TomlException`](xref:Tomlyn.TomlException) with precise source locations (`Line`, `Column`, `Offset`, `Span`).
- **Immutable options** - [`TomlSerializerOptions`](xref:Tomlyn.TomlSerializerOptions) is a `sealed record`; cache and reuse instances.
- **NativeAOT / trimming** - use source generation ([`TomlSerializerContext`](xref:Tomlyn.Serialization.TomlSerializerContext)) to avoid reflection. Reflection can be disabled via a feature switch.
