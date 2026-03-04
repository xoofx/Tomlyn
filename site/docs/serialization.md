---
title: Serialization
---

Tomlyn provides a `System.Text.Json`-style API for mapping TOML to .NET objects:

- Entry point: `TomlSerializer`
- Options: `TomlSerializerOptions`
- Metadata: `TomlTypeInfo<T>` and `TomlSerializerContext` (source-generated)

Tomlyn targets **TOML 1.1.0** and aims to be explicit about allocations and performance characteristics.

## Basic usage

```csharp
using Tomlyn;

public sealed record Person(string Name);

var toml = TomlSerializer.Serialize(new { Name = "Ada" });
var model = TomlSerializer.Deserialize<Person>(toml)!;
```

## Streams and UTF-8 payloads

Tomlyn provides `Stream` overloads (UTF-8) and `byte[]` overloads:

```csharp
using System.IO;
using Tomlyn;

using var stream = File.OpenRead("config.toml");
var config = TomlSerializer.Deserialize<MyConfig>(stream);
```

```csharp
using System.Text;
using Tomlyn;

var utf8 = Encoding.UTF8.GetBytes("Name = \"Ada\"");
var person = TomlSerializer.Deserialize<Person>(utf8);
```

## Reflection vs generated metadata

Tomlyn can resolve object-mapping metadata in two ways:

1. **Generated metadata** via `TomlSerializerContext` and `TomlTypeInfo<T>` (recommended for NativeAOT and hot paths).
2. **Reflection fallback** (enabled by default).

Reflection fallback can be disabled globally before first serializer use:

```csharp
AppContext.SetSwitch("Tomlyn.TomlSerializer.IsReflectionEnabledByDefault", false);
```

When reflection is disabled, POCO/object mapping requires `TomlTypeInfo<T>` (typically from a `TomlSerializerContext`) or a custom `ITomlTypeInfoResolver` configured on `TomlSerializerOptions.TypeInfoResolver`.

## Using a source-generated context

```csharp
using System.Text.Json.Serialization;
using Tomlyn;
using Tomlyn.Serialization;

[JsonSerializable(typeof(MyConfig))]
internal partial class MyTomlContext : TomlSerializerContext
{
}

var context = MyTomlContext.Default;
var toml = TomlSerializer.Serialize(config, context.MyConfig);
var roundTrip = TomlSerializer.Deserialize(toml, context.MyConfig);
```

## Options

`TomlSerializerOptions` is immutable and can be cached and reused.

Common options:

- `PropertyNamingPolicy` / `DictionaryKeyPolicy` (`System.Text.Json.JsonNamingPolicy`)
- `WriteIndented`, `IndentSize`, `NewLine`
- `DefaultIgnoreCondition`
- `PropertyNameCaseInsensitive`
- `DuplicateKeyHandling`
- `DottedKeyHandling`
- `RootValueHandling` / `RootValueKeyName`

```csharp
using System.Text.Json;
using Tomlyn;

var options = new TomlSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    IndentSize = 2,
    DefaultIgnoreCondition = TomlIgnoreCondition.WhenWritingNull,
    DuplicateKeyHandling = TomlDuplicateKeyHandling.Error,
};

var toml = TomlSerializer.Serialize(config, options);
var roundTrip = TomlSerializer.Deserialize<MyConfig>(toml, options);
```

## Supported types (built-in)

Tomlyn supports common scalar types out of the box (via built-in converters):

- `bool`
- numeric types: `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `nint`, `nuint`, `float`, `double`, `decimal`
- `Half` (when available)
- `Int128` / `UInt128` (when available)
- `char`, `string`
- `DateTime`, `DateTimeOffset`, `TomlDateTime`
- `DateOnly` / `TimeOnly` (when available)
- `Guid`, `TimeSpan`, `Uri`, `Version`
- DOM types: `TomlTable`, `TomlArray`, `TomlTableArray`
- `object` (untyped TOML values)

Tomlyn also supports common collection shapes (arrays, lists, sets, dictionaries, and immutable collections) and POCO mapping.

## TOML and JSON attributes

Tomlyn supports TOML-specific attributes, and also supports a subset of `System.Text.Json.Serialization` attributes so you can reuse models across JSON and TOML.

### Supported attributes

Member-level attributes:

| Attribute | Reflection | Source generated | Notes |
| --- | --- | --- | --- |
| `TomlPropertyNameAttribute` / `JsonPropertyNameAttribute` | Yes | Yes | Overrides the serialized name. |
| `TomlIgnoreAttribute` / `JsonIgnoreAttribute` | Yes | Yes | Supports ignore conditions for writes. |
| `TomlIncludeAttribute` / `JsonIncludeAttribute` | Yes | Yes | Enables non-public members in supported scenarios. |
| `TomlPropertyOrderAttribute` / `JsonPropertyOrderAttribute` | Yes | Yes | Controls ordering within tables. |
| `TomlRequiredAttribute` / `JsonRequiredAttribute` | Yes | No | Missing required members throw `TomlException`. |
| `TomlExtensionDataAttribute` / `JsonExtensionDataAttribute` | Yes | No | Captures unmapped keys. |
| `TomlConverterAttribute` / `JsonConverterAttribute` | Yes | No | Selects a converter for a type/member. |

Type-level attributes:

| Attribute | Reflection | Source generated | Notes |
| --- | --- | --- | --- |
| `TomlConstructorAttribute` / `JsonConstructorAttribute` | Yes | No | Selects which constructor to use. |
| `TomlPolymorphicAttribute` / `JsonPolymorphicAttribute` | Yes | No | Enables discriminator-based polymorphism. |
| `TomlDerivedTypeAttribute` / `JsonDerivedTypeAttribute` | Yes | No | Registers derived types and discriminators. |

Source generation support is intentionally strict: generated metadata only models the attributes it understands at compile time.
For additional customization in source-generated scenarios, prefer:

- `TomlSourceGenerationOptionsAttribute.Converters` (compile-time registration)
- `TomlSerializerOptions.Converters` (runtime registration)

### Precedence rules

When both TOML and JSON attributes are present, TOML-specific attributes take precedence.

### Examples

Property naming:

```csharp
using System.Text.Json.Serialization;

public sealed class Person
{
    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = "";
}
```

## Converters

Customize mapping by implementing `TomlConverter<T>` (or a `TomlConverterFactory`) and registering it via:

- `TomlSerializerOptions.Converters`
- `TomlConverterAttribute` (reflection)

Converters receive a `TomlReader` / `TomlWriter` for efficient reads/writes.

```csharp
using System;
using Tomlyn.Serialization;

public sealed class UpperCaseStringConverter : TomlConverter<string>
{
    public override string? Read(TomlReader reader)
        => reader.GetString().ToUpperInvariant();

    public override void Write(TomlWriter writer, string value)
        => writer.WriteStringValue(value.ToLowerInvariant());
}
```

Register:

```csharp
using Tomlyn;
using Tomlyn.Serialization;

var options = new TomlSerializerOptions
{
    Converters = new TomlConverter[] { new UpperCaseStringConverter() }
};
```

## Extension data

Use extension data to capture unmapped keys during deserialization:

- `TomlExtensionDataAttribute` / `JsonExtensionDataAttribute`

Extension data is typically a dictionary-like member with string keys.

## Polymorphism

Tomlyn supports discriminator-based polymorphism via `TomlSerializerOptions.PolymorphismOptions` and/or polymorphism attributes.

If you need polymorphism and also need NativeAOT/trimming support, prefer source generation and keep reflection disabled only when your metadata fully models the required polymorphic graph.

## Error handling

Parsing and mapping errors throw `TomlException`.

Key properties:

- `SourceName`, `Line`, `Column`, `Offset`
- `Span` (`TomlSourceSpan?`)
- `Diagnostics` (`DiagnosticsBag`)

Use `TryDeserialize(...)` if you prefer a non-throwing API surface.
