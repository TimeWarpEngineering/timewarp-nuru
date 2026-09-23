# Round 1 — merged findings
**Date:** 2026-09-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-mcp/services/github-cache-service.cs:122
- Description: Percent-encoded `..` (`%2e%2e` / `%2E%2E`) bypasses the literal `..` check. Uri normalization can leave the allowlist tree (e.g. `samples/%2E%2E/x` → `.../master/x`) while still satisfying the org/repo AbsolutePath prefix.
- Suggestion: Assert resolved AbsolutePath remains under `/TimeWarpEngineering/timewarp-nuru/master/` + matched allowlist prefix; reject `%` or unescape+recheck; add regression test.
- Source: general
- Disposition notes: Fixed — reject `%` fail-closed; assert AbsolutePath under `master/<matchedPrefix>`; regression `Should_reject_percent_encoded_dotdot_leaving_allowlist` in mcp-08.

## Duplicates / conflicts

- None (single reviewer).
