---
title: Documentation
---

Tomlyn has two layers:

- **Low-level TOML infrastructure** for tooling scenarios: tokenization, incremental parse events, and a full-fidelity syntax tree.
- **Object mapping** via `TomlSerializer` for configuration-style serialization/deserialization (not roundtrip-preserving).

## Where to start

- New to Tomlyn: start with [Getting started](getting-started.md).
- Coming from `System.Text.Json`: start with [Serialization](serialization.md) and the JSON attribute section in it.
- Writing tooling (formatters, analyzers, refactorings): start with [Low-level APIs](low-level.md) and `SyntaxParser`.

## Key concepts

- **TOML version**: Tomlyn v1 targets **TOML 1.1.0 only**.
- **Fast paths**: for performance, Tomlyn avoids building intermediate models unless you ask for them.
- **Diagnostics**: parsing/mapping failures surface as `TomlException` with precise locations (`TomlSourceSpan`, `Line`, `Column`, `Offset`).
- **NativeAOT/trimming**: use source generation (`TomlSerializerContext`) to avoid reflection.
