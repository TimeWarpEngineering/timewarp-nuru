# Delete runtime infrastructure after migration

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Once all parity tests pass and AppB is proven correct, delete the runtime infrastructure that is no longer needed. This is the final cleanup step.

## Requirements

- All parity tests passing
- AppB is the sole build target
- Remove unused runtime code
- Remove unused package dependencies
- Update public API if needed

## Checklist

- [ ] Confirm all parity tests pass
- [ ] Remove AppA csproj (dual-build infrastructure)
- [ ] Delete `NuruRouteRegistry` class
- [ ] Delete `EndpointCollection` class (or make internal)
- [ ] Delete runtime `CompiledRouteBuilder` usage paths
- [ ] Delete `InvokerRegistry` runtime lookup code
- [ ] Delete `EndpointResolver` (matching now generated)
- [ ] Remove `IPipelineBehavior` infrastructure
- [ ] Remove MS.Extensions.DependencyInjection reference
- [ ] Remove Mediator dependency (if fully replaced)
- [ ] Update NuGet package dependencies
- [ ] Run full test suite
- [ ] Update public API documentation

## Notes

### Code to Delete

| File/Class | Reason |
|------------|--------|
| `NuruRouteRegistry` | No runtime registration |
| `EndpointCollection.Sort()` | Pre-sorted at compile time |
| `NuruCoreAppBuilder` runtime logic | Replaced by generator |
| `EndpointResolver` | Matching inlined |
| `InvokerRegistry` dictionary lookup | Direct handler calls |
| `HelpRouteGenerator` | Help pre-generated |
| `ServiceCollectionExtensions.AddNuru()` | No DI container |

### Package References to Remove

- `Microsoft.Extensions.DependencyInjection` (if fully replaced)
- `Mediator.Abstractions` (if direct invocation only)
- `Mediator.SourceGenerator` (if not using Mediator)

### Breaking Changes

Document any public API changes:
- `NuruCoreAppBuilder` API may change
- Service registration pattern changes
- Behavior registration pattern changes

Users upgrading will need to update their code.
