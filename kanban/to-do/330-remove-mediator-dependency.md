# Remove Mediator Dependency

## Summary

Remove the unused Mediator library dependency. The "mediator pattern" functionality in TimeWarp.Nuru is now accomplished via attributed routes and behaviors - we no longer use the external Mediator library.

## Background

The `timewarp-nuru-testapp-mediator` test app has compilation errors due to `Unit` type ambiguity between `Mediator.Unit` and `TimeWarp.Nuru.Unit`. Rather than fixing these errors, we should remove the Mediator dependency entirely since it's no longer used.

## Blocked By

- #331 - Fix catch-all parameter variable name mismatch in generator

## Checklist

- [x] Comment out `timewarp-nuru-testapp-mediator` from `timewarp-nuru.slnx`
- [x] Fix embedded resource path in `timewarp-nuru-mcp.csproj` (syntax-examples.cs moved)
- [x] Remove Mediator packages from `Directory.Packages.props`
- [x] Remove `<PackageReference Include="Mediator.Abstractions">` from:
  - [x] `source/timewarp-nuru/timewarp-nuru.csproj`
  - [x] `source/timewarp-nuru-core/timewarp-nuru-core.csproj`
  - [x] `source/timewarp-nuru-telemetry/timewarp-nuru-telemetry.csproj`
- [x] Remove `<PackageReference Include="Mediator.*">` from:
  - [x] `tests/test-apps/timewarp-nuru-testapp-delegates/timewarp-nuru-testapp-delegates.csproj`
- [x] Remove `global using Mediator;` from:
  - [x] `source/timewarp-nuru-core/global-usings.cs`
  - [x] `source/timewarp-nuru/global-usings.cs`
- [x] Remove `builder.Services.AddMediator();` from `timewarp-nuru-testapp-delegates/program.cs`
- [ ] Delete `tests/test-apps/timewarp-nuru-testapp-mediator/` directory
- [ ] Verify full solution builds successfully (blocked by #331)

## Progress

All Mediator package references have been removed. The solution build now fails due to a **pre-existing bug** in the source generator (not related to Mediator removal):

```
error CS0103: The name 'everything' does not exist in the current context
```

The generator emits catch-all parameters with unique variable names (`__everything_34`) but then calls the handler with the original parameter name (`everything`), which is undefined.

This bug was hidden before because the Mediator-based code path was being used. Created task #331 to fix this.

## Notes

- The routing scenarios tested in `timewarp-nuru-testapp-mediator` (sub-commands, options, catch-all, etc.) are covered by samples and `timewarp-nuru-testapp-delegates`
- TimeWarp.Nuru has its own `Unit` type, so the external Mediator library is redundant
- Also fixed: `timewarp-nuru-mcp.csproj` embedded resource path updated from `../../samples/syntax-examples.cs` to `../../samples/04-syntax-examples/syntax-examples.cs`
