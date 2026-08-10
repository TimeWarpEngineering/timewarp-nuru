# Round 1 — general
**Date:** 2026-08-10
**Scope reviewed:** commit dc3f79c9 keyword identifier escape

## Summary

Reviewed commit `dc3f79c9` (`fix: escape C# keyword identifiers…`) against plan `83105343` and the current sources under `source/timewarp-nuru-analyzers/`, `samples/fluent/08-type-converters/`, and `tests/.../generator-39-keyword-param-identifiers.cs`. Compared live code to the pre-change `master` worktree for falsifiable deltas.

**EscapeIfKeyword / idempotency:** Implementation strips a single leading `@`, rejects empty-after-strip, and classifies via `SyntaxFacts.GetKeywordKind` / `GetContextualKeywordKind`. Double application yields a single `@` for keywords (`event` → `@event` → `@event`). Handler extractor now stores `Identifier.ValueText` (not `Text`), so stored names never carry `@`, which removes the double-`@` path if emit always re-escapes.

**Emit sites:** The primary bug (untyped positional `string event = …`) is fixed by `PositionalCaptureVar`, used in both primary and alias match paths for required/optional/catch-all captures. Typed conversions already emitted `EscapeIfKeyword` on the final variable; options/flags already escaped. `BuildArgumentList` (method handlers) now mirrors the delegate path and escapes `ParameterName`. Delegate `BuildParameterList` / `BuildArgumentListFromRoute` were already escaped.

**SyntaxFacts vs hand list (`value`):** The old `HashSet` over-escaped `value`. Bare `value` is a legal C# identifier (not a reserved or Roslyn contextual keyword), so not escaping it is correct. Sample `id {value:Guid}` and the generator-39 comment document this deliberately. SyntaxFacts also picks up newer contextual/reserved forms the hand list omitted (`record`, `file`, `required`, …) without maintaining a table.

**Dead code deletion:** `EmitParameterExtractionFromPositionalArgs` and `EmitVariableAliases` existed on master but had **no call sites**. Removing them is safe; live paths already extracted inline via `PositionalCaptureVar`.

**Tests + sample:** generator-39 covers reserved (`event`, `class`, `ref`, `for`) and contextual (`when`) names with positive/negative string asserts; sample restores `schedule {event} {when:DateTime}` with `@event` / `when` as a living regression.

**Double-`@` risk:** Mitigated by ValueText + strip-before-classify. Lambda bodies keep user-authored `@event` via source capture; local function signatures get a single escaped name. No product-code defect found in scope.

## Issues

No issues found.
