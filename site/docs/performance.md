---
title: Performance
---

Tomlyn is designed for:

- High throughput parsing and serialization
- Low allocations (Span-friendly internals, pooled buffers where appropriate)
- Source generation for NativeAOT/trimming scenarios

Benchmarks will be published once numbers stabilize.

## Practical tips

- Cache and reuse a single `TomlSerializerOptions` instance (it is immutable).
- Prefer source generation (`TomlSerializerContext` / `TomlTypeInfo<T>`) for hot paths and AOT/trimming scenarios.
- Prefer `Stream` overloads for file IO to avoid `StreamReader`/`StreamWriter` boilerplate.
- If you need precise source mapping for tooling, set `TomlSerializerOptions.SourceName` so exceptions include a meaningful file/path.
