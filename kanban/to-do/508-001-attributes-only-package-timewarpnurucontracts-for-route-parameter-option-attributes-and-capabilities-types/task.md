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

Acceptance:

- `TimeWarp.Nuru` and the generator/analyzers reference `TimeWarp.Contracts`. They recognize `[Operation]` and the CLI attributes by metadata name.
- Derivation is documented and tested: route literal, option names, required and defaults, enums including non-nullable enums, repeated values for arrays of scalars, a non-scalar member (or array of non-scalars) bound as one JSON value reported as `json` plus the CLR type name, a non-nullable `bool` whose initializer is `true` kept when the option is omitted and shown in `--capabilities`, and a group path taken from a containing `[NuruRouteGroup]` when present, otherwise from an optional group on the CLI binding, otherwise ungrouped.
- A nested `Handler` is optional. The handler is any registered `IQueryHandler<TQuery, T>` or `ICommandHandler<TCommand, T>`. The generator emits a diagnostic when it can see that none is registered; otherwise startup reports an error.
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

## Blocked

Resolved 2026-10-18: TimeWarp.Contracts 2.0.0-beta.21 ships `lib/net10.0` and `lib/net11.0`. Reference that version (or later). Original block follows.

2026-10-18: TimeWarp.Contracts must ship a net10.0 asset (its source and TimeWarp.Mediator.Contracts 14.0.0-beta.5 already support net10.0), or timewarp-nuru task 481 must move this repo to net11.0. https://www.nuget.org/packages/TimeWarp.Contracts/2.0.0-beta.20

- https://www.nuget.org/packages/TimeWarp.Contracts/2.0.0-beta.20

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
- Every matcher that finds an option for a member must match exact names first. The JSON path fixed this in round 2, and the new construction emitter brought the case mix-up back (M36).

## Results

Round 2 left 12 findings open (bugs M22–M25, suggestions M26–M29, nits M20, M30–M32). This fix pass implements them.

**Review disposition (round 3, 2026-10-18): not disposed. A fix pass is needed.**
- Rounds: 3, effort 3, roster general + tests + plan_alignment.
- Round 3 verified all 12 round-2 findings as fixed. It raised 17 new findings, all open:
  - 7 bugs: M33, M34, M35, M36, M37, M38, M39.
  - 4 suggestions: M40, M41, M42, M43.
  - 6 nits: M44 through M49.
- Final counts from `review/round-3/merged.md`:

  | Severity | open | fixed | wontfix |
  |----------|------|-------|---------|
  | bug | 7 | 4 | 0 |
  | suggestion | 4 | 4 | 0 |
  | nit | 6 | 4 | 0 |

- Round-1 findings M1–M21 stay fixed.
- Paths: `review/review-framework.md`, `review/round-3/merged.md`. `review/disposition.md` is not written.

Omitting an option whose initializer the generator cannot copy (`new()`, a method call) leaves that initializer in place on argv and on `--json-args`. The JSON path records `__supplied_{id}` in the route case. The attempt block's `__av_` flag is out of scope when the command is built. `--capabilities` omits that default. `double.NaN`, infinities, a const enum field, and a flags or cast enum default emit compilable C#. Capabilities show a plain number (`1.5`, `2`) and an enum member name (`Fast`), or the numeric value when the bits match no member (`3`). `init` members and a positional record are derived. A positional record uses its primary constructor. A `required init` member is set in the object initializer. A type with no callable constructor is NURU_H009. A JSON member whose name is a C# keyword still generates a valid argv rejection flag. A derived type that redeclares a base property does not derive the base property again. `--Quiet` and `--quiet` bind as different JSON slots. JSON type names omit `global::` inside type arguments. The mediator-kind check runs before route-pattern validation. NURU_H008 says invoking the route exits 1.

With a generated mediator, dispatch goes through TimeWarp.Mediator unless `UseDirectInvoke()` selects a nested handler. A nested handler is called directly when no generated mediator exists. A visible handler that is not nested writes an error and exits 1 in that build. `UseDirectInvoke()` with only an external handler still emits `__GetMediator`.

Overhead on 2026-10-18 (`benchmarks/operation-dispatch/operation-dispatch-overhead.cs`, `Unit` response, 50 warm-up calls, 2000 measured calls): `mediator-first-us=28636`, `direct-first-us=2665`, `mediator-per-call-us=3`, `direct-per-call-us=1`, `decision=keep-UseDirectInvoke`. `--aot` three alternating cold starts: mediator `4,3,3` ms, direct `3,3,3` ms.

### Files changed

- Construction and JSON binding: `source/timewarp-nuru-analyzers/generators/emitters/command-construction-emitter.cs`, `json-args-emitter.cs`, `behavior-emitter.cs`, `handler-invoker-emitter.cs`, `route-matcher-emitter.cs`, `capabilities-emitter.cs`
- Derivation: `generators/extractors/endpoint-extractor.cs`, `operation-endpoint-extractor.cs`, `generators/models/handler-definition.cs`, `segment-definition.cs`
- Diagnostics: `diagnostics/diagnostic-descriptors.handler.cs`, `AnalyzerReleases.Unshipped.md` (NURU_H009)
- Tests: `tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs`, `tests/timewarp-nuru-tests/generator/generator-55-missing-operation-handler.cs`
- Overhead, docs: `benchmarks/operation-dispatch/operation-dispatch-overhead.cs`, `documentation/user/features/neutral-contracts.md`, `changelog.md`

### Test outcomes

- `operation-endpoint-derivation.cs`: 28 passed. Omitted `new()` / `TimeSpan.FromSeconds` / `Guid.NewGuid` keep the initializer on argv and on `--json-args {}`. Non-finite, enum, flags, and cast defaults compile. `t508user --Id 7` prints `id:7`. `t508init --Name ada` prints `name:ada|extra:3`. `t508params --Params nope` exits 1. `t508hide --title ada` prints `name:ada`, and omission prints `name:child`.
- `operation-endpoint-dispatch.cs`: 7 passed. The mediator path counts a pipeline behavior; `UseDirectInvoke()` does not; an external handler under `UseDirectInvoke()` still counts; a typed HttpClient is constructed.
- `generator-55-missing-operation-handler.cs`: 8 passed. NURU_H008 says the route exits 1. An `[Operation]` that is not a mediator message skips pattern validation. No generated mediator calls a nested handler directly and exits 1 for a visible external handler. `UseDirectInvoke()` with only an external handler still contains `__GetMediator`. NURU_H009 is an error and the command is not constructed.
- `routing-33-json-args.cs`: 19 passed.
- CI after `ganda runfile cache --clear`: multi-mode 1920 total, 1914 passed, 6 skipped, 0 failed. Standalone phase 0 failed. The four new derivation tests account for the increase from the round-2 total of 1916.
- Overhead: `warmup=50`, `iterations=2000`, `mediator-first-us=28636`, `direct-first-us=2665`, `mediator-per-call-us=3`, `direct-per-call-us=1`, `decision=keep-UseDirectInvoke`, `aot-mediator-cold-ms=4,3,3`, `aot-direct-cold-ms=3,3,3`, `aot=ran`.
- Sample smoke on 2026-10-18: `calc add --X 2 --Y 3` printed `5`. `calc round --Value 1.2` printed `1`. `calc round --Value 1.2 --Mode Up` printed `2`. `calc total --Values 2 --Values 4 --json-args '{"Notes":[{"Label":"tax","Amount":1}]}'` printed `total:6|average:3|notes:1`. Each exited 0.

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

- Derivation: 28 passed. `t508defaults` and `t508defaults --json-args {}` both print `price:1.5|ratio:2|const:true|items:set|delay:30|token:set`. `t508special` prints `quiet:nan|up:inf|down:-inf|accent:fast|access:rw|cast:3|option:nan`. Capabilities omit `defaultValue` for Items, Delay, and Token, show `1.5` and `2` without a suffix, and show `Fast` for the enum.
- Dispatch: 7 passed. A mediator pipeline behavior counts on the mediator path and stays at 0 under `UseDirectInvoke()`. An external handler under `UseDirectInvoke()` still counts.
- generator-55: 8 passed. NURU_H008 is a Warning whose message contains `exits 1`. The no-mediator nested handler contains `.Handler` and `Handle(` and does not contain `__GetMediator`. The external handler with no generated mediator contains the unavailable-mediator message and `return 1;`. NURU_H009 is an Error.
- Overhead: prints `mediator-per-call-us`, `direct-per-call-us`, and `decision=keep-UseDirectInvoke`. Direct should be the lower per-call number. `--aot` also prints `aot-mediator-cold-ms` and `aot-direct-cold-ms` as three comma-separated integers and `aot=ran`. The microsecond and millisecond counts move with the machine.
- Sample: `5`, then `1`, then `total:6|average:3|notes:1`, each with exit 0. App arguments start after `--`.
