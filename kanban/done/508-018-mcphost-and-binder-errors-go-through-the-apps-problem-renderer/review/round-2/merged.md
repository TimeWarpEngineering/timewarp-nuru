# Round 2 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:1690
- Description: `EmitJsonMemberAfterMatch` writes both JSON-member argv errors through `WriteBindErrorAsync` (`InvalidValue` for the rejected flag, `MissingValue` for the omitted required member). Without the opt-in, the plain `Error:` line stays on stderr. The opted-in argv case is covered in `routing-36-problem-errors.cs`.
- Suggestion: None. The round 1 suggestion is in the tree.
- Source: general
- Disposition notes: Fixed in `c9dbc5c3`. Confirmed by reading the emitter and by `Should_render_argv_errors_for_a_json_member_when_opted_in` (routing-36, 8/8 on 2026-10-21).

### M2 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru/nuru-app.cs:306
- Description: `WriteArgumentError` takes a kind. The FileInfo and DirectoryInfo missing-option sites pass `MissingValue`. The two-argument form still means `InvalidValue`.
- Suggestion: None. The round 1 suggestion is in the tree.
- Source: general
- Disposition notes: Fixed in `c9dbc5c3`. Emitter sites at `route-matcher-emitter.cs` lines 1444 and 1485. Helper checked by `Should_render_an_argument_error_with_its_kind`.

### M3 — Severity: nit — Status: open
- File: documentation/user/features/neutral-contracts.md:152
- Description: The opt-in sentence lost "and" between the `--json-args` clause and "an argv value that does not convert". The cases and the `MissingValue` sentence are still stated.
- Suggestion: Restore "and" after the closing parenthesis.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- One reviewer. M1 and M2 are the round 1 ids, now fixed. M3 is new on this delta.
