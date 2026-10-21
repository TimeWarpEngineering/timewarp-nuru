# Round 2 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: changelog.md:18 and changelog.md:28
- Description: Round 1: the Unreleased **Served apps** bullet still said the socket was `$XDG_RUNTIME_DIR/nuru/<app>.sock` and that "A server from another build of the app is stopped, not used". The fix pass revises that sentence and adds a Fixed bullet for 508-017. Re-checked against `ForwardAsync`, `ShortHash` (SHA-256, 8 hex), and `StatusAsync` (other builds listed only for the default socket). The notes match the code.
- Suggestion: Add a Changed or Fixed bullet for 508-017 and revise the 508-005 sentence so it does not say a different build is stopped.
- Source: general
- Disposition notes: fixed in the implement-fix-2 pass (2026-10-21). Confirmed in round 2. No new findings on the changelog delta.

## Duplicates / conflicts

- One carried finding. Nothing to collapse.
