---
uid: Tomlyn.Serialization
---

# Summary
Object serialization layer, aligned with `System.Text.Json` concepts, attributes, and source generation.

# Remarks
This namespace contains the building blocks used by `TomlSerializer`:

- Metadata (`TomlTypeInfo<T>`, resolver interfaces, and source-generated contexts).
- Attribute support (Tomlyn attributes and key `System.Text.Json.Serialization` attributes).
- Converters (`TomlConverter<T>` / `TomlConverterFactory`) and reader/writer primitives (`TomlReader`, `TomlWriter`).

