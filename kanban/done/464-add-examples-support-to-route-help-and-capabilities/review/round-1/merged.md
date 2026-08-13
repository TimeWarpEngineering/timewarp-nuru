# Round 1 — merged findings
**Date:** 2026-08-13
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 2 | 1 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:1787
- Description: `ExtractStringArgumentAt` indexes arguments positionally and ignores `NameColon`,
  so `.WithExample(description: "d", command: "c")` compiles clean but silently swaps command and
  description in help and capabilities output (empirically verified).
- Suggestion: Resolve arguments by `NameColon` name when present, falling back to position for
  unnamed args (preferred), or fail with the existing InvalidOperationException → NURU_S999 path
  on out-of-slot named args.
- Source: general
- Disposition notes: Fixed - `ExtractStringArgumentAt` now takes an optional `parameterName` and
  prefers a `NameColon`-matched argument before falling back to positional indexing (unchanged for
  existing single-string callers); added `Should_show_example_via_fluent_with_out_of_order_named_args`
  regression test to help-08-route-examples.cs.

### M2 — Severity: nit — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:1571
- Description: Fluent path only checks `command is null`, so `.WithExample("")` renders a blank
  line and `Command: ""` in capabilities; attribute path skips empty commands. Inconsistent.
- Suggestion: Use `string.IsNullOrEmpty` skip in `DispatchWithExample` to match the attribute path.
- Source: general
- Disposition notes: Fixed - `DispatchWithExample` now skips (returns the builder unchanged, no
  throw) when `string.IsNullOrEmpty(command)`, mirroring `ExtractNuruRouteExampleAttributes`'s
  silent-skip behavior.

### M3 — Severity: nit — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/capabilities-emitter.cs:365
- Description: Empty-string descriptions emit `"description": ""` in capabilities but are
  suppressed in help (`IsNullOrEmpty` vs `is not null` guards). Same model, different rules.
- Suggestion: Normalize empty descriptions to null at extraction time (both DSL paths), or use
  `IsNullOrEmpty` in the capabilities emitter.
- Source: general
- Disposition notes: Fixed - normalized `""` to `null` at extraction time in both
  `EndpointExtractor.ExtractNuruRouteExampleAttributes` and `DslInterpreter.DispatchWithExample`;
  emitters' existing guards (`IsNullOrEmpty` in help, `is not null` in capabilities) left unchanged
  since both now observe the same value.

### M4 — Severity: nit — Status: wontfix
- File: source/timewarp-nuru-analyzers/generators/emitters/emitter-string-utils.cs:16
- Description: Pre-existing systemic gap: `EscapeForStringLiteral` doesn't escape U+0085/U+2028/
  U+2029, which the C# lexer treats as newlines — such characters in any description (routes,
  parameters, options, and now examples) produce invalid generated code. Not introduced by this
  diff; exposure predates 464 across every description call site.
- Suggestion: Fix in a dedicated follow-up task covering all call sites at once.
- Source: general
- Disposition notes: Pre-existing systemic gap across all description call sites; dispositioned to
  follow-up task 465 (decided by orchestrator).

## Duplicates / conflicts

- None — single reviewer.
