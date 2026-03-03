---
title: Converters
---

Customize mapping by implementing `TomlConverter<T>` (or a `TomlConverterFactory`) and registering it via:

- `TomlSerializerOptions.Converters`
- `TomlConverterAttribute` on a type or member

Converters receive a `TomlReader` / `TomlWriter` for efficient streaming reads/writes.
