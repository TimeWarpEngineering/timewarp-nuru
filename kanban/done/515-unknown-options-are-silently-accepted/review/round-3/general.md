# Round 3 — general
**Date:** 2026-10-21
**Scope reviewed:** Delta `d32f985f..0ef92207` (M2 fix). Shape check in `route-matcher-emitter.cs`, the four new routing-37 tests, and the changelog sentence. Re-checked M2 against that diff and the goal. The implementer restatement matches the original checklist.

## Summary

The empty-prefix route now records an unknown option before the option-guard skip, when the positional args fit the route. The two round-2 cases suggest, the handler does not run, and `routing-37` is 26 of 26 passed. The fit check also drops the token after the unknown option. On a flag, that token is not a value, so a stray word makes a non-matching route suggest its flag.

## Resolved prior

M2 is fixed on `0ef92207`. `Map("--check-updates")` with `--check-update` writes `Unknown option '--check-update'. Did you mean '--check-updates'?`. `Map("{file} --output {output}")` with `readme --outpt out` writes `Unknown option '--outpt'. Did you mean '--output'?`. Neither handler runs. A missing `--count` value on `{file} --count {count:int}` stays `Unknown command`. The optional twin `{file} --output? {output?}` suggests. M1 still holds: `routing-37` includes `commit --ammend` and `kanban create Title --foldr x`.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:577
- Description: `EmitUnknownOptionShapeCheck` accepts the positional list with the argv token after each unknown option removed (`__positionalNoValues`, built at line 518). That token is removed for every route, including a flag, which takes no value. The route then looks like it matches, and the no-match path suggests the flag. The corrected argv still has the extra word, so the route would not match. Probed: `Map("--check-updates")` with `--check-update extra` exits 1 with `Unknown option '--check-update'. Did you mean '--check-updates'?`, and the handler does not run. `Map("{file} --check-updates")` with `readme --check-update extra` does the same. `Map("{file} --output {output} --force")` with `readme --forc extra` suggests `--force`. A real extra positional that is not the immediate next token stays `Unknown command` (`readme --outpt out extra`, and `readme extra --outpt`). `readme --outpt out` on `{file} --output {output}` still suggests `--output`.
- Suggestion: Treat the following token as a possible option value only when a declared value-taking option is close to the typed name. A flag-only route must not drop that word. `--check-update extra` and `readme --check-update extra` should exit 1 with `Unknown command` and should not suggest the flag. `readme --forc extra` on a route that also has a value option should stay `Unknown command` when `--force` is the close form. `readme --outpt out` must still suggest `--output`.
- Status: open
