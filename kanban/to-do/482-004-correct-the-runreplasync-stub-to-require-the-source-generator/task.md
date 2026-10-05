# Correct the RunReplAsync stub to require the source generator

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding R-6 from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

The `RunReplAsync` fallback tells the user to disable the source generator. The stub throws `RunReplAsync was not intercepted. Ensure AddRepl() is called and the source generator is not enabled.` `RunAsync` in the same file says the generator must be enabled. The REPL sentence is inverted. The XML exception doc on the same method says the generator must be enabled.

Evidence: `source/timewarp-nuru/nuru-app.cs`. Parent record: `review/runtime-core.md` R-6.

## Checklist

- [x] The stub message says the source generator must be enabled, and that `AddRepl()` was called
- [x] The XML exception doc and the thrown string agree
- [x] A test or snapshot asserts the thrown message

## How to validate

Smoke:

```bash
dotnet run tests/timewarp-nuru-tests/repl/repl-48-run-repl-async-stub-message.cs
```

Expect: The assertion reads the thrown message and it tells the caller to enable the source generator. It does not say to disable it.

## Session

- Created: 528163 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)

- Implemented: claude implement oracle (2026-10-05)
- Review: grok 01a10b69-695f-7993-8369-4a7454a72d28 (2026-10-05)

## Results

- `source/timewarp-nuru/nuru-app.cs`: `RunReplAsync` stub now throws
  `RunReplAsync was not intercepted. Ensure AddRepl() is called and the source generator is enabled.`
  The `<exception>` XML doc was reworded to the same guidance (call not intercepted; call
  `AddRepl()` and enable the source generator).
- New test `tests/timewarp-nuru-tests/repl/repl-48-run-repl-async-stub-message.cs` reaches the
  stub through a method-group conversion (interceptors only replace invocations, so the fallback
  body runs) and asserts the exact message and that it does not contain "not enabled".
- The original smoke command referenced a non-existent `timewarp-nuru-tests.csproj`; tests in
  this repo are runfiles, so the validation below uses the runfile directly.
- Full CI multi-mode run (`dotnet run tests/ci-tests/run-ci-tests.cs`) passes, including the new test.

## Review

- Rounds: 1. Roster: general. Effort: 1.
- Counts (round 1): bug 0 open / 0 fixed / 0 wontfix. Suggestion 0. Nit 0.
- Disposition: clean. No wontfix and no escalation.
- Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/repl/repl-48-run-repl-async-stub-message.cs
```

Expect: `Should_tell_caller_to_enable_source_generator` passes — the assertion reads the thrown
`InvalidOperationException` message, which says the source generator must be **enabled** and
that `AddRepl()` must be called; it does not say to disable it.

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
- Implementation review: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`. Outcome clean (no findings).
