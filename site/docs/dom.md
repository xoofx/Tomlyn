---
title: DOM model
---

Tomlyn exposes a DOM for dynamic TOML manipulation.

The primary DOM types are:

- `TomlTable` for tables (key/value mappings)
- `TomlArray` for arrays
- `TomlTableArray` for arrays of tables

These types live in `Tomlyn.Model`.

## Parse into a DOM

If you want a dynamic model without defining POCOs, deserialize to `TomlTable`:

```csharp
using Tomlyn;
using Tomlyn.Model;

TomlTable table = TomlSerializer.Deserialize<TomlTable>("name = \"Ada\"")!;
```

## Serialize a DOM

```csharp
using Tomlyn;
using Tomlyn.Model;

var table = new TomlTable
{
    ["name"] = "Ada",
    ["age"] = 37
};

var toml = TomlSerializer.Serialize(table);
```

## Metadata and trivia (optional)

Tomlyn can capture and attach metadata (source spans and trivia such as comments/whitespace) without polluting your CLR types by using a metadata store:

```csharp
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Serialization;

var store = new TomlMetadataStore();
var options = new TomlSerializerOptions { MetadataStore = store };

var value = TomlSerializer.Deserialize<TomlTable>("# hi\nname = \"Ada\"", options)!;

if (store.TryGetProperties(value, out var metadata) && metadata is not null &&
    metadata.TryGetProperty("name", out var property) && property is not null)
{
    // property.Span, property.LeadingTrivia, property.TrailingTrivia
}
```

Metadata capture is intended for tooling and round-tripping scenarios. For full-fidelity syntax round-trip, prefer `SyntaxParser` (see [Low-level APIs](low-level.md)).
