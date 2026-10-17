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

## Checklist

- [ ] Package reference and metadata-name resolution
- [ ] Derivation rules, generator and tests
- [ ] Handler placement: registered handlers, diagnostics
- [ ] Mediator dispatch with Nuru behaviors as middleware; overhead measured
- [ ] `IResponseRenderer` and problem rendering
- [ ] Sample and docs

## Notes

- Parent: 508. Waits for the first published `TimeWarp.Contracts` (architecture task 384); until then develop against a project reference or a local feed.
- Earlier scope of this task (a Nuru-only attributes and capabilities package) is superseded: the capabilities DTOs become the neutral catalog in the shared package (508-002 projects it).

## Session

- Created: 1479989 (2026-10-17)
- Rescoped to the shared neutral-contract model: 1479989 (2026-10-17)
