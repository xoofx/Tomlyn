---
title: Migration (v0.x to v1.0)
---

Tomlyn v1.0 is a breaking-change release aligned with modern .NET serialization patterns and TOML 1.1.0.

## TOML version

- v1 targets **TOML 1.1.0 only**.

## Main API shape

Tomlyn v1 uses a `System.Text.Json`-style entry point:

```csharp
using Tomlyn;

var toml = TomlSerializer.Serialize(value);
var model = TomlSerializer.Deserialize<MyType>(toml);
```

Most configuration moves to `TomlSerializerOptions`:

```csharp
var options = new TomlSerializerOptions { WriteIndented = true };
var toml = TomlSerializer.Serialize(value, options);
```

## Source generation and NativeAOT

Tomlyn v1 supports source generation through `TomlSerializerContext` and `JsonSerializableAttribute` roots:

```csharp
using System.Text.Json.Serialization;
using Tomlyn.Serialization;

[JsonSerializable(typeof(MyType))]
internal partial class MyTomlContext : TomlSerializerContext
{
}
```

Use generated `TomlTypeInfo<T>` metadata:

```csharp
var context = MyTomlContext.Default;
var toml = TomlSerializer.Serialize(value, context.MyType);
var model = TomlSerializer.Deserialize(toml, context.MyType);
```

## JSON attribute interop

Tomlyn v1 supports a subset of `System.Text.Json.Serialization` attributes for mapping (for example `JsonPropertyNameAttribute`, `JsonIgnoreAttribute`, `JsonIncludeAttribute`).
See [Serialization](serialization.md) for details and the supported-attribute matrix.

## Reflection control

Reflection-based serialization can be disabled:

```csharp
AppContext.SetSwitch("Tomlyn.TomlSerializer.IsReflectionEnabledByDefault", false);
```

When reflection is disabled, object mapping requires metadata via `TomlSerializerContext`/`TomlTypeInfo<T>` (or a custom `ITomlTypeInfoResolver`).

## Low-level vs object mapping

- Use `Tomlyn.Syntax` / `SyntaxParser` for lossless roundtrip and source span tooling.
- Use `TomlSerializer` for mapping to .NET objects (does not preserve formatting/comments).
