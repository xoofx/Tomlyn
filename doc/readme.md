# Tomlyn Documentation (v1)

Tomlyn is a high-performance TOML 1.1 library for .NET, shaped like `System.Text.Json`.
It provides two main layers:

- **Object serialization** via `TomlSerializer` - maps TOML documents to .NET objects and back.
  Supports reflection and source generation. Does not preserve comments or formatting.
- **Low-level parsing** via `TomlLexer`, `TomlParser`, and `SyntaxParser` - token streams,
  incremental events, and a full-fidelity syntax tree for tooling and round-tripping.

Tomlyn v1 targets **TOML 1.1.0 only**.

## 1. Basic serialization

```csharp
using Tomlyn;

public sealed class ServerConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 8080;
}

var config = new ServerConfig { Host = "example.com", Port = 443 };
var toml = TomlSerializer.Serialize(config);
var roundTrip = TomlSerializer.Deserialize<ServerConfig>(toml)!;
```

`TomlSerializer` provides overloads for `string`, `byte[]` (UTF-8), `Stream`, and `TextReader`/`TextWriter`.

## 2. Untyped model (`TomlTable`, `TomlArray`)

Use this when you want a dynamic representation without defining POCOs.

```csharp
using Tomlyn;
using Tomlyn.Model;

var toml = """
    title = "TOML Example"

    [owner]
    name = "Ada"
    """;

var table = TomlSerializer.Deserialize<TomlTable>(toml)!;
var title = (string)table["title"]!;
var owner = (TomlTable)table["owner"]!;
var name = (string)owner["name"]!;

table["title"] = "Updated";
var output = TomlSerializer.Serialize(table);
```

## 3. Options

`TomlSerializerOptions` is an immutable `sealed record` - create it once and reuse it.

```csharp
using System.Text.Json;
using Tomlyn;

var options = new TomlSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    WriteIndented = true,
    IndentSize = 2,
    DefaultIgnoreCondition = TomlIgnoreCondition.WhenWritingNull,
};

var toml = TomlSerializer.Serialize(config, options);
```

Key options: `PropertyNamingPolicy`, `DictionaryKeyPolicy`, `WriteIndented`, `IndentSize`, `NewLine`,
`DefaultIgnoreCondition`, `DuplicateKeyHandling`, `DottedKeyHandling`, `MappingOrder`,
`RootValueHandling`, `InlineTablePolicy`, `TableArrayStyle`, `StringStylePreferences`,
`PolymorphismOptions`, `MetadataStore`, `SourceName`, `Converters`, `TypeInfoResolver`.

## 4. POCO mapping (source generation / NativeAOT)

Source generation follows `System.Text.Json` patterns: declare a context derived from `TomlSerializerContext`,
and add `JsonSerializable` roots.

```csharp
using System.Text.Json.Serialization;
using Tomlyn;
using Tomlyn.Serialization;

public sealed class Config
{
    public string? Title { get; set; }
}

[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(Config))]
internal partial class MyTomlContext : TomlSerializerContext { }

var config = TomlSerializer.Deserialize("title = \"x\"", MyTomlContext.Default.Config);
var toml = TomlSerializer.Serialize(config, MyTomlContext.Default.Config);
```

## 5. Attribute support (STJ parity)

Tomlyn supports TOML-specific attributes and a subset of `System.Text.Json.Serialization` attributes:

- **Member-level**: `TomlPropertyName`/`JsonPropertyName`, `TomlIgnore`/`JsonIgnore`,
  `TomlInclude`/`JsonInclude`, `TomlPropertyOrder`/`JsonPropertyOrder`,
  `TomlRequired`/`JsonRequired`, `TomlExtensionData`/`JsonExtensionData`,
  `TomlConverter`/`JsonConverter`.
- **Type-level**: `TomlConstructor`/`JsonConstructor`, `TomlPolymorphic`/`JsonPolymorphic`,
  `TomlDerivedType`/`JsonDerivedType`.

TOML-specific attributes take precedence when both are present.

## 6. Converters

Implement `TomlConverter<T>` (or `TomlConverterFactory`) for custom type mapping:

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

Register via `TomlSerializerOptions.Converters`, `[TomlConverter]` attribute (reflection),
or `TomlSourceGenerationOptionsAttribute.Converters` (source generation).

## 7. Preserving comments/trivia

Tomlyn can capture TOML trivia (comments, formatting) using a metadata store:

```csharp
using Tomlyn;
using Tomlyn.Serialization;

var store = new TomlMetadataStore();
var options = new TomlSerializerOptions { MetadataStore = store };

var config = TomlSerializer.Deserialize<TomlTable>(toml, options)!;

// Access per-property metadata: LeadingTrivia, TrailingTrivia, Span, DisplayKind
if (store.TryGetProperties(config, out var metadata) && metadata is not null)
{
    metadata.TryGetProperty("title", out var prop);
}

var tomlOut = TomlSerializer.Serialize(config, options);
```

## 8. Syntax tree (roundtrip / tooling)

For full-fidelity parsing (including trivia), use `SyntaxParser`:

```csharp
using Tomlyn.Parsing;

var doc = SyntaxParser.Parse(toml);
if (doc.HasErrors)
{
    foreach (var diag in doc.Diagnostics)
        Console.WriteLine(diag);
}

Console.WriteLine(doc.ToString()); // roundtrippable - reproduces exact original text
```

## 9. Error handling

All parsing errors throw `TomlException` with precise source locations:

```csharp
try
{
    var config = TomlSerializer.Deserialize<Config>("bad = [",
        new TomlSerializerOptions { SourceName = "config.toml" });
}
catch (TomlException ex)
{
    // ex.SourceName, ex.Line, ex.Column, ex.Offset, ex.Span
    Console.WriteLine(ex.Message);
}
```

Use `TryDeserialize(...)` for a non-throwing API.

