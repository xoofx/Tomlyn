---
title: Source generation
---

Tomlyn source generation is based on `System.Text.Json` concepts:

- Annotate roots with `JsonSerializableAttribute`
- Configure with `TomlSourceGenerationOptionsAttribute`
- Use the generated `TomlSerializerContext` for NativeAOT and trimming scenarios

The generated context provides `TomlTypeInfo<T>` instances used by `TomlSerializer`.
