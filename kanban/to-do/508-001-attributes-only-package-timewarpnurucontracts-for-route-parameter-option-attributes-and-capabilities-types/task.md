# Attributes-only package TimeWarp.Nuru.Contracts for route, parameter, option attributes and capabilities types

## Description

Ship the types a contract assembly needs to declare Nuru endpoints, without the CLI runtime. Nuru already references `TimeWarp.Mediator.Contracts` rather than the whole mediator; this is the same split for Nuru itself. A contracts assembly (request classes carrying `[NuruRoute]`, `[Parameter]`, `[Option]`, `[NuruRouteExample]`, `[NuruRouteGroup]`, `[GroupOption]`) can then be referenced by an engine, a daemon or an MCP host that never runs the CLI.

## Requirements

- New package `TimeWarp.Nuru.Contracts` (name open; must not collide with existing PackageIds) containing the endpoint attributes and the `capabilities` DTOs (`CapabilitiesResponse`, `EndpointCapability`, `ParameterCapability`, `OptionCapability`, `ExampleCapability`, `InvocationCapability`, `EndpointKind`) plus their `JsonSerializerContext`.
- `TimeWarp.Nuru` references the new package and type-forwards so existing consumers compile unchanged.
- The analyzers and generators resolve the attributes by metadata name from either assembly.
- No runtime dependency from the contracts package on `TimeWarp.Terminal`, DI or the REPL.
- Docs: a short page on declaring endpoints in a separate contracts assembly and which package each project references.

## Checklist

- [ ] Package scaffold and csproj, central package versions
- [ ] Move attributes and capabilities types; type-forward from core
- [ ] Generator and analyzer tests pass against both assemblies
- [ ] Sample: endpoints in a contracts project consumed by a CLI project
- [ ] Docs page

## Notes

- Parent: 508. Unblocks 508-004 cleanly (the MCP host should not need the CLI runtime) and Roslynk's engine/daemon split.
- Check whether `[Parameter]`/`[Option]` are also used by the fluent DSL generator paths before moving them.

## Session

- Created: 1479989 (2026-10-17)
