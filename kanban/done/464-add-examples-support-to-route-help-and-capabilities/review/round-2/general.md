# Round 2 — general
**Date:** 2026-08-13
**Scope reviewed:** fix commit 6c7ac49a (dsl-interpreter.cs, endpoint-extractor.cs,
help-08-route-examples.cs, plus review artifacts) + re-verification of round-1 M1-M3. Ran
`ganda runfile cache --clear`, then help-08 (8/8 pass) and capabilities-06 (4/4 pass).
Empirically probed M1/M2 edge cases with a temporary in-repo runfile (deleted after; tree clean):
mixed named+positional args, empty-string command, and a const (non-literal) command argument.

## Summary
All three fixes are correct and pinned. Name-preferred argument resolution handles every legal
argument shape I could construct — reordered fully-named (pinned by the new test), mixed
positional+named (`.WithExample("MIXED-CMD --flag", description: "MIXED-DESC")` renders
unswapped, verified empirically), lone-named single-string callers, and positional-only — while
existing `ExtractStringArgument` callers are provably behavior-identical (parameterName defaults
to null, skipping the name loop; the null/count guards are equivalent to the old code). One new
suggestion-level finding: the M2 skip now conflates "empty literal" with "not a literal", so a
const-string command silently drops the example where it previously produced a loud NURU_S999
diagnostic (and where `WithAlias` still errors today).

## Prior findings
- M1: **verified-fixed** — `ExtractStringArgumentAt(invocation, index, parameterName)` prefers a
  `NameColon`-matched argument anywhere in the list, positional fallback otherwise
  (dsl-interpreter.cs:1808). Adversarial checks: `.WithExample(description: "d", command: "c")`
  binds correctly (new test `Should_show_example_via_fluent_with_out_of_order_named_args`, which
  also asserts the description does NOT appear as the undimmed command line — it would fail
  against the pre-fix code); mixed `.WithExample("cmd", description: "d")` verified empirically
  (command line and dimmed description both correct); `.WithExample(command: "c")` and
  positional-only forms resolve via name/position respectively. Single-string callers
  (`WithDescription`/`WithAlias`/`WithName` via `ExtractStringArgument`) pass
  `parameterName = null` so the name loop is skipped and the remaining logic (`args is null`,
  `Count <= index`, literal-only switch, now via `ExtractLiteralStringValue`) is semantically
  identical to the pre-refactor code — including the lone-named-argument-at-index-0 case. The
  only mis-binding construction I found requires code that does not compile
  (`.WithExample(description: "d")` with the required `command` missing), so it can never ship.
- M2: **verified-fixed** — `DispatchWithExample` now returns `routeBuilder` (correct receiver for
  continued chaining) on `string.IsNullOrEmpty(command)` (dsl-interpreter.cs:1575-1579).
  Empirically: `.WithExample("", "EMPTY-DESC")` produces no Examples section and no diagnostic,
  matching the attribute path. See Issue 1 for a tradeoff this introduced for non-literal args.
- M3: **verified-fixed** — both extraction paths normalize `""` → null before constructing
  `ExampleDefinition` (endpoint-extractor.cs:313-316, dsl-interpreter.cs:1583-1586), so the
  capabilities emitter's `is not null` guard and the help emitter's `IsNullOrEmpty` guard can no
  longer diverge — an empty string never reaches the model. Normalization-at-extraction was one
  of the two remedies round 1 suggested; applied consistently in both DSLs.
- M4: **wontfix → task 465 (acknowledged)** —
  kanban/to-do/465-escape-u0085-u2028-u2029-in-generated-string-literals.md exists and is
  committed; correct disposition since the gap is pre-existing and systemic across all
  `EscapeForStringLiteral` call sites, not specific to 464.

## Issues

### Issue 1 — Severity: suggestion
- File: source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:1575
- Description: The M2 fix widened the old `command is null` throw into an
  `IsNullOrEmpty(command)` silent skip, but `ExtractStringArgumentAt` returns null for BOTH "the
  argument is an empty literal" and "the argument is present but not a string literal". Verified
  empirically: with `const string ConstCmd = "repro-const-example";`,
  `.WithExample(ConstCmd, "NONLIT-DESC")` compiles and runs cleanly but the example silently
  vanishes from help and capabilities — no diagnostic. Pre-fix, this same code produced a loud
  NURU_S999 build error ("WithExample() requires a command string"), and `DispatchWithAlias`
  still throws for non-literal arguments today, so the interpreter now treats two adjacent
  string-argument DSL methods inconsistently. The commit comment justifies the skip as mirroring
  the attribute path, but that path is not comparable: attribute arguments are compile-time
  constants whose real values reach the extractor (a const string in `[NuruRouteExample]` works
  fine), so it only ever skips genuinely-empty commands — it never drops a valid example.
- Suggestion: Distinguish the two cases in `DispatchWithExample`: if `args.Arguments` has a
  resolvable command argument whose expression is NOT a string-literal, keep the old
  InvalidOperationException (surfaces as NURU_S999, matching WithAlias); skip silently only when
  the resolved literal is empty. E.g. resolve the ArgumentSyntax first (by name, then position)
  and branch on `expression is LiteralExpressionSyntax` before extracting the value.
- Status: open

No other new issues found in the fix delta: the `ExtractLiteralStringValue` helper is a faithful
extraction of the old switch; the XML docs on the new parameters accurately describe the
lone-named-argument behavior existing callers rely on; the new test's `.Dim()`-based assertions
genuinely discriminate the swapped from the unswapped rendering.
