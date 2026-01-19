# Emit --check-updates route in source generator

## Parent

#265 Epic: V2 Source Generator Implementation

## Description

When user calls `AddCheckUpdatesRoute()` in DSL, the source generator should emit the `--check-updates` handler code that:
- Fetches releases from GitHub API
- Compares current version against latest
- Displays update status with colored output

Currently this is runtime code in `nuru-app-builder-extensions.updates.cs`. It should become generated code.

## Checklist

### Phase 1: Interpreter (DSL Recognition)
- [ ] Add `HasCheckUpdatesRoute` flag to `AppModel` record
- [ ] Update `AppModel.Empty()` factory methods
- [ ] Add `AddCheckUpdatesRoute()` to `IIrAppBuilder` interface
- [ ] Implement `AddCheckUpdatesRoute()` in `IrAppBuilder<TSelf>`
- [ ] Add explicit interface implementation
- [ ] Add dispatch case in `DslInterpreter.DispatchMethodCall()`
- [ ] Add `DispatchAddCheckUpdatesRoute()` method

### Phase 2: Emitter (Code Generation)
- [ ] Create `check-updates-emitter.cs` with `CheckUpdatesEmitter` class
- [ ] Emit `CheckForUpdatesAsync(ITerminal terminal)` async method
- [ ] Emit `FetchGitHubReleasesAsync()` HTTP client helper
- [ ] Emit `FindLatestRelease()` version comparison helper
- [ ] Emit `NormalizeTagVersion()` tag normalization helper
- [ ] Emit nested `SemVerComparer` static class with full implementation
- [ ] Emit `GitHubRelease` record with JSON attributes
- [ ] Emit `CheckUpdatesJsonSerializerContext` for AOT-compatible JSON
- [ ] Emit `[GeneratedRegex]` attribute for GitHub URL parsing
- [ ] Update `InterceptorEmitter.EmitBuiltInFlags()` to conditionally emit `--check-updates` handling
- [ ] Update `InterceptorEmitter.EmitNamespaceAndUsings()` to add required usings
- [ ] Update `InterceptorEmitter.EmitClassEnd()` to call `CheckUpdatesEmitter`

### Phase 3: Cleanup (Remove Runtime Code)
- [ ] Convert `AddCheckUpdatesRoute<TBuilder>()` to no-op stub
- [ ] Remove `CheckForUpdatesAsync()` from runtime
- [ ] Remove `FetchGitHubReleasesAsync()` from runtime
- [ ] Remove `FindLatestRelease()` from runtime
- [ ] Remove `NormalizeTagVersion()` from runtime
- [ ] Remove `[GeneratedRegex]` and constants from runtime
- [ ] Delete `source/timewarp-nuru/sem-ver-comparer.cs`
- [ ] Delete `source/timewarp-nuru/github-release.cs`

### Phase 4: Verification
- [ ] Build solution successfully
- [ ] Verify AOT compilation works
- [ ] Test with sample app using `AddCheckUpdatesRoute()`

## Files to Modify

| Action | File |
|--------|------|
| Modify | `source/timewarp-nuru-analyzers/generators/models/app-model.cs` |
| Modify | `source/timewarp-nuru-analyzers/generators/ir-builders/abstractions/iir-app-builder.cs` |
| Modify | `source/timewarp-nuru-analyzers/generators/ir-builders/ir-app-builder.cs` |
| Modify | `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs` |
| Create | `source/timewarp-nuru-analyzers/generators/emitters/check-updates-emitter.cs` |
| Modify | `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` |
| Modify | `source/timewarp-nuru/nuru-app-builder-extensions.updates.cs` |
| Delete | `source/timewarp-nuru/sem-ver-comparer.cs` |
| Delete | `source/timewarp-nuru/github-release.cs` |

## Design Decisions

1. **Error output**: Use `terminal.WriteErrorLineAsync()` for consistency with other generated code
2. **HTTP timeout**: Hardcoded 10-second timeout (configurable option deferred to future task)
3. **Runtime files**: Remove `SemVerComparer.cs` and `github-release.cs` entirely (logic moved to emitter)

## Reference Implementation

See: `source/timewarp-nuru/nuru-app-builder-extensions.updates.cs`

## Notes

Similar to how `--version` is already emitted as built-in. The difference is `--check-updates` is opt-in via DSL method, not always present.

**Execution order:**
1. Interpreter changes (4 files) - follow AddHelp/AddRepl pattern
2. Create CheckUpdatesEmitter - emit ~250 lines of C# as strings
3. Update InterceptorEmitter - wire up conditional emission
4. Runtime cleanup - stub extension method
5. Delete files - remove SemVerComparer and GitHubRelease
6. Build and verify
