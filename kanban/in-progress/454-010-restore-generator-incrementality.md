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
- [~] Location/ImmutableDictionary removed from emit model (the THIRD killer):
      - [x] InterceptSiteModel → precomputed string; InterceptSitesByMethod dict → EquatableArray<InterceptSiteGroup> (commit 6a9df1ad)
      - [ ] ServiceDefinition.RegistrationLocation → remove + side-channel to ServiceValidator (see progress note)
- [ ] CompilationProvider dependency narrowed (M4 enum-info extraction) + pipeline split
- [ ] Verify cacheability (generator-37: trackIncrementalGeneratorSteps → Cached)
- [ ] `ganda runfile cache --clear` + run CI tests

## Progress (reviewer, 2026-07-13) — 4 commits, all validated green

Done and behavior-preserving (full CI 1383 total / 1376 passed / 0 failed after each):
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
- **3c — ServiceDefinition.RegistrationLocation**: it is a `Location` in the emit model →
  breaks value equality. Remove it and side-channel service→Location to `ServiceValidator`.
  Pattern exists: `ModelValidator.Validate(model, routeLocations, extensionMethods)` already
  takes a non-cached `IReadOnlyDictionary<string,Location> routeLocations` (nuru-generator.cs
  builds it at :72-96, threads it at :382). Service locations are captured at
  service-extractor.cs:254 (`invocation.GetLocation()`), so the map must be produced by
  `AppExtractor` and flow via `ExtractionResult` → `CreateGeneratorModelWithValidation`
  (nuru-generator.cs:~309) → `ModelValidator.Validate` → new
  `ServiceValidator.Validate(model, serviceLocations)`. The 3 use sites are
  service-validator.cs:266/292/316 (`service.RegistrationLocation ?? Location.None`). Key the
  map by `ImplementationTypeName`. ExtractionResult carrying Locations is fine — it is upstream
  of the cached GeneratorModel; only the model that feeds EMIT must be Location-free.
- **Commit 4 — M4 + pipeline split** (nuru-generator.cs:104-144): split the single
  `RegisterSourceOutput` into (1) an uncached diagnostics/logger-warning output over
  `generatorModelWithDiagnostics`, and (2) a CACHEABLE emit output over
  `modelProvider.Combine(enumInfoProvider)` — no `Compilation`, no diagnostics. Add
  `EnumInfo(string MetadataTypeName, EquatableArray<string> MemberNames)` +
  `EnumInfoExtractor.Resolve(model, compilation, ct)`; the only two `compilation` enum
  consumers are completion-data-extractor.cs:138 and capabilities-emitter.cs:364 (both drop
  their `Compilation` param, consume the precomputed set). Add `.WithTrackingName("NuruGeneratorModel")`
  / `("NuruEnumInfo")`.
- **Commit 5 — generator-37** cacheability test (see D5 in plan) + CI wiring.

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
