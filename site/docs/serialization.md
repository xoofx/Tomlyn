---
title: Serialization
---

Tomlyn provides a [`System.Text.Json`](xref:System.Text.Json)-style API for mapping TOML to .NET objects and back.

- Entry point: [`TomlSerializer`](xref:Tomlyn.TomlSerializer)
- Options: [`TomlSerializerOptions`](xref:Tomlyn.TomlSerializerOptions)
- Metadata: [`TomlTypeInfo<T>`](xref:Tomlyn.TomlTypeInfo`1) and [`TomlSerializerContext`](xref:Tomlyn.Serialization.TomlSerializerContext) (source-generated)

## Basic usage

```csharp
using Tomlyn;

public sealed record Person(string Name, int Age);

var toml = TomlSerializer.Serialize(new Person("Ada", 37));
var person = TomlSerializer.Deserialize<Person>(toml)!;
```

[`TomlSerializer`](xref:Tomlyn.TomlSerializer) provides overloads for `string`, `Stream`, and `TextReader`/`TextWriter`:

```csharp
// From a file stream (UTF-8)
using var stream = File.OpenRead("config.toml");
var config = TomlSerializer.Deserialize<MyConfig>(stream);

// To a TextWriter
using var writer = new StreamWriter("output.toml");
TomlSerializer.Serialize(writer, config);
```

## Reflection vs source-generated metadata

Tomlyn can resolve object-mapping metadata in two ways:

1. **Source-generated metadata** via [`TomlSerializerContext`](xref:Tomlyn.Serialization.TomlSerializerContext) and [`TomlTypeInfo<T>`](xref:Tomlyn.TomlTypeInfo`1) - recommended for NativeAOT, trimming, and hot paths.
2. **Reflection fallback** - enabled by default; convenient for development.

Reflection fallback can be disabled globally before first serializer use:

```csharp
AppContext.SetSwitch("Tomlyn.TomlSerializer.IsReflectionEnabledByDefault", false);
```

> [!TIP]
> For NativeAOT or trimmed apps, disable reflection and use source-generated contexts exclusively.
> This ensures no trimmer warnings and optimal startup performance.

When reflection is disabled, POCO/object mapping requires [`TomlTypeInfo<T>`](xref:Tomlyn.TomlTypeInfo`1) (typically from a [`TomlSerializerContext`](xref:Tomlyn.Serialization.TomlSerializerContext)).
Built-in scalar converters and untyped containers ([`TomlTable`](xref:Tomlyn.Model.TomlTable), [`TomlArray`](xref:Tomlyn.Model.TomlArray)) remain supported without reflection.

See [Source generation and NativeAOT](source-generation.md) for full details.

## Using a source-generated context

```csharp
using Tomlyn;
using Tomlyn.Serialization;

[TomlSerializable(typeof(MyConfig))]
internal partial class MyTomlContext : TomlSerializerContext { }

var context = MyTomlContext.Default;
var toml = TomlSerializer.Serialize(config, context.MyConfig);
var roundTrip = TomlSerializer.Deserialize(toml, context.MyConfig);
```

For APIs that take a `Type`, use overloads accepting a context:

```csharp
var toml = TomlSerializer.Serialize(value, typeof(MyConfig), context);
var roundTrip = TomlSerializer.Deserialize(toml, typeof(MyConfig), context);
```

## Options

[`TomlSerializerOptions`](xref:Tomlyn.TomlSerializerOptions) is an immutable `sealed record` - create it once and reuse it.

> [!IMPORTANT]
> [`TomlSerializerOptions`](xref:Tomlyn.TomlSerializerOptions) caches internal metadata on first use. Always store and reuse options instances
> rather than creating new ones per call - this avoids repeated reflection/compilation overhead.

```csharp
using System.Text.Json;
using Tomlyn;

var options = new TomlSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    PreferredObjectCreationHandling = JsonObjectCreationHandling.Replace,
    WriteIndented = true,
    IndentSize = 4,
    MaxDepth = 64,
    DefaultIgnoreCondition = TomlIgnoreCondition.WhenWritingNull,
};

var toml = TomlSerializer.Serialize(config, options);
```

### Option reference

| Option | Type | Default | Description |
| --- | --- | --- | --- |
| [`PropertyNamingPolicy`](xref:Tomlyn.TomlSerializerOptions.PropertyNamingPolicy) | [`JsonNamingPolicy?`](xref:System.Text.Json.JsonNamingPolicy) | `null` | Naming policy for CLR member names (e.g. `CamelCase`, `SnakeCaseLower`). |
| [`DictionaryKeyPolicy`](xref:Tomlyn.TomlSerializerOptions.DictionaryKeyPolicy) | [`JsonNamingPolicy?`](xref:System.Text.Json.JsonNamingPolicy) | `null` | Naming policy for dictionary keys during serialization. |
| [`PreferredObjectCreationHandling`](xref:Tomlyn.TomlSerializerOptions.PreferredObjectCreationHandling) | [`JsonObjectCreationHandling`](xref:System.Text.Json.Serialization.JsonObjectCreationHandling) | `Replace` | Controls whether nested object and collection members are replaced or populated during deserialization. |
| [`PropertyNameCaseInsensitive`](xref:Tomlyn.TomlSerializerOptions.PropertyNameCaseInsensitive) | `bool` | `false` | Case-insensitive property matching when reading. |
| [`MaxDepth`](xref:Tomlyn.TomlSerializerOptions.MaxDepth) | `int` | `0` (`64` effective) | Maximum nesting depth when reading or writing TOML containers. |
| [`DefaultIgnoreCondition`](xref:Tomlyn.TomlSerializerOptions.DefaultIgnoreCondition) | [`TomlIgnoreCondition`](xref:Tomlyn.TomlIgnoreCondition) | `WhenWritingNull` | Skips null/default values when writing. |
| [`WriteIndented`](xref:Tomlyn.TomlSerializerOptions.WriteIndented) | `bool` | `true` | Enables indentation for nested tables. |
| [`IndentSize`](xref:Tomlyn.TomlSerializerOptions.IndentSize) | `int` | `2` | Spaces per indent level. |
| [`NewLine`](xref:Tomlyn.TomlSerializerOptions.NewLine) | [`TomlNewLineKind`](xref:Tomlyn.TomlNewLineKind) | `Lf` | Line ending style (`Lf` or `CrLf`). |
| [`MappingOrder`](xref:Tomlyn.TomlSerializerOptions.MappingOrder) | [`TomlMappingOrderPolicy`](xref:Tomlyn.TomlMappingOrderPolicy) | `Declaration` | Property ordering within tables. |
| [`DuplicateKeyHandling`](xref:Tomlyn.TomlSerializerOptions.DuplicateKeyHandling) | [`TomlDuplicateKeyHandling`](xref:Tomlyn.TomlDuplicateKeyHandling) | `Error` | Behavior when duplicate keys are encountered. |
| [`DottedKeyHandling`](xref:Tomlyn.TomlSerializerOptions.DottedKeyHandling) | [`TomlDottedKeyHandling`](xref:Tomlyn.TomlDottedKeyHandling) | `Literal` | How dotted keys are emitted (`Literal` or `Expand`). |
| [`RootValueHandling`](xref:Tomlyn.TomlSerializerOptions.RootValueHandling) | [`TomlRootValueHandling`](xref:Tomlyn.TomlRootValueHandling) | `Error` | Behavior for root-level non-table values. |
| [`RootValueKeyName`](xref:Tomlyn.TomlSerializerOptions.RootValueKeyName) | `string` | `"value"` | Key name used when `RootValueHandling` is `WrapInRootKey`. |
| [`InlineTablePolicy`](xref:Tomlyn.TomlSerializerOptions.InlineTablePolicy) | [`TomlInlineTablePolicy`](xref:Tomlyn.TomlInlineTablePolicy) | `Never` | When to emit inline tables (`Never`, `WhenSmall`, `Always`). |
| [`TableArrayStyle`](xref:Tomlyn.TomlSerializerOptions.TableArrayStyle) | [`TomlTableArrayStyle`](xref:Tomlyn.TomlTableArrayStyle) | `Headers` | Array-of-tables style (`Headers` = `[[a]]`, `InlineArrayOfTables` = `a = [{...}]`). |
| [`StringStylePreferences`](xref:Tomlyn.TomlSerializerOptions.StringStylePreferences) | [`TomlStringStylePreferences`](xref:Tomlyn.TomlStringStylePreferences) | *(see below)* | Controls string emission style (basic, literal, multiline). |
| [`PolymorphismOptions`](xref:Tomlyn.TomlSerializerOptions.PolymorphismOptions) | [`TomlPolymorphismOptions`](xref:Tomlyn.TomlPolymorphismOptions) | *(see below)* | Polymorphism discriminator settings. |
| [`MetadataStore`](xref:Tomlyn.TomlSerializerOptions.MetadataStore) | [`ITomlMetadataStore?`](xref:Tomlyn.Serialization.ITomlMetadataStore) | `null` | Captures trivia (comments, source spans) during deserialization. |
| [`SourceName`](xref:Tomlyn.TomlSerializerOptions.SourceName) | `string?` | `null` | File/path name included in [`TomlException`](xref:Tomlyn.TomlException) messages. |
| [`TypeInfoResolver`](xref:Tomlyn.TomlSerializerOptions.TypeInfoResolver) | [`ITomlTypeInfoResolver?`](xref:Tomlyn.ITomlTypeInfoResolver) | `null` | Custom metadata resolver (advanced). |
| [`Converters`](xref:Tomlyn.TomlSerializerOptions.Converters) | `IReadOnlyList<TomlConverter>` | empty | Custom converters for type mapping. |

### Naming policy defaults

By default, [`PropertyNamingPolicy`](xref:Tomlyn.TomlSerializerOptions.PropertyNamingPolicy) is `null`, so CLR member names are used as-is.
This matches [`System.Text.Json.JsonSerializer`](xref:System.Text.Json.JsonSerializer) default behavior. Common policies:

- [`JsonNamingPolicy.CamelCase`](xref:System.Text.Json.JsonNamingPolicy.CamelCase) → `myProperty`
- [`JsonNamingPolicy.SnakeCaseLower`](xref:System.Text.Json.JsonNamingPolicy.SnakeCaseLower) → `my_property`
- [`JsonNamingPolicy.KebabCaseLower`](xref:System.Text.Json.JsonNamingPolicy.KebabCaseLower) → `my-property`

### Object creation handling

Tomlyn matches [`System.Text.Json`](xref:System.Text.Json) object creation semantics:

- [`JsonObjectCreationHandling.Replace`](xref:System.Text.Json.Serialization.JsonObjectCreationHandling.Replace) is the default.
- Under `Replace`, writable members are assigned fresh values and read-only members are left untouched.
- [`JsonObjectCreationHandling.Populate`](xref:System.Text.Json.Serialization.JsonObjectCreationHandling.Populate) reuses existing mutable object and collection instances.
- Collection population appends incoming items; existing items are not cleared first.
- A member-level [`JsonObjectCreationHandlingAttribute`](xref:System.Text.Json.Serialization.JsonObjectCreationHandlingAttribute) is strict: if `Populate` is applied to an unsupported read-only member such as a struct-without-setter or an immutable type, deserialization throws.
- Type-level and global populate preferences are best-effort: members that cannot be populated are left unchanged instead of throwing.

You can opt into `Populate` globally with [`TomlSerializerOptions.PreferredObjectCreationHandling`](xref:Tomlyn.TomlSerializerOptions.PreferredObjectCreationHandling), on a type, or on an individual member:

```csharp
using System.Text.Json.Serialization;
using Tomlyn;

[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
public sealed class ReleaserConfiguration
{
    public List<string> Channels { get; } = ["stable"];
}

var options = new TomlSerializerOptions
{
    PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
};
```

Property-level [`JsonObjectCreationHandlingAttribute`](xref:System.Text.Json.Serialization.JsonObjectCreationHandlingAttribute) overrides the type-level or options default.

> [!NOTE]
> As with `System.Text.Json`, populate semantics currently do not apply to types deserialized through a parameterized constructor.

### Single value or array collections

Use [`TomlSingleOrArrayAttribute`](xref:Tomlyn.Serialization.TomlSingleOrArrayAttribute) on a collection member when the TOML input may contain either a single value or an array:

```csharp
using System.Text.Json.Serialization;
using Tomlyn.Serialization;

public sealed class PackagingConfiguration
{
    public PackagingConfiguration()
    {
        RuntimeIdentifiers = new List<string>();
    }

    [TomlSingleOrArray]
    [JsonPropertyName("rid")]
    public List<string> RuntimeIdentifiers { get; }
}
```

Then both of these inputs are accepted:

```toml
rid = "win-x64"
```

```toml
rid = ["win-x64", "linux-x64"]
```

Behavior matches the declared collection semantics:

- A single TOML value is treated as a collection containing exactly one element.
- Mutable read-only list/set-style members append into the existing collection instance.
- Writable members still use replace semantics unless object creation handling says to populate.
- Without [`TomlSingleOrArrayAttribute`](xref:Tomlyn.Serialization.TomlSingleOrArrayAttribute), collection members require a TOML array.

### Mapping order

[`MappingOrder`](xref:Tomlyn.TomlSerializerOptions.MappingOrder) controls the order properties appear in the serialized output:

| Value | Behavior |
| --- | --- |
| [`Declaration`](xref:Tomlyn.TomlMappingOrderPolicy.Declaration) | Properties appear in CLR declaration order (default, diff-friendly). |
| [`Alphabetical`](xref:Tomlyn.TomlMappingOrderPolicy.Alphabetical) | Properties are sorted alphabetically. |
| [`OrderThenDeclaration`](xref:Tomlyn.TomlMappingOrderPolicy.OrderThenDeclaration) | Properties with [`[TomlPropertyOrder]`](xref:Tomlyn.Serialization.TomlPropertyOrderAttribute) first, then declaration order. |
| [`OrderThenAlphabetical`](xref:Tomlyn.TomlMappingOrderPolicy.OrderThenAlphabetical) | Properties with [`[TomlPropertyOrder]`](xref:Tomlyn.Serialization.TomlPropertyOrderAttribute) first, then alphabetical. |

### String style preferences

[`TomlStringStylePreferences`](xref:Tomlyn.TomlStringStylePreferences) controls how strings are emitted:

```csharp
var options = new TomlSerializerOptions
{
    StringStylePreferences = new TomlStringStylePreferences
    {
        DefaultStyle = TomlStringStyle.Basic,           // default
        PreferLiteralWhenNoEscapes = true,              // default
        AllowHexEscapes = true,                         // default
    }
};
```

The four string styles are: [`Basic`](xref:Tomlyn.TomlStringStyle.Basic) (`"..."`), [`Literal`](xref:Tomlyn.TomlStringStyle.Literal) (`'...'`), [`MultilineBasic`](xref:Tomlyn.TomlStringStyle.MultilineBasic) (`"""..."""`), and [`MultilineLiteral`](xref:Tomlyn.TomlStringStyle.MultilineLiteral) (`'''...'''`).

## Supported types

### Scalars (built-in converters)

| Category | Types |
| --- | --- |
| Boolean | `bool` |
| Numeric | `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `nint`, `nuint`, `float`, `double`, `decimal`, `Half`, `Int128`, `UInt128` |
| Text | `char`, `string` |
| Date/time | `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, [`TomlDateTime`](xref:Tomlyn.TomlDateTime) |
| Other | `Guid`, `TimeSpan`, `Uri`, `Version` |

### Collections

Tomlyn supports common .NET collection shapes:

- **Arrays**: `T[]`, `IList<T>`, `List<T>`, `IReadOnlyList<T>`
- **Sets**: `ISet<T>`, `HashSet<T>`, `SortedSet<T>`, `IReadOnlySet<T>`
- **Dictionaries**: `IDictionary<string, T>`, `Dictionary<string, T>`, `IReadOnlyDictionary<string, T>`, `SortedDictionary<string, T>`
- **Immutable collections**: `ImmutableArray<T>`, `ImmutableList<T>`, `ImmutableDictionary<string, T>`, etc.

### DOM types

- [`TomlTable`](xref:Tomlyn.Model.TomlTable) - untyped table (key/value mapping)
- [`TomlArray`](xref:Tomlyn.Model.TomlArray) - untyped array
- [`TomlTableArray`](xref:Tomlyn.Model.TomlTableArray) - array of tables
- `object` - untyped TOML values

See [DOM model](dom.md) for details.

### POCOs and records

Classes, records, and structs with public properties are mapped automatically.
Constructor-based deserialization is supported (matched by parameter name to TOML key).

## Attributes

Tomlyn supports TOML-specific attributes **and** a subset of [`System.Text.Json.Serialization`](xref:System.Text.Json.Serialization) attributes,
so you can reuse models across JSON and TOML. When both are present, the TOML-specific attribute takes precedence.

### Member-level attributes

| Attribute | JSON equivalent | Description |
| --- | --- | --- |
| [`TomlPropertyNameAttribute`](xref:Tomlyn.Serialization.TomlPropertyNameAttribute) | [`JsonPropertyNameAttribute`](xref:System.Text.Json.Serialization.JsonPropertyNameAttribute) | Overrides the serialized key name. |
| [`TomlIgnoreAttribute`](xref:Tomlyn.Serialization.TomlIgnoreAttribute) | [`JsonIgnoreAttribute`](xref:System.Text.Json.Serialization.JsonIgnoreAttribute) | Ignores the member. Supports conditions (`Never`, `WhenWritingNull`, `WhenWritingDefault`). |
| [`TomlIncludeAttribute`](xref:Tomlyn.Serialization.TomlIncludeAttribute) | [`JsonIncludeAttribute`](xref:System.Text.Json.Serialization.JsonIncludeAttribute) | Includes non-public members. |
| [`TomlPropertyOrderAttribute`](xref:Tomlyn.Serialization.TomlPropertyOrderAttribute) | [`JsonPropertyOrderAttribute`](xref:System.Text.Json.Serialization.JsonPropertyOrderAttribute) | Controls ordering within tables. |
| [`TomlRequiredAttribute`](xref:Tomlyn.Serialization.TomlRequiredAttribute) | [`JsonRequiredAttribute`](xref:System.Text.Json.Serialization.JsonRequiredAttribute) | Member must be present in the TOML input; missing values throw [`TomlException`](xref:Tomlyn.TomlException). |
|  | [`JsonObjectCreationHandlingAttribute`](xref:System.Text.Json.Serialization.JsonObjectCreationHandlingAttribute) | Overrides replace/populate behavior for a specific member during deserialization. |
| [`TomlSingleOrArrayAttribute`](xref:Tomlyn.Serialization.TomlSingleOrArrayAttribute) |  | Allows a collection member to accept either a single TOML value or a TOML array during deserialization. |
| [`TomlExtensionDataAttribute`](xref:Tomlyn.Serialization.TomlExtensionDataAttribute) | [`JsonExtensionDataAttribute`](xref:System.Text.Json.Serialization.JsonExtensionDataAttribute) | Captures unmapped keys into a dictionary. |
| [`TomlConverterAttribute`](xref:Tomlyn.Serialization.TomlConverterAttribute) | [`JsonConverterAttribute`](xref:System.Text.Json.Serialization.JsonConverterAttribute) | Selects a custom converter for a type or member (reflection only). |

### Type-level attributes

| Attribute | JSON equivalent | Description |
| --- | --- | --- |
| [`TomlConstructorAttribute`](xref:Tomlyn.Serialization.TomlConstructorAttribute) | [`JsonConstructorAttribute`](xref:System.Text.Json.Serialization.JsonConstructorAttribute) | Selects which constructor to use for deserialization. |
|  | [`JsonObjectCreationHandlingAttribute`](xref:System.Text.Json.Serialization.JsonObjectCreationHandlingAttribute) | Sets the default replace/populate behavior inherited by members of the type. |
| [`TomlPolymorphicAttribute`](xref:Tomlyn.Serialization.TomlPolymorphicAttribute) | [`JsonPolymorphicAttribute`](xref:System.Text.Json.Serialization.JsonPolymorphicAttribute) | Enables discriminator-based polymorphism on a base type. |
| [`TomlDerivedTypeAttribute`](xref:Tomlyn.Serialization.TomlDerivedTypeAttribute) | [`JsonDerivedTypeAttribute`](xref:System.Text.Json.Serialization.JsonDerivedTypeAttribute) | Registers a derived type and its discriminator value. |

### Example: property naming

```csharp
using System.Text.Json.Serialization;

public sealed class Person
{
    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = "";

    [JsonPropertyName("last_name")]
    public string LastName { get; set; } = "";

    [JsonIgnore]
    public string FullName => $"{FirstName} {LastName}";
}
```

### Example: required properties

```csharp
using Tomlyn.Serialization;

public sealed class DatabaseConfig
{
    [TomlRequired]
    public string Host { get; set; } = "";

    [TomlRequired]
    public int Port { get; set; }

    public string? Username { get; set; }
}
```

If `host` or `port` is missing from the TOML input, `TomlException` is thrown.
Source-generated contexts also honor the C# `required` keyword and `init` accessors during deserialization.

### Example: constructor-based deserialization

```csharp
using System.Text.Json.Serialization;

public sealed class Endpoint
{
    [JsonConstructor]
    public Endpoint(string host, int port) { Host = host; Port = port; }

    public string Host { get; }
    public int Port { get; }
}
```

## Converters

Customize type mapping by implementing [`TomlConverter<T>`](xref:Tomlyn.Serialization.TomlConverter`1) (or [`TomlConverterFactory`](xref:Tomlyn.Serialization.TomlConverterFactory) for open generic types).

### Implementing a converter

```csharp
using Tomlyn.Serialization;

public sealed class UpperCaseStringConverter : TomlConverter<string>
{
    public override string? Read(TomlReader reader)
        => reader.GetString().ToUpperInvariant();

    public override void Write(TomlWriter writer, string value)
        => writer.WriteStringValue(value.ToLowerInvariant());
}
```

### Registering converters

**Via options** (runtime):

```csharp
var options = new TomlSerializerOptions
{
    Converters = [new UpperCaseStringConverter()]
};
```

**Via attribute** (reflection only):

```csharp
public sealed class Config
{
    [TomlConverter(typeof(UpperCaseStringConverter))]
    public string Name { get; set; } = "";
}
```

**Via source generation** (compile-time):

```csharp
[TomlSourceGenerationOptions(Converters = [typeof(UpperCaseStringConverter)])]
[TomlSerializable(typeof(Config))]
internal partial class MyTomlContext : TomlSerializerContext { }
```

### TomlReader and TomlWriter

Custom converters interact with [`TomlReader`](xref:Tomlyn.Serialization.TomlReader) and [`TomlWriter`](xref:Tomlyn.Serialization.TomlWriter) which provide efficient token-level access:

**[`TomlReader`](xref:Tomlyn.Serialization.TomlReader)** methods:
- `Read()` - advance to the next token
- `GetString()`, `GetInt64()`, `GetDouble()`, `GetDecimal()`, `GetBoolean()`, `GetTomlDateTime()` - read scalar values
- `GetRawText()` - get the original TOML text
- `PropertyNameEquals(string)` - check current property name (ordinal, case-sensitive)
- `Skip()` - skip the current value (including nested containers)

**[`TomlWriter`](xref:Tomlyn.Serialization.TomlWriter)** methods:
- `WritePropertyName(string)` - write a key
- `WriteStringValue(string)`, `WriteIntegerValue(long)`, `WriteFloatValue(double)`, `WriteBooleanValue(bool)`, `WriteDateTimeValue(TomlDateTime)` - write scalar values
- `WriteStartTable()` / `WriteEndTable()` - write a table
- `WriteStartInlineTable()` / `WriteEndInlineTable()` - write an inline table
- `WriteStartArray()` / `WriteEndArray()` - write an array
- `WriteStartTableArray()` / `WriteEndTableArray()` - write an array of tables

### Converter factories

Use [`TomlConverterFactory`](xref:Tomlyn.Serialization.TomlConverterFactory) for open generic types or types that need runtime inspection:

```csharp
using Tomlyn.Serialization;

public sealed class MyConverterFactory : TomlConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsEnum;

    public override TomlConverter CreateConverter(Type typeToConvert, TomlSerializerOptions options)
        => /* return a concrete TomlConverter<T> for the enum type */;
}
```

## Extension data

Capture unmapped TOML keys during deserialization using [`TomlExtensionDataAttribute`](xref:Tomlyn.Serialization.TomlExtensionDataAttribute) (or [`JsonExtensionDataAttribute`](xref:System.Text.Json.Serialization.JsonExtensionDataAttribute)):

```csharp
using Tomlyn.Serialization;

public sealed class Config
{
    public string Name { get; set; } = "";

    [TomlExtensionData]
    public IDictionary<string, object?>? Extra { get; set; }
}
```

Any key in the TOML input that doesn't match a mapped property is placed in `Extra`.
The extension data member should be a dictionary type with string keys (e.g. `IDictionary<string, object?>`, [`TomlTable`](xref:Tomlyn.Model.TomlTable)).

## Polymorphism

Tomlyn supports discriminator-based polymorphism via base-type attributes, reflection-time runtime mappings, and source-generated context mappings.

### Attribute-based

```csharp
using Tomlyn.Serialization;

[TomlPolymorphic]
[TomlDerivedType(typeof(Cat), "cat")]
[TomlDerivedType(typeof(Dog), "dog")]
public abstract class Animal
{
    public string Name { get; set; } = "";
}

public sealed class Cat : Animal { public bool Indoor { get; set; } }
public sealed class Dog : Animal { public string Breed { get; set; } = ""; }
```

The discriminator key defaults to `$type` and can be customized via [`TomlPolymorphicAttribute.TypeDiscriminatorPropertyName`](xref:Tomlyn.Serialization.TomlPolymorphicAttribute.TypeDiscriminatorPropertyName)
or [`TomlPolymorphismOptions.TypeDiscriminatorPropertyName`](xref:Tomlyn.TomlPolymorphismOptions.TypeDiscriminatorPropertyName) on options.

TOML input:

```toml
[[ animals ]]
"$type" = "cat"
Name = "Whiskers"
Indoor = true

[[ animals ]]
"$type" = "dog"
Name = "Rex"
Breed = "Labrador"
```

### JSON attribute equivalents

`JsonPolymorphicAttribute` and `JsonDerivedTypeAttribute` also work:

```csharp
using System.Text.Json.Serialization;

[JsonPolymorphic]
[JsonDerivedType(typeof(Cat), "cat")]
[JsonDerivedType(typeof(Dog), "dog")]
public abstract class Animal { /* ... */ }
```

> [!NOTE]
> When both Toml and Json polymorphic attributes are present on the same type, the Toml-specific attributes take precedence.

### Cross-project runtime mappings

Use [`TomlPolymorphismOptions.DerivedTypeMappings`](xref:Tomlyn.TomlPolymorphismOptions.DerivedTypeMappings) when the base type and derived types live in different projects and you're using the reflection resolver:

```csharp
using Tomlyn;

var options = new TomlSerializerOptions
{
    PolymorphismOptions = new TomlPolymorphismOptions
    {
        TypeDiscriminatorPropertyName = "kind",
        DerivedTypeMappings = new Dictionary<Type, IReadOnlyList<TomlDerivedType>>
        {
            [typeof(Animal)] =
            [
                new(typeof(Cat), "cat"),
                new(typeof(Dog), "dog"),
            ],
        },
    },
};
```

This is especially useful for clean architecture and plugin-style applications where the base type cannot reference every concrete implementation.

### Cross-project source generation mappings

Use [`TomlDerivedTypeMappingAttribute`](xref:Tomlyn.Serialization.TomlDerivedTypeMappingAttribute) on a [`TomlSerializerContext`](xref:Tomlyn.Serialization.TomlSerializerContext) when you want the same pattern in NativeAOT / trimming-safe source-generated code:

```csharp
using Tomlyn.Serialization;

[TomlSerializable(typeof(Animal))]
[TomlDerivedTypeMapping(typeof(Animal), typeof(Cat), "cat")]
[TomlDerivedTypeMapping(typeof(Animal), typeof(Dog), "dog")]
internal partial class AnimalContext : TomlSerializerContext
{
}
```

Mapped derived types are discovered automatically by the generator, so they do not also need their own `[TomlSerializable]` roots.

If the base type doesn't declare [`TomlPolymorphicAttribute`](xref:Tomlyn.Serialization.TomlPolymorphicAttribute) or a JSON equivalent, Tomlyn still supports the mapping and falls back to [`TomlPolymorphismOptions`](xref:Tomlyn.TomlPolymorphismOptions) for the discriminator property name and unknown-derived-type handling.

### Default derived type

Register one derived type **without a discriminator** to act as the default. When a TOML table has no discriminator key (or an unknown discriminator), it deserializes as the default type. When serializing the default type, no discriminator is emitted.

```csharp
[TomlPolymorphic(TypeDiscriminatorPropertyName = "type")]
[TomlDerivedType(typeof(Circle))]         // default - no discriminator
[TomlDerivedType(typeof(Square), "square")]
public abstract class Shape
{
    public string Color { get; set; } = "";
}

public sealed class Circle : Shape { public double Radius { get; set; } }
public sealed class Square : Shape { public double Side { get; set; } }
```

```toml
# Serialized Circle (default) - no "type" key
Color = "red"
Radius = 5.0

# Serialized Square - "type" key is present
type = "square"
Color = "blue"
Side = 3.0
```

The same works with `JsonDerivedTypeAttribute` using the single-argument constructor `[JsonDerivedType(typeof(Circle))]`.

### Integer discriminators

In addition to strings, [`TomlDerivedTypeAttribute`](xref:Tomlyn.Serialization.TomlDerivedTypeAttribute) accepts an `int` discriminator which is stored as a string in TOML:

```csharp
[TomlPolymorphic(TypeDiscriminatorPropertyName = "type")]
[TomlDerivedType(typeof(Circle), 1)]
[TomlDerivedType(typeof(Square), 2)]
public abstract class Shape { /* ... */ }
```

```toml
type = "1"
Color = "red"
Radius = 5.0
```

`JsonDerivedTypeAttribute` integer discriminators (e.g. `[JsonDerivedType(typeof(Circle), 1)]`) are also supported.

### Unknown discriminator handling

By default, encountering an unrecognized discriminator throws a [`TomlException`](xref:Tomlyn.TomlException). You can change this behavior at the **attribute level** or **globally**.

**Attribute-level** (takes priority):

```csharp
[TomlPolymorphic(
    TypeDiscriminatorPropertyName = "kind",
    UnknownDerivedTypeHandling = TomlUnknownDerivedTypeHandling.FallBackToBaseType)]
[TomlDerivedType(typeof(Cat), "cat")]
public class Animal
{
    public string Name { get; set; } = "";
}
```

**Global options:**

```csharp
var options = new TomlSerializerOptions
{
    PolymorphismOptions = new TomlPolymorphismOptions
    {
        UnknownDerivedTypeHandling = TomlUnknownDerivedTypeHandling.FallBackToBaseType,
    },
};
```

The priority chain is:

1. [`TomlPolymorphicAttribute.UnknownDerivedTypeHandling`](xref:Tomlyn.Serialization.TomlPolymorphicAttribute.UnknownDerivedTypeHandling) (if not [`Unspecified`](xref:Tomlyn.TomlUnknownDerivedTypeHandling.Unspecified))
2. `JsonPolymorphicAttribute.UnknownDerivedTypeHandling` (mapped from `JsonUnknownDerivedTypeHandling`)
3. [`TomlPolymorphismOptions.UnknownDerivedTypeHandling`](xref:Tomlyn.TomlPolymorphismOptions.UnknownDerivedTypeHandling) (global default)

Derived type registrations are merged additively with this precedence:

1. [`TomlDerivedTypeAttribute`](xref:Tomlyn.Serialization.TomlDerivedTypeAttribute) on the base type
2. [`JsonDerivedTypeAttribute`](xref:System.Text.Json.Serialization.JsonDerivedTypeAttribute) on the base type
3. [`TomlDerivedTypeMappingAttribute`](xref:Tomlyn.Serialization.TomlDerivedTypeMappingAttribute) on a source-generated context
4. [`TomlPolymorphismOptions.DerivedTypeMappings`](xref:Tomlyn.TomlPolymorphismOptions.DerivedTypeMappings) at runtime

> [!NOTE]
> [`TomlUnknownDerivedTypeHandling.Unspecified`](xref:Tomlyn.TomlUnknownDerivedTypeHandling.Unspecified) is a sentinel value for attribute properties and **cannot** be used on [`TomlPolymorphismOptions`](xref:Tomlyn.TomlPolymorphismOptions) - doing so throws `ArgumentOutOfRangeException`.

## Serialization callbacks

Implement callback interfaces on your types to run logic before/after serialization or deserialization:

| Interface | Method | When called |
| --- | --- | --- |
| [`ITomlOnSerializing`](xref:Tomlyn.Serialization.ITomlOnSerializing) | `OnTomlSerializing()` | Before the object is serialized. |
| [`ITomlOnSerialized`](xref:Tomlyn.Serialization.ITomlOnSerialized) | `OnTomlSerialized()` | After the object is serialized. |
| [`ITomlOnDeserializing`](xref:Tomlyn.Serialization.ITomlOnDeserializing) | `OnTomlDeserializing()` | Before the object's properties are populated. |
| [`ITomlOnDeserialized`](xref:Tomlyn.Serialization.ITomlOnDeserialized) | `OnTomlDeserialized()` | After the object's properties are populated. |

```csharp
using Tomlyn.Serialization;

public sealed class Config : ITomlOnDeserialized
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 8080;

    public void OnTomlDeserialized()
    {
        if (Port is < 1 or > 65535)
            throw new InvalidOperationException($"Invalid port: {Port}");
    }
}
```

## Error handling

Parsing and mapping errors throw [`TomlException`](xref:Tomlyn.TomlException) with source location details.

Key properties on [`TomlException`](xref:Tomlyn.TomlException):

| Property | Type | Description |
| --- | --- | --- |
| [`SourceName`](xref:Tomlyn.TomlException.SourceName) | `string?` | File/path name (set via [`TomlSerializerOptions.SourceName`](xref:Tomlyn.TomlSerializerOptions.SourceName)). |
| [`Line`](xref:Tomlyn.TomlException.Line) | `int?` | 1-based line number. |
| [`Column`](xref:Tomlyn.TomlException.Column) | `int?` | 1-based column number. |
| [`Offset`](xref:Tomlyn.TomlException.Offset) | `int?` | 0-based character offset. |
| [`Span`](xref:Tomlyn.TomlException.Span) | [`TomlSourceSpan?`](xref:Tomlyn.Text.TomlSourceSpan) | Full start/end span. |
| [`Diagnostics`](xref:Tomlyn.TomlException.Diagnostics) | [`DiagnosticsBag`](xref:Tomlyn.Syntax.DiagnosticsBag) | All collected diagnostics. |

For a non-throwing API, use `TryDeserialize`:

> [!TIP]
> Prefer `TryDeserialize` in scenarios where invalid TOML input is expected (e.g. user-provided configuration).
> It avoids the overhead of exception throwing for expected failures.

```csharp
if (!TomlSerializer.TryDeserialize<MyConfig>(toml, out var config))
{
    // config is null/default - parsing or mapping failed
}
```

`TryDeserialize` is available for all input types (`string`, `Stream`, `TextReader`)
and with all metadata styles (options, context, type info).
