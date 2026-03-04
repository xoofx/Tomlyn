---
title: Performance
---

Tomlyn is designed for high throughput and low allocations.

## Architecture

| Layer | Approach |
| --- | --- |
| [`TomlLexer`](xref:Tomlyn.Parsing.TomlLexer) | Allocation-free struct-based token iteration. |
| [`TomlParser`](xref:Tomlyn.Parsing.TomlParser) | Pull-based event stream - no intermediate DOM. |
| [`TomlSerializer`](xref:Tomlyn.TomlSerializer) | Reads directly from the parser stream when possible; source-generated metadata avoids reflection. |
| [`SyntaxParser`](xref:Tomlyn.Parsing.SyntaxParser) | Allocates tree nodes - use only when full-fidelity round-tripping is needed. |

## Practical tips

| Tip | Why |
| --- | --- |
| **Cache [`TomlSerializerOptions`](xref:Tomlyn.TomlSerializerOptions)** | It is an immutable `sealed record`. Creating it once avoids repeated metadata resolution. |
| **Prefer source generation** | [`TomlSerializerContext`](xref:Tomlyn.Serialization.TomlSerializerContext) / [`TomlTypeInfo<T>`](xref:Tomlyn.TomlTypeInfo`1) avoids reflection, reduces startup, and is required for NativeAOT. |
| **Use `Stream` overloads** | [`TomlSerializer.Deserialize<T>(Stream)`](xref:Tomlyn.TomlSerializer) reads UTF-8 directly, avoiding a full-string allocation. |
| **Use `byte[]` overloads** | [`TomlSerializer.Deserialize<T>(byte[])`](xref:Tomlyn.TomlSerializer) avoids the `string` → `byte[]` conversion for pre-loaded data. |
| **Set `SourceName` once** | Attach it to cached options, not per-call - it doesn't affect parsing, only exception messages. |
| **Avoid [`SyntaxParser`](xref:Tomlyn.Parsing.SyntaxParser) for mapping** | [`SyntaxParser`](xref:Tomlyn.Parsing.SyntaxParser) builds a full tree. If you only need .NET objects, use [`TomlSerializer`](xref:Tomlyn.TomlSerializer) directly. |

> [!TIP]
> For the best performance, combine source generation with `Stream` or `byte[]` overloads.
> This gives you zero-reflection, zero-intermediate-string deserialization.

## When to use each API

| Scenario | Best API | Reason |
| --- | --- | --- |
| Configuration file → POCO | [`TomlSerializer.Deserialize<T>`](xref:Tomlyn.TomlSerializer) | Fastest path, no intermediate model. |
| Dynamic access (no POCOs) | [`TomlSerializer.Deserialize<TomlTable>`](xref:Tomlyn.TomlSerializer) | Minimal allocations for untyped access. |
| Syntax highlighting | [`TomlLexer`](xref:Tomlyn.Parsing.TomlLexer) | Token-level, no tree construction. |
| Custom streaming reader | [`TomlParser`](xref:Tomlyn.Parsing.TomlParser) | Event-based, no allocations beyond the parser. |
| Formatter / linter | [`SyntaxParser`](xref:Tomlyn.Parsing.SyntaxParser) | Full-fidelity tree required for round-tripping. |
| Validation only | [`TomlParser`](xref:Tomlyn.Parsing.TomlParser) with [`TomlParserMode.Tolerant`](xref:Tomlyn.Parsing.TomlParserMode.Tolerant) | Collects all errors without building a tree. |
