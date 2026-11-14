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

## Checklist

- [ ] Decide: skip anonymous types in the JSON context, or report a diagnostic at the handler
- [ ] Generated code compiles for an anonymous-type return (or the diagnostic names the handler)
- [ ] Test covering an anonymous-type return
- [ ] Update the output-handling.md note

## Session

- Created: 1411579 (2026-11-14), filed from task 532

## Notes

- Related: 532 (docs), `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.output.cs`,
  the MSBuild task that writes `NuruUserTypesJsonContext`.
