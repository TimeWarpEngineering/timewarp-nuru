# Round 2 — merged findings
**Date:** 2026-09-23
**Sources:** general
**Scope:** commit 8cd05197 fix delta vs round-1 ledger

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 1 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-devcli/content/any/services/nuget-version-service.cs:164
- Re-verified: null body, `{}`, `{"versions":[]}`, `{"versions":null}` all throw `HttpRequestException`. Unlisted versions remain in the flat-container index, so no false positive for a published package.
- Source: general

### M2 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru-devcli/content/any/services/nuget-version-service.cs:154, :179
- Re-verified: `JsonException` wrapped with status 200; `SendAsync` filter wraps HttpClient timeout (caller token untouched) and lets caller cancellation propagate. Both endpoint catch blocks cover every lookup failure.
- Source: general

### M3 — Severity: suggestion — Status: wontfix
- File: source/timewarp-nuru-devcli/content/any/endpoints/release-command.cs:303
- Re-verified: round-1 rationale accepted by round-2 reviewer (release step 7 gated by git-state preconditions; shared service contract now tested directly; residual untested delta is the validation call and the catch).
- Source: general

### M4 — Severity: nit — Status: fixed
- File: tests/timewarp-nuru-tests/devcli/check-version-07-endpoint-fail-closed.cs
- Re-verified: both tests reset `Environment.ExitCode = 0` before running the handler; original value restored in `finally`.
- Source: general

## New findings

None. Considered and not raised: `{"versions":[null]}` reaches `CompareVersions` and fails with `ArgumentNullException` (still non-zero exit, no realistic producer).

## Duplicates / conflicts

- None; single reviewer.
