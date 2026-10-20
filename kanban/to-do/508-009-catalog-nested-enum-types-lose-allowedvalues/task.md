# Catalog: nested enum types lose allowedValues

## Description

When a route parameter or option is typed as an enum declared inside a class (a nested type), the generated capabilities catalog omits `allowedValues` for it. A namespace-level enum works. `enum-info-extractor.cs` already documents that a missing enum "silently drops AllowedValues/completion", and `capabilities-emitter.cs:297` emits `AllowedValues` only when `ExtractEnumValues` resolves the type name. The nested case most likely fails on the type-name key: the extractor and the emitter spell a nested type differently (`Outer.Inner` versus `Outer+Inner` or a minimally qualified `Inner`), so the lookup misses.

Consequences today: `--capabilities` shows no allowed values, shell completion offers none, and the 508-004 MCP input schema has no `enum` for that parameter, so an agent cannot see the valid set. The 508-004 test uses a namespace-level enum to sidestep it.

## Requirements

- Nested enums (any depth, including generic outer types if the extractor supports them) produce the same `allowedValues` as namespace-level enums, in the catalog, in completion, and in the MCP schema.
- Resolve the enum through the semantic model symbol, not through a display string, when keying the lookup (see the Roslyn note in `.agent/local/nuru-specific.md`).
- Nullable nested enums (`Outer.Inner?`) keep `allowedValues` and the `?` null type.

## Goal (implementer restatement)

An enum declared inside a class (`Outer.Inner`, any nesting depth, including inside a generic
outer type) must yield the same enum member list as a namespace-level enum everywhere the
generator emits it: `allowedValues` in `app.Capabilities` and `--capabilities`, generated shell
completion (`__complete`) candidates, and the McpHost tool input schema `enum`. Root cause:
`EnumInfoExtractor.Resolve` calls `GetTypeByMetadataName` on a C# display name, and a nested
type's metadata name uses `+`, so the lookup misses and the enum is dropped.

Acceptance:

- A route with a nested enum positional parameter and a nested enum option reports
  `allowedValues` in `app.Capabilities` and in `--capabilities` JSON.
- A nullable nested enum (`Outer.Inner?`) keeps `allowedValues` and its nullable type.
- Doubly nested and generic-outer nested enums also resolve.
- `__complete` offers the nested enum members for the positional parameter.
- The McpHost input schema carries `enum` for a nested enum member.
- Enum resolution goes through Roslyn symbols (namespace/type member walk), not by rewriting
  the display string into a metadata name.
- Full CI green after `ganda runfile cache --clear`; `ganda repo audit` clean.

## Checklist

- [x] Failing test: route with a nested enum parameter and a nested enum option, assert `allowedValues` in `app.Capabilities` and in `--capabilities`
- [x] Fix the lookup key in the extractor or emitter
- [x] McpHost test: nested enum appears as `enum` in the tool input schema
- [x] Full CI after `ganda runfile cache --clear`

## Notes

- Filed from 508-004 task Results (2026-10-19): "Enum allowedValues are missing from the catalog when the enum is a nested type. Worth its own task."
- Related: 508 (parent), 508-002, 508-004.

## Session

- Created: 3915793 (2026-10-19)
- 2026-10-20 implementer (claude, headless `ganda task work`): root cause confirmed, fixed in the
  extractor (symbol walk) and the capabilities emitter (handler-type fallback); also fixed the
  nullable optional positional enum binder bug the nullable acceptance needed. New test file
  `capabilities-09-nested-enum.cs` (7 tests) fails 6/7 with the extractor fix reverted, passes 7/7
  with it. Full CI and `ganda repo audit` green. The commit-time audit flagged
  `runfile-executable` on the new test file; set its executable bit (git mode 100755).

## Results

### What changed

- `source/timewarp-nuru-analyzers/generators/extractors/enum-info-extractor.cs`: `Resolve` now
  calls `ResolveEnumSymbol`. It tries `GetTypeByMetadataName` first. When that misses, it splits the
  `FullyQualifiedFormat` name on top-level dots into (name, generic arity) segments and walks
  `Compilation.GlobalNamespace` members (namespaces, then types matched by name and `Arity`). That
  resolves `NS.Outer.Inner`, `NS.Outer.Mid.Inner`, and `NS.Outer<int>.Inner` to their enum symbols.
  The lookup key stays the normalized display string the model carries, so the emitters are unchanged.
- `.../emitters/capabilities-emitter.cs`: a typed constraint such as `{target:NeTarget}` sets
  `ResolvedClrTypeName` to `global::NeTarget`, which names no type, so `allowedValues` was dropped
  for every typed enum parameter whose enum is not in the global namespace (nested or not). Lookup
  now tries `ResolvedClrTypeName`, then the matching handler parameter type.
- `.../emitters/route-matcher-emitter.cs` (`EmitEnumTypeConversion`): an optional positional enum
  with a nullable handler type (`{t:Env?}` + `Env? t`) emitted `Env??` and `EnumTypeConverter<Env?>`
  and failed to compile. It now strips the trailing `?`. This bug is not specific to nested enums;
  fixed here because the nullable acceptance criterion needs it.
- `models/enum-info.cs`: the key doc now says it is the normalized display name, not a metadata name.
- Tests: new `tests/timewarp-nuru-tests/capabilities/capabilities-09-nested-enum.cs` (catalog,
  `--capabilities`, nullable, generic outer, `[NuruRoute]` options, run, `__complete`).
  `mcphost-01-tools.cs` gains route `mh-tier` with a nullable nested positional and a doubly nested
  option, asserting the schema `enum` lists and `["string","null"]`.

### Gaps found (not fixed here; worth their own tasks)

- An untyped positional (`ne-deploy {target}`) with an enum handler parameter is never converted;
  the generator assigns the string and the build fails. The same applies to `[Parameter]` enum
  properties on `[NuruRoute]` classes. Today positional enums need a type constraint
  (`{target:Env}`). This is true of all enums, not only nested ones.
- `[NuruRoute]` `[Option]` enum properties report catalog type `string` with no `?` for a nullable
  enum, so the MCP schema type is not nullable. `allowedValues` is now present. All enums share this.

### Verification

- `dotnet run tests/timewarp-nuru-tests/capabilities/capabilities-09-nested-enum.cs`: 7/7 pass;
  6/7 fail with only the extractor change reverted (run passes either way).
- `dotnet run tests/timewarp-nuru-mcphost-tests/mcphost-01-tools.cs`: 16/16.
- `ganda runfile cache --clear` then `dotnet run tests/ci-tests/run-ci-tests.cs`: multi-mode 2015
  total, 2009 passed, 6 skipped, 0 failed; standalone 0 failed.
- `ganda repo audit`: passes all checks (nothing to fix).

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/capabilities/capabilities-09-nested-enum.cs
dotnet run tests/timewarp-nuru-mcphost-tests/mcphost-01-tools.cs
```

Expect: `Total: 7 / Passed: 7` for the nested-enum file, including
`Should_include_allowed_values_for_nested_enum_parameter_and_option` (target `[Alpha, Beta]`,
tier `[Free, Pro, Enterprise]`), `Should_keep_allowed_values_for_nullable_nested_enum` (type
`NeTarget?`, not required), `Should_include_allowed_values_for_enum_nested_in_generic_type`, and
`Should_complete_nested_enum_parameter_values` (`__complete` lists `Alpha` and `Beta`).
`Total: 16 / Passed: 16` for McpHost, where `Should_build_input_schema_from_parameters_and_options`
asserts `mh-tier.tier` has `type: ["string","null"]` and `enum: [Basic, Premium]`, and `size`
has `enum: [Small, Large]`.
