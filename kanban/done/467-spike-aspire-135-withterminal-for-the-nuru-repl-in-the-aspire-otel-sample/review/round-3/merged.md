# Round 3 — merged findings
**Date:** 2026-09-02
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 4 | 0 |
| suggestion | 0 | 4 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: samples/aspire-otel/overview.md:10
- Description: Stale `_aspire-host-otel` run path.
- Suggestion: Use `aspire-otel`.
- Source: general
- Disposition notes: Confirmed-fixed in round 3.

### M2 — Severity: bug — Status: fixed
- File: samples/aspire-otel/overview.md:110
- Description: Standalone extra `--` skipped REPL.
- Suggestion: `./nuru-client.cs --interactive`.
- Source: general
- Disposition notes: Confirmed-fixed in round 3.

### M3 — Severity: suggestion — Status: fixed
- File: samples/aspire-otel/overview.md:24
- Description: Stale launch-profile shebang / unqualified `nuru-repl-client`.
- Suggestion: Align with real shebang; qualify launchSettings.
- Source: general
- Disposition notes: Confirmed-fixed in round 3.

### M4 — Severity: suggestion — Status: fixed
- File: samples/aspire-otel/overview.md:12
- Description: Invented `.aspire/settings.json` locator.
- Suggestion: Drop the claim.
- Source: general
- Disposition notes: Confirmed-fixed in round 3.

### M5 — Severity: suggestion — Status: fixed
- File: samples/aspire-otel/readme.md:10
- Description: `terminalCommandsEnabled` overstated as mandatory before run.
- Suggestion: Split dashboard vs CLI attach.
- Source: general
- Disposition notes: Confirmed-fixed in round 3.

### M6 — Severity: suggestion — Status: fixed
- File: samples/aspire-otel/nuru-client.cs:47
- Description: Restating AppHost `--interactive` comment.
- Suggestion: Drop it.
- Source: general
- Disposition notes: Confirmed-fixed in round 3.

### M7 — Severity: bug — Status: fixed
- File: samples/aspire-otel/overview.md:16
- Description: File-Based Apps intro still used bare `./nuru-client.cs`.
- Suggestion: `./nuru-client.cs --interactive`.
- Source: general
- Disposition notes: Confirmed-fixed in round 3.

### M8 — Severity: bug — Status: fixed
- File: samples/aspire-otel/readme.md:9-19
- Description: After `cd samples/aspire-otel`, standalone still used a repo-root path.
- Suggestion: `./nuru-client.cs greet Alice`.
- Source: general
- Disposition notes: Confirmed-fixed in round 3.

## Duplicates / conflicts

- None new. Prior M1–M8 carried forward; all confirmed-fixed. Zero new findings.
