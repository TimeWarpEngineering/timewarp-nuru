# Reference TimeWarp.Contracts: derive endpoints from neutral operation contracts

## Description

Nuru consumes the shared neutral contract package (working name `TimeWarp.Contracts`, defined by timewarp-architecture task 384) instead of shipping its own attributes-only package. A request class that carries `[Operation]` and a TimeWarp.Mediator kind interface is a complete Nuru endpoint: Nuru synthesizes the route from the operation name and every public settable member as an option named after the property, with descriptions from `System.ComponentModel.Description`, required from nullability and `required`, defaults from initializers and allowed values from enums. `[NuruRoute]`, `[Parameter]`, `[Option]`, `[NuruRouteExample]` remain as CLI-specific overrides (positional parameters, aliases, different spelling, examples) and keep working unchanged for existing apps.

This task also owns the three framework changes the neutral model needs from Nuru.

## Requirements

- Package reference to `TimeWarp.Contracts`; the generator and analyzers resolve `[Operation]` and the CLI attributes by metadata name. Nuru's own attribute package split (the earlier scope of this task) is dropped: CLI attributes stay in `TimeWarp.Nuru` for now, and may move to a `TimeWarp.Nuru.Contracts` binding package later if a consumer needs CLI overrides without the runtime.
- Derivation rules documented and tested: route literal, option names, required and defaults, enums, repeated values for arrays, group path from a containing `[NuruRouteGroup]` when present.
- Handler placement: a nested `Handler` is a convenience, not a requirement. An endpoint's handler is any registered `IQueryHandler<TQuery, T>` / `ICommandHandler<TCommand, T>`; the generator emits a diagnostic when none is registered at build time where it can tell, otherwise a startup error. This is what lets a CLI edition register a forwarding handler and a daemon the real one for the same request type.
- Mediator dispatch: endpoints dispatch through TimeWarp.Mediator so mediator behaviors (FluentValidation as in architecture) run identically in every host. Nuru behaviors (`INuruBehavior`) remain CLI middleware around the dispatch. Measure startup and per-call overhead against the current direct invoke; keep direct invoke as an opt-out if the cost matters for AOT CLIs.
- Rendering: honor an optional `IResponseRenderer<TResponse>` (from the shared package) before the existing `OutputStrategy` JSON fallback. `SharedProblemDetails` outcomes render to stderr with a non-zero exit code, with the problem type and title on the first line.
- `--capabilities` entries for derived endpoints are indistinguishable from attribute-declared ones.
- Calculator sample gets a sibling written contracts-first; docs page on neutral contracts in `documentation/user/features/`.

## Goal (implementer restatement)

A request that carries `TimeWarp.Contracts.OperationAttribute` and a TimeWarp.Mediator kind (`IQuery<T>`, `ICommand<T>`, or `IIdempotentCommand<T>`) is a complete Nuru endpoint, including a record class. The generator builds the route literal from the operation name and one option per public settable member, including members inherited from a base that is not a `[NuruRouteGroup]` marker. The option name is the property name, the description comes from `System.ComponentModel.DescriptionAttribute`, required comes from the `required` modifier and nullability, the default comes from a constant or field initializer, and allowed values come from enum constants. An `[Operation]` type with no mediator kind is not an endpoint. `[NuruRoute]`, `[Parameter]`, `[Option]`, and `[NuruRouteExample]` stay in `TimeWarp.Nuru` as CLI overrides. Existing nested-handler routes keep working when this compilation has no generated mediator: they call the nested handler directly. When the generated mediator exists, dispatch goes through it unless the app calls `UseDirectInvoke()`. An endpoint with no nested handler still goes through the mediator. This task does not split the CLI attributes into their own package.

Fix-pass acceptance (review round 2, still open when this restatement was written):

- Omitting an option whose initializer the generator cannot emit (`new()`, `TimeSpan.FromSeconds`, `Guid.NewGuid`) leaves that initializer in place on argv and on `--json-args`. `--capabilities` still omits that default.
- `double.NaN`, infinities, a const enum field, and a flags or cast enum default emit compilable C#. `--capabilities` shows a plain number and the enum member name.
- `init` members and a positional record are derived. A positional record is constructed through its primary constructor. A `required init` member is set in the object initializer.
- A JSON member whose name is a C# keyword still generates a valid rejection flag.
- A derived type that redeclares a base property does not also derive the base property.
- Direct invoke with only an external handler still emits `__GetMediator`. A compilation with no generated mediator calls a nested handler directly.
- The overhead script reports microseconds, a separate first call, and several alternating AOT cold starts.
- The docs name when no generated mediator exists. Invoking an external handler in that build exits 1 with a message.
- JSON type names omit `global::` inside type arguments. The mediator-kind check runs before route-pattern validation. NURU_H008 says invoking the route exits 1.

Fix-pass acceptance (review round 3, implemented in fix pass 4):

- Several `init` members whose initializers the generator cannot copy produce code that grows linearly, not one branch per combination. `= null!` is copied as `null!`. Omitted members keep the initializer, and supplied members win, on argv and on `--json-args`.
- A `required` member is always in the object initializer, even when its initializer cannot be copied.
- With no generated mediator, a visible external handler plus an `INuruBehavior` compiles. The exit-1 guard runs before the behavior pipeline.
- Command construction, the `--json-args` supplied flag, and JSON slot assignment use one option matcher that tries exact names first.
- A nullable enum `= null` stays null. A nullable enum with an initializer keeps it when omitted, on argv as on `--json-args`.
- A positional record parameter default is the option default. A record property whose initializer is just the parameter takes that default. Any other initializer that reads a parameter gives no default.
- A constructor is only selected when every parameter matches a property that is bound for the endpoint kind. A constructor whose parameters are all optional counts as parameterless. NURU_H009 gives abstract and static types their own reason and names the matching rule.
- `--capabilities` shows a `[Flags]` combination as member names and omits a non-constant field default. `float` NaN and infinity are tested.
- The overhead benchmark warms shared Nuru code before either first call and reports per-call cost in nanoseconds. The AOT cold start is the startup comparison.
- The docs cover `init` members, constructor binding, NURU_H009, and runtime DI under no generated mediator. The changelog lists NURU_H009.

Acceptance:

- `TimeWarp.Nuru` and the generator/analyzers reference `TimeWarp.Contracts`. They recognize `[Operation]` and the CLI attributes by metadata name.
- Derivation is documented and tested: route literal, option names, required and defaults, enums including non-nullable enums, repeated values for arrays of scalars, a non-scalar member (or array of non-scalars) bound as one JSON value reported as `json` plus the CLR type name, a non-nullable `bool` whose initializer is `true` kept when the option is omitted and shown in `--capabilities`, and a group path taken from a containing `[NuruRouteGroup]` when present, otherwise from an optional group on the CLI binding, otherwise ungrouped.
- A nested `Handler` is optional. The handler is any registered `IQueryHandler<TQuery, T>` or `ICommandHandler<TCommand, T>`. The generator emits a diagnostic when it can see that none is registered; otherwise invoking that route exits 1 (a deliberate change from a startup error; see Learnings).
- Endpoints dispatch through TimeWarp.Mediator when this compilation has a generated mediator, so mediator behaviors run in that host. A nested handler is called directly when no generated mediator exists or when `UseDirectInvoke()` is set. A visible handler that is not nested exits 1 in a build with no generated mediator. `INuruBehavior` stays CLI middleware around that dispatch. Startup and per-call cost against direct invoke is measured, and direct invoke stays available as an opt-out.
- A registered `IResponseRenderer<TResponse>` supplies the text before the existing `OutputStrategy` JSON fallback. A `SharedProblemDetails` outcome is written to stderr with a non-zero exit code, and the first line is the problem type and title.
- `--capabilities` entries for derived endpoints match attribute-declared entries.
- The calculator sample has a contracts-first sibling, and `documentation/user/features/` has a page on neutral contracts.

## Checklist

- [x] Package reference and metadata-name resolution
- [x] Derivation rules, generator and tests (including complex members as JSON, boolean initializer defaults, grouping, non-nullable enums per the 506 findings)
- [x] Handler placement: registered handlers, diagnostics
- [x] Mediator dispatch with Nuru behaviors as middleware; overhead measured
- [x] `IResponseRenderer` and problem rendering
- [x] Sample and docs

## Review

- Round 1: 21 findings, all marked fixed in `review/round-1/merged.md` (8 bugs, 9 suggestions, 4 nits).
- Round 2 (2026-10-18, `e316fd32`): 20 of the 21 round-1 findings are verified fixed. M20 is reopened. 11 new findings: M22–M32. 12 findings were open: bugs M22, M23, M24, M25; suggestions M26, M27, M28, M29; nits M20, M30, M31, M32. CI was green. The fix pass is in Session. See `review/round-2/merged.md`.
- Round 3 (2026-10-18, `bb2d2b9f`): all 12 round-2 findings are verified fixed (M20, M22–M32). The fix delta adds 17 new findings, M33–M49, and all are open:
  - Bugs: M33 (init-only conditional construction grows exponentially), M34 (CS9035 on `required` with an initializer the generator cannot copy), M35 (CS8031 when there is no mediator, the handler is external, and a behavior applies), M36 (case-insensitive `MatchOption`), M37 (nullable enum `= null` becomes `(E)0`), M38 (record property initialized from a constructor parameter), M39 (primary-constructor defaults ignored).
  - Suggestions: M40–M43. Nits: M44–M49.

  CI is green: 1920 total, 1914 passed, 6 skipped, 0 failed. A fix pass is needed. See `review/round-3/merged.md`.
- Round 4 (2026-10-18, `50e92396`): M33–M49 are fixed. No new findings. Disposition is clean. See `review/round-4/merged.md` and `review/disposition.md`.
- Round 4: Done
## Notes

- Parent: 508. Waits for the first published `TimeWarp.Contracts` (architecture task 384); until then develop against a project reference or a local feed.
- Findings from task 506 (Roslynk spike, 2026-10-18) that the derivation rules must cover:
  - **Complex members.** Two Roslynk inputs are arrays of objects (`multi_query.operations`: list of `{tool: enum, arguments: dictionary}`; `apply_patch.baseVersions`: list of `{Path, Version}`). The generator today maps a nested object to `string` and an object array to `string[]`, and the binding probes for these do not compile. Rule to add: a member whose type is not a scalar, enum, or array of scalars binds as one JSON value (the whole object or array) under its member name; `--capabilities` reports its type as `json` plus the CLR type name. Document that argv cannot supply such a member and `--json-args` must.
  - **Boolean defaults.** Six Roslynk parameters are non-nullable `bool` with initializer `= true` (`get_diagnostics.includeAnalyzers`; `get_members.includeMethods`, `includeFields`, `includeProperties`, `includeEvents`, `includeNestedTypes`). A Nuru flag's generated local starts `false`, so omission is indistinguishable from `false` and the default flips. A derived `bool` member with a `true` initializer must keep that default when omitted: emit it as a non-flag option whose omitted value is the initializer (the `bool?` shape the spike used by hand), and show the default in `--capabilities`.
  - **Grouping.** A flat catalog of Roslynk's 29 tools is about 16,000 tokens; `--group-filter` is the intended cut but only works when routes have a group path. Derived endpoints need a group: from a containing `[NuruRouteGroup]` when present, otherwise from an optional group argument on the CLI binding, otherwise ungrouped. Record the decision; 508-004 projects the same group into MCP tool filtering.
  - **Enums.** A non-nullable enum option does not compile today (the argv path yields a nullable enum). Roslynk has no top-level enum parameter, so the gap is latent; derivation should handle non-nullable enums or diagnose them.
- Earlier scope of this task (a Nuru-only attributes and capabilities package) is superseded: the capabilities DTOs become the neutral catalog in the shared package (508-002 projects it).
- Group decision (2026-10-18): the group path is the containing `[NuruRouteGroup]` full prefix when one is present; otherwise `[NuruCliBinding(Group = "segment")]`, one prefix whose spaces are extra literals; otherwise the endpoint is ungrouped. `TimeWarp.Contracts.CliOperationBinding` has `Pattern` only, so the group argument lives on the Nuru attribute. `[NuruCliBinding]` does not add a group type, and `DiscoverEndpoints(typeof(SomeGroup))` still follows `[NuruRouteGroup]` only. 508-004 should read this same `groupPath`.

## Session

- Created: 1479989 (2026-10-17)
- Rescoped to the shared neutral-contract model: 1479989 (2026-10-17)
- 506 findings folded into requirements: 1479989 (2026-10-18)
- Implement walk stopped before product code (2026-10-18). `TimeWarp.Contracts` 2.0.0-beta.20 is on nuget.org and contains only `lib/net11.0`. This repo's `Directory.Build.props` targets `net10.0`. Restoring a net10.0 project that references that package fails with NU1202. `TimeWarp.Mediator.Contracts` 14.0.0 (this repo) and 14.0.0-beta.5 (the contracts package dependency) both ship `lib/net10.0`, so the contracts library can be packed for net10.0 without a mediator bump. `CliOperationBinding` in that package has `Pattern` only; the optional group argument the derivation notes describe is not on the published type. No product files changed.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Blocked — 2026-10-18T05:19:31Z
- Block cleared (2026-10-18): TimeWarp.Contracts 2.0.0-beta.21 published multi-targeted net10.0 + net11.0; relaunched headless.
- Implemented (2026-10-18): implementer-grok, session 01a14e4f-7534-72d0-b741-b68ae80e4fc4. Derived endpoints from TimeWarp.Contracts 2.0.0-beta.21. JSON members deserialize through the source-generated `NuruJsonMemberContext` (property names as declared). `tests/Directory.Build.targets` imports the Nuru build task so file-based tests get that context. A problem outcome sets `Environment.ExitCode` and each `ExecuteRouteAsync` starts by clearing it, so a later success is 0. Direct invoke stays behind `UseDirectInvoke()` and still requires a nested handler.
- Pack fix (2026-10-18): `TimeWarp.Nuru` 3.0.0 is the published stable package. Packing it with a prerelease `TimeWarp.Contracts` dependency fails NU5104. `source/Directory.Build.props` version is now `3.1.0-beta.1`. `ganda repo audit --fix` set the executable bit on the four new shebang runfiles.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-18T10:37:03Z
- Review round 1 (2026-10-18): review oracle claude-opus-5-5 (headless `ganda task work`), effort 3, roster general + tests + plan_alignment (Claude subagents), reviewed `f522c6e2`. The result is 21 open findings (8 bugs), and the full CI suite fails: 6 multi-mode failures. A fix pass is needed. See `review/round-1/merged.md`.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-18T10:50:50Z
- Fix pass (2026-10-18): implementer-grok, session 01a14ea2-bf2e-7013-aaf9-b6dae6d313fb. Disposed M1–M21 as fixed. Nested handlers fall back to direct invoke when no generated mediator exists. External handlers and typed HttpClients stay on the mediator. NURU_H008 is a Warning. Required JSON members are checked after the route matches. AOT cold start was not run.
- Fix pass verified (2026-10-18): implementer-grok, session 01a14f63-0ae5-7df2-a802-244af765040e. Focused tests passed. `ganda runfile cache --clear`, then `tests/ci-tests/run-ci-tests.cs`: multi-mode 1916 total, 1910 passed, 6 skipped, 0 failed; standalone 0 failed. Overhead re-run: warmup 50, iterations 2000, mediator-ticks 6275013, direct-ticks 3483383. `ganda repo audit` passed.
- Review round 2 (2026-10-18): review oracle claude-opus-5-5 (headless `ganda task work`), effort 3, roster general + tests + plan_alignment (Claude subagents), reviewed the fix delta `d769f8b1..e316fd32`.
  - Full CI after `ganda runfile cache --clear`: multi-mode 1916 total, 1910 passed, 6 skipped (all of them already skipped on master), 0 failed. Standalone: 0 failed.
  - Open at review time: 12 findings, 4 of them bugs. See `review/round-2/merged.md`.
- Fix pass for round 3 (2026-10-18): implementer-grok, session 01a14f7a-d5a3-7f90-a5af-bfe3a8b94efb. Omitting an unemittable initializer keeps it. `--json-args` records that the member was supplied in the route case, because the attempt-local flag is out of scope at construction. `--Quiet` and `--quiet` stay different JSON slots. Capabilities show plain numbers and enum member names. `init` members and positional records construct. NURU_H009 reports a type with no callable constructor. A build with no generated mediator calls a nested handler directly and exits 1 for a visible external handler. Review round 3 has not run.
- Review round 3 (2026-10-18): review oracle claude-opus-5-5 (headless `ganda task work`), effort 3, roster general + tests + plan_alignment (Claude subagents), reviewed the fix delta `572ecdc1..bb2d2b9f`.
  - Full CI after `ganda runfile cache --clear`: multi-mode 1920 total, 1914 passed, 6 skipped (all of them already skipped on master), 0 failed. Standalone: 0 failed.
  - Open at review time: 17 findings, 7 of them bugs (M33–M39). The general reviewer reproduced every bug in a throwaway app. A fix pass is needed. See `review/round-3/merged.md`.
- Fix pass 4 (2026-10-18): implementer claude-opus-5-5 (headless `ganda task work`, implement-fix-4). Marked M33–M49 fixed in `review/round-3/merged.md`, with disposition notes. Full CI after `ganda runfile cache --clear`: multi-mode 1927 total, 1921 passed, 6 skipped, 0 failed. Standalone: 30 files, 0 failed. `ganda repo audit` passed with nothing to fix. Review round 4 has not run.
- Review round 4 (2026-10-18): review oracle grok-4.7 (headless `ganda task work`), session `01a14fcd-3a2e-7e22-abc9-51e42db9f7fc`, effort 3, roster general, reviewed the fix delta `bb2d2b9f..50e92396`.
  - `operation-endpoint-derivation.cs`: 35 passed. `generator-55-missing-operation-handler.cs`: 12 passed. Full CI and the AOT script were not re-run.
  - Open at review time: 0. Disposition is clean. See `review/round-4/merged.md` and `review/disposition.md`.
- Version changed to 4.0.0-beta.1 at the merge gate (2026-10-18), per Steven: the neutral-contract derivation and mediator dispatch are a major change, not a 3.1 minor.

## Blocked

Resolved 2026-10-18: review round 4 disposition is clean (0 open). The host raised the review-round limit, and fix pass 4 implemented M33–M49. Original block follows.

2026-10-18: 3 rounds, M33, M34, M35, M36, M37, M38, M39, M40, M41, M42, M43, M44, M45, M46, M47, M48, M49 still open (raise --review-rounds or mark findings fixed or wontfix in review/round-3/merged.md)

## Learnings

- Restatement check (review round 1): no misread of substance. The restatement keeps every requirement, folds in the 506 findings and the group decision, and adds `IIdempotentCommand<T>`. It leaves out the Notes requirement to document that argv cannot supply a JSON member, but the docs cover that anyway.
- NURU_H008 is a Warning. The build is accepted, and invoking a route whose handler the compilation cannot see exits 1 (M8).
- A nested handler uses direct invoke when this compilation has no generated mediator. An external handler still uses the mediator, including under `UseDirectInvoke()` (M2, M4).
- The DSL interpreter does not record `RunAsync` inside a `for` loop or a local function. The overhead benchmark assigns `Build()` to a field and calls `RunAsync` on that field. The benchmark folder has its own `Directory.Build.props` because `benchmarks/Directory.Build.props` does not load the Nuru analyzer.
- Restatement check (review round 2): no misread of substance. Falling back to direct invoke when there is no generated mediator is a documented compatibility carve-out. The acceptance bullet still says mediator behaviors "run in every host" (it dropped "identically"), but the Goal paragraph above it limits that to builds with a generated mediator. Make the bullet match the Goal paragraph.
- A `default!` sentinel for an initializer the generator cannot emit must not reach the runtime assignment. It overwrites the contract's own initializer (M22). Omit it from capabilities only.
- Run tests standalone *and* through `tests/ci-tests/run-ci-tests.cs`. `.DiscoverEndpoints()` fails in CI multi-mode because another file's `[NuruRoute("")]` matches inputs these tests reject (M1). The tests use `.Map<T>()`.
- Restatement check (review round 3): no misread of substance. The round-2 "run in every host" wording is fixed. One acceptance bullet still says "startup reports an error". The behavior is now that invoking the route exits 1 (M49).
- Choosing whether to assign a member based on whether it was supplied must still respect C# rules. A `required` member must be in every object initializer (M34). Code emitted inside the behavior lambda cannot `return` a value (M35). One branch per combination of init-only members grows exponentially (M33).
- Pre-existing bug found during fix pass 4 and not fixed here (master behaves the same): the argv matcher converts a typed option value with a default before it checks that route's literal. `t508special` has `[Option("quiet")] double`, so `t508case --quiet loud` fails with "Invalid value 'loud' for option '--quiet'. Expected: double" before `t508case` is tried. The test uses `--mute`. Moving that conversion after the literal check needs its own task and review.
- Copying an initializer that has side effects is not the only safe linear form. An init-only member can read a second instance built only for defaults: `P = supplied ? p : __commandDefaults.P` (M33).
- Every matcher that finds an option for a member must match exact names first. The JSON path fixed this in round 2, and the new construction emitter brought the case mix-up back (M36).
- Restatement check (review round 4): no misread of substance. The acceptance bullet now says invoking the route exits 1, which matches the Goal paragraph and the deliberate change recorded above.

## Results

Review round 4 disposition is **clean**. 4 rounds, roster general on this round (rounds 1–3 also used tests and plan_alignment), effort 3. Final counts: bug 0 open / 11 fixed, suggestion 0 open / 8 fixed, nit 0 open / 10 fixed, 0 wontfix. Paths: `review/review-framework.md`, `review/round-4/merged.md`, `review/disposition.md`.

Fix pass 4 implements the 17 round-3 findings (bugs M33–M39, suggestions M40–M43, nits M44–M49). Round 4 verified each one against `50e92396`. No new findings.

- M33: Init-only members whose initializers cannot be copied now read `__commandDefaults`, a second instance built once, instead of one object initializer per combination. Generated code grows linearly. `= null!` on a non-nullable reference is copied as `null!` (`NullForgivingDefaultLiteral`) and needs no supplied test. `--capabilities` omits it.
- M34: `OptionDefinition.IsRequiredMember` carries the `required` modifier. A required member always goes in the object initializer, and `--json-args` declares no supplied flag for it.
- M35: `InterceptorEmitter.EmitMissingHandlerOnDispatch` writes the exit-1 guard for a visible external handler with no generated mediator. It runs before the behavior pipeline, so the behavior lambda never contains `return 1;`.
- M36: `OptionBindingMatcher` (new file `emitters/option-binding-matcher.cs`) matches exact names first. It falls back to ignore-case only for an option that no other binding claims by exact name. Command construction, `NeedsJsonSuppliedFlag`, and `AssignVariables` share it.
- M37, M41: The enum branch needs a non-null constant. A nullable enum local starts from the initializer, so `= null` stays null and `= Shade.Green` stays Green when omitted.
- M38, M39: A positional record property takes its primary-constructor parameter default. An initializer that is just a parameter (`= Name`) takes that parameter's default. Any other initializer that reads a parameter gives no default. `= default` on a struct parameter becomes `default`.
- M40, M44, M43: A constructor parameter must match a property that is bound for the endpoint kind. A `[NuruRoute]` type binds only marked properties. An `[Operation]` type also derives unmarked ones, but not from a `[NuruRouteGroup]` base. A constructor whose parameters are all optional or `params` counts as parameterless. NURU_H009 now reads "Nuru cannot construct '{0}': {1}". The reason is static, abstract, not a class or struct, or the matching rule. The docs and the changelog `### Added` cover `init` members, constructor binding, and NURU_H009.
- M45: `--capabilities` shows a `[Flags]` combination as comma-joined member names (`Read, Write`) and omits a non-constant field or property default. Capabilities no longer fall back to the code literal.
- M46: `T508SpecialQuery` covers `float.NaN` and `float.PositiveInfinity`.
- M42: New tests supply values for members whose initializers cannot be copied (`--Delay`, `{"Delay":...}`, `--Extra`).
- M47: A delegate route runs first to warm shared Nuru code. Per-call cost is reported in nanoseconds. The comment and the changelog name the AOT cold start as the startup comparison.
- M48: The docs say that under `UseMicrosoftDependencyInjection()` Nuru resolves `IMediator` from the container, and that call throws unless a mediator is registered.
- M49: The restatement's acceptance bullet says invoking the route exits 1.

Overhead on 2026-10-18 (`benchmarks/operation-dispatch/operation-dispatch-overhead.cs -- --aot`, `Unit` response, 50 warm-up calls, 2000 measured calls): `mediator-first-us=22708`, `direct-first-us=2548`, `mediator-per-call-ns=3296`, `direct-per-call-ns=1788`, `decision=keep-UseDirectInvoke`. AOT cold starts, three alternating runs: mediator `4,3,3` ms, direct `3,2,2` ms. Even after priming, the mediator first call still includes building the mediator's service provider and its JIT.

### Files changed

- Construction and JSON binding: `source/timewarp-nuru-analyzers/generators/emitters/command-construction-emitter.cs`, `option-binding-matcher.cs` (new in fix pass 4), `json-args-emitter.cs`, `behavior-emitter.cs`, `handler-invoker-emitter.cs`, `interceptor-emitter.cs`, `route-matcher-emitter.cs`, `capabilities-emitter.cs`
- Derivation: `generators/extractors/endpoint-extractor.cs`, `operation-endpoint-extractor.cs`, `generators/models/handler-definition.cs`, `segment-definition.cs`
- Diagnostics: `diagnostics/diagnostic-descriptors.handler.cs`, `AnalyzerReleases.Unshipped.md` (NURU_H009)
- Tests: `tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs`, `tests/timewarp-nuru-tests/generator/generator-55-missing-operation-handler.cs`
- Overhead, docs: `benchmarks/operation-dispatch/operation-dispatch-overhead.cs`, `documentation/user/features/neutral-contracts.md`, `changelog.md`

### Test outcomes

Run on 2026-10-18 after fix pass 4:

- `operation-endpoint-derivation.cs`: 35 passed (28 before, plus 7 new):
  - `t508initmany` omitted prints `wait:4|seed:41|token:set|label:null|note:n`. Supplied values print `wait:9|seed:5|token:set|label:x|note:m`. `--json-args {}` matches the omitted output, and supplied JSON values win.
  - `t508reqdefault` with `required` plus `= new()`, `= null!`, and `TimeSpan.FromSeconds` compiles and prints `wait:2|tag:t|body:ada`.
  - `t508case --Mute` prints `level:seed|mute:True`. `--mute loud` prints `level:loud|mute:False`. `{"Mute":true}` and `{"mute":"loud"}` bind the same way.
  - `t508nullenum` prints `unset:null|preset:Slow|combined:Read, Write|shared:Slow` on argv and on `--json-args {}`. Capabilities show `null`, `Slow`, and `Read, Write`, and omit `Shared`.
  - `t508posdefault --Name ada` prints `name:ada|count:5|mode:Slow`. Capabilities show `Count` as not required with default `5`.
  - `t508posinit --Name " ada "` prints `name:ada|note:n`. Omitting `--Name` exits 1.
  - `t508defaults --Delay 00:00:05` and `{"Delay":"00:00:05"}` print `delay:5`. `t508init --Name ada --Extra 5` prints `extra:5`.
  - `t508special` prints `...|soft:nan|rise:inf`. Capabilities show `Read, Write` for `Access` and `Casted`.
- `generator-55-missing-operation-handler.cs`: 12 passed (8 before, plus 4 new). The new cases:
  - A behavior plus an external handler with `TimeWarpMediatorAssembly=false` compiles with no errors. The only error filtered out is the harness's own CS9137. With the M35 fix reverted, this case fails with two CS8031 errors.
  - An unbound constructor parameter on `[NuruRoute]` is NURU_H009.
  - An all-optional constructor is `new()`.
  - An abstract `[Operation]` type gets the "it is abstract" reason.
- `operation-endpoint-dispatch.cs`: 7 passed. `routing-33-json-args.cs`: 19 passed.
- CI after `ganda runfile cache --clear`, `dotnet run tests/ci-tests/run-ci-tests.cs`, exit 0: multi-mode 1927 total, 1921 passed, 6 skipped, 0 failed. Standalone: 30 files, 0 failed.
- Sample: `calc add --X 2 --Y 3` printed `5`. `calc round --Value 1.2` printed `1`. `--Mode Up` printed `2`. `calc total --Values 2 --Values 4 --json-args '{"Notes":[{"Label":"tax","Amount":1}]}'` printed `total:6|average:3|notes:1`. Each exited 0.
- `ganda repo audit`: passes all checks.

### How to validate

**Smoke**

```bash
cd /home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/task-508-001-attributes-only-package-timewarpnurucontracts-for
dotnet run --file tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs
dotnet run --file tests/timewarp-nuru-tests/auto/operation-endpoint-dispatch.cs
dotnet run --file tests/timewarp-nuru-tests/generator/generator-55-missing-operation-handler.cs
dotnet run --file benchmarks/operation-dispatch/operation-dispatch-overhead.cs -- --aot
dotnet run --file samples/endpoints/02-calculator-contracts/calculator-contracts.cs -- calc add --X 2 --Y 3
dotnet run --file samples/endpoints/02-calculator-contracts/calculator-contracts.cs -- calc round --Value 1.2
dotnet run --file samples/endpoints/02-calculator-contracts/calculator-contracts.cs -- calc total --Values 2 --Values 4 --json-args '{"Notes":[{"Label":"tax","Amount":1}]}'
```

**Expect**

- Derivation: 35 passed. `t508defaults` and `t508defaults --json-args {}` both print `price:1.5|ratio:2|const:true|items:set|delay:30|token:set`. `t508special` prints `quiet:nan|up:inf|down:-inf|accent:fast|access:rw|cast:3|option:nan|soft:nan|rise:inf`. Capabilities omit `defaultValue` for Items, Delay, and Token. They show `1.5` and `2` without a suffix, `Fast` for the enum, and `Read, Write` for the flags combination. `t508initmany`, `t508reqdefault`, `t508case`, `t508nullenum`, `t508posdefault`, and `t508posinit` print the values listed under Test outcomes.
- Dispatch: 7 passed. A mediator pipeline behavior counts on the mediator path and stays at 0 under `UseDirectInvoke()`. An external handler under `UseDirectInvoke()` still counts.
- generator-55: 12 passed. The behavior plus external handler case with no generated mediator compiles with no errors. An unbound constructor parameter is NURU_H009, an all-optional constructor is `new()`, and an abstract type gets the "it is abstract" reason. NURU_H008 is a Warning whose message contains `exits 1`. The no-mediator nested handler contains `.Handler` and `Handle(` and does not contain `__GetMediator`. The external handler with no generated mediator contains the unavailable-mediator message and `return 1;`. NURU_H009 is an Error.
- Overhead: prints `mediator-per-call-ns`, `direct-per-call-ns`, and `decision=keep-UseDirectInvoke`. Direct should be the lower per-call number. `--aot` also prints `aot-mediator-cold-ms` and `aot-direct-cold-ms` as three comma-separated integers and `aot=ran`. The microsecond and millisecond counts move with the machine.
- Sample: `5`, then `1`, then `total:6|average:3|notes:1`, each with exit 0. App arguments start after `--`.
