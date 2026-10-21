# Round 4 — general
**Date:** 2026-10-22
**Scope reviewed:** Delta `df64f004..fb21baaf` (M3 fix). `TakesValue` in `nuru-unknown-option.cs`, the value-form set and `__unknownAt` gate in `route-matcher-emitter.cs`, and the four new routing-37 tests. Re-checked M1–M3 on that delta. The implementer restatement matches the original task (no misread).

## Summary

The no-literal shape check drops the argv token after an unknown option only when `NuruUnknownOption.TakesValue` says the closest declared form is a value-taking option and the token has no inline `=value`. A flag typo leaves the next word positional, so the route no longer looks like a match. `routing-37` is 30 of 30 passed. No new defect on this delta.

## Resolved prior

M3 is fixed on `fb21baaf`. `Map("--check-updates")` with `--check-update extra`, `Map("{file} --check-updates")` with `readme --check-update extra`, and `Map("{file} --output {output} --force")` with `readme --forc extra` each exit 1 with `Unknown command` and no `Did you mean`. The handler does not run. `readme --outpt out` on `{file} --output {output} --force` still writes `Unknown option '--outpt'. Did you mean '--output'?`.

M2 still holds: `--check-update` on `--check-updates`, and `readme --outpt out` on `{file} --output {output}`, suggest the declared form. A missing `--count` value stays `Unknown command`. M1 still holds: `commit --ammend` and `kanban create Title --foldr x` are in the same 30 passing tests.

`TakesValue` uses the same `Suggest` as the error line, then checks that form against the route's non-flag forms (`!option.IsFlag`). A flag-only route has an empty value-form set and never drops the next token. An inline `=value` does not drop the following token. The drop still runs only for a route with no leading literal (`trackUnknownValues`), which is the path M3 named.

## Issues

None.
