---
title: Source generation and NativeAOT
---

Tomlyn supports an incremental source generator for high-performance, NativeAOT-friendly serialization.
It follows the same patterns as [`System.Text.Json`](xref:System.Text.Json) source generation.

## When to use source generation

> [!TIP]
> Source generation is recommended for production applications. It eliminates reflection, enables trimming,
> and catches serialization configuration errors at compile time.

- Publishing with `PublishAot=true` (NativeAOT).
- Trimming aggressively (`PublishTrimmed=true`).
- Avoiding reflection overhead on hot paths.
- Wanting compile-time validation of serialization metadata.

## Define a context

Declare a `partial` class that inherits from [`TomlSerializerContext`](xref:Tomlyn.Serialization.TomlSerializerContext) and annotate it with [`[TomlSerializable]`](xref:Tomlyn.Serialization.TomlSerializableAttribute) for each root type:

```csharp
using Tomlyn.Serialization;

public sealed class ServerConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 8080;
}

[TomlSerializable(typeof(ServerConfig))]
internal partial class MyTomlContext : TomlSerializerContext { }
```

The generator produces a `Default` singleton and a typed [`TomlTypeInfo<T>`](xref:Tomlyn.TomlTypeInfo`1) property for each root.
Nested types referenced by the root are discovered transitively - you only need to annotate top-level types.
Set [`TomlSerializableAttribute.TypeInfoPropertyName`](xref:Tomlyn.Serialization.TomlSerializableAttribute.TypeInfoPropertyName) when you want to customize the generated property name exposed by the context.

## Use generated metadata

Use the generated [`TomlTypeInfo<T>`](xref:Tomlyn.TomlTypeInfo`1) property directly (recommended):

```csharp
using Tomlyn;

var context = MyTomlContext.Default;

var toml = TomlSerializer.Serialize(config, context.ServerConfig);
var roundTrip = TomlSerializer.Deserialize(toml, context.ServerConfig);
```

For APIs that take a `Type`, pass the context:

```csharp
var toml = TomlSerializer.Serialize(value, typeof(ServerConfig), context);
var roundTrip = TomlSerializer.Deserialize(toml, typeof(ServerConfig), context);
```

## Compile-time options

Use [`TomlSourceGenerationOptionsAttribute`](xref:Tomlyn.Serialization.TomlSourceGenerationOptionsAttribute) to fix serialization options at build time:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using Tomlyn;
using Tomlyn.Serialization;

[TomlSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true,
    IndentSize = 2,
    DefaultIgnoreCondition = TomlIgnoreCondition.WhenWritingNull)]
[TomlSerializable(typeof(ServerConfig))]
internal partial class MyTomlContext : TomlSerializerContext { }
```

### Registering converters at compile time

```csharp
[TomlSourceGenerationOptions(
    Converters = [typeof(MyCustomConverter)])]
[TomlSerializable(typeof(ServerConfig))]
internal partial class MyTomlContext : TomlSerializerContext { }
```

## Naming policy and generated code

For source generation, member names are resolved **at build time** using:

1. [`TomlPropertyNameAttribute`](xref:Tomlyn.TomlPropertyNameAttribute) / [`JsonPropertyNameAttribute`](xref:System.Text.Json.Serialization.JsonPropertyNameAttribute) (when present - these override naming policies).
2. [`TomlSourceGenerationOptionsAttribute.PropertyNamingPolicy`](xref:Tomlyn.Serialization.TomlSourceGenerationOptionsAttribute) (when no explicit name attribute).

The generated serializer stores the resolved names directly and does **not** call `ConvertName(...)` at runtime.

## Supported attributes (source generation)

The source generator supports these attributes at compile time:

| Member-level | Type-level |
| --- | --- |
| [`JsonPropertyNameAttribute`](xref:System.Text.Json.Serialization.JsonPropertyNameAttribute) / [`TomlPropertyNameAttribute`](xref:Tomlyn.TomlPropertyNameAttribute) | [`JsonConstructorAttribute`](xref:System.Text.Json.Serialization.JsonConstructorAttribute) / [`TomlConstructorAttribute`](xref:Tomlyn.Serialization.TomlConstructorAttribute) |
| [`JsonIgnoreAttribute`](xref:System.Text.Json.Serialization.JsonIgnoreAttribute) / [`TomlIgnoreAttribute`](xref:Tomlyn.Serialization.TomlIgnoreAttribute) | [`JsonPolymorphicAttribute`](xref:System.Text.Json.Serialization.JsonPolymorphicAttribute) / [`TomlPolymorphicAttribute`](xref:Tomlyn.Serialization.TomlPolymorphicAttribute) |
| [`JsonIncludeAttribute`](xref:System.Text.Json.Serialization.JsonIncludeAttribute) / [`TomlIncludeAttribute`](xref:Tomlyn.Serialization.TomlIncludeAttribute) | [`JsonDerivedTypeAttribute`](xref:System.Text.Json.Serialization.JsonDerivedTypeAttribute) / [`TomlDerivedTypeAttribute`](xref:Tomlyn.Serialization.TomlDerivedTypeAttribute) |
| [`JsonPropertyOrderAttribute`](xref:System.Text.Json.Serialization.JsonPropertyOrderAttribute) / [`TomlPropertyOrderAttribute`](xref:Tomlyn.Serialization.TomlPropertyOrderAttribute) | |
| [`JsonRequiredAttribute`](xref:System.Text.Json.Serialization.JsonRequiredAttribute) / [`TomlRequiredAttribute`](xref:Tomlyn.Serialization.TomlRequiredAttribute) | |
| [`JsonExtensionDataAttribute`](xref:System.Text.Json.Serialization.JsonExtensionDataAttribute) / [`TomlExtensionDataAttribute`](xref:Tomlyn.Serialization.TomlExtensionDataAttribute) | |

[`JsonConverterAttribute`](xref:System.Text.Json.Serialization.JsonConverterAttribute) / [`TomlConverterAttribute`](xref:Tomlyn.Serialization.TomlConverterAttribute) is supported by the **reflection resolver** but is **not** modeled by generated metadata.
For source-generated scenarios, register converters via [`TomlSourceGenerationOptionsAttribute.Converters`](xref:Tomlyn.Serialization.TomlSourceGenerationOptionsAttribute) or [`TomlSerializerOptions.Converters`](xref:Tomlyn.TomlSerializerOptions.Converters).

> [!WARNING]
> Using `[TomlConverter]` or `[JsonConverter]` attributes with source generation will silently fall back to reflection.
> Always register converters via `TomlSourceGenerationOptionsAttribute.Converters` for AOT safety.

## Reflection control

Reflection fallback can be disabled globally before first serializer use:

```csharp
AppContext.SetSwitch("Tomlyn.TomlSerializer.IsReflectionEnabledByDefault", false);
```

When reflection is disabled:

- Object mapping (POCOs, records, collections of POCOs) requires metadata via [`TomlTypeInfo<T>`](xref:Tomlyn.TomlTypeInfo`1) or [`TomlSerializerOptions.TypeInfoResolver`](xref:Tomlyn.TomlSerializerOptions.TypeInfoResolver).
- Built-in scalar converters and untyped containers ([`TomlTable`](xref:Tomlyn.Model.TomlTable), [`TomlArray`](xref:Tomlyn.Model.TomlArray)) remain supported without reflection.

You can also disable reflection via MSBuild in your project file:

```xml
<PropertyGroup>
  <TomlSerializerIsReflectionEnabledByDefault>false</TomlSerializerIsReflectionEnabledByDefault>
</PropertyGroup>
```

## Multiple contexts

You can define multiple contexts for different parts of your application:

```csharp
[TomlSerializable(typeof(ServerConfig))]
internal partial class ServerContext : TomlSerializerContext { }

[TomlSerializable(typeof(DatabaseConfig))]
internal partial class DatabaseContext : TomlSerializerContext { }
```

## Troubleshooting

| Problem | Solution |
| --- | --- |
| Generated properties are missing | Ensure the project references the `Tomlyn` NuGet package (the generator is shipped under `analyzers/dotnet/cs`). |
| Compilation errors on context | Ensure the context class is `partial`. |
| Type not found on context | Ensure root types are declared via `[TomlSerializable(typeof(...))]`. |
| Nested types not serialized | Nested types are discovered transitively - verify they're reachable from a root type. |
| Converter not applied | Use `TomlSourceGenerationOptionsAttribute.Converters` instead of `TomlConverterAttribute` for source-generated contexts. |
