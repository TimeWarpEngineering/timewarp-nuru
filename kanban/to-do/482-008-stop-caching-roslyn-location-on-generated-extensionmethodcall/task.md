# Stop caching Roslyn Location on generated ExtensionMethodCall

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding A-1 from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

`ExtensionMethodCall` stores a Roslyn `Location` and is part of the emit-stage model. A live `Location` is tied to a `SyntaxTree` and breaks value equality, so any app that calls `ConfigureServices` with an extension method misses the emit cache on every edit.

`ServiceDefinition.RegistrationLocation` was already converted to `LocationInfo`. This record was not. `service-extractor.cs` still passes `invocation.GetLocation()` for `AddLogging` and for any `AddX` that was not lowered.

Evidence: `source/timewarp-nuru-analyzers/generators/models/service-extraction-result.cs`, `generators/extractors/service-extractor.cs`. Parent record: `review/analyzers.md` A-1. Do not store `SemanticModel` on the model. Use `LocationInfo` (or drop the location from the equality key and keep it only for the diagnostic).

## Checklist

- [x] `ExtensionMethodCall` no longer stores a Roslyn `Location` in the value compared for emit
- [x] NURU052 still reports a source location for the extension call
- [x] An edit that does not change the `ConfigureServices` extension calls does not rebuild the emit output
- [x] Extend the incrementality test (`generator-37` or its successor) to cover an opaque `AddX`

## How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/generator/generator-37-incrementality-caching.cs
```

Expect: 3/3 pass, including `Model_caches_with_opaque_extension_call_when_reparsed_unchanged` (opaque `AddX` in `ConfigureServices`, NURU052 located on the `AddGen37Opaque()` call in the app file).

(The original smoke referenced a non-existent `timewarp-nuru-tests.csproj`; tests are Jaribu runfiles.)

## Session

- Created: 533981 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)

- Implemented: claude-opus-5-5 implementer (2026-10-05)
- Review: grok 01a10b54-b754-76b0-a925-425e6568800f (2026-10-05)

## Results

- `ExtensionMethodCall.Location` is now `LocationInfo?` (value-equatable), built with
  `LocationInfo.CreateFrom(invocation.GetLocation())` in `service-extractor.cs` (both the
  special-cased `AddLogging`/`AddHttpClient` path and the un-lowerable `AddX` path).
- `ServiceValidator.ValidateExtensionMethods` takes the `Compilation` and rebuilds the
  location with `LocationInfo.ToLocation(compilation)` (tree-bound, so `#pragma` still
  suppresses NURU052); `ModelValidator` passes it through.
- `generator-37`:
  - New test `Model_caches_with_opaque_extension_call_when_reparsed_unchanged`. Verified
    it fails (`NuruGeneratorModel -> Modified`) without the fix and passes with it.
  - **Pre-existing defect fixed:** the two existing tests were vacuous. Their app used
    `NuruApp.CreateBuilder(args)` (no such overload → CS1501) and had no `RunAsync`, so the
    generator produced nothing and "cached" trivially. Fixed the source, and every test now
    asserts no compile errors and non-empty generated output.
  - Once those tests were real, the "cosmetic trailing edit to app file" scenario could not
    cache. The `[InterceptsLocation]` data embeds a checksum of the file's content, so any
    text edit to a file with an intercept site has to change the emit output. That test is
    now `Model_caches_when_app_file_is_reparsed_unchanged`: a new tree with identical text,
    so the Roslyn objects are new but the values are the same. This is the case
    A-1 is about.
- Practical note: A-1 only affects edits that re-parse the app file without changing the
  intercept checksum (or other files whose trees are re-created). An edit that changes the
  text of the app file rebuilds emit no matter what, because of the checksum.
- Review: 1 round, roster general, effort 1. Counts: bug 0 open / 0 fixed / 0 wontfix;
  suggestion 0; nit 0. Disposition: clean. No wontfix and no escalation.
  Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/generator/generator-37-incrementality-caching.cs
dotnet run tests/timewarp-nuru-tests/generator/generator-42-extension-method-lowering-diagnostics.cs
```

Expect: generator-37 3/3 pass (opaque `AddX` caches; NURU052 in-source on the
`AddGen37Opaque()` span). generator-42 8/8 pass (NURU052 still reported for opaque calls).

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
- Implementation review: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`. Outcome clean (no findings).
