# Correct the RunReplAsync stub to require the source generator

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding R-6 from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

The `RunReplAsync` fallback tells the user to disable the source generator. The stub throws `RunReplAsync was not intercepted. Ensure AddRepl() is called and the source generator is not enabled.` `RunAsync` in the same file says the generator must be enabled. The REPL sentence is inverted. The XML exception doc on the same method says the generator must be enabled.

Evidence: `source/timewarp-nuru/nuru-app.cs`. Parent record: `review/runtime-core.md` R-6.

## Checklist

- [ ] The stub message says the source generator must be enabled, and that `AddRepl()` was called
- [ ] The XML exception doc and the thrown string agree
- [ ] A test or snapshot asserts the thrown message

## How to validate

Smoke:

```bash
dotnet test tests/timewarp-nuru-tests/timewarp-nuru-tests.csproj --filter FullyQualifiedName~RunRepl
```

Expect: The assertion reads the thrown message and it tells the caller to enable the source generator. It does not say to disable it.

## Session

- Created: 528163 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
