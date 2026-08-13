# Round 2 — merged findings
**Date:** 2026-08-13
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 2 | 1 |

## Prior findings (carried from round 1)

- M1: verified-fixed — name-preferred argument resolution correct for all legal argument shapes;
  single-string callers provably behavior-identical; new named-args test discriminates the swap
- M2: verified-fixed — empty-string command skips silently, matching attribute path (refined by
  M5 fix to only skip empty literals, not non-literal expressions)
- M3: verified-fixed — empty descriptions normalized to null at extraction in both DSL paths
- M4: wontfix → task 465 (acknowledged; task exists and is committed)

## Issues

### M5 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:1575
- Description: The M2 skip conflates "empty literal" with "not a string literal":
  `.WithExample(ConstCmd, ...)` with a const-string command now silently drops the example,
  where pre-fix code raised NURU_S999 and `WithAlias` still errors today. Attribute path is not
  comparable — attribute args deliver real constant values, so it never drops a valid example.
- Suggestion: Resolve the command ArgumentSyntax first (by name, then position) and branch:
  non-literal expression → keep the InvalidOperationException (NURU_S999); empty literal →
  silent skip.
- Source: general
- Disposition notes: Fixed as suggested — `DispatchWithExample` now resolves the command
  `ArgumentSyntax` via a new `ResolveArgumentAt` helper (name-preferred, falls back to position;
  `ExtractStringArgumentAt` now delegates to it, so other callers are unaffected) and branches:
  argument missing → throw (existing message); literal and empty → silent skip (M2 preserved);
  present but not a string literal → throw `InvalidOperationException("...requires a command
  string literal...")`, surfacing as NURU_S999, consistent with WithAlias. Description keeps its
  existing lenient (non-literal → null) behavior per WithDescription's precedent.
  While writing the regression test (generator-40), discovered and fixed a **real, more severe
  latent bug** this finding's premise depended on: `AppExtractor.ExtractFromBuildCall`'s "no
  model produced" fallback returned `ExtractionResult.Empty`, discarding every diagnostic
  collected before an uncaught exception aborted a Build() chain (NURU_S999 here, but also any
  H005/etc. from earlier routes in the same chain) — so pre-fix, a non-literal `.WithExample()`
  command compiled with **zero warnings/errors** and only surfaced at runtime as an opaque
  `InvalidOperationException: RunAsync was not intercepted`. Confirmed via a real `dotnet build`
  repro (not just the Roslyn-hosted harness) before and after. Fixed by returning
  `ExtractionResult.Failure(result.Diagnostics)` instead — `CreateGeneratorModelWithValidation`
  (nuru-generator.cs) already collects and reports diagnostics from null-Model results, so this
  was the one broken link; safe because `BuildLocator.IsConfirmedBuildCall` has already
  semantically confirmed a genuine `NuruApp.Build()` call by the time this fallback is reached
  (never hit for non-Nuru code). generator-40 now asserts both NURU_S999 (WithExample) and
  NURU_H005 (sibling route, locally caught) survive together, mirroring generator-31's pattern.

## Duplicates / conflicts

- None — single reviewer.
