# Tomlyn Documentation (v1)

Tomlyn provides two main layers:

- **Low-level parsing** for tools and high-performance readers:
  - `TomlLexer` → tokens (+ trivia) with precise `TomlSourceSpan`
  - `TomlParser` → pull-based semantic events (`MoveNext()`)
  - `TomlReader` → serializer-oriented reader built on `TomlParser`
- **High-level serialization** shaped like `System.Text.Json`:
  - `TomlSerializer`, `TomlSerializerOptions`
  - `TomlSerializerContext` source generation (`TomlTypeInfo<T>` metadata)

Tomlyn 1.0 targets **TOML 1.1.0** only.

## 1. Untyped model (`TomlTable`, `TomlArray`)

Use this when you want a convenient runtime representation without defining POCOs.

```csharp
using Tomlyn;
using Tomlyn.Model;

var toml = """
    title = "TOML Example"

    [owner]
    name = "Ada"
    """;

var table = TomlSerializer.Deserialize<TomlTable>(toml)!;
Console.WriteLine((string)table["title"]!);

table["title"] = "Updated";
var roundtrip = TomlSerializer.Serialize(table);
```

## 2. POCO mapping (reflection)

Reflection-based mapping is available via `TomlSerializer.Serialize<T>(...)` / `Deserialize<T>(...)`.

```csharp
using Tomlyn;

public sealed class Config
{
    public string? Title { get; set; }
}

var config = TomlSerializer.Deserialize<Config>("title = \"x\"");
var toml = TomlSerializer.Serialize(config);
```

Notes:
- Reflection-based mapping is **not** compatible with trimming/NativeAOT unless you opt into the required metadata.
- When publishing with NativeAOT, reflection-based mapping is disabled by default (feature switch).

## 3. POCO mapping (source generation / NativeAOT)

Source generation follows `System.Text.Json` patterns: declare a context derived from `TomlSerializerContext`, and add `JsonSerializable` roots.

```csharp
using System.Text.Json.Serialization;
using Tomlyn;
using Tomlyn.Serialization;

public sealed class Config
{
    public string? Title { get; set; }
}

#pragma warning disable SYSLIB1224
[TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Config))]
internal partial class MyTomlContext : TomlSerializerContext
{
}
#pragma warning restore SYSLIB1224

var config = TomlSerializer.Deserialize("title = \"x\"", MyTomlContext.Default.Config);
var toml = TomlSerializer.Serialize(config, MyTomlContext.Default.Config);
```

## 4. Attribute support (STJ parity)

Tomlyn supports a broad subset of `System.Text.Json.Serialization` attributes, including:
- `JsonPropertyName`, `JsonIgnore` (+ conditions), `JsonPropertyOrder`
- `JsonConstructor`, `JsonRequired` / `required`
- `JsonExtensionData` (table-shaped extension data)
- `JsonPolymorphic`, `JsonDerivedType` (discriminator-based polymorphism)

Tomlyn-specific equivalents exist for TOML-specific behavior (for example `TomlIgnore`, `TomlPolymorphic`, `TomlDerivedType`).

## 5. Preserving comments/trivia without polluting POCOs

Tomlyn can capture and re-apply TOML trivia (comments / formatting hints) using an external metadata store:

```csharp
using Tomlyn;
using Tomlyn.Serialization;

var store = new TomlMetadataStore();
var options = TomlSerializerOptions.Default with { MetadataStore = store };

var config = TomlSerializer.Deserialize<MyConfig>(toml, options);
var tomlOut = TomlSerializer.Serialize(config, options: options);
```

## 6. Syntax tree (roundtrip / tooling)

For full-fidelity parsing (including trivia and invalid tokens), use `SyntaxParser`:

```csharp
using Tomlyn.Parsing;

var doc = SyntaxParser.Parse(toml);
if (doc.HasErrors)
{
    foreach (var diag in doc.Diagnostics)
    {
        Console.WriteLine(diag);
    }
}

Console.WriteLine(doc.ToString()); // roundtrippable
```

## 7. Error locations

All parsing errors throw `TomlException` with a precise `TomlSourceSpan` pointing into the original document.

