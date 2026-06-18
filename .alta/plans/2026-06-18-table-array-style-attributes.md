# Table array serialization and style attributes

- Status: Implemented
- Plan file: `.alta/plans/2026-06-18-table-array-style-attributes.md`
- Created: 2026-06-18
- Task: Improve typed table-array serialization and add attribute-level formatting policy overrides.
- Git: `.alta/plans/` is not ignored; commit this plan with related implementation work. Existing dirty file `.github/workflows/ci.yml` is user-owned and should not be touched unless separately requested.

## Objective
- Fix issue #128 so non-empty typed collections of table-shaped objects serialize as TOML arrays of tables by default, allowing `TomlTableArrayStyle.Headers` to emit `[[Foo]]`.
- Add attribute-level formatting overrides:
  - property/field `[TomlTableArrayStyle(...)]`
  - property/field `[TomlInlineTable(...)]`
  - property/field `[TomlStringStyle(...)]` for string style/preferences overrides on string properties/fields
  - type-level `[TomlMappingOrder(...)]`
  - type-level `[TomlDottedKeyHandling(...)]`
- Keep TOML output syntactically valid by preserving the model-writer ordering rule: scalar/array/inline-table values first, named tables second, table arrays last.
- Non-goals: change parser semantics, preserve byte-for-byte input ordering around table arrays, or make custom converters introspectable as table-shaped by default.

## Context and evidence
- Issue #128 reports source-generated `Foo[]` serializing as `Foo = [{First = 0, Second = 0}, ...]` even with `TomlTableArrayStyle.Headers`, because collection type infos call `writer.WriteStartArray()`.
- `src/Tomlyn/Serialization/Internal/TomlSourceGeneratedCollectionTypeInfos.cs:24-35` and `:126-137` write source-generated arrays/lists as `TomlArray`; reflection collection type infos do the same in `src/Tomlyn/Serialization/Internal/TomlCollectionTypeInfos.cs:25-36` and `:120-131`.
- `src/Tomlyn/Serialization/TomlWriter.cs:227-258` already supports building a semantic `TomlTableArray` via `WriteStartTableArray()`/`WriteEndTableArray()`.
- `src/Tomlyn/Serialization/Internal/TomlModelTextWriter.cs:57-108` already emits table arrays after ordinary values and named tables, and `:62-65` / `:99-106` already applies `TomlTableArrayStyle` when the model value is a `TomlTableArray`.
- `src/Tomlyn/TomlSerializerOptions.cs:212-217` defines global `InlineTablePolicy` and `TableArrayStyle`; `:140-147` defines global `MappingOrder` and `DottedKeyHandling`; `:207` and `:406-416` define global `StringStylePreferences`.
- Existing public TOML attributes live in `src/Tomlyn/Serialization/TomlAttributes.cs`; source generation models members in `TomlSerializerContextGenerator.PocoMember`; reflection models members in `TomlReflectionTypeInfoResolver.MemberModel`.
- `src/Tomlyn/Model/TomlPropertiesMetadata.cs:68-94` already stores per-property display metadata, and `TomlModelTextWriter.WriteKeyValuePair` consumes `DisplayKind` for scalar formatting.
- `TomlStringStylePreferences` currently appears to be defined but not used by `TomlModelTextWriter`; string output is currently driven mainly by `TomlPropertyDisplayKind` and defaults to basic strings.

## Decisions and assumptions
- User-selected API shapes:
  - Use `[TomlTableArrayStyle(TomlTableArrayStyle.Headers)]` / `[TomlTableArrayStyle(TomlTableArrayStyle.InlineArrayOfTables)]` on properties/fields.
  - Use `[TomlInlineTable(TomlInlineTablePolicy.WhenSmall)]` (or other enum value) on properties/fields.
  - Use a single `[TomlStringStyle(...)]` attribute for property-level string style and string preferences on string properties/fields. Unspecified preference properties fall back to global `TomlStringStylePreferences`.
  - Use separate type-level attributes `[TomlMappingOrder(...)]` and `[TomlDottedKeyHandling(...)]`.
- Property-level `TomlInlineTable` applies only to the annotated property value. Nested members use their own attributes, their declaring type attributes, or global options.
- Type-level `TomlDottedKeyHandling` applies only to member names declared on that type. Dictionary keys written by dictionary members continue to use existing global dictionary/dotted-key behavior.
- Attribute-level overrides affect serialization formatting only unless an existing option already affects deserialization.
- Property-level string preferences use a single `[TomlStringStyle]` attribute. Because CLR attributes cannot accept a `TomlStringStylePreferences` record directly and cannot represent nullable bools directly, implement nullable-like enum/sentinel properties for preferences so unspecified values fall back to global options.
- Explicit attributes override global options. If `MetadataStore` supplies trivia/display metadata, attributes should override formatting hints while preserving comments/trivia.
- Empty typed object collections should continue to serialize as `Foo = []` because TOML has no header syntax for an empty array of tables.
- Custom converters are not treated as table-shaped by default; their collections stay normal arrays unless the converter explicitly writes a `TomlTableArray`.

## Design notes
- Build semantic model first, format second. Collection writers should create `TomlTableArray` when appropriate; they should not stream `[[Foo]]` directly.
- Add an internal table-shape capability to `TomlTypeInfo`, for example `internal virtual bool WritesTable => false` or an internal `TomlTypeShape` enum. Mark generated POCO, reflection POCO, dictionary/table-like, and `TomlTable` metadata as table-shaped.
- Add an internal writer context check, for example `TomlWriter.CanWriteTableArrayValue`, so collection type infos only choose `WriteStartTableArray()` when the collection is being written as a property of a table. Collections nested inside ordinary arrays must stay normal TOML arrays.
- Collection write algorithm:
  - Resolve element type info once.
  - If collection is non-empty, element type is table-shaped, and writer context allows a table-array property, call `WriteStartTableArray()` and write each element table.
  - Otherwise call `WriteStartArray()` as today.
- Preserve the current empty collection behavior by using normal arrays when the count is zero or when count cannot be cheaply known and the first element is absent.
- For `IEnumerable<T>` without a count, either buffer minimally before deciding or write normal arrays to avoid changing streaming semantics unsafely. Recommended: buffer only for source/reflection list-backed enumerable implementations that already materialize or enumerate; document any unavoidable one-pass behavior.
- Extend property metadata with formatting overrides that `TomlModelTextWriter` can consume:
  - nullable `TomlTableArrayStyle? TableArrayStyle`
  - nullable `TomlInlineTablePolicy? InlineTablePolicy`
  - nullable `TomlStringStyle? StringStyle`
- Add an internal `TomlWriter.SetPropertyMetadata(string propertyName, TomlPropertyMetadata metadata)`/merge helper so reflection and source-generated serializers can attach attribute-derived metadata while building the model.
- Update `TomlModelTextWriter` to resolve formatting in this precedence order:
  1. property-level metadata/attribute override
  2. syntax metadata captured by `MetadataStore` when not overridden
  3. type-level override where applicable
  4. global options
  5. hardcoded default
- Type-level `TomlMappingOrderPolicy`:
  - Reflection: read `[TomlMappingOrder(...)]` in `TomlReflectionTypeInfoResolver`, order that type's members using the type attribute before falling back to `Options.MappingOrder`.
  - Source generation: collect the attribute while building `PocoShape`/type model and emit the selected ordering in generated code.
- Type-level `TomlDottedKeyHandling`:
  - Add an internal write-name path or scoped writer override so member names declared on the annotated type are expanded/literal according to `[TomlDottedKeyHandling(...)]`, falling back to `Options.DottedKeyHandling`.
  - Do not scope this type-level setting into dictionary keys.
- Property-level string style/preferences:
  - Add a single `[TomlStringStyle(...)]` attribute for `string` properties/fields; invalid usage should produce source-generation diagnostics and reflection-time validation/exception consistent with existing attribute validation patterns.
  - The attribute constructor sets the preferred `TomlStringStyle`. Add preference override properties on the same attribute for `PreferLiteralWhenNoEscapes` and `AllowHexEscapes` using a nullable-like enum/sentinel such as `TomlBooleanPreference.Unspecified`, `True`, `False` so omitted properties fall back to global `TomlStringStylePreferences`.
  - Map `TomlStringStyle.Basic` to basic strings, `Literal` to `TomlPropertyDisplayKind.StringLiteral`, `MultilineBasic` to `StringMulti`, and `MultilineLiteral` to `StringLiteralMulti`.
  - Make global `TomlStringStylePreferences` effective in `TomlModelTextWriter` where currently unused, then layer property-level string style/preferences on top.
- Public attribute implementation details:
  - Add `TomlTableArrayStyleAttribute` with C# usage `[TomlTableArrayStyle(...)]`.
  - Add `TomlInlineTableAttribute` with usage `[TomlInlineTable(...)]`.
  - Add `TomlStringStyleAttribute` with usage like `[TomlStringStyle(TomlStringStyle.Literal, PreferLiteralWhenNoEscapes = TomlBooleanPreference.False)]` and nullable-like preference overrides.
  - Add `TomlMappingOrderAttribute` with usage `[TomlMappingOrder(...)]`.
  - Add `TomlDottedKeyHandlingAttribute` with usage `[TomlDottedKeyHandling(...)]`.
  - All public attributes need XML documentation and enum argument validation where appropriate.

## Risks and challenges
- TOML ordering: emitting `[[Foo]]` while still writing ordinary properties would produce invalid or semantically wrong TOML. The model-first approach avoids this.
- Empty arrays of tables cannot be represented with headers; they must remain `Foo = []` or be omitted. Recommended: keep `Foo = []`.
- Per-property table-array style on multiple table-array properties is valid, but the final writer may reorder those properties after scalar properties regardless of CLR declaration order.
- Custom converters may write tables but there is no reliable existing metadata to know that. Conservatively treating them as non-table avoids accidental exceptions and invalid arrays.
- Adding public attributes is an API change; names and string preference semantics are user-selected but still need consistent XML docs and diagnostics.
- `TomlStringStylePreferences` has option properties that are not currently honored; making them effective may reveal existing expectations in tests or require careful fallback behavior.
- Source generator and reflection paths must stay behaviorally aligned; otherwise AOT/source-gen users will see different TOML.
- Metadata merging must avoid losing comments/trivia captured by `ITomlMetadataStore`.

## Implementation checklist
- [x] Add failing tests for issue #128 in source-generation path: non-empty `Foo[]` round-trips and serializes with `[[Foo]]` by default.
- [x] Add matching reflection tests for arrays/lists of table-shaped objects.
- [x] Add table-array edge-case tests: empty object collection stays `Foo = []`, primitive arrays are unchanged, nested table arrays remain valid, multiple table-array properties serialize after scalar members.
- [x] Add tests for global `TomlTableArrayStyle.InlineArrayOfTables` still producing inline arrays where inlineable after typed object collections become `TomlTableArray`.
- [x] Add tests for `[TomlTableArrayStyle(...)]` overriding global table-array style on a specific property/field.
- [x] Add tests for `[TomlInlineTable(...)]` overriding global inline-table policy only for the annotated property/field.
- [x] Add tests for property-level string style/preferences on string properties/fields, including reflection validation for non-string usage. Source-generation emits `TOMLYN011` for non-string `[TomlStringStyle]` usage.
- [x] Add tests for type-level `[TomlMappingOrder(...)]` overriding global member order for the annotated type.
- [x] Add tests for type-level `[TomlDottedKeyHandling(...)]` affecting annotated type member names but not dictionary keys inside that type.
- [x] Add attribute contract tests for the new public attributes and XML-doc-covered public API.
- [x] Add an internal table-shape capability to `TomlTypeInfo` in `src/Tomlyn/TomlTypeInfo.cs`.
- [x] Mark table-shaped built-in and object metadata in `src/Tomlyn/Serialization/Internal/TomlBuiltInTypeInfoResolver.cs`, `TomlReflectionTypeInfoResolver.cs`, source-generated POCO type infos, dictionary type infos, and `TomlTable` converter metadata.
- [x] Add an internal `TomlWriter` context check for whether the pending value can legally be a `TomlTableArray` property.
- [x] Update reflection collection type infos in `src/Tomlyn/Serialization/Internal/TomlCollectionTypeInfos.cs` to write non-empty table-shaped collections as `TomlTableArray` only when legal.
- [x] Update source-generated collection type infos in `src/Tomlyn/Serialization/Internal/TomlSourceGeneratedCollectionTypeInfos.cs` with the same logic.
- [x] Add public attribute types in `src/Tomlyn/Serialization/TomlAttributes.cs`: `TomlTableArrayStyleAttribute`, `TomlInlineTableAttribute`, `TomlStringStyleAttribute`, the nullable-like string preference enum, `TomlMappingOrderAttribute`, and `TomlDottedKeyHandlingAttribute`.
- [x] Extend source generator constants, parsing, models, and diagnostics in `src/Tomlyn.SourceGeneration/TomlSerializerContextGenerator.cs` to recognize the new attributes.
- [x] Extend reflection member/type models in `src/Tomlyn/Serialization/Internal/TomlReflectionTypeInfoResolver.cs` to read and apply the new attributes.
- [x] Extend `TomlPropertyMetadata` in `src/Tomlyn/Model/TomlPropertiesMetadata.cs` with formatting override fields and metadata merge helpers as needed.
- [x] Add internal `TomlWriter` helper(s) to attach/merge attribute-derived property metadata while preserving existing metadata-store trivia.
- [x] Update `src/Tomlyn/Serialization/Internal/TomlModelTextWriter.cs` to use property-level `TableArrayStyle`, `InlineTablePolicy`, and string style metadata before global options.
- [x] Make global `TomlStringStylePreferences` effective if it is currently unused, then layer property-level string style/preferences on top.
- [x] Keep the top-level `readme.md` concise and add detailed typed table-array/style attribute documentation in `site/docs/serialization.md`. (`doc/readme.md` was not present in this checkout.)

## Verification checklist
- [x] Run targeted tests for new/changed tests: `dotnet test src/Tomlyn.Tests/Tomlyn.Tests.csproj -c Release --no-restore --filter "NewApiCollectionTests|NewApiStyleAttributeTests"`.
- [x] Run full unit tests: `dotnet test src/Tomlyn.Tests/Tomlyn.Tests.csproj -c Release --no-restore`.
- [x] Run `dotnet build src -c Release --no-restore` and confirm no warnings, especially CS1591 XML-doc warnings for new public attributes.
- [ ] Inspect generated source in source-generation tests where practical to ensure attributes are emitted as constants/branches without reflection.
- [x] Manually review serialized TOML examples for valid ordering: scalar values before `[table]`, table arrays last, multiple table arrays valid.
- [x] Self-review diff for public API naming consistency, AOT/source-generation compatibility, metadata-store trivia preservation, and no changes to unrelated dirty files.

## Handoff notes
- User resolved open decisions: `[TomlTableArrayStyle]`, `[TomlInlineTable]`, single `[TomlStringStyle]` attribute with nullable-like preference override properties and global fallback for unspecified preferences, separate `[TomlMappingOrder]`/`[TomlDottedKeyHandling]`, dotted-key type override applies to member names only, and inline-table property override applies only to the annotated property.
- Start with tests for issue #128 and table-array model semantics; they constrain the riskiest behavior before adding formatting attributes.
- Keep source-generation and reflection paths in sync throughout; do not fix only generated serialization.
- Prefer internal metadata/style scopes over direct text emission. The final TOML text writer must remain responsible for table/table-array ordering.
- Be conservative with custom converters and unknown enumerable shapes; preserving valid TOML is more important than guessing table-array semantics.
- User approved execution and explicitly allowed committing the work in logical steps.
- Do not modify `.github/workflows/ci.yml`; it was already dirty before this plan.
