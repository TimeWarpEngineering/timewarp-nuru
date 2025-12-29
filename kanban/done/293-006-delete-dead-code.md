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

### Files DELETED (entire files)

#### Execution Infrastructure
- [x] `source/timewarp-nuru-core/execution/delegate-executor.cs` - Never called, source gen emits inline
- [x] `source/timewarp-nuru-core/execution/delegate-request.cs` - Never called
- [x] `source/timewarp-nuru-core/execution/mediator-executor.cs` - Never resolved from DI
- [x] `source/timewarp-nuru-core/execution/route-execution-context.cs` - Never resolved from DI
- [x] `source/timewarp-nuru-core/execution/invoker-registry.cs` - Source gen doesn't use it
- [x] `source/timewarp-nuru-core/core-invoker-registration.cs` - Registers invokers for dead code

#### Resolution Infrastructure
- [x] `source/timewarp-nuru-core/resolution/endpoint-resolver.cs` - Never called
- [x] `source/timewarp-nuru-core/resolution/endpoint-resolver.segments.cs` - Part of EndpointResolver
- [x] `source/timewarp-nuru-core/resolution/endpoint-resolver.options.cs` - Part of EndpointResolver
- [x] `source/timewarp-nuru-core/resolution/endpoint-resolver.helpers.cs` - Part of EndpointResolver
- [x] `source/timewarp-nuru-core/resolution/endpoint-resolution-result.cs` - Only used by EndpointResolver

#### Help Infrastructure (runtime version - source gen provides help)
- [x] `source/timewarp-nuru-core/help/help-route-generator.cs` - Never called
- [x] `source/timewarp-nuru-core/help/help-provider.cs` - Only called from HelpRouteGenerator

#### Service Extensions
- [x] `source/timewarp-nuru-core/extensions/service-collection-extensions.cs` - `AddNuru()` never called

#### Endpoint Builder Infrastructure
- [x] `source/timewarp-nuru-core/endpoints/default-endpoint-collection-builder.cs` - Only registered by deleted `AddNuru()`

### Code DELETED (within files)

#### `nuru-core-app-builder.routes.cs` (lines 144-308)
- [x] Delete `#pragma warning disable IDE0051` block
- [x] Delete `MapPatternTyped()` method
- [x] Delete `MapInternalTyped()` method
- [x] Delete `MapMediatorTyped()` method
- [x] Delete `MapNestedTyped()` method
- [x] Delete `GeneratePatternFromCompiledRoute()` method
- [x] Delete `#pragma warning restore IDE0051`

#### `endpoint-builder.cs`
- [x] Delete temporary constructor `EndpointBuilder(TBuilder builder, Endpoint? _)`
- [x] Delete temporary constructor in non-generic `EndpointBuilder`

#### `group-endpoint-builder.cs`
- [x] Delete temporary constructor `GroupEndpointBuilder(GroupBuilder<TGroupParent> parent, Endpoint? _)`

#### `group-builder.cs`
- [x] Delete old constructor with unused parameters and `#pragma warning disable IDE0060` block

### Kept (still used by completion system)

- `EndpointCollection` class - Used by `timewarp-nuru-completion`
- `Endpoint` class - Used by completion
- `TypeConverterRegistry` class/field - Used by completion for enum expansion
- `IEndpointCollectionBuilder` interface - Used by completion
- `SessionContext` class - Keep for future REPL rebuild

### Kept (used by source generator)

- `PatternParser` and parsing infrastructure
- `CompiledRoute`, `RouteMatcher`, `LiteralMatcher`, `ParameterMatcher`, `OptionMatcher`
- All of `timewarp-nuru-parsing` package

## Verification

- [x] `dotnet build` - Succeeded with no errors
- [x] `dotnet test` - All tests pass

## Summary

Deleted 16 files and removed ~300 lines of dead code from builder files:
- 6 execution infrastructure files
- 5 resolution infrastructure files  
- 2 help infrastructure files
- 2 service/builder infrastructure files
- 1 invoker registration file
- Temporary constructors and dead private methods from 4 builder files
