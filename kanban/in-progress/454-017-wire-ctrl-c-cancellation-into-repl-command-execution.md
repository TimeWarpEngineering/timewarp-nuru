# Wire Ctrl C Cancellation Into REPL Command Execution

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M15).

## Description

`source/timewarp-nuru/repl/repl-session.cs:316-321` (with :243-268) — `OnCancelKeyPress`
only sets `Running = false`. No CancellationTokenSource is linked to it, and the token
passed to CommandExecutor is the external one from `RunAsync`. Ctrl+C during command
execution therefore does NOT cancel the in-flight command — the REPL only stops after it
returns, so a hung or long-running command cannot be aborted.

## Requirements

- Create a per-command CancellationTokenSource linked to the external token; cancel it
  from OnCancelKeyPress; pass the linked token to CommandExecutor.
- First Ctrl+C should cancel the running command and return to the prompt (not exit the
  REPL); decide and document behavior for Ctrl+C at an idle prompt / double Ctrl+C.

## Checklist

- [ ] Linked CTS per command execution
- [ ] Ctrl+C cancels in-flight command, REPL survives
- [ ] Behavior at idle prompt decided + documented
- [ ] Non-interactive tests where feasible (cancellation plumbing unit-testable)

## Notes

Claude cannot run interactive REPL tests; human verification needed for the key handling.

## Verification protocol (reviewer, 2026-07-07)

Implement now with TestTerminal-based unit coverage of the state/logic layer; do NOT
block on interactive verification. Interactive confirmation is batched into ONE human
REPL verification session tracked on parent task 454 (together with 454-007's pending
Windows multiline check). Leave a "Human verification pending" line in this task's
Results listing exactly what the human should try.

## Implementation Plan (2026-08-05)

Grounded in current `repl-session.cs` (post-454-030: broadened catch, local-instance
lifecycle). `TestTerminal.SimulateCancelKeyPress(ConsoleSpecialKey = ControlC)` exists in
TimeWarp.Terminal 1.0.0, so Ctrl+C is fully simulatable — automated tests, no human gate.

**Decided semantics (conventional REPL model — bash/python/PSReadLine):**
- Ctrl+C with a command in flight → cancel THAT command (linked CTS); print `^C` newline;
  REPL survives and re-prompts. `OperationCanceledException` from the executor is caught
  and displayed as "Command cancelled" (exit code 130, the Unix SIGINT convention), NOT
  rethrown (rethrow would tear down the loop via the external-token path).
- Ctrl+C at an idle prompt → newline + re-prompt; does NOT exit the REPL (exit stays
  available via `exit` / Ctrl+D). This intentionally replaces the old "Ctrl+C exits the
  REPL" behavior per the task requirement.
- Double Ctrl+C → second press is a no-op for the already-cancelled command.
- External token (host shutdown) still exits the loop as before — only the in-flight
  command's OCE is converted; if the EXTERNAL token is cancelled, rethrow so RunAsync
  unwinds as today.

**Changes (`repl-session.cs` only):**
1. Field `CancellationTokenSource? CurrentCommandCts` (volatile write/read via property or
   Interlocked — OnCancelKeyPress fires on a threadpool/console thread).
2. `ExecuteCommandAsync`: create `using var cts = CancellationTokenSource.
   CreateLinkedTokenSource(cancellationToken)`; publish to `CurrentCommandCts`; pass
   `cts.Token` to `CommandExecutor`; clear the field in `finally`.
3. `catch (OperationCanceledException)`: if `cancellationToken` (external) is cancelled →
   rethrow (host shutdown, current behavior). Else (our Ctrl+C) → stopwatch stop, display
   "Command cancelled" via DisplayCommandResult path, return 130, REPL continues
   (regardless of ContinueOnError — cancellation is user intent, not a command failure).
4. `OnCancelKeyPress`: `e.Cancel = true`; snapshot `CurrentCommandCts`; if non-null →
   `Cancel()` it (command in flight); no `Running = false` in either case. Keep the
   newline write.
5. `ProcessCommandLoopAsync`/`ReadCommandInputAsync` unchanged — idle Ctrl+C simply
   returns control to the reader; console reader already redraws.

**Tests — `tests/timewarp-nuru-tests/repl/repl-42-ctrl-c-cancellation.cs`:**
- In-flight cancel: handler awaits `Task.Delay(30s, ct)`; test fires
  `SimulateCancelKeyPress` shortly after the command starts; assert REPL survived (next
  command runs) and cancellation was reported.
- Idle Ctrl+C: simulate at prompt; assert REPL still accepts the next command (doesn't
  exit).
- External token still exits: cancel the external token; assert RunAsync completes.
- Cancelled command's OCE does not trip ContinueOnError=false teardown.

Timing note: the in-flight test needs the Ctrl+C to fire while the executor awaits — use a
TaskCompletionSource handshake (handler signals started; test thread simulates Ctrl+C) to
avoid flakiness rather than sleeps.
