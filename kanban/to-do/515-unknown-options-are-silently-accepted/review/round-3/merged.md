# Round 3 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 2 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:193
- Description: Re-checked on `0ef92207` through `routing-37`. A close typo of a required option or unbound flag on a route with a leading literal is still suggested. `Should_suggest_required_value_option_for_close_typo` and `Should_suggest_discriminator_flag_for_close_typo` passed. The alias guard test returned.
- Suggestion: none remaining.
- Source: general
- Disposition notes: Fixed in `72cf0f08`. Still passing in `routing-37` (26 of 26) on `0ef92207`.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:199
- Description: Re-checked on `0ef92207`. A route with no leading literal records the unknown option in `EmitUnknownOptionShapeCheck` before `EmitOptionGuardSkip`. `Should_suggest_discriminator_flag_on_route_without_literal` expects `Unknown option '--check-update'. Did you mean '--check-updates'?`. `Should_suggest_required_value_option_on_route_without_literal` expects `Unknown option '--outpt'. Did you mean '--output'?`. The optional twin suggests. A missing value stays `Unknown command`. Handlers do not run. `routing-37` passed 26 of 26.
- Suggestion: none remaining for those probed cases.
- Source: general
- Disposition notes: Fixed in `0ef92207`. Verified by `routing-37` and the generated matcher for those two routes.

### M3 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:577
- Description: The shape check also accepts the positional list with the next argv token removed. That drop runs for every unknown option, including a flag. A stray word then counts as the option's value, so a route that would not match suggests its flag. Probed: `--check-update extra` on `Map("--check-updates")`, and `readme --check-update extra` on `Map("{file} --check-updates")`, both exit 1 with `Did you mean '--check-updates'?`. `readme --forc extra` on `Map("{file} --output {output} --force")` suggests `--force`. The handler does not run. `readme --outpt out extra` and `readme extra --outpt` stay `Unknown command`. `readme --outpt out` still suggests `--output`.
- Suggestion: Drop the following token only when a declared value-taking option is close to the typed name. The three flag cases above should exit 1 with `Unknown command` and no suggestion. `readme --outpt out` must still suggest `--output`.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single reviewer. M1 and M2 stay fixed. M3 is the value-token heuristic on the M2 shape check, applied to flags.
