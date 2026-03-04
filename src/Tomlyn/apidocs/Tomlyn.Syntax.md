---
uid: Tomlyn.Syntax
---

# Summary
Lossless TOML syntax tree APIs with source locations for roundtripping and tooling.

# Remarks
Use the syntax layer when you need:

- Exact source spans (line/column) for diagnostics and editor tooling.
- Roundtripping without losing structure/formatting details (including trivia/comments).

The syntax layer is independent from the object serializer.

