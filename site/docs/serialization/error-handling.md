---
title: Error handling
---

Parsing and mapping errors throw `TomlException`.

Key properties:

- Error messages aim to be actionable.
- Exceptions include precise locations (`TomlSourceSpan`) so you can point to the failing part of the document.

Use `TryDeserialize(...)` if you prefer a non-throwing API surface.
