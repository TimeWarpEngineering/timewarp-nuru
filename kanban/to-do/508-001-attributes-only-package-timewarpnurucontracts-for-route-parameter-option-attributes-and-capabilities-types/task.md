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

- [ ] Package reference and metadata-name resolution
- [ ] Derivation rules, generator and tests (including complex members as JSON, boolean initializer defaults, grouping, non-nullable enums per the 506 findings)
- [ ] Handler placement: registered handlers, diagnostics
- [ ] Mediator dispatch with Nuru behaviors as middleware; overhead measured
- [ ] `IResponseRenderer` and problem rendering
- [ ] Sample and docs

## Notes

- Parent: 508. Waits for the first published `TimeWarp.Contracts` (architecture task 384); until then develop against a project reference or a local feed.
- Findings from task 506 (Roslynk spike, 2026-10-18) that the derivation rules must cover:
  - **Complex members.** Two Roslynk inputs are arrays of objects (`multi_query.operations`: list of `{tool: enum, arguments: dictionary}`; `apply_patch.baseVersions`: list of `{Path, Version}`). The generator today maps a nested object to `string` and an object array to `string[]`, and the binding probes for these do not compile. Rule to add: a member whose type is not a scalar, enum, or array of scalars binds as one JSON value (the whole object or array) under its member name; `--capabilities` reports its type as `json` plus the CLR type name. Document that argv cannot supply such a member and `--json-args` must.
  - **Boolean defaults.** Six Roslynk parameters are non-nullable `bool` with initializer `= true` (`get_diagnostics.includeAnalyzers`; `get_members.includeMethods`, `includeFields`, `includeProperties`, `includeEvents`, `includeNestedTypes`). A Nuru flag's generated local starts `false`, so omission is indistinguishable from `false` and the default flips. A derived `bool` member with a `true` initializer must keep that default when omitted: emit it as a non-flag option whose omitted value is the initializer (the `bool?` shape the spike used by hand), and show the default in `--capabilities`.
  - **Grouping.** A flat catalog of Roslynk's 29 tools is about 16,000 tokens; `--group-filter` is the intended cut but only works when routes have a group path. Derived endpoints need a group: from a containing `[NuruRouteGroup]` when present, otherwise from an optional group argument on the CLI binding, otherwise ungrouped. Record the decision; 508-004 projects the same group into MCP tool filtering.
  - **Enums.** A non-nullable enum option does not compile today (the argv path yields a nullable enum). Roslynk has no top-level enum parameter, so the gap is latent; derivation should handle non-nullable enums or diagnose them.
- Earlier scope of this task (a Nuru-only attributes and capabilities package) is superseded: the capabilities DTOs become the neutral catalog in the shared package (508-002 projects it).

## Session

- Created: 1479989 (2026-10-17)
- Rescoped to the shared neutral-contract model: 1479989 (2026-10-17)
- 506 findings folded into requirements: 1479989 (2026-10-18)
- Implement walk stopped before product code (2026-10-18). `TimeWarp.Contracts` 2.0.0-beta.20 is on nuget.org and contains only `lib/net11.0`. This repo's `Directory.Build.props` targets `net10.0`. Restoring a net10.0 project that references that package fails with NU1202. `TimeWarp.Mediator.Contracts` 14.0.0 (this repo) and 14.0.0-beta.5 (the contracts package dependency) both ship `lib/net10.0`, so the contracts library can be packed for net10.0 without a mediator bump. `CliOperationBinding` in that package has `Pattern` only; the optional group argument the derivation notes describe is not on the published type. No product files changed.
