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

A request class that carries `TimeWarp.Contracts.OperationAttribute` and a TimeWarp.Mediator kind (`IQuery<T>`, `ICommand<T>`, or `IIdempotentCommand<T>`) is a complete Nuru endpoint. The generator builds the route literal from the operation name and one option per public settable member: the option name is the property name, the description comes from `System.ComponentModel.DescriptionAttribute`, required comes from the `required` modifier and nullability, the default comes from the initializer, and allowed values come from enum constants. `[NuruRoute]`, `[Parameter]`, `[Option]`, and `[NuruRouteExample]` stay in `TimeWarp.Nuru` as CLI overrides and keep working for existing apps. This task does not split those attributes into their own package.

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

## Blocked

Resolved 2026-10-18: TimeWarp.Contracts 2.0.0-beta.21 ships `lib/net10.0` and `lib/net11.0`. Reference that version (or later). Original block follows.

2026-10-18: TimeWarp.Contracts must ship a net10.0 asset (its source and TimeWarp.Mediator.Contracts 14.0.0-beta.5 already support net10.0), or timewarp-nuru task 481 must move this repo to net11.0. https://www.nuget.org/packages/TimeWarp.Contracts/2.0.0-beta.20

- https://www.nuget.org/packages/TimeWarp.Contracts/2.0.0-beta.20

## Learnings

- Restatement check (review round 1): no misread of substance. The restatement keeps every requirement, folds in the 506 findings and the group decision, and adds `IIdempotentCommand<T>`. It leaves out the Notes requirement to document that argv cannot supply a JSON member, but the docs cover that anyway. The implementation does drift from two requirements the restatement states correctly:
  - "otherwise startup reports an error": NURU_H008 is an Error, so the startup path needs suppression (M8).
  - "existing apps keep working unchanged": mediator dispatch became the default for nested-handler `[NuruRoute]` endpoints (M2, M3).
- Run tests standalone *and* through `tests/ci-tests/run-ci-tests.cs`. The new tests passed standalone (19/4/2) but failed in CI multi-mode, because `.DiscoverEndpoints()` pulled in another file's `[NuruRoute("")]` (M1).

## Results

### Review status (round 1, not yet disposed)

- Rounds: 1. Effort 3, roster general, tests and plan_alignment.
- Counts: bug 8 open; suggestion 9 open; nit 4 open; 0 fixed; 0 wontfix.
- Disposition: pending. A fix pass on this id is required, then round 2.
- Gate results on `f522c6e2`:
  - Build: 0 warnings.
  - The three new test files pass standalone.
  - `run-ci-tests.cs` exits 1: multi-mode 1897/1909 passed, 6 failed, all in `OperationEndpointDerivationTests`.
- Paths: `review/review-framework.md`, `review/round-1/{general,tests,plan_alignment,merged}.md`.

Nuru derives a complete endpoint from a `TimeWarp.Contracts` 2.0.0-beta.21 request that carries `[Operation]` and a TimeWarp.Mediator kind. CLI attributes stay in `TimeWarp.Nuru` as overrides. The group path is the containing `[NuruRouteGroup]` prefix, otherwise `[NuruCliBinding(Group)]`, otherwise empty. Dispatch goes through TimeWarp.Mediator. `UseDirectInvoke()` still calls a nested `Handler` and is the opt-out. One local timing run (not a budget) wrote `mediator-first-ms=5 mediator-30-ms=0 direct-first-ms=4 direct-30-ms=0`. Both paths returned `direct:ok`. The 30-call loops were under 1 ms.

JSON members bind only from `--json-args`, through the source-generated `NuruJsonMemberContext`, with property names as declared. A `true` bool initializer is a value option, not a flag. Non-nullable enums keep their initializer. `SharedProblemDetails` goes to stderr with a non-zero exit code, and each route invocation clears `Environment.ExitCode` first.

### Files changed

- Package reference: `Directory.Packages.props`, `source/timewarp-nuru/timewarp-nuru.csproj`, `source/timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj`. Package version is `3.1.0-beta.1` so the stable `3.0.0` package does not gain a prerelease dependency.
- Derivation and dispatch: `source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs` and the generator emitters, models, and `NuruCliBinding`
- AOT JSON member context: `source/timewarp-nuru-build/GenerateNuruJsonContextTask.cs`, `tests/Directory.Build.targets`
- Tests: `tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs`, `operation-endpoint-dispatch.cs`, `tests/timewarp-nuru-tests/generator/generator-55-missing-operation-handler.cs`
- Sample and docs: `samples/endpoints/02-calculator-contracts/`, `documentation/user/features/neutral-contracts.md`

### Test outcomes

- `operation-endpoint-derivation.cs`: 19 passed
- `operation-endpoint-dispatch.cs`: 4 passed. Overhead file `/tmp/t508-overhead.txt` matched the numbers above. Generated direct-invoke code calls `__handler.Handle`; the default calls `__mediator.Send`.
- `generator-55-missing-operation-handler.cs`: 2 passed (NURU_H008)
- `endpoint-nullable-option-01`: 7 passed (nested-handler routes still compile)
- Sample smoke on 2026-10-18: `calc add --X 2 --Y 3` printed `5` and exited 0. `calc round --Value 1.2` printed `1`. `calc round --Value 1.2 --Mode Up` printed `2`. `calc total --Values 2 --Values 4 --json-args '{"Notes":[{"Label":"tax","Amount":1}]}'` printed `total:6|average:3|notes:1`. `--Notes` on argv printed `Error: --Notes is JSON. Pass it with --json-args.` and exited 1. `--capabilities --group-filter calc` listed the three routes with `groupPath` `calc`, `IncludeAverage` default `true` and `isFlag` false, `Mode` allowed values, and `Notes` type `json global::CalculatorContracts.Endpoints.LabeledAmount[]`.

### How to validate

**Smoke**

```bash
cd /home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/task-508-001-attributes-only-package-timewarpnurucontracts-for
dotnet run tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs
dotnet run tests/timewarp-nuru-tests/auto/operation-endpoint-dispatch.cs
dotnet run tests/timewarp-nuru-tests/generator/generator-55-missing-operation-handler.cs
dotnet run samples/endpoints/02-calculator-contracts/calculator-contracts.cs -- calc add --X 2 --Y 3
dotnet run samples/endpoints/02-calculator-contracts/calculator-contracts.cs -- calc round --Value 1.2
dotnet run samples/endpoints/02-calculator-contracts/calculator-contracts.cs -- calc total --Values 2 --Values 4 --json-args '{"Notes":[{"Label":"tax","Amount":1}]}'
```

**Expect**

- Derivation: 19 passed. A handlerless `[Operation]` is not in that file. JSON members come from `--json-args` with the property name as declared. A `bool` initialized to `true` stays true when omitted. A non-nullable enum compiles and keeps its initializer. `--group-filter` matches the `[NuruRouteGroup]` prefix, and `[NuruCliBinding(Group)]` supplies the prefix when no route group is present.
- Dispatch: 4 passed. The trace behavior runs around mediator dispatch. The renderer output wins over JSON. `--Fail` exits 17 and writes the problem type and title on the first stderr line; the next call without `--Fail` exits 0. `/tmp/t508-overhead.txt` records both paths. The numbers move with the machine. Both paths print `direct:ok`.
- generator-55: 2 passed. NURU_H008 is reported when the compilation can see no handler, and the route still exists so startup can report a missing registration.
- Sample: `5`, then `1`, then `total:6|average:3|notes:1`, each with exit 0. App arguments start after `--`. A token before that separator, such as `--nologo`, is not an application argument and the route will not match.
