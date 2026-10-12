# Round 1 — merged findings
**Date:** 2026-10-12
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: suggestion — Status: wontfix
- File: source/timewarp-nuru/repl/repl-session.cs:286
- Description: broad `InvalidOperationException` catch around the key reader can mask a non-console reader bug.
- Suggestion: log the exception or narrow the catch to `ReadKey`.
- Source: general
- Disposition notes: wontfix (orchestrator). The task requires that no exception escape `RunReplAsync`;
  the failure is still surfaced (stderr line + exit 1), which is strictly better than the prior crash.
  Narrowing requires restructuring `ReplConsoleReader`, out of scope for a launch-gate fix.

### M2 — Severity: nit — Status: wontfix
- File: source/timewarp-nuru/repl/repl-session.cs:296
- Description: prompt still printed per piped line.
- Suggestion: suppress the prompt when redirected.
- Source: general
- Disposition notes: wontfix (orchestrator). Matches the task's documented expected output and keeps
  transcripts readable; a behaviour change here belongs in its own task if wanted.

## Duplicates / conflicts

- none
