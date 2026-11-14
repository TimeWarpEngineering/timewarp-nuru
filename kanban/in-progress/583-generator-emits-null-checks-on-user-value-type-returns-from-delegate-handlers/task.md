# Generator emits null checks on user value-type returns from delegate handlers

## Description

A fluent delegate handler that returns a user value type (an enum, or a user struct) generates code that does
not compile in any assembly. Found by task 529's consumer lane (`generator-75-consumer-surface-no-ivt`):

```csharp
.Map("mode {shade:Shade}").WithHandler((Shade shade) => shade).AsQuery().Done()
public enum Shade { Light, Dark }
```

Generated output:

- `if (result is not null)`: CS0037, cannot convert null to the non-nullable `Shade`
- `result?.ToString() ?? ""` in the JSON fallback: CS0023, `?` on a value type

Cause: `HandlerInvokerEmitter.IsKnownValueType` and `GetOutputStrategy`
(`source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.output.cs`) decide from the type
*name*, so only BCL value types are known. A user value type is treated as a reference type and goes to
`OutputStrategy.Json` with a null guard.

## Goal (implementer restatement)

A handler whose result is a non-nullable user value type (an enum or a user struct) must generate code that
compiles. Today the invoker emitter decides "value type or not" from the type name, so only BCL value types
skip the `is not null` guard and the `?.ToString()` JSON fallback; a user enum or struct gets both and the
consumer build fails with CS0037 / CS0023.

Acceptance criteria:

- The extractors read the result type's shape from the semantic model (non-nullable value type, enum, or
  may-be-null) and carry it on `HandlerReturnType` (and on the problem-pair success type).
- The invoker emits no null guard and no `?.` for a non-nullable value type result, on every path that writes a
  result (delegate, method group, command/mediator, `--json-args` render of a captured result).
- An enum result prints raw (`ToString()`, the member name), not JSON; capabilities reports it as `Primitive`.
- A user struct result still goes through the JSON strategy, with a plain `ToString()` fallback.
- `Nullable<T>` results keep the null guard.
- Regression: generator-75 compiles an enum and a user struct return (delegate, async delegate, method group,
  `IQuery<TEnum>` endpoint); a runtime routing test pins that an enum result prints its member name.
- `ganda runfile cache --clear`, then `./bin/dev build`, `./bin/dev test`, `ganda repo audit` green.

## Checklist

- [x] Carry the return type's `IsValueType` (and nullable-ness) from the semantic model into the handler model
- [x] Skip the null guard and `?.` for non-nullable value types; decide whether enums print raw (`ToString()`) rather than JSON
- [x] Add a regression case (enum and user struct return) to generator-75 or its own generator test
- [x] `ganda runfile cache --clear`, then `./bin/dev test` green

## Notes

- Workaround in 529's lane: the handler returns `shade.ToString()`.
- Filed from task 529 (2026-11-14).

## Session

- Created: 1411579 (2026-11-13)
- Implement (2026-11-14): merged origin/master (land kanban 586) before the first commit; model, extractors,
  emitter and tests per ## Results.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 3bff5411-c66b-433c-a679-cd4589e1a003, max-turns 500, Done — 2026-11-14T05:29:36Z

## Results

### What changed

- New `ResultValueKind` (`MaybeNull`, `ValueType`, `Enum`) in
  `source/timewarp-nuru-analyzers/generators/models/result-value-kind.cs`, read from the `ITypeSymbol` by
  `ResultValueKinds.Of` (`Nullable<T>` stays `MaybeNull`).
- `HandlerReturnType.ValueKind` (the result, or `T` of `Task<T>`) and `HandlerDefinition.SuccessValueKind` (problem
  pair success type), set by every extractor that builds a return type: lambda expression and block bodies, method
  groups, `[NuruRoute]` command classes (`IQuery<T>`/`ICommand<T>`), and `[Operation]` mediator endpoints.
  `HandlerDefinition.RenderedValueKind` picks the one the invoker writes.
- `HandlerInvokerEmitter.EmitResultOutput` skips `is not null` for any non-nullable value type, and the JSON
  fallback writes `result.ToString()` with no `?.` for one. `GetOutputStrategy` sends a non-nullable enum to
  `Raw`, so an enum result prints its member name (`Dark`) instead of JSON; capabilities now reports such a route's
  `returnType` kind as `Primitive`. User structs keep the JSON strategy.
- Decision: enums print raw. JSON of an enum is its number (no converter in the built-in contexts), which is worse
  for a CLI than the member name, and matches how the binder takes enum input by name.
- Tests: generator-75 now returns the enum and the struct directly (the 529 workaround `shade.ToString()` is gone)
  and adds an async enum delegate and an enum method group; new `tests/timewarp-nuru-tests/routing/routing-39-value-type-results.cs`
  pins runtime output (delegate, async delegate, method group, `IQuery<TEnum>` endpoint, user struct,
  `Nullable<TEnum>` null writes nothing) in the CI multi-mode assembly.
- Audit boy-scout: `ganda repo audit --fix` set the VS Code window title/icon in the git-ignored
  `.vscode/settings.json` (advisory `vscode-window-icon` warning); nothing to commit.

### Follow-up (not fixed here)

- A delegate or method-group result whose type is declared in a namespace the generated file does not import
  fails with CS0246: `GetReturnTypeName`/`GetUnwrappedReturnTypeName` and `EmitRender` declare the result local by
  `ShortTypeName` (minimally qualified). It hits reference types too, so it is its own defect; routing-39 keeps its
  result types in the global namespace for that reason. Worth a separate task (fully qualified names need the
  nullable reference `?` kept, or `var`).

### Gates

| Gate | Command | Result |
|------|---------|--------|
| Build | `./bin/dev build` | pass (exit 0) |
| Test | `ganda runfile cache --clear` then `./bin/dev test` | pass (exit 0; CI multi-mode incl. routing-39, standalone incl. generator-75 6/6) |
| Audit | `ganda repo audit` | pass; first run had one advisory `vscode-window-icon` warning, fixed with `--fix`, rerun "Repository passes all audit checks." |

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/routing/routing-39-value-type-results.cs
dotnet run tests/timewarp-nuru-tests/generator/generator-75-consumer-surface-no-ivt.cs
```

Expect: routing-39 6/6 pass (`vtr-mode Dark` prints `Dark`, the `IQuery<VtrShade>` endpoint prints `Light`,
the struct writes both members, the null `VtrShade?` writes nothing); generator-75 6/6 pass, with
`.WithHandler((Gen75Shade shade) => shade)` compiling as a consumer. In
`artifacts/generated/routing-39-value-type-results.cs/.../NuruGenerated.g.cs` the enum route emits
`app.Terminal.WriteLine(result.ToString());` with no `if (result is not null)`, and the struct's JSON fallback is
`result.ToString()` with no `?.`.
