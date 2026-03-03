---
title: Serialization
---

Tomlyn provides a `System.Text.Json`-style API for mapping TOML to .NET objects:

- Entry point: `TomlSerializer`
- Options: `TomlSerializerOptions`
- Metadata: `TomlTypeInfo` and `TomlSerializerContext` (source-generated)

This section documents object mapping, supported types, converters, attributes, and error handling.
