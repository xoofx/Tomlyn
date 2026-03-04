---
uid: Tomlyn.Parsing
---

# Summary
Low-level parsing APIs: allocation-free lexer and incremental parser, plus the lossless syntax parser.

# Remarks
This namespace provides:

- `TomlLexer`: an allocation-free token stream with precise source locations.
- `TomlParser`: a pull-based, incremental event stream (`MoveNext()`) suitable for readers/serializers.
- `SyntaxParser`: a full-fidelity parser that builds a round-trippable `DocumentSyntax` (trivia-preserving).

