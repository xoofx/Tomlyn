---
title: Performance
---

Tomlyn is designed for high throughput and low allocations for **configuration-style** TOML workloads.

## Architecture

| Layer | Approach |
| --- | --- |
| [`TomlLexer`](xref:Tomlyn.Parsing.TomlLexer) | Allocation-free struct-based token iteration. |
| [`TomlParser`](xref:Tomlyn.Parsing.TomlParser) | Pull-based event stream - no intermediate DOM. |
| [`TomlSerializer`](xref:Tomlyn.TomlSerializer) | Reads directly from the parser stream when possible; source-generated metadata avoids reflection. |
| [`SyntaxParser`](xref:Tomlyn.Parsing.SyntaxParser) | Allocates tree nodes - use only when full-fidelity round-tripping is needed. |

## What “high-performance” means for Tomlyn

Tomlyn is optimized around the common TOML use-case: **configuration files** that can be loaded in memory.
Tomlyn is not a streaming parser and it does not aim to match the design goals of `System.Text.Json` for high-throughput web API scenarios.

Internally, Tomlyn parses a **UTF-16 `ReadOnlyMemory<char>`** view of the TOML payload.
This means that:

- Passing TOML as `string` is the most direct path.
- When you use `Stream` or `TextReader`, Tomlyn reads the entire payload into memory before parsing.

## Practical tips

| Tip | Why |
| --- | --- |
| **Cache [`TomlSerializerOptions`](xref:Tomlyn.TomlSerializerOptions)** | It is an immutable `sealed record`. Creating it once avoids repeated metadata resolution. |
| **Prefer source generation** | [`TomlSerializerContext`](xref:Tomlyn.Serialization.TomlSerializerContext) / [`TomlTypeInfo<T>`](xref:Tomlyn.TomlTypeInfo`1) avoids reflection, reduces startup, and is required for NativeAOT. |
| **Use `Stream`/`TextReader` for convenience** | Tomlyn reads the entire content into memory before parsing; use these overloads to integrate with existing IO code. |
| **Set `SourceName` once** | Attach it to cached options, not per-call - it doesn't affect parsing, only exception messages. |
| **Avoid [`SyntaxParser`](xref:Tomlyn.Parsing.SyntaxParser) for mapping** | [`SyntaxParser`](xref:Tomlyn.Parsing.SyntaxParser) builds a full tree. If you only need .NET objects, use [`TomlSerializer`](xref:Tomlyn.TomlSerializer) directly. |

> [!TIP]
> For the best performance, combine source generation with a cached `TomlSerializerContext` and cached `TomlSerializerOptions`.

## When to use each API

| Scenario | Best API | Reason |
| --- | --- | --- |
| Configuration file → POCO | [`TomlSerializer.Deserialize<T>`](xref:Tomlyn.TomlSerializer) | Fastest path, no intermediate model. |
| Dynamic access (no POCOs) | [`TomlSerializer.Deserialize<TomlTable>`](xref:Tomlyn.TomlSerializer) | Minimal allocations for untyped access. |
| Syntax highlighting | [`TomlLexer`](xref:Tomlyn.Parsing.TomlLexer) | Token-level, no tree construction. |
| Custom streaming reader | [`TomlParser`](xref:Tomlyn.Parsing.TomlParser) | Event-based, no allocations beyond the parser. |
| Formatter / linter | [`SyntaxParser`](xref:Tomlyn.Parsing.SyntaxParser) | Full-fidelity tree required for round-tripping. |
| Validation only | [`TomlParser`](xref:Tomlyn.Parsing.TomlParser) with [`TomlParserMode.Tolerant`](xref:Tomlyn.Parsing.TomlParserMode.Tolerant) | Collects all errors without building a tree. |
