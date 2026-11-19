# Round 1 — general
**Date:** 2026-11-19
**Scope reviewed:** `cdbd12eb..HEAD` (generator emitters and extractors, `OptionAttribute` / `ParameterAttribute`,
NURU_A004 descriptor, sample `Order` additions, migration guide, migrate skill, routing-43 / generator-76 tests),
against `## Goal (implementer restatement)` and the original `## Description`.

## Summary

All five task items are addressed: a positional cap for routes without a catch-all, a missing-value guard
keyed on "value token taken" instead of "raw is null", default-true bools as value options, multi-value
repeated options, `Order`/`Name` read with NURU_A002 reported, and NURU_A004 for `IsRepeated` on a scalar.
The matcher/constructor local naming change for `Name` is consistent for identifier-shaped names. Recorded
gates cover build, the three touched runfiles, and the audit; the full CI suite is left to the PR, which the
contract allows. The main risk is item 2 being widened to every flag: a flag now swallows a following
positional spelled like a boolean, which is the same silent-drop class this task exists to remove. Two
doc/validation gaps follow from the new `Order`/`Name` surface.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.option-parsing.cs:51
- Description: Every plain flag now consumes a following token that `BooleanConverter.TryParse` accepts
  (`true/false/yes/no/1/0/on/off/enabled/disabled`). A positional argument that happens to have one of those
  spellings and follows a flag is taken as the flag's value. Example: `.Map("seek --verbose {pos:int?}")`,
  input `seek --verbose 0` now binds `verbose = false` (the user asked for verbose) and `pos = null`, exit 0.
  With a required positional (`repeat --quiet 1` on `repeat --quiet {count:int}`) the call becomes
  `Unknown command`. Before this change both bound correctly. The original task item 2 only asked that a
  default-true bool accept `--create false`; that is already solved by making it a value option. The
  widening to all flags is not in the task, is not tested for the positional-collision case, and the
  migration guide and skill do not warn that a boolean-looking positional after a flag is swallowed.
- Suggestion: Drop the boolean-token consumption for ordinary flags (keep `--flag false` support only on the
  default-true value option), or restrict it to `true`/`false` and document plus test the collision. If it is
  kept, add a routing test for a flag followed by a `0`/`1` positional and a "Behavior differences" entry.
- Status: open

### Issue 2 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.properties.cs:359
- Description: `ParameterSegmentName` accepts any non-blank `[Parameter(Name = ...)]` string, only trimmed and
  lowercased. The matcher declares the positional local from `ParameterDefinition.CamelCaseName` (first char
  lowered, hyphens kept; `route-matcher-emitter.unknown-options.cs:121`), while the command constructor now
  uses `ToCamelCase(SourceName)` (hyphens removed; `command-construction-emitter.cs:169`). `Name = "source-file"`
  therefore emits `string source-file = ...` in the matcher and reads `sourceFile` in construction: the
  generated file does not compile, and the user gets CS errors in generated code instead of a NURU
  diagnostic. A fluent pattern `{source-file}` is rejected up front by the route syntax diagnostic
  ("Parameter names must be valid identifiers"); the attribute path skips that check.
- Suggestion: Validate `Name` with the same identifier rule the pattern parser uses and report a diagnostic
  (reuse the existing syntax descriptor or add one), or normalize it to one identifier form used by both
  the matcher and the constructor. Add a generator test for an invalid `Name`.
- Status: open

### Issue 3 — Severity: bug
- File: documentation/user/features/endpoints.md:132
- Description: The `[Parameter]` section of the user endpoint guide still says `Order` is "inferred from
  declaration order if omitted", and its example (lines ~118–127) declares three parameters where `Env` and
  the catch-all `Args` omit `Order`. Under this change that example reports NURU_A002 twice, which is an
  error, so the guide teaches code that no longer compiles. The developer doc
  `documentation/developer/design/source-generators/endpoint-generator.md:106-111` shows the same
  two-parameter shape without `Order`.
- Suggestion: Set `Order` on every parameter in both examples and change the table row to say `Order` is
  required when a command has two or more parameters (NURU_A002), and that the catch-all is forced last.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.option-attributes.cs:193
- Description: A default-true bool is a value option with `ParameterIsOptional = false`, so a bare
  `--create` (no value token) now skips the route: `Unknown command`, exit 1. Results records this, but
  the migration guide and skill only say "`--create false` and `--create true` bind". A user porting
  `Option<bool>` with `DefaultValueFactory = _ => true` from System.CommandLine, where bare `--create` means
  true, gets `Unknown command` with no hint.
- Suggestion: Either let a bare default-true bool bind `true` (treat a missing value token as `true` for this
  option shape), or state in the concept-map row and "Behavior differences" that bare `--create` is rejected.
- Status: open

### Issue 5 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.properties.cs:272
- Description: `ExtractSegmentFromProperty(PropertyDeclarationSyntax, ...)` has no callers. The diff edited it
  (throwaway diagnostics builder, `out _` for `Order`), so it now silently discards NURU_A004 and ignores
  `Order` if anyone revives it.
- Suggestion: Delete the unused overload.
- Status: open

## Checked, no finding

- Positional cap (`EmitMaxPositionalCheck`) is applied in both the main and alias blocks and skips when a
  catch-all exists; optional parameters raise the cap by one each.
- Missing-value guard: the rewrite is equivalent to the old required/optional branches plus the new
  "present but no value token" case; `--opt=` sets `valueTaken`.
- `HasOptionGuard` now covers repeated options with a required element, so the bare repeated flag skip is
  actually emitted.
- `ReorderParametersByOrder` writes parameters back into their original segment slots, so options
  interleaved in the segment list are not moved; catch-all sorts last.
- In-repo endpoints with two or more `[Parameter]` all set `Order` (scripted scan of `samples/`, `tests/`,
  `source/`); `skills/tw-nuru/SKILL.md` already teaches `Order`.
- NURU_A004 is added to `AnalyzerReleases.Unshipped.md`.
