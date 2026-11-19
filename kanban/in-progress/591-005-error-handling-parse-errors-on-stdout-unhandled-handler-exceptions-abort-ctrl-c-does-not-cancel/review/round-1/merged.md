# Round 1 — merged findings
**Date:** 2026-11-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: suggestion — Status: wontfix
- File: documentation/user/features/served-apps.md:101 (behavior: source/timewarp-nuru/invocation/nuru-app.cli-run.cs:119-148, interceptor-emitter.builtin-routes.cs:149)
- Description: A foreground `serve` (or a detached server that gets SIGTERM) now also arms the CLI `ProcessTerminationTimeout`. A call that ignores its cancelled token for more than two seconds can be cut off, and the socket file can be left behind. served-apps.md still says the server removes the socket file after Ctrl+C or SIGTERM.
- Suggestion: One sentence in served-apps.md Lifetime, or have the serve route opt out of the CLI timeout.
- Source: general
- Disposition notes: wontfix (review oracle, 2026-11-19). This is the System.CommandLine graceful-shutdown contract the task adopts. Every daemon call gets a token linked to the stop, so a call that honors it finishes inside the window. The next start deletes a stale socket under the server lock, so nothing breaks. Not worth a fix round. It can go into a served-apps doc pass later.

### M2 — Severity: nit — Status: wontfix
- File: source/timewarp-nuru/invocation/nuru-app.cli-run.cs:106
- Description: `TryArm` reads `delayCancellation.Token` from a signal thread. That can throw `ObjectDisposedException` if the signal lands exactly while the run's `finally` disposes the source (line 147).
- Suggestion: Capture the delay token once before the handlers are registered.
- Source: general
- Disposition notes: wontfix (review oracle, 2026-11-19). The window is a Ctrl+C at the instant the handler finishes. The exception lands in the signal dispatch task after the exit code is decided, and it has no visible effect. A nit does not justify a fix round.

## Duplicates / conflicts

- None (single reviewer).
