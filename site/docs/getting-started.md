---
title: Getting started
---

## Install

```sh
dotnet add package Tomlyn
```

Tomlyn targets `net8.0`, `net10.0`, and `netstandard2.0`.

## Serialize and deserialize

```csharp
using Tomlyn;

public sealed record Person(string Name, int Age);

var toml = TomlSerializer.Serialize(new Person("Ada", 37));
var person = TomlSerializer.Deserialize<Person>(toml)!;
```

## Streams (UTF-8)

Tomlyn provides `Stream` overloads to avoid `StreamReader`/`StreamWriter` boilerplate:

```csharp
using System.IO;
using Tomlyn;

using var stream = File.OpenRead("config.toml");
var config = TomlSerializer.Deserialize<MyConfig>(stream);
```

## Configure options

`TomlSerializerOptions` is immutable and can be cached and reused.

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

## Error locations

When parsing or mapping fails, Tomlyn throws `TomlException` with precise locations:

```csharp
try
{
    var config = TomlSerializer.Deserialize<MyConfig>("bad = [", new TomlSerializerOptions { SourceName = "config.toml" });
}
catch (TomlException ex)
{
    // ex.SourceName, ex.Line, ex.Column, ex.Offset, ex.Span
    Console.WriteLine(ex.Message);
}
```

If you prefer a non-throwing API, use `TryDeserialize(...)`:

```csharp
if (!TomlSerializer.TryDeserialize<MyConfig>(toml, out var value))
{
    // value is null/default when parsing fails
}
```

## Source generation (NativeAOT friendly)

For NativeAOT/trimming scenarios, prefer `TomlSerializerContext` and generated `TomlTypeInfo<T>` metadata.
See [Source generation and NativeAOT](source-generation.md).

## Next

- [Serialization](serialization.md)
- [Source generation and NativeAOT](source-generation.md)
- [Low-level APIs](low-level.md)
