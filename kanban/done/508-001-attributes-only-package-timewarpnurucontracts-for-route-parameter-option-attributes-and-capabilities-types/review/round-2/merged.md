# Round 2 — merged findings
**Date:** 2026-10-18
**Sources:** general, tests, plan_alignment
**Reviewed commit:** `e316fd32` (fix delta `d769f8b1..e316fd32`, product paths only)

The orchestrator checked these claims against the code:
- `route-matcher-emitter.cs:1640-1670`: the flag name is built from the escaped variable name (`@params`). Confirmed (M25).
- `endpoint-extractor.cs:1082-1150`: NaN and infinity values go through `ToString("R")`, and a const field of enum type falls through to `IFormattable`. Confirmed (M23).
- `operation-endpoint-extractor.cs:666`: `IsInitOnly: false` excludes init members. Confirmed (M24).
- `operation-endpoint-derivation.cs:372,797-798` asserts `items:null`, while `neutral-contracts.md:17` says "Omission keeps the initializer". Confirmed (M22).

Runs by the tests reviewer at `e316fd32` after `ganda runfile cache --clear`:
- Focused tests: derivation 24/24, dispatch 7/7, generator-55 3/3.
- Full CI suite exit 0. Multi-mode: 1916 total, 1910 passed, 0 failed, 6 skipped. All 6 skips already exist on master. Standalone: 0 failed.
- Calculator-contracts sample: correct output.
- Sample 02-calculator built with `-p:TimeWarpMediatorAssembly=false` uses direct invoke and prints `5`.
- Sample 15-httpclient: no DI error.
- Benchmark `--aot` cold start: mediator 3.66 ms, direct 2.86 ms, one sample each. Repeat runs were about 6 ms for both, so the gap is noise.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 4 | 8 | 0 |
| suggestion | 4 | 9 | 0 |
| nit | 4 | 3 | 0 |

Carried round-1 IDs: M1–M19 and M21 are fixed. M20 is reopened. M12 is counted as fixed for records with `{ get; set; }`. Its remaining gap (positional records and `init` members) is the new bug M24.

## Prior findings (round 1)

| ID | Status | Evidence |
|----|--------|----------|
| M1 | fixed | No `.DiscoverEndpoints()` in either new file. Multi-mode has 0 failures. |
| M2 | fixed | `handler-invoker-emitter.cs:301-302` and `generator-model.cs:99-113`. Manual no-mediator build of sample 02 prints `5`. No automated test exists (see M27). |
| M3 | fixed | `interceptor-emitter.cs:489-499`. Sample 15 runs. |
| M4 | fixed | `generator-model.cs:88-95`. The test does not guard the fix (see M27). |
| M5 | fixed | `endpoint-extractor.cs:1546-1552`, `type-conversion-map.cs`, `default-type-converters.cs` |
| M6 | fixed for the listed cases | `endpoint-extractor.cs:1082-1158`. The `default!` sentinel has a new runtime defect (M22), and some constants still format wrong (M23). |
| M7 | fixed | `endpoint-extractor.cs:91-93`. Ordering nit M30. |
| M8 | fixed | Warning in the descriptor, `AnalyzerReleases.Unshipped.md:36`, docs, and generator-55. Message wording nit M32. |
| M9 | fixed | `nuru-generator.cs:57,314-326` |
| M10 | fixed | `operation-endpoint-extractor.cs:477-499`. The cache is a `ConditionalWeakTable` keyed on the compilation, so it is thread-safe and does not keep old compilations alive or go stale. |
| M11 | fixed | `handler-invoker-emitter.cs:439-512` |
| M12 | fixed (records with `set`) | `nuru-generator.cs:306-309`, `T508RecordQuery`. Positional records and `init` members: M24. |
| M13 | fixed | `route-matcher-emitter.cs:240,359,1641-1664` |
| M14 | fixed | `operation-endpoint-extractor.cs:234-254`, `endpoint-extractor.cs:821-848`. Edge case M26. |
| M15 | fixed | Pipeline-behavior count differs between paths. No `/tmp` write. The timing is in the benchmark. Benchmark output: M28. |
| M16 | fixed | Every option property except `name`, plus the endpoint-level keys. |
| M17 | fixed | `changelog.md:10-16` |
| M18 | fixed | `interceptor-emitter.cs:1218-1256`, `route-matcher-emitter.cs:244,361`, `json-args-emitter.cs:375` |
| M19 | fixed | `operation-endpoint-extractor.cs:92` |
| M20 | open | See below. |
| M21 | fixed | The first stderr line equals `https://example.test/t508 Bad input`. |

## Issues

### M22 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:1101-1107; route-matcher-emitter.cs:1622; tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs:372,797-798
- Description: When an option is omitted, the `default!` sentinel overwrites the contract's own initializer. The sentinel becomes the local's initializer, and the object initializer always assigns that local. This happens on argv and on `--json-args`. Examples:
  - `public Payload Body { get; set; } = new();` reaches the handler as null, even though it is non-nullable.
  - `= TimeSpan.FromSeconds(30)` reaches it as `TimeSpan.Zero`.
  - `= Guid.NewGuid()` reaches it as `Guid.Empty`.

  `neutral-contracts.md:17` says "Omission keeps the initializer", and the test asserts `items:null`, which locks in the data loss.
- Suggestion: Keep omitting such a default from capabilities. At runtime, do not assign the member when the option is omitted. For example, track whether it was supplied and assign it after construction, or leave it out of the object initializer. Change the test to expect the initializer's value.
- Source: general (Issue 4), tests (Issue 1), plan_alignment (Issue 1)
- Disposition notes:

### M23 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:1082-1158 (`FormatConstantLiteral`)
- Description: Some constant defaults emit code that does not compile. These values come out bare:
  - `double.NaN` → `NaN`
  - `double.PositiveInfinity` → `Infinity`
  - `float.NaN` → `NaN.0f`

  A const field of enum type (`= Defaults.Color`, where the field is not on the enum type) and a flags or cast enum value (`A | B`, `(Color)3`) come out as a bare integer (CS0266 for non-zero). The NaN and const-enum cases also break existing `[Option]` properties that compiled on 3.0.0.
- Suggestion: Emit `double.NaN`, `double.PositiveInfinity`, `double.NegativeInfinity`, and the float equivalents explicitly. When the property type (or its `Nullable<T>` argument) is an enum, emit a cast to the fully qualified enum type, or keep the symbol form. Add regression tests for NaN and a const-enum default on an `[Option]` property.
- Source: general (Issue 3)
- Disposition notes:

### M24 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs:666 (`IsDerivableProperty`)
- Description: The fix added `IsInitOnly: false`, so `init` members are never derived. This causes three failures:
  - `public required string Name { get; init; }` is left out of the generated object initializer, so the build fails with CS9035.
  - A non-required `init` member silently disappears from the CLI. Before this delta, `init` members were derived.
  - A positional record such as `[Operation("get-user")] public sealed record GetUserQuery(int Id) : IQuery<User>;` has no parameterless constructor, and `new() { ... }` fails with CS7036. This is the usual contract shape, and the requirement covers every public settable member.
- Suggestion: Drop the `IsInitOnly` restriction, because object initializers can set `init` members. For a type with no accessible parameterless constructor, either bind through the primary constructor by parameter name or skip it with a diagnostic. Add tests for a positional record and a `required init` member.
- Source: general (Issue 2; residual of M12)
- Disposition notes:

### M25 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:1651,1666 (`JsonArgvRejectedFlag`)
- Description: The JSON-argv rejection flag name is built from the keyword-escaped variable name. A JSON member whose camel-cased name is a C# keyword (`Params`, `Event`, `Object`, `Class`) produces `__jsonArgvRejected_@params_0`. That identifier is not valid, so the generated code does not compile.
- Suggestion: Build the flag name from the unescaped name, or strip the leading `@`. Add a test with a keyword-named JSON member.
- Source: general (Issue 1)
- Disposition notes:

### M26 — Severity: suggestion — Status: open
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:833,861-868
- Description: The inherited-member walk compares the base property name with existing segments' `LongForm` / `Name`. Attribute segments are lowercased or renamed, so a derived type that overrides a base property with `[Parameter]` or `[Option("other")]` gets a second derived option. Its local collides with the positional local (CS0128). The bindings walk compares `ParameterName == property.Name` and is not affected, so the two walks disagree.
- Suggestion: Skip a base property when the derived type declares or overrides a property of the same name (compare symbols or `OverriddenProperty`), not segment spellings. Add a test.
- Source: general (Issue 5)
- Disposition notes:

### M27 — Severity: suggestion — Status: open
- File: tests/timewarp-nuru-tests/auto/operation-endpoint-dispatch.cs:80-94
- Description: Two fixes have no test that guards them:
  - M4 fails only when every app in the compilation calls `UseDirectInvoke()`. Other apps in the same file and in CI do not, so `Should_keep_an_external_handler_on_the_mediator_when_direct_invoke_is_set` passes with the fix reverted.
  - The M2 fallback (no generated mediator → direct invoke) has no automated test. Every test compilation gets a generated mediator, and only a manual run covers it.
- Suggestion: Add a generator test (in the style of generator-55) that compiles one app with `UseDirectInvoke()` and an external-handler operation and asserts no CS0103. Add one more that compiles with `TimeWarpMediatorAssembly` off and asserts that the nested handler is invoked directly.
- Source: tests (Issues 2, 3)
- Disposition notes:

### M28 — Severity: suggestion — Status: open
- File: benchmarks/operation-dispatch/operation-dispatch-overhead.cs
- Description: The requirement says to measure startup and per-call overhead. Startup is not recorded. The first mediator call, which builds the container, falls inside the warm-up runs, and task.md says AOT was not run. The tests reviewer ran `--aot` once (3.66 ms mediator, 2.86 ms direct; about 6 ms each on repeat), so the script works, but the task has no figures. The output also has these problems:
  - It prints raw Stopwatch ticks without the frequency or unit. They are nanoseconds on Linux.
  - `decision=keep-UseDirectInvoke` is hard-coded, not computed.
  - The AOT cold start is one sample per binary, and the mediator binary always runs first.
- Suggestion: Report elapsed time in µs or ms, measure first-call (container build) separately, alternate the binaries and take several cold-start samples, and record the startup and AOT figures in Results.
- Source: plan_alignment (Issue 2), tests (Issue 4)
- Disposition notes:

### M29 — Severity: suggestion — Status: open
- File: documentation/user/features/neutral-contracts.md; source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:456-458
- Description: The docs do not say when "no generated mediator exists" (`TimeWarpMediatorAssembly` not true, or the `Aot`/`Link` mediator profile). In that build:
  - A nested-handler `[Operation]` endpoint silently skips mediator behaviors such as FluentValidation.
  - An endpoint whose handler is visible but not nested throws `InvalidOperationException` from `__GetMediator()` instead of exiting 1 with a message.

  The generator knows this at build time and reports nothing.
- Suggestion: Document the condition. Emit a warning when an `[Operation]` endpoint compiles without a generated mediator, or turn the `__GetMediator()` failure into the same exit-1 message that NURU_H008 routes use.
- Source: plan_alignment (Issue 3)
- Disposition notes:

### M20 — Severity: nit — Status: open (reopened)
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs:668-671 (`StripGlobalPrefix`)
- Description: Only a leading `global::` is removed. `json List<global::Ns.Note>` still shows the alias inside type arguments. The test covers only an array.
- Suggestion: Format with a display format that omits the global namespace (`SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(Omitted)`), and add a generic-list case to the test.
- Source: general (Issue 8)
- Disposition notes:

### M30 — Severity: nit — Status: open
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:91-93
- Description: The M7 skip of a non-mediator `[Operation]` type runs after route-pattern validation, so a type that is not an endpoint can still get a route-pattern diagnostic.
- Suggestion: Run the mediator-kind check before pattern validation.
- Source: general (Issue 6)
- Disposition notes:

### M31 — Severity: nit — Status: open
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs (default literal reused by capabilities)
- Description: `--capabilities` `defaultValue` now reuses the C# literal. It shows `1.5m` and `2.0f`, including for existing `[Option]` defaults, and an enum default shows `global::Ns.RoundingMode.Nearest` while `allowedValues` shows `Nearest`. No test pins this format.
- Suggestion: Keep a separate display value for capabilities: the plain number, and the enum member name only. Assert it in the parity test.
- Source: general (Issue 7), tests (Issue 4)
- Disposition notes:

### M32 — Severity: nit — Status: open
- File: source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.handler.cs:108
- Description: The NURU_H008 message still says "or the app fails at startup". Since M18, the app fails only when that route is invoked.
- Suggestion: Say "invoking this route exits 1".
- Source: plan_alignment (Issue 4)
- Disposition notes:

## Duplicates / conflicts

- M22: general Issue 4, tests Issue 1, and plan_alignment Issue 1 report the same defect.
- M24: general Issue 2. It is the residual of M12, which stays fixed for the record shape it named.
- M27: tests Issues 2 and 3, both about missing regression guards for the dispatch-selection fixes.
- M28: plan_alignment Issue 2 and the benchmark parts of tests Issue 4.
- M31: general Issue 7 and the enum-default part of tests Issue 4.
- M20: general Issue 8 (partial fix), reopened under its round-1 ID.

## Verdict

12 open findings (4 bugs: M22, M23, M24, M25). A fix pass is needed. The full CI suite is green, but none of these bugs is covered by a test.
