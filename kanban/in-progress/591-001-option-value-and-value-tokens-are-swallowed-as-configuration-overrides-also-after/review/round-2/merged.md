# Round 2 — merged findings
**Date:** 2026-11-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/configuration-emitter.cs:106
- Description: Round 1: with `AddConfiguration()`, arguments before `--` still went to `AddCommandLine`, so a declared `--name:value` was read as a key that took the next argument. Round 2: `AddCommandLine` reads only `__configArgList`, the tokens `RouteTokenOrConfig` drops. A declared `--format:json` stays on the route. `Should_keep_override_after_declared_colon_option_with_configuration` covers `report --format:json --Database:Host=prod`.
- Suggestion: None. Fixed as suggested.
- Source: general
- Disposition notes: Fixed in `587436ba`. Re-verified on the delta. No new hole on this path.

### M2 — Severity: suggestion — Status: fixed
- File: documentation/user/guides/migrating-from-system-commandline.md:235
- Description: Round 1: the guide qualified slash `=` / `:` forms with "whose name is not a long option" and said short options with `=` are not bound. Round 2: the guide and the skill say every slash form is configuration, and that `-o=value` binds a declared short value option. The guide also says `-o:value` does not bind, which matches the matcher.
- Suggestion: None. Fixed as suggested.
- Source: general
- Disposition notes: Fixed in `587436ba`. Re-verified against `RouteTokenOrConfig` and the `-{ShortForm}=` matcher branch.

## Duplicates / conflicts

- Single reviewer. Prior M1 and M2 carried forward. No new findings.
