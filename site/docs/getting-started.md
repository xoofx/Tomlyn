---
title: Getting started
---

Tomlyn targets **TOML 1.1.0** and exposes:

- Low-level APIs: `TomlLexer`, `TomlParser`, `SyntaxParser`
- High-level API: `TomlSerializer` / `TomlSerializerOptions` / `TomlSerializerContext`

## Install

```sh
dotnet add package Tomlyn
```

## Serialize / deserialize

```csharp
using Tomlyn;

var toml = TomlSerializer.Serialize(new { Name = "Ada", Age = 37 });
var person = TomlSerializer.Deserialize<Person>(toml);
```

## Options

Use `TomlSerializerOptions` to configure formatting, naming policies, duplicate key handling, dotted keys, root wrapping, and more.

## Source generation

For NativeAOT and trimming scenarios, prefer `TomlSerializerContext` and source generation.
