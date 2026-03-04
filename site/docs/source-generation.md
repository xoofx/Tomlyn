---
title: Source generation and NativeAOT
---

Tomlyn supports an incremental source generator for high-performance, NativeAOT-friendly serialization.

## When to use source generation

Use source generation when:

- You publish with `PublishAot=true`.
- You trim aggressively (`PublishTrimmed=true`).
- You want to avoid reflection and reduce startup overhead.

## Define a context

Tomlyn reuses `System.Text.Json.Serialization` source-generation attributes.

```csharp
using System.Text.Json.Serialization;
using Tomlyn.Serialization;

[TomlSourceGenerationOptions(PropertyNamingPolicy = System.Text.Json.JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(MyConfig))]
internal partial class MyTomlContext : TomlSerializerContext
{
}
```

The context type must be `partial` so the generator can add metadata properties.

## Compile-time options

Use `TomlSourceGenerationOptionsAttribute` to fix a context's default `TomlSerializerOptions` at build time (including converter registration):

```csharp
using Tomlyn;
using Tomlyn.Serialization;

[TomlSourceGenerationOptions(
    WriteIndented = true,
    IndentSize = 2,
    DefaultIgnoreCondition = TomlIgnoreCondition.WhenWritingNull,
    Converters = new[] { typeof(MyCustomConverter) })]
internal partial class MyTomlContext : TomlSerializerContext
{
}
```

## Use generated metadata

Use the generated `TomlTypeInfo<T>` properties (recommended):

```csharp
using Tomlyn;
var context = MyTomlContext.Default;

var toml = TomlSerializer.Serialize(value, context.MyConfig);
var roundTrip = TomlSerializer.Deserialize(toml, context.MyConfig);
```

For APIs that take a `Type`, prefer the overloads that accept a context:

```csharp
var toml = TomlSerializer.Serialize(value, typeof(MyConfig), context);
var roundTrip = TomlSerializer.Deserialize(toml, typeof(MyConfig), context);
```

Prefer overloads that accept a `TomlSerializerContext` or a `TomlTypeInfo<T>` directly. This avoids reflection and works well with trimming and NativeAOT.

## Naming policy and generated code

For source generation, member names are resolved at build time using:

- `TomlPropertyNameAttribute` / `JsonPropertyNameAttribute` when present (these override naming policies).
- otherwise, `TomlSourceGenerationOptionsAttribute.PropertyNamingPolicy` (or no policy when unspecified).

The generated serializer stores the resolved names directly and does not call `ConvertName(...)` at runtime for object members.

## Reflection control

Reflection fallback can be disabled globally before first serializer use:

```csharp
AppContext.SetSwitch("Tomlyn.TomlSerializer.IsReflectionEnabledByDefault", false);
```

When reflection is disabled, you must provide metadata via `TomlTypeInfo<T>` or `TomlSerializerOptions.TypeInfoResolver`.
This applies to .NET object mapping (POCOs, collections of POCOs, etc.).
Built-in primitives and untyped containers remain supported without reflection.

## JSON/TOML attributes support (source generation)

The source generator currently supports a focused subset of mapping attributes at compile time:

- `JsonPropertyNameAttribute` / `TomlPropertyNameAttribute`
- `JsonIgnoreAttribute` / `TomlIgnoreAttribute`
- `JsonIncludeAttribute` / `TomlIncludeAttribute`
- `JsonPropertyOrderAttribute` / `TomlPropertyOrderAttribute`

Other attributes (for example `JsonRequiredAttribute`, `JsonExtensionDataAttribute`, polymorphism attributes, etc.) are supported by the reflection resolver, but are not yet fully modeled by generated metadata.
If you rely on those features, keep reflection enabled or register a custom `ITomlTypeInfoResolver`.

## Troubleshooting

- If generated properties are missing, ensure the project references the `Tomlyn` NuGet package (the generator is shipped in-package under `analyzers/dotnet/cs`).
- Ensure the context class is `partial`.
- Ensure roots are declared via `[JsonSerializable(typeof(...))]`.
