---
title: Getting started
---

## Install

```sh
dotnet add package Tomlyn
```

Tomlyn ships builds for `net8.0`, `net10.0`, and `netstandard2.0`.

## Serialize and deserialize

The main entry point is [`TomlSerializer`](xref:Tomlyn.TomlSerializer) - it works just like [`System.Text.Json.JsonSerializer`](xref:System.Text.Json.JsonSerializer):

```csharp
using Tomlyn;

public sealed class ServerConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 8080;
    public bool Ssl { get; set; }
}

// Serialize to TOML
var config = new ServerConfig { Host = "example.com", Port = 443, Ssl = true };
var toml = TomlSerializer.Serialize(config);

// Deserialize from TOML
var roundTrip = TomlSerializer.Deserialize<ServerConfig>(toml)!;
```

Output:

```toml
Host = "example.com"
Port = 443
Ssl = true
```

> [!NOTE]
> By default [`TomlSerializerOptions.DefaultIgnoreCondition`](xref:Tomlyn.TomlSerializerOptions.DefaultIgnoreCondition) is set to [`TomlIgnoreCondition.WhenWritingNull`](xref:Tomlyn.TomlIgnoreCondition.WhenWritingNull),
> so null properties are omitted from the output.

## Naming policies

By default, CLR member names are used as-is. Use [`PropertyNamingPolicy`](xref:Tomlyn.TomlSerializerOptions.PropertyNamingPolicy) for snake_case or camelCase keys:

```csharp
using System.Text.Json;
using Tomlyn;

var options = new TomlSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
};

var toml = TomlSerializer.Serialize(new ServerConfig { Host = "example.com", Port = 443 }, options);

// host = "example.com"
// port = 443
```

## Streams (UTF-8)

> [!TIP]
> `Stream` overloads are for convenience. Tomlyn reads the entire stream into memory before parsing (see [Performance](performance.md)).

Tomlyn provides `Stream` and `TextReader` overloads to avoid manual `StreamReader`/`StreamWriter` boilerplate:

```csharp
using System.IO;
using Tomlyn;

// Read from a file stream
using var input = File.OpenRead("config.toml");
var config = TomlSerializer.Deserialize<ServerConfig>(input)!;

// Write to a file stream
using var output = File.Create("config_out.toml");
TomlSerializer.Serialize(output, config);
```

## Configure options

[`TomlSerializerOptions`](xref:Tomlyn.TomlSerializerOptions) is an immutable `sealed record` - create it once and reuse it:

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
var roundTrip = TomlSerializer.Deserialize<ServerConfig>(toml, options);
```

See [Serialization](serialization.md) for the full option reference table.

## Error handling

When parsing or mapping fails, Tomlyn throws [`TomlException`](xref:Tomlyn.TomlException) with precise source locations:

```csharp
try
{
    var config = TomlSerializer.Deserialize<ServerConfig>(
        "bad = [",
        new TomlSerializerOptions { SourceName = "config.toml" });
}
catch (TomlException ex)
{
    // ex.SourceName  → "config.toml"
    // ex.Line        → 1
    // ex.Column      → 7
    Console.WriteLine(ex.Message);
}
```

If you prefer a non-throwing API, use `TryDeserialize`:

```csharp
if (!TomlSerializer.TryDeserialize<ServerConfig>(toml, out var value))
{
    // value is null/default when parsing fails
}
```

## Untyped model (no POCOs)

If you don't want to define classes, deserialize into [`TomlTable`](xref:Tomlyn.Model.TomlTable):

```csharp
using Tomlyn;
using Tomlyn.Model;

var toml = """
    title = "My App"

    [database]
    host = "localhost"
    port = 5432
    """;

var table = TomlSerializer.Deserialize<TomlTable>(toml)!;
var title = (string)table["title"]!;              // "My App"
var db = (TomlTable)table["database"]!;
var port = (long)db["port"]!;                     // 5432
```

See [DOM model](dom.md) for more details.

## Source generation (NativeAOT friendly)

For NativeAOT / trimming, declare a [`TomlSerializerContext`](xref:Tomlyn.Serialization.TomlSerializerContext) and use generated metadata:

```csharp
using Tomlyn;
using Tomlyn.Serialization;

[TomlSerializable(typeof(ServerConfig))]
internal partial class MyTomlContext : TomlSerializerContext { }

var toml = TomlSerializer.Serialize(config, MyTomlContext.Default.ServerConfig);
var roundTrip = TomlSerializer.Deserialize(toml, MyTomlContext.Default.ServerConfig);
```

See [Source generation and NativeAOT](source-generation.md) for details.

## Next steps

- [Serialization](serialization.md) - options, attributes, converters, polymorphism, and more.
- [Source generation and NativeAOT](source-generation.md) - trimming-safe metadata.
- [DOM model](dom.md) - dynamic [`TomlTable`](xref:Tomlyn.Model.TomlTable) / [`TomlArray`](xref:Tomlyn.Model.TomlArray) manipulation.
- [Low-level APIs](low-level.md) - tokenizer, parser, and syntax tree.
