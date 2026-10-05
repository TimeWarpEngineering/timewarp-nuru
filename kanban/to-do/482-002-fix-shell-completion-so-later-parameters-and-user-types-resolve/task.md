# Fix shell completion so later parameters and user types resolve

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding R-3 from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

Custom completion only binds the first parameter, and type lookup cannot see user types.

`EmitTryGetParameterInfoMethod` sets `parameterName` only when `paramPos == 0`. The prefix test is `StartsWith(commandPrefix)` with no token boundary, so `github` matches `git`. `DynamicCompletionHandler` then calls `Type.GetType(paramTypeName)` from the Nuru assembly. The emitted name is the route constraint (`int`, `LogLevel`), not an assembly-qualified name, so `RegisterForType` does not resolve user types or C# keywords.

Evidence: `source/timewarp-nuru-analyzers/generators/emitters/completion-emitter.cs`, `source/timewarp-nuru/completion/completion/dynamic-completion-handler.cs`. Parent record: `review/runtime-core.md` R-3.

## Checklist

- [x] `TryGetParameterInfo` reports the parameter at the cursor, not only parameter 0
- [x] A command prefix matches on a token boundary (`git` does not match `github`)
- [x] `RegisterForType` resolves a user type and a C# keyword constraint the app registered
- [x] Add a test that completes the second parameter of a two-parameter route

## Results

- `CompletionDataExtractor.ParameterInfo` now carries the parameter's positional index, catch-all flag, and the bound handler parameter's C# type.
- The generated `TryGetParameterInfo` matches the command prefix on a token boundary (`prefix == cmd` or `prefix.StartsWith(cmd + " ")`), checks longer prefixes first, and reports the parameter whose position equals the cursor offset (catch-all: offset at or past its position).
- Breaking (pre-3.0): `IShellCompletionProvider.TryGetParameterInfo` returns `out Type? parameterType` instead of `out string? parameterTypeName`. The generator emits `typeof(T)` from the handler parameter type with nullable `?` stripped, so user types and keyword constraints (`int`) resolve. This replaces the reflection-based `Type.GetType`, which also makes it AOT-safe. `DynamicCompletionHandler` passes the type to `registry.GetSourceForType`.
- New test `tests/timewarp-nuru-tests/completion/completion-28-parameter-info.cs` covers: the second parameter of a two-parameter route, the first parameter of the same shape, `git` not matching `github`, `RegisterForType` with a user enum, and `RegisterForType(typeof(int))` for `{ms:int}`.
- `dotnet run tests/ci-tests/run-ci-tests.cs` passes (exit 0).

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/completion/completion-28-parameter-info.cs
dotnet run tests/timewarp-nuru-tests/completion/completion-27-endpoint-protocol.cs
```

Expect: all 5 tests in completion-28 pass: second-parameter source, first-parameter source, `git`/`github` boundary, user type, and `int` keyword type. completion-27 still passes. (The `tests/timewarp-nuru-tests/timewarp-nuru-tests.csproj` named in the original smoke does not exist. Tests are runfiles.)

## Session

- Created: 525959 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)
- Implemented under ganda task work (claude implementer, 2026-10-05)

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
