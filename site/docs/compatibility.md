---
title: Compatibility
---

## TOML version

Tomlyn v1 targets **TOML 1.1.0 only**. It does not support TOML 1.0 or earlier drafts.

See the official [TOML v1.1.0 specification](https://toml.io/en/v1.1.0) for the full language reference.

## Target frameworks

| Framework | Notes |
| --- | --- |
| `net10.0` | Fully optimized, all features available. |
| `net8.0` | Fully optimized, all features available. |
| `netstandard2.0` | Broad runtime compatibility; some newer numeric types (`Half`, `Int128`) unavailable. |

## Reflection, trimming, and NativeAOT

Tomlyn supports two metadata paths for object mapping:

| Path | Description | Trimming/AOT safe |
| --- | --- | --- |
| **Source generation** | [`TomlSerializerContext`](xref:Tomlyn.Serialization.TomlSerializerContext) / [`TomlTypeInfo<T>`](xref:Tomlyn.TomlTypeInfo`1) | ✅ Yes |
| **Reflection** | Automatic at runtime (enabled by default) | ⚠️ Requires annotations |

### Disabling reflection

Reflection-based object mapping can be disabled before first use:

```csharp
AppContext.SetSwitch("Tomlyn.TomlSerializer.IsReflectionEnabledByDefault", false);
```

> [!WARNING]
> This switch must be set **before** the first call to [`TomlSerializer`](xref:Tomlyn.TomlSerializer). Setting it after will have no effect.

When reflection is disabled:
- POCO/object mapping requires metadata via [`TomlTypeInfo<T>`](xref:Tomlyn.TomlTypeInfo`1) or [`TomlSerializerOptions.TypeInfoResolver`](xref:Tomlyn.TomlSerializerOptions.TypeInfoResolver).
- Built-in scalar converters and untyped containers ([`TomlTable`](xref:Tomlyn.Model.TomlTable), [`TomlArray`](xref:Tomlyn.Model.TomlArray)) remain supported without reflection.

See [Source generation and NativeAOT](source-generation.md) for details.

## System.Text.Json interop

Tomlyn reuses [`System.Text.Json`](xref:System.Text.Json) types in several areas:

- **Naming policies** - [`TomlSerializerOptions.PropertyNamingPolicy`](xref:Tomlyn.TomlSerializerOptions.PropertyNamingPolicy) accepts any [`JsonNamingPolicy`](xref:System.Text.Json.JsonNamingPolicy).
- **Attributes** - most common [`System.Text.Json.Serialization`](xref:System.Text.Json.Serialization) attributes work out of the box.
- **Source generation** - [`[TomlSerializable]`](xref:Tomlyn.Serialization.TomlSerializableAttribute) roots declare TOML contexts, while [`JsonKnownNamingPolicy`](xref:System.Text.Json.JsonKnownNamingPolicy) remains available for compile-time naming options.

> [!NOTE]
> This allows you to share model classes between JSON and TOML serializers. TOML-specific attributes take precedence when both are present.
