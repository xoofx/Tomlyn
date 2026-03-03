---
title: Migration
---

This page will document breaking changes when moving from Tomlyn v0.x to Tomlyn v1.0:

- New `System.Text.Json`-style API (`TomlSerializer`, options, and contexts)
- Attribute changes and interop with `System.Text.Json.Serialization`
- Source generation and NativeAOT considerations

An API replacement table (old → new) will be added before the v1.0 release.
