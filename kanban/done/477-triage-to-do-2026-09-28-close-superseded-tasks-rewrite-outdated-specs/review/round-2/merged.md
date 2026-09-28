# Round 2 — merged findings
**Date:** 2026-09-28
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: nit — Status: fixed
- File: kanban/to-do/456-check-version-warn-when-source-is-multiple-versions-ahead-of-last-release.md:46
- Description: The triage note pointed below the update for requirements that sit above it.
- Suggestion: Point at the requirements above.
- Source: general (round 1); re-verified round 2
- Disposition notes: Confirmed fixed. Line 46 says the requirements above match the single NuGet methodology.

### M2 — Severity: nit — Status: fixed
- File: kanban/to-do/069-global-user-key-binding-profiles.md:15
- Description: The path correction named obsolete `AddReplOptions` as a current enablement API.
- Suggestion: Name the non-obsolete `AddRepl` overloads and mark `AddReplOptions` obsolete.
- Source: general (round 1); re-verified round 2
- Disposition notes: Confirmed fixed. Line 15 matches the `[Obsolete]` message on `AddReplOptions`.

## Resolved prior

- M1 and M2 carried from round 1; no reopen. No new findings.

## Duplicates / conflicts

- None.
