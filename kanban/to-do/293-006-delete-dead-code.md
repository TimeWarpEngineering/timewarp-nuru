# #293-006: Delete Dead Code

## Parent

#293 Make DSL Builder Methods No-Ops

## Dependencies

- #293-001, #293-002, #293-003, #293-004, #293-005 (all no-op conversions complete)

## Description

After converting all DSL builder methods to no-ops, significant runtime infrastructure becomes dead code. The source generator now:
- Intercepts `RunAsync` and emits inline route matching code
- Emits inline type conversion (no `TypeConverterRegistry` at runtime)
- Generates help text directly (no runtime `HelpProvider`)

This task removes dead code to reduce binary size and simplify the codebase.

**Note:** REPL is currently broken/reference-only. Code used only by REPL can be deleted; REPL will be rebuilt later (#293-007).

## Checklist

### Files to DELETE (entire files)

#### Execution Infrastructure
- [ ] `source/timewarp-nuru-core/execution/delegate-executor.cs` - Never called, source gen emits inline
- [ ] `source/timewarp-nuru-core/execution/delegate-request.cs` - Never called
- [ ] `source/timewarp-nuru-core/execution/mediator-executor.cs` - Never resolved from DI
- [ ] `source/timewarp-nuru-core/execution/route-execution-context.cs` - Never resolved from DI
- [ ] `source/timewarp-nuru-core/execution/invoker-registry.cs` - Source gen doesn't use it
- [ ] `source/timewarp-nuru-core/core-invoker-registration.cs` - Registers invokers for dead code

#### Resolution Infrastructure
- [ ] `source/timewarp-nuru-core/resolution/endpoint-resolver.cs` - Never called
- [ ] `source/timewarp-nuru-core/resolution/endpoint-resolver.segments.cs` - Part of EndpointResolver
- [ ] `source/timewarp-nuru-core/resolution/endpoint-resolver.options.cs` - Part of EndpointResolver
- [ ] `source/timewarp-nuru-core/resolution/endpoint-resolver.helpers.cs` - Part of EndpointResolver
- [ ] `source/timewarp-nuru-core/resolution/endpoint-resolution-result.cs` - Only used by EndpointResolver

#### Help Infrastructure (runtime version - source gen provides help)
- [ ] `source/timewarp-nuru-core/help/help-route-generator.cs` - Never called
- [ ] `source/timewarp-nuru-core/help/help-provider.cs` - Only called from HelpRouteGenerator

#### Service Extensions
- [ ] `source/timewarp-nuru-core/extensions/service-collection-extensions.cs` - `AddNuru()` never called

#### Endpoint Builder Infrastructure
- [ ] `source/timewarp-nuru-core/endpoints/default-endpoint-collection-builder.cs` - Only registered by deleted `AddNuru()`

### Code to DELETE (within files)

#### `nuru-core-app-builder.routes.cs` (lines 144-308)
- [ ] Delete `#pragma warning disable IDE0051` block
- [ ] Delete `MapPatternTyped()` method
- [ ] Delete `MapInternalTyped()` method
- [ ] Delete `MapMediatorTyped()` method
- [ ] Delete `MapNestedTyped()` method
- [ ] Delete `GeneratePatternFromCompiledRoute()` method
- [ ] Delete `#pragma warning restore IDE0051`

#### `endpoint-builder.cs`
- [ ] Delete lines 40-45: temporary constructor `EndpointBuilder(TBuilder builder, Endpoint? _)`
- [ ] Delete lines 227-234: temporary constructor in non-generic `EndpointBuilder`

#### `group-endpoint-builder.cs`
- [ ] Delete lines 33-38: temporary constructor `GroupEndpointBuilder(GroupBuilder<TGroupParent> parent, Endpoint? _)`

#### `group-builder.cs`
- [ ] Delete lines 38-61: old constructor with unused parameters and `#pragma warning disable IDE0060` block

### Keep (still used by completion system)

- `EndpointCollection` class - Used by `timewarp-nuru-completion`
- `Endpoint` class - Used by completion
- `TypeConverterRegistry` class/field - Used by completion for enum expansion
- `IEndpointCollectionBuilder` interface - Used by completion
- `SessionContext` class - Keep for future REPL rebuild

### Keep (used by source generator)

- `PatternParser` and parsing infrastructure
- `CompiledRoute`, `RouteMatcher`, `LiteralMatcher`, `ParameterMatcher`, `OptionMatcher`
- All of `timewarp-nuru-parsing` package

## Verification

After each deletion phase:
1. `dotnet build` - Must succeed with no errors
2. `dotnet test` - Must pass
3. Verify a sample app still works

## Notes

- Source generator emits inline code, doesn't use runtime executors/resolvers
- Help is now source-generated via `HelpEmitter`, not runtime `HelpProvider`
- REPL will be rebuilt separately in #293-007
- Delete in phases: execution → resolution → help → service extensions → inline code
