---
title: Low-level APIs
---

Tomlyn provides public low-level building blocks intended for tooling and advanced scenarios:

- `TomlLexer` (token stream)
- `TomlParser` (incremental parse events)
- `SyntaxParser` (full-fidelity `DocumentSyntax` for round-tripping)

If you want to map TOML to .NET objects, start with [Serialization](serialization.md) instead.

## TomlLexer (tokenization)

Use `TomlLexer` when you need token-level tooling (syntax highlighting, quick validation, etc.).

```csharp
using Tomlyn.Parsing;

var lexer = TomlLexer.Create("name = \"Ada\"", sourceName: "config.toml");
while (lexer.MoveNext())
{
    var token = lexer.Current;
    // token.Kind, token.StringValue, token.Data, lexer.CurrentSpan
}
```

## TomlParser (incremental parse events)

`TomlParser` exposes a sequence of `TomlParseEvent` values via `MoveNext()` without building an intermediate DOM.

```csharp
using Tomlyn.Parsing;

var parser = TomlParser.Create("name = \"Ada\"");
while (parser.MoveNext())
{
    ref readonly var evt = ref parser.Current;
    // evt.Kind, evt.Span, evt.PropertyName, evt.StringValue, evt.Data
    // parser.Depth tracks container nesting.
}
```

`TomlParserOptions` controls strict vs tolerant parsing, scalar decoding, and trivia capture.

## SyntaxParser (full fidelity)

If you need to preserve comments, whitespace, and formatting, parse into a syntax tree:

```csharp
using Tomlyn.Parsing;

var doc = SyntaxParser.ParseStrict("# comment\nname = \"Ada\"", sourceName: "config.toml");
// doc is a DocumentSyntax and can be round-tripped.
```

The syntax tree APIs are intended for tooling and round-tripping. Object mapping via `TomlSerializer` does not preserve formatting/comments.
