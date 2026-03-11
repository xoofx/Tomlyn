---
title: Low-level APIs
---

Tomlyn provides public low-level building blocks for tooling, analyzers, formatters, and advanced scenarios.
These APIs live in [`Tomlyn.Parsing`](xref:Tomlyn.Parsing) and [`Tomlyn.Syntax`](xref:Tomlyn.Syntax).

> [!NOTE]
> If you want to map TOML to .NET objects, start with [Serialization](serialization.md) instead.
> These low-level APIs are for building custom tooling or when you need full control over parsing.

| API | Purpose | Allocations |
| --- | --- | --- |
| [`TomlLexer`](xref:Tomlyn.Parsing.TomlLexer) | Token stream (syntax highlighting, validation) | Minimal - struct-based iteration |
| [`TomlParser`](xref:Tomlyn.Parsing.TomlParser) | Incremental semantic events (custom readers) | Minimal - no intermediate DOM |
| [`SyntaxParser`](xref:Tomlyn.Parsing.SyntaxParser) | Full-fidelity syntax tree (round-tripping, tooling) | Allocates tree nodes |

## TomlLexer (tokenization)

[`TomlLexer`](xref:Tomlyn.Parsing.TomlLexer) produces a flat stream of tokens. Use it for syntax highlighting, quick validation, or building custom parsers.

```csharp
using Tomlyn.Parsing;

var lexer = TomlLexer.Create("name = \"Ada\"", sourceName: "config.toml");
while (lexer.MoveNext())
{
    var token = lexer.Current;
    Console.WriteLine($"{token.Kind} = {token.StringValue} @ {lexer.CurrentSpan}");
}
```

### Token kinds

Tokens are identified by [`TokenKind`](xref:Tomlyn.Syntax.TokenKind):

| Category | Kinds |
| --- | --- |
| Structure | `OpenBracket`, `CloseBracket`, `OpenBracketDouble`, `CloseBracketDouble`, `OpenBrace`, `CloseBrace`, `Equal`, `Comma`, `Dot` |
| Strings | `String`, `StringMulti`, `StringLiteral`, `StringLiteralMulti` |
| Numbers | `Integer`, `IntegerHexa`, `IntegerOctal`, `IntegerBinary`, `Float` |
| Booleans | `True`, `False` |
| Date/time | `OffsetDateTimeByZ`, `OffsetDateTimeByNumber`, `LocalDateTime`, `LocalDate`, `LocalTime` |
| Special | `Infinite`, `PositiveInfinite`, `NegativeInfinite`, `Nan`, `PositiveNan`, `NegativeNan` |
| Trivia | `Whitespaces`, `NewLine`, `Comment` |
| Meta | `BasicKey`, `Invalid`, `Eof` |

### Lexer options

[`TomlLexerOptions`](xref:Tomlyn.Parsing.TomlLexerOptions) controls decoding behavior:

```csharp
var options = new TomlLexerOptions { DecodeScalars = true };
var lexer = TomlLexer.Create(toml, options, sourceName: "config.toml");
```

When `DecodeScalars` is `true`, escape sequences in strings are decoded during tokenization.

## TomlParser (incremental parse events)

[`TomlParser`](xref:Tomlyn.Parsing.TomlParser) produces a sequence of [`TomlParseEvent`](xref:Tomlyn.Parsing.TomlParseEvent) values via `MoveNext()` without building an intermediate DOM.
This is ideal for building custom readers or streaming processors.

```csharp
using Tomlyn.Parsing;

var parser = TomlParser.Create("[server]\nhost = \"localhost\"\nport = 8080");
while (parser.MoveNext())
{
    ref readonly var evt = ref parser.Current;
    switch (evt.Kind)
    {
        case TomlParseEventKind.PropertyName:
            Console.Write($"{evt.GetString()} = ");
            break;
        case TomlParseEventKind.String:
            Console.WriteLine($"\"{evt.GetString()}\"");
            break;
        case TomlParseEventKind.Integer:
            Console.WriteLine(evt.GetInt64());
            break;
        case TomlParseEventKind.StartTable:
            Console.WriteLine($"[table depth={parser.Depth}]");
            break;
    }
}
```

### Parse events

| Kind | Description |
| --- | --- |
| [`StartDocument`](xref:Tomlyn.Parsing.TomlParseEventKind.StartDocument) / [`EndDocument`](xref:Tomlyn.Parsing.TomlParseEventKind.EndDocument) | Document boundaries. |
| [`StartTable`](xref:Tomlyn.Parsing.TomlParseEventKind.StartTable) / [`EndTable`](xref:Tomlyn.Parsing.TomlParseEventKind.EndTable) | Table boundaries (including inline tables). |
| [`StartArray`](xref:Tomlyn.Parsing.TomlParseEventKind.StartArray) / [`EndArray`](xref:Tomlyn.Parsing.TomlParseEventKind.EndArray) | Array boundaries (including table arrays). |
| [`PropertyName`](xref:Tomlyn.Parsing.TomlParseEventKind.PropertyName) | A TOML key - use `evt.GetString()` to read it. |
| [`String`](xref:Tomlyn.Parsing.TomlParseEventKind.String) | A string value - use `evt.GetString()`. |
| [`Integer`](xref:Tomlyn.Parsing.TomlParseEventKind.Integer) | An integer value - use `evt.GetInt64()`. |
| [`Float`](xref:Tomlyn.Parsing.TomlParseEventKind.Float) | A float value - use `evt.GetDouble()`. |
| [`Boolean`](xref:Tomlyn.Parsing.TomlParseEventKind.Boolean) | A boolean value - use `evt.GetBoolean()`. |
| [`DateTime`](xref:Tomlyn.Parsing.TomlParseEventKind.DateTime) | A date/time value - use `evt.GetTomlDateTime()`. |

### Parser options

[`TomlParserOptions`](xref:Tomlyn.Parsing.TomlParserOptions) controls parsing behavior:

```csharp
var parserOptions = new TomlParserOptions
{
    Mode = TomlParserMode.Tolerant,   // collect errors instead of throwing
    DecodeScalars = true,             // decode escape sequences
    CaptureTrivia = true,             // include comments/whitespace in events
    EagerStringValues = false,        // defer string materialization
};

var serializerOptions = new TomlSerializerOptions
{
    MaxDepth = 64,
};

var parser = TomlParser.Create(toml, parserOptions, serializerOptions);
```

| Option | Default | Description |
| --- | --- | --- |
| [`Mode`](xref:Tomlyn.Parsing.TomlParserOptions.Mode) | `Strict` | `Strict` throws on first error; `Tolerant` collects diagnostics. |
| [`DecodeScalars`](xref:Tomlyn.Parsing.TomlParserOptions.DecodeScalars) | `false` | Decode escape sequences in strings. |
| [`CaptureTrivia`](xref:Tomlyn.Parsing.TomlParserOptions.CaptureTrivia) | `false` | Include trivia tokens (comments, whitespace). |
| [`EagerStringValues`](xref:Tomlyn.Parsing.TomlParserOptions.EagerStringValues) | `false` | Materialize string values eagerly. |

Shared limits such as maximum nesting depth are configured via [`TomlSerializerOptions.MaxDepth`](xref:Tomlyn.TomlSerializerOptions.MaxDepth)
and flow through low-level APIs like [`TomlParser`](xref:Tomlyn.Parsing.TomlParser),
[`TomlReader`](xref:Tomlyn.Serialization.TomlReader), [`TomlWriter`](xref:Tomlyn.Serialization.TomlWriter),
and the overloads on [`SyntaxParser`](xref:Tomlyn.Parsing.SyntaxParser) that accept [`TomlSerializerOptions`](xref:Tomlyn.TomlSerializerOptions).

### Input sources

[`TomlParser.Create(...)`](xref:Tomlyn.Parsing.TomlParser) accepts `string` and `TextReader`:

```csharp
// From a file stream
using var reader = new StreamReader("config.toml");
var parser = TomlParser.Create(reader);
```

## SyntaxParser (full-fidelity syntax tree)

[`SyntaxParser`](xref:Tomlyn.Parsing.SyntaxParser) builds a complete [`DocumentSyntax`](xref:Tomlyn.Syntax.DocumentSyntax) tree that preserves every character from the original input -
comments, whitespace, formatting. This is the right tool for formatters, analyzers, linters, and round-trip editing.

> [!TIP]
> `doc.ToString()` reproduces the exact original text. Modify the tree nodes in-place and call `ToString()` again
> to get the updated TOML with all formatting and comments preserved.

```csharp
using Tomlyn.Parsing;

var doc = SyntaxParser.Parse("# config\nname = \"Ada\"\nage = 37", sourceName: "config.toml");

if (doc.HasErrors)
{
    foreach (var diag in doc.Diagnostics)
        Console.WriteLine(diag);
}

// Round-trip: ToString() reproduces the exact original text
Console.WriteLine(doc.ToString());
```

### Parse vs ParseStrict

| Method | Behavior |
| --- | --- |
| [`SyntaxParser.Parse(...)`](xref:Tomlyn.Parsing.SyntaxParser) | Tolerant - collects errors in `doc.Diagnostics`, always returns a tree. |
| [`SyntaxParser.ParseStrict(...)`](xref:Tomlyn.Parsing.SyntaxParser) | Strict - throws [`TomlException`](xref:Tomlyn.TomlException) on the first error. |

Both accept `string` and [`TomlLexer`](xref:Tomlyn.Parsing.TomlLexer) inputs. `Parse` also accepts `TextReader`.

### Syntax tree structure

The tree root is [`DocumentSyntax`](xref:Tomlyn.Syntax.DocumentSyntax), which contains:

- `KeyValues` - top-level key-value pairs (`SyntaxList<`[`KeyValueSyntax`](xref:Tomlyn.Syntax.KeyValueSyntax)`>`)
- `Tables` - table and table-array sections (`SyntaxList<`[`TableSyntaxBase`](xref:Tomlyn.Syntax.TableSyntaxBase)`>`)
- `Diagnostics` - parse errors/warnings ([`DiagnosticsBag`](xref:Tomlyn.Syntax.DiagnosticsBag))
- `HasErrors` - quick check for errors

Key node types:

| Node | Description |
| --- | --- |
| [`KeyValueSyntax`](xref:Tomlyn.Syntax.KeyValueSyntax) | A `key = value` pair. |
| [`KeySyntax`](xref:Tomlyn.Syntax.KeySyntax) | The key part (may be dotted). |
| [`StringValueSyntax`](xref:Tomlyn.Syntax.StringValueSyntax) | A string value (basic, literal, multiline). |
| [`IntegerValueSyntax`](xref:Tomlyn.Syntax.IntegerValueSyntax) | An integer value. |
| [`FloatValueSyntax`](xref:Tomlyn.Syntax.FloatValueSyntax) | A float value. |
| [`BooleanValueSyntax`](xref:Tomlyn.Syntax.BooleanValueSyntax) | A boolean value. |
| [`DateTimeValueSyntax`](xref:Tomlyn.Syntax.DateTimeValueSyntax) | A date/time value. |
| [`ArraySyntax`](xref:Tomlyn.Syntax.ArraySyntax) | An array `[...]`. |
| [`InlineTableSyntax`](xref:Tomlyn.Syntax.InlineTableSyntax) | An inline table `{...}`. |
| [`TableSyntax`](xref:Tomlyn.Syntax.TableSyntax) | A `[table]` header section. |
| [`TableArraySyntax`](xref:Tomlyn.Syntax.TableArraySyntax) | A `[[table_array]]` header section. |

### Trivia

Every [`SyntaxNode`](xref:Tomlyn.Syntax.SyntaxNode) can carry `LeadingTrivia` and `TrailingTrivia` - lists of [`SyntaxTrivia`](xref:Tomlyn.Syntax.SyntaxTrivia) containing comments, whitespace, and newlines.

### Tree traversal

Use the visitor pattern or enumerable extensions:

```csharp
using Tomlyn.Syntax;

// Visitor pattern
var visitor = new MyVisitor();
visitor.Visit(doc);

// Enumerable extensions
foreach (var node in doc.Descendants())
{
    if (node is StringValueSyntax str)
        Console.WriteLine(str.Value);
}

foreach (var token in doc.Tokens(includeCommentsAndWhitespaces: true))
{
    // Process every token
}
```

To implement a visitor, inherit from [`SyntaxVisitor`](xref:Tomlyn.Syntax.SyntaxVisitor) and override the `Visit` methods for the node types you care about:

```csharp
using Tomlyn.Syntax;

public sealed class StringCollector : SyntaxVisitor
{
    public List<string> Strings { get; } = new();

    protected override void Visit(StringValueSyntax node)
    {
        if (node.Value is not null)
            Strings.Add(node.Value);
        base.Visit(node);
    }
}

var collector = new StringCollector();
collector.Visit(doc);
```

### Diagnostics

[`DiagnosticsBag`](xref:Tomlyn.Syntax.DiagnosticsBag) is a collection of [`DiagnosticMessage`](xref:Tomlyn.Syntax.DiagnosticMessage) items:

```csharp
foreach (var diag in doc.Diagnostics)
{
    // diag.Kind    - Error or Warning
    // diag.Span    - source location
    // diag.Message - description
    Console.WriteLine($"{diag.Kind}: {diag.Message} at {diag.Span}");
}
```
