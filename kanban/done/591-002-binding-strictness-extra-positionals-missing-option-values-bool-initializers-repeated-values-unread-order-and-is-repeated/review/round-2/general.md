# Round 2 — general
**Date:** 2026-11-19
**Scope reviewed:** fix delta `14104e13..HEAD` (`d9bbbc99` fix pass, `60ce4d7e` executable bit, `6b29609c` kanban land),
re-verifying round-1 M1–M5 against the diff and the task.md `## Results` / `### How to validate` record.
Reviewer: review oracle, Claude Opus 5.5, effort 2. No build, test, or audit was run; no targeted command was needed.

## Summary

The fix pass resolves all five round-1 findings. Plain flags no longer consume a following boolean token (M1),
`[Parameter(Name = ...)]` is validated with a new `NURU_A005` and falls back to the property name in both the
segment and the binding paths (M2), the two named guides set `Order` (M3), the bare default-true `--create`
rejection is documented in the migration guide and skill (M4), and the dead syntax-based extractor is deleted (M5).
New tests pin each behavior and the recorded gates (build, routing-43 20 passed, generator-76 5 passed, audit) cover
the delta. One gap remains: the M3 docs fix did not reach two multi-parameter examples in the migration guide that
this task's `NURU_A002` now rejects.

## Re-verification of round 1

- **M1 — fixed.** `route-matcher-emitter.option-parsing.cs:41-54` now sets the flag to `true` and consumes only the
  flag index; the `BooleanConverter.TryParse` lookahead is gone. Design region updated. routing-43 adds
  `seek {pos:int?} --verbose` + `seek --verbose 0` → `pos:0|verbose:True` and the required-positional variant.
  The existing default-true test dropped `--verbose false` from its argv; that is the intended contract change
  named in the fix pass and the restatement, not a silent weakening.
- **M2 — fixed.** `ParameterSegmentName` (`endpoint-extractor.properties.cs:324-350`) reports `NURU_A005` and
  returns the lowercased property name. `IsValidParameterName` matches the route parser's
  `IsValidIdentifierFormat` (`parser.validation.cs:26-44`). The binding path calls it with `diagnostics: null`, so it
  picks the same name without a duplicate diagnostic. `CSharpIdentifierUtils.ToCamelCase` splits only on `-`, so an
  accepted name (letters, digits, `_`) yields the same local in the matcher and the constructor. generator-76 pins
  one `NURU_A005`, error severity, and no `source-file` in the generated source. Release notes list `NURU_A005`.
- **M3 — fixed for the two cited files.** `endpoints.md` and `endpoint-generator.md` set `Order` on every parameter
  and say it is required. See Issue 1 for the remaining examples.
- **M4 — fixed (documented).** Migration guide concept map, behavior difference 10, and the migrate skill state that
  a bare `--create` is `Unknown command`, exit 1, and that System.CommandLine binds it as true. routing-43 adds
  `Should_reject_a_bare_default_true_bool`.
- **M5 — fixed.** `ExtractSegmentFromProperty` deleted. (Observation only: its former callee
  `ExtractPropertyDefaultValue(PropertyDeclarationSyntax)` in `endpoint-extractor.default-literals.cs` was already
  unreachable before this task; the comment now says it has no caller. Not raised.)
- `60ce4d7e` restores mode 100755 on both touched runfiles; no content change.

## Issues

### Issue 1 — Severity: bug
- File: documentation/user/guides/migrating-from-system-commandline.md:349
- Description: Two examples in the migration guide declare two `[Parameter]` properties without `Order`:
  `GetCommand` (`Url` at :349 and catch-all `Items` at :352) and `RunCommand` (`Script` at :396 and catch-all
  `Args` at :399). `ReportMissingParameterOrder` (`endpoint-extractor.properties.cs:178-195`) counts a catch-all as a
  parameter, so each example now reports `NURU_A002` (error) twice and a user who copies it gets a failing build.
  The same guide's concept map (:48) says to set `Order` on each parameter, and restatement item 4 says docs that show
  several parameters set `Order`. Round 1 M3 named only the two endpoint guides; these are the remaining instances
  (other docs, skills, and samples with `[Parameter]` have a single parameter or already set `Order`).
- Suggestion: Add `Order = 0` / `Order = 1` to both examples.
- Status: open
