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

### Interpreter
- [ ] Add `AddCheckUpdatesRoute()` to DSL dispatcher
- [ ] Set `AppModel.HasCheckUpdatesRoute = true` flag

### Emitter
- [ ] Emit `--check-updates` args check in route matching
- [ ] Emit `CheckForUpdatesAsync()` helper method
- [ ] Emit `FetchGitHubReleasesAsync()` helper
- [ ] Emit `SemVerComparer` logic (or inline comparison)
- [ ] Emit `GitHubRelease` record for JSON deserialization
- [ ] Emit AOT-compatible JSON serializer context

### Cleanup
- [ ] Remove runtime implementation from `nuru-app-builder-extensions.updates.cs`
- [ ] Keep `AddCheckUpdatesRoute()` as no-op stub
- [ ] Remove `SemVerComparer.cs` (logic moved to emitter)

## Reference Implementation

See: `source/timewarp-nuru/nuru-app-builder-extensions.updates.cs`

## Notes

Similar to how `--version` is already emitted as built-in. The difference is `--check-updates` is opt-in via DSL method, not always present.
