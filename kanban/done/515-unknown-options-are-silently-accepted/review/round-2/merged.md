# Round 2 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:193
- Description: Re-checked on `72cf0f08`. A missing required option, a missing required value, or an absent unbound discriminator flag no longer skips inside option parsing. On a route with a leading literal, the prefix check records the typo before the guard skip. `commit --ammend` writes `Unknown option '--ammend'. Did you mean '--amend'?` and `kanban create Title --foldr x` writes `Unknown option '--foldr'. Did you mean '--folder'?`. Neither handler runs. The alias guard jumps to `alias_skip_...`, which is after the alias block. `routing-37` passed 22 of 22, including `Should_reject_unknown_option_on_alias_with_failed_option_guard` (it returned; it did not hang).
- Suggestion: none remaining for routes with a leading literal.
- Source: general
- Disposition notes: Fixed in `72cf0f08`. Verified by `routing-37` and a direct probe.

### M2 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:193
- Description: The new guard skip is before `EmitUnknownOptionMatchCheck` (line 270). `EmitUnknownOptionPrefixCheck` does nothing when the route has no leading literal, so a typo of a required option or unbound flag on that shape is never recorded. The call exits 1 as `Unknown command` and does not suggest the declared form. Probed: `--check-update` on `Map("--check-updates")`, and `readme --outpt out` on `Map("{file} --output {output}")`. The optional twin and the leading-literal twin of the same option do suggest. `EmitAliasMatch` repeats the order at line 365; a non-empty alias still uses the prefix check.
- Suggestion: Record the unknown option for an empty prefix once the rest of the route matches, then skip on the guard, then emit option conversions. Add the two probed cases. An extra positional stays `Unknown command`. A missing value still skips instead of raising a conversion error.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single reviewer. M1 is the round-1 finding, now fixed for leading-literal routes. M2 is the empty-prefix remainder of that skip.
