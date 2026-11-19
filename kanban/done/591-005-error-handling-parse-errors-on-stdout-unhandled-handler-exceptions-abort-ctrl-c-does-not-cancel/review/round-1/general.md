# Round 1 — general
**Date:** 2026-11-19
**Scope reviewed:** `de5c941b..0d0e1dfe`: `source/timewarp-nuru/invocation/nuru-app.cli-run.cs` (new), `nuru-app.cs`, `nuru-app.invocation.cs`, `repl/repl-session.cs`, `interceptor-emitter.cs`, the 13 touched test runfiles, the docs, the changelog, and the migrate skill. Call sites read: generated `ExecuteRouteAsync` signature, built-in `serve` route, daemon server and session, REPL Ctrl+C handler, `repl-42` (enters the REPL through `RunAsync(["--interactive"])`), CI multi-mode globs.

## Summary

The change does what the task and the restatement ask. `WriteArgumentError` writes stderr on the plain path. The generated `RunAsync_Intercepted` now goes through `NuruApp.RunCliAsync`, which observes the handler task: success returns its code, a fault writes the message to stderr and returns 1, `OptionsValidationException` is rethrown, and a cancellation after a signal returns 130/143. Ctrl+C (`Terminal.CancelKeyPress` and Posix SIGINT, collapsed by the arm flag) cancels the handler token, and `ProcessTerminationTimeout` bounds the wait. `ExecuteAsync`/`InvokeAsync`/REPL commands are untouched, and the REPL marks itself as the Ctrl+C owner so the CLI registration neither cancels nor sets `Cancel`. Tests cover each accepted item (routing-43 7 tests, completion-27 unknown shell, routing-36 stderr, existing generator-14 for `OptionsValidationException`, existing repl-42 for REPL ownership through `RunAsync`). routing-43 is not in `CiTestExcludes`, so CI multi-mode runs it too. Docs, changelog, guide and skill all state the new behavior and what 4.0.0-beta.14 did. Recorded gates (`./bin/dev build`, each touched runfile, `ganda repo audit`) cover the change.

Considered and not raised: the CA1031 rewrite (`Task.WhenAny(run)` then inspect the task instead of `catch (Exception)`). The implementer replaced a pragma the `analyzer-suppressions` ratchet (`.timewarp/suppression-baseline.json`) rejected. This is not a silenced check: no analyzer is suppressed, the catch-all at the process boundary is the behavior the task asks for, the failure is reported (message on stderr, exit 1), and the Design region states the choice openly. Also considered: Linux SIGINT with a REPL owning the signal. The CLI registration leaves `context.Cancel` alone and the Console registration sets it, and the context is shared across registrations, so the session stays up.

No gate was re-run. No targeted command was run.

## Issues

### Issue 1 — Severity: suggestion
- File: documentation/user/features/served-apps.md:101 (behavior from source/timewarp-nuru/invocation/nuru-app.cli-run.cs:119-148 and source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.builtin-routes.cs:149)
- Description: The built-in `serve` route passes `ExecuteRouteAsync`'s token to `NuruDaemonServer.RunAsync`. That token is now the CLI signal token, so a foreground `serve` (or a detached one that gets SIGTERM) also arms the 2-second `ProcessTerminationTimeout`. The daemon cancels each call's linked token and waits for all handlers. If a call ignores cancellation for more than two seconds, `RunAsync` returns 130/143 while the session is still draining, so `Main` can exit before the daemon's `finally` removes the socket file and before the call answers. Before this change the daemon waited for its calls with no limit. served-apps.md line 101 still says the server "stops on `serve --stop`, Ctrl+C, or SIGTERM, and then removes the socket file". The next start deletes a stale socket under the lock, so nothing breaks.
- Suggestion: Add one sentence to served-apps.md (Lifetime) that a Ctrl+C or SIGTERM stop waits `ProcessTerminationTimeout` for running calls, or have the serve route ignore the CLI timeout. Either is optional.
- Status: open

### Issue 2 — Severity: nit
- File: source/timewarp-nuru/invocation/nuru-app.cli-run.cs:106
- Description: `TryArm` reads `delayCancellation.Token` after `signalSource.Cancel()`. A signal callback that is already running on another thread when the run's `finally` disposes `delayCancellation` (line 147) throws `ObjectDisposedException` from the `Token` getter. `signalSource.Cancel()` above it already guards this case. The window is a Ctrl+C at the instant the handler finishes. The throw lands in the signal dispatch task, and the run's exit code has already been decided.
- Suggestion: Capture `CancellationToken delayToken = delayCancellation.Token;` before registering the handlers, and pass `delayToken` in `TryArm`.
- Status: open
