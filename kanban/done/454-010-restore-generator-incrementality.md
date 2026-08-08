# Restore Generator Incrementality

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M4, M5).

## Description

Two issues substantially defeat incremental-generator caching, so the heavy emit stage
re-runs on every keystroke in the IDE:

- **M4**: `source/timewarp-nuru-analyzers/generators/nuru-generator.cs:121` —
  `RegisterSourceOutput(...Combine(context.CompilationProvider), ...)` feeds the raw
  `Compilation` (changes on every edit) into the output stage, forcing
  `InterceptorEmitter.Emit` to re-run regardless of model changes. The Compilation is
  only used for REPL enum resolution — narrow the input to just what that needs (e.g. a
  dedicated provider that extracts the enum info into an equatable model).
- **M5**: Pipeline model records hold `ImmutableArray<T>` fields with no equatable
  wrapper (`models/generator-model.cs:13-19`, `app-model.cs`, `route-definition.cs`,
  etc.). Record equality uses ImmutableArray's reference equality; every transform
  produces fresh arrays, so `.Collect()/.Combine().Select()` stages rarely compare equal
  and recompute even when content is unchanged. Standard fix: an `EquatableArray<T>`
  wrapper (sequence equality + cached hash).

## Requirements

- Introduce EquatableArray<T> (or equivalent) and use it in all pipeline model records.
- Remove/narrow the CompilationProvider combine at nuru-generator.cs:121.
- Ensure no ISymbol/SyntaxNode ends up captured in pipeline models while refactoring.

## Checklist

- [x] EquatableArray<T> added and applied to all models — commits 9fcecd08/b96c45c8
- [x] Location/ImmutableDictionary removed from emit model (the THIRD killer):
      - [x] InterceptSiteModel → precomputed string; InterceptSitesByMethod dict → EquatableArray<InterceptSiteGroup> (commit 6a9df1ad)
      - [x] ServiceDefinition.RegistrationLocation → value-equatable LocationInfo (commit 5e0e5cfe)
- [x] CompilationProvider dependency narrowed (M4 enum-info extraction) + pipeline split (commit 2ab48dd1)
- [x] Verify cacheability (generator-37: trackIncrementalGeneratorSteps → Cached/Unchanged — 2 tests, both green)
- [x] `ganda runfile cache --clear` + run CI tests (1383/1376/0 after each commit)

## Progress (reviewer, 2026-07-14) — 5 commits, all validated green

Done and behavior-preserving (full CI 1383 total / 1376 passed / 0 failed after each):
- **Commit 5 (3c)** `5e0e5cfe`: `ServiceDefinition.RegistrationLocation` `Location` →
  value-equatable `LocationInfo` (new `generators/models/location-info.cs`:
  `FilePath` + `TextSpan` + `LinePositionSpan`, rebuilt to a `Location` via
  `Location.Create(...)` only when a diagnostic is created). This is a **deviation from the
  originally-planned validator side-channel** — chosen because it is strictly smaller (3
  files + 1 tiny type vs threading a `serviceLocations` map through
  interpreter→ir-app-builder→app-extractor→nuru-generator→ModelValidator→ServiceValidator)
  and idiomatic (the standard Roslyn "LocationInfo" pattern). NURU051/053/054 still point at
  the exact registration site. All DI/service-diagnostic suites green.
- **Commit 1** `9fcecd08`+`b96c45c8`: `EquatableArray<T>` (generators/models/equatable-array.cs).
  net10 `[CollectionBuilder]`; crucially a **two-way implicit conversion** with
  `ImmutableArray<T>` — this is what kept ~110 emit/validation consumers unchanged (the
  signature-change approach was tried first and cascaded unboundedly through the emitter tree).
- **Commit 2** `b96c45c8`: all 13 model array fields → `EquatableArray<T>` (M5). Diagnostic
  arrays kept `ImmutableArray<Diagnostic>` (off the emit path).
- **Commit 3a/b** `6a9df1ad`: `InterceptSiteModel` now stores precomputed `string AttributeSyntax`
  (no live `InterceptableLocation`); `AppModel.InterceptSitesByMethod` dict → equatable
  `EquatableArray<InterceptSiteGroup>` with a `TryGetSites` helper. `GetDisplayLocation()`
  dropped (was unused).

Remaining (the delicate part — do with fresh focus):
- **Commit 6 — M4 + pipeline split** (nuru-generator.cs:104-144): split the single
  `RegisterSourceOutput` into (1) an uncached diagnostics/logger-warning output over
  `generatorModelWithDiagnostics`, and (2) a CACHEABLE emit output over
  `modelProvider.Combine(enumInfoProvider)` — no `Compilation`, no diagnostics. Add
  `EnumInfo(string MetadataTypeName, EquatableArray<string> MemberNames)` +
  `EnumInfoExtractor.Resolve(model, compilation, ct)`. Add `.WithTrackingName("NuruGeneratorModel")`
  / `("NuruEnumInfo")`.
  - **SCOPE REFINEMENT (2026-07-14, verified against code):** the "only 2 consumers" note
    understates the change. `Compilation` is threaded through **~13 sites across 5 emitter
    files** (interceptor-emitter, repl-emitter, completion-emitter, completion-data-extractor,
    capabilities-emitter) feeding **two distinct resolution sites** that must both be replaced
    by a precomputed lookup:
      1. `CompletionDataExtractor.ExtractEnumParameters` (completion-data-extractor.cs:138) —
         resolves `route.Handler.Parameters[Source==Parameter].ParameterTypeName`.
      2. `CapabilitiesEmitter.ExtractEnumValues` (capabilities-emitter.cs:353) — resolves
         `param.ResolvedClrTypeName ?? handlerTypeName` and
         `option.ResolvedClrTypeName ?? handlerTypeName`.
    Both normalise (strip `global::` + trailing `?`) then `GetTypeByMetadataName`.
    `EnumInfoExtractor.Resolve` must gather the **UNION** of those candidate type-name strings
    (a superset is safe — extra entries are harmless, a MISSING entry silently drops
    `AllowedValues`/completion values = regression). Good news: the ONLY external caller of the
    public emitter surface is nuru-generator.cs:142 — the whole cascade is internal to
    `generators/emitters/`, and CI suites **EnumSource (13), CapabilitiesGroup, CapabilitiesRoundtrip,
    CompletionRegistry, CompletionEndpointProtocol** cover the output, so a bad gather is caught.
    Recommended sub-steps: (a) add `EnumInfo` + `EnumInfoExtractor.Resolve` gathering the union;
    (b) thread an `EquatableArray<EnumInfo>` (or a prebuilt `Dictionary<string,string[]>` built
    at the top of `InterceptorEmitter.Emit`) exactly where `compilation` currently flows,
    deleting the `Compilation` params; (c) split the `RegisterSourceOutput`; (d) `ganda runfile
    cache --clear` + full CI, watching the enum/capabilities/completion suites specifically.
- **Commit 7 — generator-37** cacheability test (see D5 in plan) + CI wiring.
  Note D5's B-only-edit scenario validates M4/M5 but NOT the Location-stripping (tree A's
  locations are stable when only tree B changes) — to also exercise the LocationInfo win, add a
  second run that edits tree A's whitespace and asserts the model still caches.

## Verified Implementation Plan (reviewer, 2026-07-13)

Planned by a read-only agent, then every load-bearing claim independently re-verified
against the code. **Key finding: the emit stage re-runs on every keystroke for THREE
reasons, not two.** Beyond M4 (raw CompilationProvider) and M5 (unwrapped ImmutableArray),
the emit-facing model transitively carries Roslyn `Location`/`InterceptableLocation`
objects and a non-equatable `ImmutableDictionary`. Any one alone defeats model value
equality, so M4+M5 alone deliver zero measurable caching. This is not scope creep — it is
what "restore incrementality" actually costs. CI cannot catch the failure mode; only the
generator-37 step-reason assert can.

### D1 — EquatableArray<T>
`readonly struct EquatableArray<T> where T : IEquatable<T>` wrapping one `ImmutableArray<T>`,
in `generators/models/equatable-array.cs` (namespace `TimeWarp.Nuru.Generators` — no new
usings). Surface chosen to minimise the ~110 consumer sites: `Length`, `this[int]`,
`IsDefault`, `IsDefaultOrEmpty` (48 sites use IsDefault*; `default` must behave like
`default(ImmutableArray<T>)`); implements `IEnumerable<T>` (keeps ALL foreach/LINQ working
edit-free); `[CollectionBuilder]` + `EquatableArray.Create<T>(ReadOnlySpan<T>)` so `[]`/
`[.. expr]`/`[x]` initializers retarget; implicit `ImmutableArray<T> → EquatableArray<T>`,
explicit reverse, `AsImmutableArray()`, `AddRange`. `Equals` = length + SequenceEqual;
content-based `GetHashCode` (no lazy mutation — readonly struct).

### D2 — Diagnostic problem: SPLIT (Approach A). Verified: `GeneratorModel` has NO
diagnostics field; diagnostics live only in the sibling wrapper
`GeneratorModelWithDiagnostics`. So split `RegisterSourceOutput` into (1) an uncached
diagnostics/logger-warning output over the wrapper, and (2) a CACHEABLE emit output over
`modelProvider.Combine(enumInfoProvider)` — no Compilation, no diagnostics.
KEEP `ImmutableArray<Diagnostic>` in `extraction-result.cs`, `endpoint-extraction-result.cs`
(+ the private wrappers). Everything reachable from `GeneratorModel` gets `EquatableArray`.

### D3 — Strip Location from the emit model (the third killer)
- `InterceptSiteModel.InterceptableLocation` (only emit use is `GetAttributeSyntax()` at
  interceptor-emitter.cs:110,192): change record to carry the PRECOMPUTED
  `string AttributeSyntax` (+ FilePath/Line/Column), computed in `FromInterceptableLocation`.
  Makes it a pure-value record (also sidesteps unverifiable InterceptableLocation equality).
- `AppModel.InterceptSitesByMethod : ImmutableDictionary<...>` → an equatable dictionary
  (EquatableDictionary or an EquatableArray of KV records). Verified reference-equality
  hazard (ir-app-builder.cs:387 `.ToImmutableDictionary`, merged at nuru-generator.cs:334/355).
- `ServiceDefinition.RegistrationLocation : Location?` — read ONLY by service-validator
  (266/292/316), NO emitter. Remove from the model; side-channel service→Location to
  `ServiceValidator` (mirror the existing `routeLocations` ImmutableDictionary keyed by
  ImplementationTypeName). Touch: service-definition.cs, service-extractor.cs:254,
  service-validator.cs, ModelValidator.Validate signature + nuru-generator.cs:382.

### D4 — M4 enum-info extraction (FULLY achievable — verified only 2 consumers)
`completion-data-extractor.cs:138` and `capabilities-emitter.cs:364` are the ONLY emit
uses of `compilation` (both `GetTypeByMetadataName(typeName).GetMembers()...enum members`).
New `EnumInfo(string MetadataTypeName, EquatableArray<string> MemberNames)` +
`EnumInfoExtractor.Resolve(model, compilation, ct) → EquatableArray<EnumInfo>`. Pipeline:
`enumInfoProvider = modelProvider.Combine(CompilationProvider).Select(Resolve)` — re-runs
each edit but its OUTPUT is equatable, so when enum members are unchanged emit compares
equal and is CACHED. Emitter signatures drop `Compilation`, consume the precomputed set.
Leave `assemblyMetadata` (already projects to an equatable record).

### D5 — generator-37 cacheability test
`tests/timewarp-nuru-tests/generator/generator-37-incrementality-caching.cs`, mirror
generator-28 but `GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: true)`.
Two trees (A = Nuru app w/ enum route param; B = unrelated); run 1 over [A,B], capture the
returned driver; run 2 with B replaced by a whitespace/unused-field edit (A untouched);
assert every `"NuruGeneratorModel"` and `"NuruEnumInfo"` step output reason ∈
{Cached, Unchanged}. Add to run-ci-tests.cs standaloneTests AND Directory.Build.props
CiTestExcludes.

### Commit sequence (each builds green)
1. add EquatableArray (+ equatable dictionary). 2. wrap model arrays + InterceptSitesByMethod.
3. strip Location (InterceptSiteModel string; ServiceDefinition side-channel).
4. M4 enum-extract + pipeline split/rewire + WithTrackingName. 5. generator-37 + CI wiring.

Non-IEquatable audit: only `Diagnostic` fails (kept off emit path). `SegmentDefinition` is
an abstract record — synthesized IEquatable uses EqualityContract → correct polymorphic
equality. Watch `DelegateSignature`/`RouteWithSignature` reachability before wrapping;
`MiddlewareDefinition.Configuration` dict is low-priority (usually null).

## Sequencing (reviewer, 2026-07-07)

LAST of the analyzer trio: after 454-011 and 454-012 — this refactor rewrites model
types across many files and would force rebases of any concurrent analyzer work.
Verify cacheability empirically: GeneratorDriver with trackIncrementalGeneratorSteps
asserting step reasons are Cached/Unchanged on a no-op edit (the generator-28 harness
shows how to host the driver).
