# Round 3 — merged findings
**Date:** 2026-10-18
**Sources:** general, tests, plan_alignment
**Reviewed commit:** `bb2d2b9f` (fix delta `572ecdc1..bb2d2b9f`, product paths only)

The orchestrator checked these claims against the code at `bb2d2b9f`:
- `command-construction-emitter.cs:79-113`: init-only conditional members expand to `1 << initConditional.Count` object-initializer branches. `endpoint-extractor.cs:1186-1189` classifies `= null!` on a non-nullable reference as the `default!` sentinel. Confirmed (M33).
- `command-construction-emitter.cs:62-73`: every sentinel option goes to `conditional` or `discards` without a `required` check. Confirmed (M34).
- `handler-invoker-emitter.cs:348-352`: emits `return 1;` from `EmitCommandInvocation`, and that body is placed inside the `INuruBehavior` `async () => { }` lambda. Confirmed (M35).
- `command-construction-emitter.cs:173-188`: `MatchOption` compares with `StringComparison.OrdinalIgnoreCase`. Confirmed (M36).
- `endpoint-extractor.cs:1122-1130`: the enum branch runs when `constantValue.Value` is null. Confirmed (M37).

The general reviewer compiled and ran throwaway apps under `/tmp/r3` against the worktree projects for every bug from M33 to M39.

Runs by the tests reviewer at `bb2d2b9f` after `ganda runfile cache --clear`:
- Focused tests: derivation 28/28, dispatch 7/7, generator-55 8/8, routing-33 19/19.
- Full CI suite exit 0. Multi-mode: 1920 total, 1914 passed, 0 failed, 6 skipped. All 6 skips already exist at merge base `5e512c0a`. Standalone: 30 files, 0 failed.
- Calculator-contracts sample printed `5`, `1`, `2`, and `total:6|average:3|notes:1`, each with exit 0.
- Benchmark `--aot`: mediator-first-us 27971, direct-first-us 2509, per-call 3 µs vs 1 µs. Cold starts were 3,2,2 ms for both binaries.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 11 | 0 |
| suggestion | 0 | 8 | 0 |
| nit | 0 | 10 | 0 |

Carried round-2 IDs: M20 and M22–M32 are fixed. M29 counts as fixed for the docs and for the exit-1 path when there is no behavior. Its remaining defect, CS8031 when an `INuruBehavior` applies, is the new bug M35. Round-1 IDs M1–M19 and M21 stay fixed.

## Prior findings (round 2)

| ID | Status | Evidence |
|----|--------|----------|
| M22 | fixed | `command-construction-emitter.cs:62-73,116-120`, `json-args-emitter.cs:347-351,564-567`. When omitted, `TimeSpan.FromSeconds(30)` and `Guid.NewGuid()` keep their initializers on argv and on `--json-args`, and supplied values override them. The `required` and case regressions on this path are M34 and M36. |
| M23 | fixed | `endpoint-extractor.cs:1122-1130,1184-1222`. NaN, infinities, a const enum field, flags, and cast enum defaults compile and print correct values. The `= null` regression is M37. |
| M24 | fixed | `operation-endpoint-extractor.cs:686-687,696-741`, `command-construction-emitter.cs:38-50`, and NURU_H009 at `diagnostic-descriptors.handler.cs:117`. A positional record with a `required init` member and keyword-named constructor parameters works. Gaps remain in M38, M39, and M40. |
| M25 | fixed | `route-matcher-emitter.cs:1666-1672` strips the `@`. `T508ParamsQuery` compiles and rejects `--Params`. |
| M26 | fixed | `endpoint-extractor.cs:848,853,875`. A derived `[Parameter] new string Name` produces only `name` and `Keep`, with no CS0128. |
| M27 | fixed | Both new generator-55 tests fail when their fix is reverted. M4: a single app that calls only `UseDirectInvoke()` still declares `__GetMediator`. M2: with `TimeWarpMediatorAssembly=false`, the nested handler is called directly. |
| M28 | fixed | The benchmark reports µs, times the first call separately, computes the decision, and runs three alternating AOT cold starts. The figures are in Results. Order bias in the in-process first call is M47. |
| M29 | fixed (docs; no-behavior path) | `neutral-contracts.md:60` names the condition. `handler-invoker-emitter.cs:348` exits 1. CS8031 when a behavior applies is M35. Runtime-DI wording is M48. |
| M20 | fixed | `operation-endpoint-extractor.cs:690-694` uses `FullyQualifiedFormat.WithGlobalNamespaceStyle(Omitted)`. |
| M30 | fixed | `endpoint-extractor.cs:93-98` runs before `ValidateRoutePattern` at `:101`. |
| M31 | fixed | `capabilities-emitter.cs:331-339`. Capabilities show `1.5`, `Blue`, `NaN`, and `Infinity`. The `= null` display is M37, and the flags display is M45. |
| M32 | fixed | The NURU_H008 message says "invoking this route exits 1". generator-55 asserts it. |

## Issues

### M33 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/command-construction-emitter.cs:79-113; endpoint-extractor.cs:1186-1189
- Description: Each init-only member with a `default!` sentinel doubles the number of emitted object initializers. The emitter does this once for argv and once for `--json-args`. `= null!` on a non-nullable reference is classified as the sentinel, so the common DTO shape `public string X { get; init; } = null!;` triggers it.
  - 12 such members: 8192 `__command = new` sites, an 84,270-line `NuruGenerated.g.cs`, and a 117 s build. The same type with `set`: 2,402 lines and 4.9 s.
  - At 31 or more members, `1 << n` overflows.

  `[NuruRoute]` `[Option]` init members go through the same emitter, so existing 3.0 apps are affected. No test exercises the `initConditional` branch (tests Issue 2).
- Suggestion: Emit `null!` as a real literal, which needs no conditional. For initializers the generator really cannot copy, use a linear form. For example, build `var __defaults = new T(args) { /* required */ };` once and assign `Prop = __supplied ? local : __defaults.Prop`. Add a test with several init-only members that have non-copyable defaults, and assert both the omitted and the supplied values on argv and on `--json-args`.
- Source: general (Issue 1), tests (Issue 2, init branch coverage)
- Disposition notes: Fixed in fix pass 4. `command-construction-emitter.cs` builds `__commandDefaults` once and assigns `P = supplied ? p : __commandDefaults.P` inside one object initializer, so the code grows linearly. `= null!` is copied as `null!` (`EndpointExtractor.NullForgivingDefaultLiteral`), needs no supplied test, and `--capabilities` omits it. `Should_keep_or_replace_many_init_only_initializers` covers omitted and supplied values on argv and on `--json-args`.

### M34 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/command-construction-emitter.cs:62-73,116-120
- Description: A `required` member whose initializer the generator cannot copy is now assigned conditionally.
  - A settable member is assigned after `new()`.
  - An init-only member is left out of some branches.
  - An argv-unsupplied JSON member is discarded.

  Each case is CS9035 in generated code. Examples: `public required TimeSpan Wait { get; set; } = TimeSpan.FromSeconds(1);`, `required Payload Body { get; set; } = new();`, and `required ... = null!`. This compiled before the delta, including on a 3.0 `[NuruRoute]` `[Option]`. It also contradicts fix-pass acceptance bullet 3: "A `required init` member is set in the object initializer".
- Suggestion: Always put `required` members in the object initializer, because the route already rejects omission. Carry the required flag on `OptionDefinition` and keep those members out of `conditional`. Add a derivation test with `required` plus `= new()`.
- Source: general (Issue 2), plan_alignment (Issue 2)
- Disposition notes: Fixed in fix pass 4. `OptionDefinition.IsRequiredMember` is set from `IPropertySymbol.IsRequired` in every extractor. A required member always goes in the object initializer, and `NeedsJsonSuppliedFlag` returns false for it. `T508RequiredDefaultQuery` covers `required` with `TimeSpan.FromSeconds`, `= null!` on `init`, and a JSON `= new()`.

### M35 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs:348-352
- Description: The new no-generated-mediator exit-1 path emits `return 1;` from `EmitCommandInvocation`. When any `INuruBehavior` applies, that body sits inside the `async () => { ... }` lambda passed to `HandleAsync` (`behavior-emitter.cs:282-295`). That lambda is `Func<ValueTask>`, so the build fails with CS8031 on both the argv and `--json-args` paths. The trigger is `TimeWarpMediatorAssembly` off (or the `Aot`/`Link` profile), plus a behavior, plus an endpoint whose handler is visible but not nested. Before the delta this compiled and threw at runtime. The general reviewer reproduced it. The generator-55 test checks generated text with no behavior and never compiles it.
- Suggestion: Emit the guard before the behavior pipeline, next to `InterceptorEmitter.EmitMissingHandlerOnDispatch`. Alternatively, inside the lambda, set `Environment.ExitCode = 1` and `return;`. Add a generator-55 case with `.AddBehavior(...)` and `TimeWarpMediatorAssembly=false` that compiles the output and asserts no errors.
- Source: general (Issue 3), plan_alignment (Issue 1)
- Disposition notes: Fixed in fix pass 4. `InterceptorEmitter.EmitMissingHandlerOnDispatch` emits the error and `return 1;` before the behavior pipeline for a visible external handler with no generated mediator. `HandlerInvokerEmitter` emits only a comment on that path. A new generator-55 case compiles `.AddBehavior(...)` with `TimeWarpMediatorAssembly=false` and asserts no errors (CS9137 from the harness is excluded). With the fix reverted, it fails with two CS8031 errors.

### M36 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/command-construction-emitter.cs:173-188 (`MatchOption`)
- Description: `MatchOption` returns the first option whose `LongForm` or `ParameterName` matches ignoring case. It drives both the sentinel decision and the `__supplied_{id}` slot, which brings back the `--Quiet`/`--quiet` mix-up fixed in round 2. Repro: `[Option("quiet")] string Level = Init()` plus a derived `bool Quiet`. `--Quiet` and `{"Quiet":true}` both print `quiet:False`. `NeedsJsonSuppliedFlag` matches exact names first, so the two can also disagree about which `__supplied_` local exists, which can leave an undeclared identifier.
- Suggestion: Match exact names first, then fall back to ignoring case only for options not already claimed. Share one matcher between `CommandConstructionEmitter`, `NeedsJsonSuppliedFlag`, and `AssignVariables`. Add a mixed-case test with a sentinel option.
- Source: general (Issue 4)
- Disposition notes: Fixed in fix pass 4. New `emitters/option-binding-matcher.cs` matches exact names first and uses ignore-case only for an option no other binding claims exactly. Command construction, `NeedsJsonSuppliedFlag`, and `AssignVariables` all use it. `T508CaseQuery` (`[Option("mute")] string Level = T508Seed.Text()` plus `bool Mute`) covers argv and JSON. `--mute`/`--Mute` is used instead of `--quiet`/`--Quiet`; see Learnings for the unrelated pre-existing route bug.

### M37 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:1122-1130
- Description: The new enum branch also runs for a null constant. `public Shade? Color { get; set; } = null;` emits `(global::Shade)0` with display `"0"`. As a result, `--json-args {}` gives `Red` while argv gives null, and `--capabilities` shows `"defaultValue": "0"`. Before the delta this produced `null`.
- Suggestion: Guard the branch with `constantValue.Value is not null`. Add a test for a nullable enum initialized `= null`.
- Source: general (Issue 5)
- Disposition notes: Fixed in fix pass 4. The enum branch requires `constantValue.Value is not null`. `T508NullableEnumQuery.Unset = null` prints `null` on argv and on `--json-args {}`, and capabilities show `null`.

### M38 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:1160; operation-endpoint-extractor.cs:289
- Description: A positional record can redeclare its property from the parameter: `record Q(string Name) { public string Name { get; init; } = Name; }`. That initializer resolves to an `IParameterSymbol` and is copied as text, so the generated code fails with CS0103. With `= Name.Trim()` it becomes the sentinel and the option turns optional. Omitting `--Name` then passes null to the constructor, and the initializer throws NullReferenceException.
- Suggestion: For a constructor-bound property, or an initializer that references an `IParameterSymbol`, ignore the initializer as a default. Derive required from nullability and the parameter default (M39). Add a test.
- Source: general (Issue 6)
- Disposition notes: Fixed in fix pass 4. An initializer that is exactly a parameter takes that parameter default. Any other initializer that reads an `IParameterSymbol` gives no default, so a non-nullable member is required. `T508PositionalInitQuery` covers `= Name.Trim()` (omission exits 1) and `= Note`.

### M39 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:1099-1101; operation-endpoint-extractor.cs:289
- Description: Primary-constructor parameter defaults are ignored. `record Q(string Name, int Count = 5)` derives `--Count` as required. Without it, argv exits 1 and `--json-args` reports "Missing required value 'Count'". Capabilities report `required: true` with no default. Positional records are the shape M24 targets.
- Suggestion: For a positional property, read `IParameterSymbol.HasExplicitDefaultValue` / `ExplicitDefaultValue` through `FormatConstantLiteral`, and seed the local with that value. Add a test.
- Source: general (Issue 7)
- Disposition notes: Fixed in fix pass 4. A property declared by a primary-constructor parameter reads `HasExplicitDefaultValue` and `ExplicitDefaultValue` through `FormatConstantLiteral`, or through the enum path. `T508PositionalDefaultQuery(string Name, int Count = 5, T508Mode Mode = T508Mode.Slow)` prints `count:5|mode:Slow`. Capabilities show `Count` as not required with default `5`.

### M40 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/command-construction-emitter.cs:40-50; operation-endpoint-extractor.cs:755-773
- Description: Constructor selection accepts a parameter that matches any derivable property, and the emitter skips a parameter that has no binding. On a `[NuruRoute]` type, unmarked properties are not bound, so `record R([property: Option("name")] string Name, int Hidden)` emits `new(name)`, which is CS7036 instead of NURU_H009. This is not a regression, because 3.0 also failed, but NURU_H009 promises the constructor is callable.
- Suggestion: Require each matched property to be bound for the endpoint kind, or report NURU_H009 when an argument would be dropped.
- Source: general (Issue 8)
- Disposition notes: Fixed in fix pass 4. `IsConstructorBoundProperty` requires a `[Parameter]`/`[Option]`/`[GroupOption]` member, or for an `[Operation]` type a derived member that is not from a `[NuruRouteGroup]` base. A new generator-55 case: `record R([property: Option("name")] string Name, int Hidden)` is NURU_H009 and is not constructed.

### M41 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:1572-1575 (from an earlier round of this task)
- Description: When `Shade? Third { get; set; } = Shade.Green` is omitted, argv gives null, while `--json-args {}` and `--capabilities` give `Green`. The same happens for `(Tint)1` on `Tint?`. This contradicts "omission keeps the initializer".
- Suggestion: Initialize the nullable-enum local with `DefaultValueLiteral` when one is present, as the non-nullable branch at `:1587-1590` does.
- Source: general (Issue 9)
- Disposition notes: Fixed in fix pass 4. The nullable-enum local starts from `DefaultValueLiteral ?? "null"`. `T508NullableEnumQuery.Preset = T508Mode.Slow` prints `Slow` on argv and on `--json-args {}`, and `(T508Access)3` on `T508Access?` prints `Read, Write`.

### M42 — Severity: suggestion — Status: fixed
- File: tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs:379,1056
- Description: The M22 tests only omit `Delay`, `Token`, and `Items`. If the post-construction `if (supplied) __command.X = x;` path dropped supplied values, every test would still pass. `T508InitQuery.Extra` is only tested omitted, so the tests do not show that non-required `init` members are back on the CLI.
- Suggestion: Assert `t508defaults --Delay 00:00:05` and `--json-args '{"Delay":"00:00:05"}'` both print `delay:5`, and assert that `t508init --Name ada --Extra 5` prints `extra:5`. M33 covers the init-only conditional branch test.
- Source: tests (Issues 1, 2)
- Disposition notes: Fixed in fix pass 4. `Should_use_a_supplied_value_over_an_initializer_the_generator_cannot_copy` asserts `--Delay 00:00:05` and `{"Delay":"00:00:05"}` print `delay:5`, and `t508init --Name ada --Extra 5` prints `extra:5`. The init-only branch is covered under M33.

### M43 — Severity: suggestion — Status: fixed
- File: documentation/user/features/neutral-contracts.md:13; changelog.md:11; source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.handler.cs:117
- Description: The user docs do not cover `init` members, binding through the primary constructor, or NURU_H009. The changelog `### Added` lists NURU_H008 but not the new NURU_H009 Error. The NURU_H009 message has three inaccuracies:
  - It says "public parameterless constructor", but internal constructors are accepted.
  - It does not say that parameter matching is case-insensitive and only against public settable properties.
  - It fires for abstract and static types with a misleading reason.
- Suggestion: Add a sentence to "What the generator derives" and add NURU_H009 to the changelog `### Added`. Reword the message to "... no accessible parameterless constructor and no constructor whose parameters all match public settable properties". Give abstract and static types their own reason, or do not treat them as endpoints.
- Source: plan_alignment (Issue 4)
- Disposition notes: Fixed in fix pass 4. The docs describe `init` members, constructor binding, parameter defaults, `required`, and NURU_H009. The changelog `### Added` lists NURU_H009. The message is now "Nuru cannot construct '{0}': {1}". It has its own reason for static, abstract, and non-class types, and otherwise says "no accessible parameterless constructor and no constructor whose parameters all match, ignoring case, public settable properties bound as route parameters or options". A generator-55 case asserts the abstract reason.

### M44 — Severity: nit — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs:746-749
- Description: `HasCallableParameterlessConstructor` requires zero parameters. A `[NuruRoute]` type whose only constructor has all-optional parameters compiled on 3.0 and now gets NURU_H009, an Error.
- Suggestion: Treat a constructor whose parameters are all optional or `params` as parameterless.
- Source: general (Issue 10)
- Disposition notes: Fixed in fix pass 4. `HasCallableParameterlessConstructor` accepts an accessible constructor whose parameters are all optional or `params`. A generator-55 case asserts no NURU_H009 and `__command = new()`.

### M45 — Severity: nit — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:1125,1135-1138; documentation/user/features/neutral-contracts.md:17
- Description: The docs say "Numeric defaults are plain numbers. An enum default is the member name." Two cases do not match:
  - A flags combination shows `"3"`, while `allowedValues` lists member names.
  - A non-const `static readonly` field default shows `global::Ns.Defaults.Color` in `--capabilities`.
- Suggestion: Show comma-joined member names for an exact `[Flags]` decomposition, or omit the display for a non-const symbol. Otherwise narrow the docs sentence to match.
- Source: general (Issue 11), plan_alignment (Issue 5)
- Disposition notes: Fixed in fix pass 4. `EnumDisplay` shows an exact `[Flags]` decomposition as comma-joined member names in `Enum.ToString` order (`Read, Write`). A non-constant field or property default has no display, and capabilities no longer fall back to the code literal. The docs sentence covers both cases.

### M46 — Severity: nit — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:1193-1202
- Description: The `float` NaN and infinity branch has no test. `T508SpecialQuery` covers only `double`, but round-2 M23 named `float.NaN` → `NaN.0f`.
- Suggestion: Add `float Soft = float.NaN` and one infinity to `T508SpecialQuery`, and assert the runtime value and the capabilities value.
- Source: tests (Issue 3)
- Disposition notes: Fixed in fix pass 4. `T508SpecialQuery` has `float Soft = float.NaN` and `float Rise = float.PositiveInfinity`. The output asserts `soft:nan|rise:inf`, and capabilities assert `NaN` and `Infinity`.

### M47 — Severity: nit — Status: fixed
- File: benchmarks/operation-dispatch/operation-dispatch-overhead.cs:19-22
- Description: The in-process run always measures the mediator path first. `mediator-first-us` (about 28 ms) therefore includes the shared runtime JIT, so the comparison with `direct-first-us` (about 2.5 ms) in the changelog and Results overstates mediator startup cost. The alternating AOT cold starts show parity. Per-call figures are truncated to whole µs.
- Suggestion: Alternate the order or prime the shared code first, or note that the AOT cold start is the startup comparison. Report per-call figures in ns or with one decimal.
- Source: tests (Issue 4), plan_alignment (Issue 7)
- Disposition notes: Fixed in fix pass 4. `PrimeSharedAsync` runs a delegate route first, so shared Nuru code is JIT-compiled before either first call. Per-call cost is `*-per-call-ns`. The header comment and the changelog name the `--aot` cold start as the startup comparison. New run: mediator 3296 ns vs direct 1788 ns per call. AOT cold starts: `4,3,3` ms vs `3,2,2` ms.

### M48 — Severity: nit — Status: fixed
- File: documentation/user/features/neutral-contracts.md:60
- Description: The docs say a visible handler that is not nested "writes an error and exits 1 instead of throwing". That is true only for source-gen DI, because the guard is `!useRuntimeDI && !hasGeneratedMediator`. Under `UseMicrosoftDependencyInjection` the code resolves `IMediator` from the container and throws unless the user registers one.
- Suggestion: Qualify the sentence for runtime DI.
- Source: plan_alignment (Issue 3)
- Disposition notes: Fixed in fix pass 4. `neutral-contracts.md` says exit 1 applies with source-generated DI. Under `UseMicrosoftDependencyInjection()`, `IMediator` comes from the container and the call throws unless a mediator is registered.

### M49 — Severity: nit — Status: fixed
- File: kanban/to-do/508-001-attributes-only-package-timewarpnurucontracts-for-route-parameter-option-attributes-and-capabilities-types/task.md:39
- Description: The restatement's acceptance bullet still says "otherwise startup reports an error". Since M18, invoking that route exits 1, and `--help`/`--capabilities` still run.
- Suggestion: Reword it to "otherwise invoking that route exits 1 (a deliberate change from a startup error; see Learnings)".
- Source: plan_alignment (Issue 6)
- Disposition notes: Fixed in fix pass 4. The acceptance bullet now reads "otherwise invoking that route exits 1 (a deliberate change from a startup error; see Learnings)".

## Duplicates / conflicts

- M33: general Issue 1 (exponential growth), merged with the init-only branch coverage part of tests Issue 2.
- M34: general Issue 2 and plan_alignment Issue 2.
- M35: general Issue 3 and plan_alignment Issue 1. This is the residual code defect of M29.
- M42: tests Issue 1 and the `Extra` part of tests Issue 2.
- M45: general Issue 11 and plan_alignment Issue 5.
- M47: tests Issue 4 and plan_alignment Issue 7.

## Verdict

17 findings are open: 7 bugs (M33, M34, M35, M36, M37, M38, M39), 4 suggestions (M40–M43), and 6 nits (M44–M49). A fix pass is needed. All 12 round-2 findings are fixed, and the full CI suite is green. Each new bug was reproduced by compiling a throwaway app, and no test covers any of them.
