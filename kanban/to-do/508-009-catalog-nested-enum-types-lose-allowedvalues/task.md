# Catalog: nested enum types lose allowedValues

## Description

When a route parameter or option is typed as an enum declared inside a class (a nested type), the generated capabilities catalog omits `allowedValues` for it. A namespace-level enum works. `enum-info-extractor.cs` already documents that a missing enum "silently drops AllowedValues/completion", and `capabilities-emitter.cs:297` emits `AllowedValues` only when `ExtractEnumValues` resolves the type name. The nested case most likely fails on the type-name key: the extractor and the emitter spell a nested type differently (`Outer.Inner` versus `Outer+Inner` or a minimally qualified `Inner`), so the lookup misses.

Consequences today: `--capabilities` shows no allowed values, shell completion offers none, and the 508-004 MCP input schema has no `enum` for that parameter, so an agent cannot see the valid set. The 508-004 test uses a namespace-level enum to sidestep it.

## Requirements

- Nested enums (any depth, including generic outer types if the extractor supports them) produce the same `allowedValues` as namespace-level enums, in the catalog, in completion, and in the MCP schema.
- Resolve the enum through the semantic model symbol, not through a display string, when keying the lookup (see the Roslyn note in `.agent/local/nuru-specific.md`).
- Nullable nested enums (`Outer.Inner?`) keep `allowedValues` and the `?` null type.

## Checklist

- [ ] Failing test: route with a nested enum parameter and a nested enum option, assert `allowedValues` in `app.Capabilities` and in `--capabilities`
- [ ] Fix the lookup key in the extractor or emitter
- [ ] McpHost test: nested enum appears as `enum` in the tool input schema
- [ ] Full CI after `ganda runfile cache --clear`

## Notes

- Filed from 508-004 task Results (2026-10-19): "Enum allowedValues are missing from the catalog when the enum is a nested type. Worth its own task."
- Related: 508 (parent), 508-002, 508-004.

## Session

- Created: 3915793 (2026-10-19)
