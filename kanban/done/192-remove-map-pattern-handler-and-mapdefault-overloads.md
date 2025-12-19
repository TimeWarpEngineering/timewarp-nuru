# Remove Map(pattern, handler) and MapDefault Overloads

## Description

Remove the old `Map(string pattern, Delegate handler)` and `MapDefault(Delegate handler)` API in favor of the new fluent pattern:

```csharp
// Old (removed)
app.Map("deploy {env}", (string env) => Deploy(env));
app.MapDefault(() => ShowHelp());

// New (fluent API)
app.Map("deploy {env}")
   .WithHandler((string env) => Deploy(env))
   .WithDescription("Deploy to environment")
   .AsCommand()
   .Done();

app.Map("")
   .WithHandler(() => ShowHelp())
   .AsQuery()
   .Done();
```

**Breaking change for 3.0** - Compiler errors will guide migration.

## Parent

151-implement-delegate-generation-phase-2

## Checklist

- [x] Remove `Map(string pattern, Delegate handler)` overloads from `NuruCoreAppBuilder`
- [x] Remove `MapDefault(Delegate handler)` overloads from `NuruCoreAppBuilder`
- [x] Add `Map(string pattern)` overload returning `EndpointBuilder<TBuilder>`
- [x] Add `WithDescription(string description)` method to `EndpointBuilder`
- [x] Remove `description` parameter from all Map/MapMultiple variants
- [x] Verify `EndpointBuilder.WithHandler(Delegate)` exists and works
- [x] Update internal source files to use new fluent API
- [x] Build to confirm removal - expect compile errors in samples/tests
- [ ] Do NOT fix samples/tests yet (that's Task 198)

## Implementation Notes

### New Fluent API

Added `WithDescription()` to `EndpointBuilder` to replace the `description` parameter that was removed from all Map overloads. This enables the fully fluent pattern:

```csharp
app.Map("pattern")
   .WithHandler(handler)
   .WithDescription("description")
   .AsCommand()
   .Done();
```

### Files Modified

**Core API changes:**
- `source/timewarp-nuru-core/endpoint-builder.cs` - Added `WithDescription()`, removed old Map/MapDefault forwarding methods
- `source/timewarp-nuru-core/nuru-core-app-builder.routes.cs` - Added `Map(string pattern)`, removed `description` from all overloads

**Internal usages updated to new fluent API:**
- `source/timewarp-nuru-core/help/help-route-generator.cs`
- `source/timewarp-nuru-completion/nuru-app-builder-extensions.cs`
- `source/timewarp-nuru-repl/nuru-app-extensions.cs`
- `source/timewarp-nuru/nuru-app-builder-extensions.cs`

### Expected Compile Errors

After this change, the following have expected compile errors (to be fixed in Task 198):
- `samples/timewarp-nuru-sample/program.cs` - 1 error (MapDefault)
- `tests/test-apps/timewarp-nuru-testapp-delegates/program.cs` - ~35 errors
- `benchmarks/timewarp-nuru-benchmarks/commands/nuru-direct-command.cs` - 1 error

All source libraries compile successfully.
