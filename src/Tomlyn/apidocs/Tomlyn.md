---
uid: Tomlyn
---

# Summary
Top-level APIs for parsing, round-trippable syntax trees, and `System.Text.Json`-style TOML serialization with Tomlyn.

# Remarks
This namespace contains the main entry points for most applications:

- `TomlSerializer` and `TomlSerializerOptions` for object serialization.
- `TomlException` for errors with precise source locations.
- `TomlTypeInfo<T>` and `ITomlTypeInfoResolver` for metadata-driven (AOT-friendly) serialization.

