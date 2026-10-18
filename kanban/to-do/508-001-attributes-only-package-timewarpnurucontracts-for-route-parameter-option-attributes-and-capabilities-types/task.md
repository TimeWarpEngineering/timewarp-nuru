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

Acceptance:

- `TimeWarp.Nuru` and the generator/analyzers reference `TimeWarp.Contracts`. They recognize `[Operation]` and the CLI attributes by metadata name.
- Derivation is documented and tested: route literal, option names, required and defaults, enums including non-nullable enums, repeated values for arrays of scalars, a non-scalar member (or array of non-scalars) bound as one JSON value reported as `json` plus the CLR type name, a non-nullable `bool` whose initializer is `true` kept when the option is omitted and shown in `--capabilities`, and a group path taken from a containing `[NuruRouteGroup]` when present, otherwise from an optional group on the CLI binding, otherwise ungrouped.
- A nested `Handler` is optional. The handler is any registered `IQueryHandler<TQuery, T>` or `ICommandHandler<TCommand, T>`. The generator emits a diagnostic when it can see that none is registered; otherwise startup reports an error.
- Endpoints dispatch through TimeWarp.Mediator so mediator behaviors run in every host. `INuruBehavior` stays CLI middleware around that dispatch. Startup and per-call cost against today's direct invoke is measured, and direct invoke stays available as an opt-out.
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

- Round 1: 21 findings, all marked fixed in `review/round-1/merged.md` (8 bugs, 9 suggestions, 4 nits). Round 2 has not run.

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

## Blocked

Resolved 2026-10-18: TimeWarp.Contracts 2.0.0-beta.21 ships `lib/net10.0` and `lib/net11.0`. Reference that version (or later). Original block follows.

2026-10-18: TimeWarp.Contracts must ship a net10.0 asset (its source and TimeWarp.Mediator.Contracts 14.0.0-beta.5 already support net10.0), or timewarp-nuru task 481 must move this repo to net11.0. https://www.nuget.org/packages/TimeWarp.Contracts/2.0.0-beta.20

- https://www.nuget.org/packages/TimeWarp.Contracts/2.0.0-beta.20

## Learnings

- Restatement check (review round 1): no misread of substance. The restatement keeps every requirement, folds in the 506 findings and the group decision, and adds `IIdempotentCommand<T>`. It leaves out the Notes requirement to document that argv cannot supply a JSON member, but the docs cover that anyway.
- NURU_H008 is a Warning. The build is accepted, and invoking a route whose handler the compilation cannot see exits 1 (M8).
- A nested handler uses direct invoke when this compilation has no generated mediator. An external handler still uses the mediator, including under `UseDirectInvoke()` (M2, M4).
- The DSL interpreter does not record `RunAsync` inside a `for` loop or a local function. The overhead benchmark assigns `Build()` to a field and calls `RunAsync` on that field. The benchmark folder has its own `Directory.Build.props` because `benchmarks/Directory.Build.props` does not load the Nuru analyzer.
- Run tests standalone *and* through `tests/ci-tests/run-ci-tests.cs`. `.DiscoverEndpoints()` fails in CI multi-mode because another file's `[NuruRoute("")]` matches inputs these tests reject (M1). The tests use `.Map<T>()`.

## Results

### Review status (round 1 disposed, round 2 not run)

- Rounds: 1 reviewed `f522c6e2`. Effort 3, roster general, tests and plan_alignment.
- Counts after the fix pass: bug 0 open / 8 fixed; suggestion 0 open / 9 fixed; nit 0 open / 4 fixed; 0 wontfix.
- Disposition: all 21 findings marked fixed in `review/round-1/merged.md`. Round 2 has not run.
- Paths: `review/review-framework.md`, `review/round-1/{general,tests,plan-alignment,merged}.md`.

Nuru derives a complete endpoint from a `TimeWarp.Contracts` 2.0.0-beta.21 request that carries `[Operation]` and a TimeWarp.Mediator kind, including a record class. An `[Operation]` type with no mediator kind is skipped. CLI attributes stay in `TimeWarp.Nuru` as overrides. The group path is the containing `[NuruRouteGroup]` prefix, otherwise `[NuruCliBinding(Group)]`, otherwise empty. Inherited public settable members are options too.

With a generated mediator, dispatch goes through TimeWarp.Mediator unless `UseDirectInvoke()` selects a nested handler. A nested handler still uses the direct call when no generated mediator exists. An endpoint with no nested handler still uses the mediator, and a typed HttpClient registered for that handler is constructed there. `INuruBehavior` stays around both paths. A mediator pipeline behavior runs only on the mediator path.

In-process overhead on 2026-10-18 (`benchmarks/operation-dispatch/operation-dispatch-overhead.cs`, `Unit` response): `warmup=50`, `iterations=2000`, `mediator-ticks=6275013`, `direct-ticks=3483383`. Direct is lower, so `UseDirectInvoke()` stays. AOT cold start was not run.

JSON members bind only from `--json-args`, through the source-generated `NuruJsonMemberContext`, with property names as declared. A required JSON member that is missing after the route matches exits 1 with `Error: pass --{name} via --json-args.` A `true` bool initializer is a value option, not a flag. Non-nullable enums keep their initializer. Enum member defaults stay member names. `decimal` and `float` defaults keep their suffixes (`1.5m`, `2.0f`). An initializer the generator cannot emit becomes `default!` and is omitted from capabilities. `SharedProblemDetails` goes to stderr with a non-zero exit code, the first line is the problem type and title, and each route invocation clears `Environment.ExitCode` first. NURU_H008 is a Warning. `--capabilities` reports a JSON member as `json` plus the CLR name without `global::`.

### Files changed

- Package reference: `Directory.Packages.props`, `source/timewarp-nuru/timewarp-nuru.csproj`, `source/timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj`. Package version is `3.1.0-beta.1` so the stable `3.0.0` package does not gain a prerelease dependency.
- Derivation and dispatch: `source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs` and the generator emitters, models, and `NuruCliBinding`
- AOT JSON member context: `source/timewarp-nuru-build/GenerateNuruJsonContextTask.cs`, `tests/Directory.Build.targets`
- Tests: `tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs`, `operation-endpoint-dispatch.cs`, `tests/timewarp-nuru-tests/generator/generator-55-missing-operation-handler.cs`
- Overhead: `benchmarks/operation-dispatch/operation-dispatch-overhead.cs` and `benchmarks/operation-dispatch/Directory.Build.props`
- Sample and docs: `samples/endpoints/02-calculator-contracts/`, `documentation/user/features/neutral-contracts.md`, `changelog.md`

### Test outcomes

- `operation-endpoint-derivation.cs`: 24 passed (standalone, after the fix pass)
- `operation-endpoint-dispatch.cs`: 7 passed (standalone). The mediator path counts a pipeline behavior; `UseDirectInvoke()` does not; an external handler under `UseDirectInvoke()` still counts; a typed HttpClient is constructed. The first stderr line of the problem case is `https://example.test/t508 Bad input`.
- `generator-55-missing-operation-handler.cs`: 3 passed. NURU_H008 is a Warning, and an `[Operation]` type that is not a mediator message is skipped.
- Overhead: `warmup=50`, `iterations=2000`, `mediator-ticks=6275013`, `direct-ticks=3483383`, `decision=keep-UseDirectInvoke`. AOT was not run.
- CI after `ganda runfile cache --clear`: multi-mode 1916 total, 1910 passed, 6 skipped, 0 failed. `OperationEndpointDerivation` was 24 passed. Standalone phase 0 failed.
- Sample smoke on 2026-10-18 (implement pass): `calc add --X 2 --Y 3` printed `5` and exited 0. `calc round --Value 1.2` printed `1`. `calc round --Value 1.2 --Mode Up` printed `2`. `calc total --Values 2 --Values 4 --json-args '{"Notes":[{"Label":"tax","Amount":1}]}'` printed `total:6|average:3|notes:1`. `--Notes` on argv printed `Error: --Notes is JSON. Pass it with --json-args.` and exited 1. `--capabilities --group-filter calc` listed the three routes with `groupPath` `calc`, `IncludeAverage` default `true` and `isFlag` false, and `Mode` allowed values. The notes type no longer includes `global::`.

### How to validate

**Smoke**

```bash
cd /home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/task-508-001-attributes-only-package-timewarpnurucontracts-for
dotnet run tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs
dotnet run tests/timewarp-nuru-tests/auto/operation-endpoint-dispatch.cs
dotnet run tests/timewarp-nuru-tests/generator/generator-55-missing-operation-handler.cs
dotnet run benchmarks/operation-dispatch/operation-dispatch-overhead.cs
dotnet run samples/endpoints/02-calculator-contracts/calculator-contracts.cs -- calc add --X 2 --Y 3
dotnet run samples/endpoints/02-calculator-contracts/calculator-contracts.cs -- calc round --Value 1.2
dotnet run samples/endpoints/02-calculator-contracts/calculator-contracts.cs -- calc total --Values 2 --Values 4 --json-args '{"Notes":[{"Label":"tax","Amount":1}]}'
```

**Expect**

- Derivation: 24 passed. JSON members come from `--json-args` with the property name as declared. A `bool` initialized to `true` stays true when omitted. A non-nullable enum compiles and keeps its initializer. `decimal` and `float` defaults keep suffixes. `--group-filter` matches the `[NuruRouteGroup]` prefix, and `[NuruCliBinding(Group)]` supplies the prefix when no route group is present. Inherited members bind. The notes type does not contain `global::`.
- Dispatch: 7 passed. The trace behavior runs around mediator dispatch. A mediator pipeline behavior counts on the mediator path and stays at 0 under `UseDirectInvoke()`. An external handler under `UseDirectInvoke()` still counts. A typed HttpClient is constructed. The renderer output wins over JSON. `--Fail` exits 17 and the first stderr line is `https://example.test/t508 Bad input`; the next call without `--Fail` exits 0.
- generator-55: 3 passed. NURU_H008 is a Warning. The route still exists, so invoking it exits 1. An `[Operation]` type that is not a mediator message produces no endpoint and no NURU_H008.
- Overhead: prints `warmup=50`, `iterations=2000`, `mediator-ticks`, `direct-ticks`, and `decision=keep-UseDirectInvoke`. The tick counts move with the machine. Direct should be the lower number on this query. Nothing is written for the handler result.
- Sample: `5`, then `1`, then `total:6|average:3|notes:1`, each with exit 0. App arguments start after `--`. A token before that separator, such as `--nologo`, is not an application argument and the route will not match.
