---
title: Compatibility
---

## TOML version

- Tomlyn v1 targets **TOML 1.1.0 only**.

## Target frameworks

- `netstandard2.0`
- `net8.0`
- `net10.0`

## Reflection, trimming, and NativeAOT

Tomlyn supports two metadata paths for object mapping:

1. **Generated metadata** using `TomlSerializerContext` / `TomlTypeInfo<T>` (recommended for NativeAOT/trimming).
2. **Reflection fallback** (enabled by default).

You can disable reflection-based object mapping before first use:

```csharp
AppContext.SetSwitch("Tomlyn.TomlSerializer.IsReflectionEnabledByDefault", false);
```

When reflection is disabled, POCO/object mapping requires metadata via `TomlTypeInfo<T>` or `TomlSerializerOptions.TypeInfoResolver`.
Built-in scalar converters and untyped containers remain supported without reflection.
