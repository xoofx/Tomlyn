---
title: DOM model
---

Tomlyn exposes a DOM (Document Object Model) for dynamic TOML manipulation without defining POCOs.
The DOM types live in [`Tomlyn.Model`](xref:Tomlyn.Model).

| Type | Description |
| --- | --- |
| [`TomlTable`](xref:Tomlyn.Model.TomlTable) | A TOML table - implements `IDictionary<string, object>`. |
| [`TomlArray`](xref:Tomlyn.Model.TomlArray) | A TOML array - implements `IList<object?>`. |
| [`TomlTableArray`](xref:Tomlyn.Model.TomlTableArray) | An array of tables - implements `IList<TomlTable>`. |

Scalar TOML values are represented as standard .NET types: `string`, `long`, `double`, `bool`, and [`TomlDateTime`](xref:Tomlyn.TomlDateTime).

> [!NOTE]
> The DOM model does not require reflection and works seamlessly with NativeAOT and trimmed applications.

## Parse into a DOM

Deserialize to [`TomlTable`](xref:Tomlyn.Model.TomlTable) for a dynamic model:

```csharp
using Tomlyn;
using Tomlyn.Model;

var toml = """
    title = "My App"

    [database]
    host = "localhost"
    port = 5432
    enabled = true
    ports = [8000, 8001, 8002]
    """;

var table = TomlSerializer.Deserialize<TomlTable>(toml)!;

// Access values with casts
var title = (string)table["title"]!;               // "My App"
var db = (TomlTable)table["database"]!;
var host = (string)db["host"]!;                    // "localhost"
var port = (long)db["port"]!;                      // 5432
var enabled = (bool)db["enabled"]!;                // true
var ports = (TomlArray)db["ports"]!;
var firstPort = (long)ports[0]!;                   // 8000
```

## Build and serialize a DOM

```csharp
using Tomlyn;
using Tomlyn.Model;

var table = new TomlTable
{
    ["title"] = "My App",
    ["database"] = new TomlTable
    {
        ["host"] = "localhost",
        ["port"] = 5432L,
        ["enabled"] = true,
        ["ports"] = new TomlArray { 8000L, 8001L, 8002L },
    }
};

var toml = TomlSerializer.Serialize(table);
```

Output:

```toml
title = "My App"

[database]
host = "localhost"
port = 5432
enabled = true
ports = [8000, 8001, 8002]
```

## Array of tables

Use [`TomlTableArray`](xref:Tomlyn.Model.TomlTableArray) to represent `[[array_of_tables]]`:

```csharp
using Tomlyn;
using Tomlyn.Model;

var table = new TomlTable
{
    ["servers"] = new TomlTableArray
    {
        new TomlTable { ["name"] = "alpha", ["ip"] = "10.0.0.1" },
        new TomlTable { ["name"] = "beta",  ["ip"] = "10.0.0.2" },
    }
};

var toml = TomlSerializer.Serialize(table);
```

Output:

```toml
[[servers]]
name = "alpha"
ip = "10.0.0.1"

[[servers]]
name = "beta"
ip = "10.0.0.2"
```

## Inline tables

Pass `true` to the [`TomlTable`](xref:Tomlyn.Model.TomlTable) constructor to emit an inline table:

```csharp
var table = new TomlTable
{
    ["point"] = new TomlTable(inline: true)
    {
        ["x"] = 1L,
        ["y"] = 2L,
    }
};

var toml = TomlSerializer.Serialize(table);
// point = { x = 1, y = 2 }
```

## Date/time values

TOML has four date/time types. Use [`TomlDateTime`](xref:Tomlyn.TomlDateTime) to represent them precisely:

```csharp
using Tomlyn;

// Offset date-time
var odt = new TomlDateTime(new DateTimeOffset(2023, 7, 15, 12, 0, 0, TimeSpan.FromHours(2)), 0, TomlDateTimeKind.OffsetDateTimeByNumber);

// Local date-time (no timezone)
var ldt = new TomlDateTime(new DateTime(2023, 7, 15, 12, 0, 0), 0, TomlDateTimeKind.LocalDateTime);

// Local date
var ld = new TomlDateTime(2023, 7, 15);

// Local time
var lt = new TomlDateTime(new DateTimeOffset(1, 1, 1, 12, 30, 0, TimeSpan.Zero), 0, TomlDateTimeKind.LocalTime);
```

[`TomlDateTimeKind`](xref:Tomlyn.TomlDateTimeKind) values:

| Value | TOML example |
| --- | --- |
| [`OffsetDateTimeByZ`](xref:Tomlyn.TomlDateTimeKind.OffsetDateTimeByZ) | `2023-07-15T12:00:00Z` |
| [`OffsetDateTimeByNumber`](xref:Tomlyn.TomlDateTimeKind.OffsetDateTimeByNumber) | `2023-07-15T12:00:00+02:00` |
| [`LocalDateTime`](xref:Tomlyn.TomlDateTimeKind.LocalDateTime) | `2023-07-15T12:00:00` |
| [`LocalDate`](xref:Tomlyn.TomlDateTimeKind.LocalDate) | `2023-07-15` |
| [`LocalTime`](xref:Tomlyn.TomlDateTimeKind.LocalTime) | `12:30:00` |

## Metadata and trivia

Tomlyn can capture source spans, comments, and formatting hints during deserialization using an external metadata store.
This keeps your CLR types clean - no marker properties or base classes required.

> [!TIP]
> Metadata is useful for building TOML editors, formatters, or tools that need to preserve comments across round-trips.

```csharp
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Serialization;

var toml = """
    # Application title
    title = "My App"

    # Database settings
    [database]
    host = "localhost" # primary host
    """;

var store = new TomlMetadataStore();
var options = new TomlSerializerOptions { MetadataStore = store };

var table = TomlSerializer.Deserialize<TomlTable>(toml, options)!;

// Retrieve metadata for a specific object
if (store.TryGetProperties(table, out var metadata) && metadata is not null)
{
    if (metadata.TryGetProperty("title", out var prop) && prop is not null)
    {
        // prop.LeadingTrivia  - comments before the key ("# Application title")
        // prop.TrailingTrivia - comments after the value
        // prop.Span           - source location (line, column, offset)
        // prop.DisplayKind    - how the value was written in the source
    }
}
```

### ITomlMetadataStore

The [`ITomlMetadataStore`](xref:Tomlyn.Serialization.ITomlMetadataStore) interface has two methods:

- `TryGetProperties(object instance, out TomlPropertiesMetadata? metadata)` - retrieve metadata for an object.
- `SetProperties(object instance, TomlPropertiesMetadata? metadata)` - attach metadata to an object.

The built-in [`TomlMetadataStore`](xref:Tomlyn.Serialization.TomlMetadataStore) implementation uses a `ConditionalWeakTable` so metadata is garbage-collected along with the target object.

### TomlPropertiesMetadata

Per-property metadata is accessed via `TryGetProperty(string key, out TomlPropertyMetadata? metadata)`.
Each [`TomlPropertyMetadata`](xref:Tomlyn.Model.TomlPropertyMetadata) contains:

| Property | Type | Description |
| --- | --- | --- |
| `LeadingTrivia` | `List<`[`TomlSyntaxTriviaMetadata`](xref:Tomlyn.Model.TomlSyntaxTriviaMetadata)`>?` | Comments/whitespace before the key. |
| `TrailingTrivia` | `List<`[`TomlSyntaxTriviaMetadata`](xref:Tomlyn.Model.TomlSyntaxTriviaMetadata)`>?` | Comments/whitespace after the value. |
| `TrailingTriviaAfterEndOfLine` | `List<`[`TomlSyntaxTriviaMetadata`](xref:Tomlyn.Model.TomlSyntaxTriviaMetadata)`>?` | Trivia after the end-of-line. |
| `Span` | [`TomlSourceSpan`](xref:Tomlyn.Text.TomlSourceSpan) | Source location of the key-value pair. |
| `DisplayKind` | [`TomlPropertyDisplayKind`](xref:Tomlyn.Model.TomlPropertyDisplayKind) | How the value appeared in the original source. |

> [!NOTE]
> For full-fidelity syntax round-trip (preserving every character), prefer [`SyntaxParser`](xref:Tomlyn.Parsing.SyntaxParser) - see [Low-level APIs](low-level.md).
