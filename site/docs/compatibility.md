---
title: Compatibility
---

- TOML version: **1.1.0 only** (Tomlyn 1.0 does not target TOML 1.0).
- Target frameworks: `netstandard2.0`, `net8.0`, `net10.0`.
- Reflection-based serialization can be disabled for NativeAOT; prefer source generation via `TomlSerializerContext`.
