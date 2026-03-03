---
title: Mapping overview
---

Tomlyn maps TOML keys to .NET members using:

- `TomlPropertyNameAttribute` / `JsonPropertyNameAttribute`
- Naming policies from `TomlSerializerOptions`
- Reflection (when enabled) or source-generated `TomlSerializerContext`

TOML tables map naturally to .NET objects; arrays map to .NET collections.
