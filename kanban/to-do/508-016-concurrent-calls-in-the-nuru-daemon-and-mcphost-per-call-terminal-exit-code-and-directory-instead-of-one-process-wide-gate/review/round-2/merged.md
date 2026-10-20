# Round 2 — merged findings
**Date:** 2026-10-20
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru/nuru-app.cs:226
- Description: Invoke-then-render used to drop the handler's `NuruCallContext.CurrentExitCode` and the call's working directory. Render now continues that call: the context passed to `RenderAsync` is seeded with the invoke exit code, directory, console capture, and exclusive hold, and the rendered code is written back. Generated route execution does not reset `CurrentExitCode` when rendering, so problem details still overwrite the handler's code. Daemon invoke and McpHost (in-process and forwarded) use this overload.
- Suggestion: none remaining
- Source: general (round 1); re-verified round 2
- Disposition notes: Fixed in `f2b2bccc`. Round 2 re-ran `routing-35` (7 passed), `daemon-04` (6 passed), and `mcphost-03` (6 passed) on 2026-10-20. No new findings on the fix delta.

## Duplicates / conflicts

- Single reviewer. M1 is round-1 general issue 1, carried forward. No new issues to merge.
