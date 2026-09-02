# Round 2 — merged findings
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
- Description: Run instructions still said `cd samples/_aspire-host-otel`.
- Suggestion: Replace with `aspire-otel`.
- Source: general
- Disposition notes: Confirmed-fixed in round 2. No `_aspire-host-otel` remains.

### M2 — Severity: bug — Status: fixed
- File: samples/aspire-otel/overview.md:110
- Description: Standalone `./nuru-client.cs -- --interactive` would not enter REPL.
- Suggestion: `./nuru-client.cs --interactive`; keep AppHost `-- --interactive`.
- Source: general
- Disposition notes: Confirmed-fixed in round 2 for Step 2. Related leftover at File-Based Apps intro is M7.

### M3 — Severity: suggestion — Status: fixed
- File: samples/aspire-otel/overview.md:24
- Description: Stale launch-profile shebang claims and unqualified `nuru-repl-client`.
- Suggestion: Align with real `dotnet --` shebang; qualify launchSettings / resource name.
- Source: general
- Disposition notes: Confirmed-fixed in round 2.

### M4 — Severity: suggestion — Status: fixed
- File: samples/aspire-otel/overview.md:12
- Description: Invented `.aspire/settings.json` locator.
- Suggestion: Drop the claim.
- Source: general
- Disposition notes: Confirmed-fixed in round 2.

### M5 — Severity: suggestion — Status: fixed
- File: samples/aspire-otel/readme.md:10
- Description: `terminalCommandsEnabled` overstated as mandatory before `aspire run`.
- Suggestion: Split dashboard vs CLI attach.
- Source: general
- Disposition notes: Confirmed-fixed in round 2. Related cwd/path leftover in the same readme block is M8.

### M6 — Severity: suggestion — Status: fixed
- File: samples/aspire-otel/nuru-client.cs:47
- Description: Restating AppHost `--interactive` comment.
- Suggestion: Drop it.
- Source: general
- Disposition notes: Confirmed-fixed in round 2.

### M7 — Severity: bug — Status: fixed
- File: samples/aspire-otel/overview.md:16
- Description: File-Based Apps intro still documents `./nuru-client.cs` with no args as the standalone run. The sample does not set `AutoStartWhenEmpty`, and the REPL gate only accepts exact `["--interactive"]` or `["-i"]`, so this never enters REPL. Step 2 correctly uses `--interactive`; the intro contradicts it.
- Suggestion: Change to `./nuru-client.cs --interactive` (or `-i`), matching Step 2.
- Source: general
- Disposition notes: Intro standalone is now `./nuru-client.cs --interactive`.

### M8 — Severity: bug — Status: fixed
- File: samples/aspire-otel/readme.md:9-19
- Description: The Run It block now starts with `cd samples/aspire-otel` then later runs `dotnet run samples/aspire-otel/nuru-client.cs -- greet Alice`. After the `cd`, that path is nested and fails. Pre-fix the block was repo-root relative; the M5 rewrite added `cd` without updating the standalone path.
- Suggestion: After the `cd`, use `./nuru-client.cs greet Alice` (or `dotnet run ./nuru-client.cs -- greet Alice`). Alternatively drop the `cd` and keep all commands repo-root relative.
- Source: general
- Disposition notes: After `cd samples/aspire-otel`, standalone is `./nuru-client.cs greet Alice`.

## Duplicates / conflicts

- M7 is a leftover of M2’s class of defect (bare vs `--interactive` standalone), in a different block. Kept as a new ID.
- M8 is a leftover of the M5 rewrite (cwd vs path). Kept as a new ID.
