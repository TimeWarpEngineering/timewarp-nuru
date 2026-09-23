# Round 2 — merged findings
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
- File: source/timewarp-nuru-mcp/services/github-cache-service.cs:117-171
- Description: Percent-encoded `..` bypassed literal checks and could leave allowlist prefixes after Uri normalization.
- Suggestion: Reject `%`; assert AbsolutePath under `master/<matchedPrefix>`; regression test.
- Source: general (round 1); re-verified round 2
- Disposition notes: Confirmed fixed; mcp-08 `Should_reject_percent_encoded_dotdot_leaving_allowlist` passes.

## Resolved prior

- M1 carried from round 1; no reopen.

## Duplicates / conflicts

- None.
