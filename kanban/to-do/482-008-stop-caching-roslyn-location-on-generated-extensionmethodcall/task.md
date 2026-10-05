# Stop caching Roslyn Location on generated ExtensionMethodCall

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding A-1 from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

`ExtensionMethodCall` stores a Roslyn `Location` and is part of the emit-stage model. A live `Location` is tied to a `SyntaxTree` and breaks value equality, so any app that calls `ConfigureServices` with an extension method misses the emit cache on every edit.

`ServiceDefinition.RegistrationLocation` was already converted to `LocationInfo`. This record was not. `service-extractor.cs` still passes `invocation.GetLocation()` for `AddLogging` and for any `AddX` that was not lowered.

Evidence: `source/timewarp-nuru-analyzers/generators/models/service-extraction-result.cs`, `generators/extractors/service-extractor.cs`. Parent record: `review/analyzers.md` A-1. Do not store `SemanticModel` on the model. Use `LocationInfo` (or drop the location from the equality key and keep it only for the diagnostic).

## Checklist

- [ ] `ExtensionMethodCall` no longer stores a Roslyn `Location` in the value compared for emit
- [ ] NURU052 still reports a source location for the extension call
- [ ] An edit that does not change the `ConfigureServices` extension calls does not rebuild the emit output
- [ ] Extend the incrementality test (`generator-37` or its successor) to cover an opaque `AddX`

## How to validate

Smoke:

```bash
dotnet test tests/timewarp-nuru-tests/timewarp-nuru-tests.csproj --filter FullyQualifiedName~Incrementality
```

Expect: The incrementality test passes with an opaque extension call in `ConfigureServices`. NURU052 still has a non-empty location.

## Session

- Created: 533981 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
