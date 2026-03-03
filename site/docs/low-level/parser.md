---
title: TomlParser
---

`TomlParser` is an incremental parser that exposes parse events via `MoveNext()`.

This powers both:

- the syntax tree builder (round-trippable)
- higher-level readers/serializers (allocation-conscious)
