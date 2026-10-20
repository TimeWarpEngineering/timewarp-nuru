# Round 3 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:1690
- Description: `EmitJsonMemberAfterMatch` writes both JSON-member argv errors through `WriteBindErrorAsync`. Confirmed in round 2. This delta does not touch that code.
- Suggestion: None.
- Source: general
- Disposition notes: Fixed in `c9dbc5c3`. Still fixed. Round 3 product delta is the docs comma only.

### M2 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru/nuru-app.cs:306
- Description: `WriteArgumentError` takes a kind. Missing-option sites pass `MissingValue`. Confirmed in round 2. This delta does not touch that code.
- Suggestion: None.
- Source: general
- Disposition notes: Fixed in `c9dbc5c3`. Still fixed.

### M3 — Severity: nit — Status: fixed
- File: documentation/user/features/neutral-contracts.md:152
- Description: The opt-in sentence is a three-item list. A comma follows the `--json-args` parenthesis, and "and" introduces the JSON-member case.
- Suggestion: None. The round 2 punctuation gap is closed. A comma before the second item is the list join; "and" stays before the last item.
- Source: general
- Disposition notes: Fixed in `b71d5d68`. Read on 2026-10-21 at `neutral-contracts.md:152`.

## Duplicates / conflicts

- One reviewer. M1, M2, and M3 keep their ids. No new findings on this delta.
