# Fix shell completion so later parameters and user types resolve

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding R-3 from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

Custom completion only binds the first parameter, and type lookup cannot see user types.

`EmitTryGetParameterInfoMethod` sets `parameterName` only when `paramPos == 0`. The prefix test is `StartsWith(commandPrefix)` with no token boundary, so `github` matches `git`. `DynamicCompletionHandler` then calls `Type.GetType(paramTypeName)` from the Nuru assembly. The emitted name is the route constraint (`int`, `LogLevel`), not an assembly-qualified name, so `RegisterForType` does not resolve user types or C# keywords.

Evidence: `source/timewarp-nuru-analyzers/generators/emitters/completion-emitter.cs`, `source/timewarp-nuru/completion/completion/dynamic-completion-handler.cs`. Parent record: `review/runtime-core.md` R-3.

## Checklist

- [ ] `TryGetParameterInfo` reports the parameter at the cursor, not only parameter 0
- [ ] A command prefix matches on a token boundary (`git` does not match `github`)
- [ ] `RegisterForType` resolves a user type and a C# keyword constraint the app registered
- [ ] Add a test that completes the second parameter of a two-parameter route

## How to validate

Smoke:

```bash
dotnet test tests/timewarp-nuru-tests/timewarp-nuru-tests.csproj --filter FullyQualifiedName~Completion
```

Expect: Completion tests pass, including a case where the cursor is on the second parameter and a case where `git` is not a prefix of `github`.

## Session

- Created: 525959 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
