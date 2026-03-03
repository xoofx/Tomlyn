---
title: Low-level APIs
---

Tomlyn provides public low-level building blocks:

- `TomlLexer` to tokenize TOML input
- `TomlParser` to iterate parse events incrementally
- `SyntaxParser` to build a full-fidelity `DocumentSyntax`

These APIs are intended for tooling, analyzers, formatters, and advanced scenarios.
