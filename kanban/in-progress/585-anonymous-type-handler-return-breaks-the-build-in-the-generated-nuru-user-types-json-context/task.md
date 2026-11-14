# Anonymous-type handler return breaks the build in the generated NuruUserTypesJsonContext

## Description

A fluent handler that returns an anonymous type fails the consumer build. The generated
`NuruUserTypesJsonContext.g.cs` writes the anonymous type's display name into a
`[JsonSerializable(typeof(...))]` attribute, which gives a cascade of syntax errors
(`CS1001 Identifier expected`, `CS1026`, `CS1525 Invalid expression term 'string'`).

Repro (found by task 532, against the repo source on master 2026-11-14):

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .Map("info")
    .WithHandler(() => new { Name = "MyApp", Version = "1.0.0" })
    .AsQuery()
    .Done()
  .Build();
```

The output strategy in `handler-invoker-emitter.output.cs` says complex results are JSON, and
System.Text.Json source generation cannot name an anonymous type. The fix is either to skip
anonymous types when emitting the context (and fall back to reflection-free `ToString()` or a
clear runtime message), or to report an analyzer diagnostic at the handler that says "return a
named record or class". Generated code that does not compile is the defect either way.

The user docs no longer show anonymous returns (task 532 changed them to records and notes that
`new { ... }` does not build). Update that note in `documentation/user/features/output-handling.md`
when this is fixed.

## Goal (implementer restatement)

A delegate handler that returns an anonymous type (`new { ... }`, or a type built from one such as
`List<anon>`, in sync or async form) must not break the consumer build. Two places wrote the
unnameable `<anonymous type: ...>` display name into generated C#: the JSON context MSBuild task
(`[JsonSerializable(typeof(...))]`) and, hidden behind those errors, the generator's own invoker code
(`<anonymous type: ...> result = ...` and the local function's return type).

Acceptance criteria:
- The repro from the description builds and runs; no generated file contains an anonymous type name.
- The decision (skip vs diagnostic) is recorded: anonymous results build and print through `ToString()`.
- A test covers anonymous results (sync, async, list, command, null).
- `documentation/user/features/output-handling.md` no longer says `new { ... }` does not build.

## Checklist

- [x] Decide: skip anonymous types in the JSON context, or report a diagnostic at the handler (skip: erase to object, print via ToString())
- [x] Generated code compiles for an anonymous-type return (or the diagnostic names the handler)
- [x] Test covering an anonymous-type return
- [x] Update the output-handling.md note

## Session

- Created: 1411579 (2026-11-14), filed from task 532
- 2026-11-14 implement (claude): reproduced in a runfile under tests/. The JSON context error hid a
  second defect: once the context skipped the type, the generator's own invoker declared
  `<anonymous type: ...> result` and the local function's return type with that name too (sync, async,
  `List<anon>`, command, and the `__invocation.Result` cast). Decision: skip, not diagnose. An anonymous
  result is a natural thing to write and ToString() gives readable output, so it builds and prints
  rather than failing with a new diagnostic.
- Boy-scout: `ganda repo audit` reported the advisory `vscode-window-icon` warning; `ganda repo audit --fix`
  wrote the git-ignored `.vscode/settings.json` (no tracked change).
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session dcef3df4-f714-42b8-8077-c159d876e8c2, max-turns 500, Done — 2026-11-14T07:27:39Z

## Notes

- Related: 532 (docs), `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.output.cs`,
  the MSBuild task that writes `NuruUserTypesJsonContext`.

## Results

Anonymous-type handler results now build and print through `ToString()`.

- `source/timewarp-nuru-analyzers/generators/models/result-value-kind.cs`: new `ResultValueKind.Anonymous`,
  set by `ResultValueKinds.Of` when the symbol is or contains an anonymous type (type argument, array
  element, containing type; semantic check via `ITypeSymbol.IsAnonymousType`). `CanBeNull` keeps the null
  guard for it.
- `handler-extractor.method-symbols.cs`: one `ValueReturnType` helper for method symbols and inferred lambda
  returns. An anonymous result's type name becomes `global::System.Object` (Task<object> when async), so the
  local function, the result local and the `__invocation.Result` cast all compile (`object` accepts the value).
- `handler-invoker-emitter.output.cs`: `GetOutputStrategy` returns Raw for `Anonymous` (ToString(), no JSON
  try/catch); capabilities describe the return as `System.Object`, kind Primitive (same strategy call).
- `GenerateNuruJsonContextTask.Extraction.cs`: `GetReturnTypeName` returns null for `Anonymous`, so no
  `[JsonSerializable]` is written for it (the root cause of the reported CS1001/CS1525 cascade).
- `documentation/user/features/output-handling.md`: the note now says `new { ... }` builds and prints via
  ToString(); return a named record or class for JSON.
- Test: `tests/timewarp-nuru-tests/routing/routing-40-anonymous-type-results.cs` (runs in the CI multi-mode
  assembly, which also runs the JSON context MSBuild task): sync query, async block body, `List<anon>`,
  command, and a null anonymous result.

Known limits: a `List<anon>` prints its CLR type name (`List`1[...]`), as `ToString()` does; JSON output still
needs a named type.

### Gates

- `./bin/dev build` — pass (Build completed successfully).
- `./bin/dev test` — pass (runfile cache cleared first; 4705 tests passed, 0 failed, standalone phase 0 failed;
  routing-40 5/5).
- `ganda repo audit` — pass after `ganda repo audit --fix` for the advisory `vscode-window-icon` warning
  (git-ignored file only); rerun: "Repository passes all audit checks." The commit-hook attest then failed
  `runfile-executable` (routing-40 lacked the executable bit); set it, and `ganda repo audit` passes again.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
(cd tests/timewarp-nuru-tests/routing && dotnet run routing-40-anonymous-type-results.cs)
```

Expect: the file builds (no CS1001/CS1525 from `NuruUserTypesJsonContext.g.cs` or `NuruGenerated.g.cs`), and
all 5 tests pass; `atr-info` prints `{ Name = MyApp, Version = 1.0.0 }`, `atr-async 2` prints `{ Count = 2 }`,
`atr-cmd` prints `{ Ok = True }`, and `atr-none` prints nothing.
