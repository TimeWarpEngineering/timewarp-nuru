# Round 1 — general
**Date:** 2026-08-13
**Scope reviewed:** Full diff of commit a1ae675d (22 files) plus surrounding unchanged code in
route-help-emitter.cs, capabilities-emitter.cs, dsl-interpreter.cs (DispatchWithDescription /
DispatchWithAlias / exception-to-NURU_S999 handling), emitter-string-utils.cs, equatable-array.cs,
capabilities-json-serializer-context.cs, endpoint-extractor.cs, and
generator-37-incrementality-caching.cs. Ran both new test runfiles: help-08-route-examples (7/7
pass) and capabilities-06-examples (4/4 pass). Empirically verified the named-argument behavior of
`.WithExample()` with a temporary in-repo runfile (deleted after; working tree left clean).
Verified docs snippets against the actual API surface.

## Summary
Solid implementation that follows the plan closely: escaping is correct for the realistic threat
class (quotes, braces, backslashes, \n/\r/\t — pinned by the special-chars test), EquatableArray
`default` and `[]` compare equal and hash identically so `Examples = default` vs `[]` cannot cause
cache instability, two routes differing only in examples compare unequal (record + sequence
equality) so no stale-cache risk, the capabilities `examples` key is genuinely absent (not null/[])
for unannotated routes with `Examples` declared last so key order is unchanged, serializer context
registrations are complete for AOT, and the Examples help section renders correctly even for
routes with no Options/Parameters (it sits outside the `if (options.Any())` block). One verified
bug: out-of-order named arguments on `.WithExample()` silently swap command and description in the
generated output.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:1787
- Description: `ExtractStringArgumentAt` indexes `ArgumentList.Arguments` purely positionally and
  never inspects `NameColon`. `.WithExample()` is the first two-string-parameter DSL method, so
  valid C# with out-of-order named arguments compiles clean and silently produces swapped output.
  Verified empirically: `.WithExample(description: "THE-DESCRIPTION", command: "THE-COMMAND")`
  renders

  ```
  Examples:
    THE-DESCRIPTION
      THE-COMMAND   <- dimmed
  ```

  i.e. the description becomes the command line and vice versa, in both help and capabilities.
  Pre-existing single-string callers (`WithDescription`, `WithAlias`, `WithName`) are unaffected
  behaviorally by the refactor (old `ExtractStringArgument` semantics preserved exactly), but they
  never had a swappable second argument.
- Suggestion: In `ExtractStringArgumentAt` (or in `DispatchWithExample`), resolve arguments by
  `NameColon` when present — e.g. accept a parameter name alongside the index and prefer the
  argument whose `NameColon.Name.Identifier.Text` matches, falling back to position for unnamed
  arguments. Alternatively (cheaper), have `DispatchWithExample` throw the existing
  InvalidOperationException (surfaces as NURU_S999) when any argument carries a `NameColon` whose
  name does not match its positional slot, converting silent wrong output into a build error.
- Status: open

### Issue 2 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:1571
- Description: Empty-command handling is inconsistent between the two DSLs. The attribute path
  silently skips `[NuruRouteExample("")]` (endpoint-extractor.cs:299, `string.IsNullOrEmpty`), but
  the fluent path only checks `command is null`, so `.WithExample("")` flows through and renders a
  blank two-space line under `Examples:` (and `Command = ""` in capabilities JSON). The plan's
  resolved fork says empty commands are silently skipped.
- Suggestion: In `DispatchWithExample`, treat an empty-string literal like the attribute path does
  (skip, i.e. `return routeBuilder;` without adding) — or check `string.IsNullOrEmpty(command)`
  before the throw/add decision so both DSLs agree.
- Status: open

### Issue 3 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/emitters/capabilities-emitter.cs:365
- Description: Empty-string descriptions diverge between surfaces: route-help-emitter.cs:209
  guards with `!string.IsNullOrEmpty(example.Description)` (skips the dim line), while the
  capabilities emitter guards with `is not null`, so `[NuruRouteExample("x", Description = "")]`
  emits `"description": ""` in JSON but nothing in help. Harmless for well-formed input, but the
  two emitters read the same model and should apply the same emptiness rule.
- Suggestion: Use `!string.IsNullOrEmpty(...)` in `EmitExampleCapability` too (or normalize empty
  descriptions to null at extraction time in both EndpointExtractor and DispatchWithExample).
- Status: open

### Issue 4 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/emitters/emitter-string-utils.cs:16
- Description: Pre-existing (shared helper, not introduced by this diff, but this diff extends its
  use to two new user-supplied strings): `EscapeForStringLiteral` handles `\\ " \n \r \t` but not
  the other characters the C# lexer treats as new-lines inside a string literal — U+0085 (NEL),
  U+2028 (LS), U+2029 (PS). A user writing `[NuruRouteExample("a\u2028b")]` (the U+2028 written as a unicode escape in their own source) gets that character
  verbatim from Roslyn's `ValueText`, and re-emitting it unescaped produces an invalid generated
  string literal (CS1010/CS1003 in generated code). Same exposure already exists for every route,
  parameter, and option description, so this is a systemic follow-up, not a blocker for 464.
- Suggestion: Extend `EscapeForStringLiteral` to escape U+0085/U+2028/U+2029 (emit them as `\u0085` / `\u2028` / `\u2029` escape sequences) in a small follow-up task covering all call sites at once.
- Status: open

## Verification notes (claims checked, no issue found)

- **Escaping / .Dim() placement:** Generated help lines are plain (non-interpolated) literals, so
  braces need no escaping; the special-chars test pins quotes, braces, and backslashes. `.Dim()`
  wraps only the description literal (`"    " + "desc".Dim()`), so the indent stays un-styled and
  ANSI codes cannot corrupt the literal itself.
- **EquatableArray:** `default` and `Empty` are `SequenceEqual` (both empty spans) and both hash
  to 0, so `Examples = default` vs `[]` cannot flip record equality; `HasExamples` handles both
  via `IsDefaultOrEmpty`. Routes differing only in examples compare unequal (ExampleDefinition is
  a record of two strings), so the emit stage re-runs — no stale cache. generator-37's app source
  contains no examples, so it doesn't exercise this path, but ExampleDefinition carries no
  Location/span data, so there is nothing cache-unstable to guard.
- **EndpointExtractor:** `GetAttributes()` preserves declaration order; accumulation (not
  first-match) is correct for AllowMultiple; null/non-string/empty ctor args are skipped;
  `Description` is read from NamedArguments only (matching the attribute shape — it is a property,
  so named-arg is the only spelling); simple-name matching (`NuruRouteExample` /
  `NuruRouteExampleAttribute`) follows the existing NuruRoute/NuruRouteAlias precedent.
- **DslInterpreter refactor:** `ExtractStringArgument` → `ExtractStringArgumentAt(0)` is
  behavior-identical for existing callers (`Count == 0` vs `Count <= 0`, same literal-only
  switch). Non-literal command arg → InvalidOperationException → caught at the invocation
  processor (dsl-interpreter.cs:241) → NURU_S999 diagnostic (matches WithAlias). Non-literal
  description silently drops to null — consistent with WithDescription's `description ?? ""`
  precedent.
- **Capabilities JSON:** `WhenWritingNull` + camelCase confirmed on the serializer context;
  `Examples` is the last declared property on EndpointCapability so STJ source-gen key order for
  pre-existing keys is unchanged; both `ExampleCapability` and `IReadOnlyList<ExampleCapability>`
  are registered for AOT; trailing-comma logic in the emitter keeps generated code byte-identical
  when no examples exist. The omission test asserts key absence via JsonDocument, which would
  catch an `[]`/`null` regression.
- **Help placement:** Examples block sits after and outside `if (options.Any())`, so it renders
  for routes with no options and no parameters; app-level help-emitter.cs and group help are
  untouched by the diff.
- **Fluent group routes:** GroupEndpointBuilder.WithExample flows through the same
  IIrRouteBuilder dispatch; pinned by Should_show_examples_via_fluent_in_group (passes).
- **Tests:** Both runfiles executable (mode 100755), unique `h08-`/`cap06-` literals, Jaribu
  conventions (TestTag, ModuleInitializer, JARIBU_MULTI guard) match sibling tests. They would
  catch an escaping regression (special-chars test), an omission regression (JsonDocument key
  check), and an ordering regression (Options-before-Examples index comparison).
- **Docs:** Attribute and fluent snippets match the real API (`Description` named property,
  `.WithExample(command, description = null)` before `.Done()`); the auto-help.md cross-reference
  to the pre-existing "Examples:" sample under Generated Help Output is now accurate. Minor
  observation, no issue filed: the pre-existing "Include Examples" best-practice section
  (auto-help.md:251) still advises embedding example-ish text in descriptions and could point at
  `[NuruRouteExample]` in a doc follow-up.
