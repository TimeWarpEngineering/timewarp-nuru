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
