# Generator: escape C# keyword identifiers in emitted code (event, when route params)

## Description

Found 2026-08-08 while fixing task 459: a route like
`schedule {event} {when:DateTime}` with handler `(string @event, DateTime when)`
is legal Nuru + legal C#, but NuruGenerator emits the parameter identifiers
UNESCAPED into generated interceptor code, producing:

- `error CS0065: 'GeneratedInterceptor.': event property must have both add and
  remove accessors` (the raw `event` token parsed as a keyword)
- `error CS0246: The type or namespace name 'when' could not be found`
- cascading CS0708/CS8802 in NuruGenerated.g.cs

Repro: restore `{event}`/`{when:DateTime}` param names in
`samples/fluent/08-type-converters/fluent-type-converters-builtin.cs`
(renamed to `{name}`/`{at:DateTime}` in the 459 fix to unblock CI) and build.

## Checklist

- [ ] Generator escapes identifiers that are C# keywords/contextual keywords (`@`-prefix) everywhere parameter names are emitted
- [ ] Analyzer or generator test covering keyword-named route params (event, when, class, ref, ...)
- [ ] Restore a keyword-named param somewhere in samples as a living regression check (or add a dedicated test)

## Notes

# Implementation Plan: Task 460 — Escape C# keyword identifiers

## Summary
Route params named C# keywords (`{event}`, `{when}`) are legal Nuru + legal C# with `@event`, but the generator emits unescaped identifiers in untyped positional extraction → CS0065/CS0246. Partial fix from task 323 left helper + some sites; primary untyped path still broken.

## Approach
1. Harden `CSharpIdentifierUtils.EscapeIfKeyword` (strip existing `@`, prefer Roslyn SyntaxFacts)
2. Escape at every C# identifier emit site — especially untyped positional extraction in route-matcher-emitter
3. Fix method-handler BuildArgumentList path
4. Generator-39 regression test + restore sample `{event}`/`{when}`

## Steps
### A — csharp-identifier-utils.cs
- Idempotent EscapeIfKeyword via SyntaxFacts.GetKeywordKind / GetContextualKeywordKind
- Strip leading @ before check

### B — route-matcher-emitter.cs
- Primary + alias match: wrap untyped CamelCaseName with EscapeIfKeyword
- Prefer PositionalCaptureVar helper; fix or delete dead unescaped helpers

### C — handler-invoker-emitter.cs
- BuildArgumentList: EscapeIfKeyword on ParameterName (mirror BuildArgumentListFromRoute)

### D — Optional: handler-extractor Identifier.ValueText (no stored @)

### E — Tests + sample
- generator-39-keyword-param-identifiers.cs
- Restore samples/fluent/08-type-converters/fluent-type-converters-builtin.cs to schedule {event} {when:DateTime}

## Out of scope
Forbidding keywords; escaping user lambda bodies; help string content; method-handler unique-var alignment for non-keyword cases

## Validation
```
ganda runfile cache --clear
dotnet build source/timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj
dotnet run tests/timewarp-nuru-tests/generator/generator-39-keyword-param-identifiers.cs
dotnet run samples/fluent/08-type-converters/fluent-type-converters-builtin.cs -- schedule Standup 2024-12-25T14:30:00
dotnet run tests/ci-tests/run-ci-tests.cs
```

## Session

Orchestrator: grok (2026-08-10) — Phase 2 plan finalized
