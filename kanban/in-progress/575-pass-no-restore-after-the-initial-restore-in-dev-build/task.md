# Pass --no-restore after the initial restore in dev build

## Description

`dev build` should pass `--no-restore` (`WithNoRestore()`) after the initial solution restore so later per-project builds do not restore again.

Current pattern in `tools/dev-cli/endpoints/build-command.cs:53-116`: always `DotNet.Restore()` on `timewarp-nuru.slnx`, optionally `DotNet.Clean()`, then `DotNet.Build().WithProject(...).WithConfiguration("Release")` for each project **without** `WithNoRestore()`. Amuru already supports the flag (`source/timewarp-amuru-tools/dot-net-commands/dot-net.build.cs:362`).

## Requirements

- Keep the explicit first restore of the solution.
- After that restore (and after an optional `--clean`, which should still restore if needed), each `DotNet.Build()` in the per-project loop must use `WithNoRestore()`.
- Do not change the project order or Release configuration.

## Ganda dev-cli template

The shared Ganda template `timewarp-ganda/source/timewarp-ganda/templates/dev-cli/endpoints/build-command.cs.txt:76-80` is a **different** pattern: a single `DotNet.Build().WithConfiguration("Release")` and no explicit prior restore. It does not have Nuru's restore-then-per-project loop, so this task does not require a template change. If that template later gains an explicit restore plus follow-on builds, those follow-on builds should also use `WithNoRestore()`.

## Goal (implementer restatement)

`dev build` restores `timewarp-nuru.slnx` once, optionally cleans, then builds eight projects one by one in Release. Each of those per-project builds currently restores again. Make each per-project `DotNet.Build()` pass `--no-restore` via `WithNoRestore()` so the solution restore is the only restore.

Acceptance:
- The single explicit solution restore stays first.
- Every per-project build command line includes `--no-restore`.
- Project order and `Release` configuration are unchanged.
- `dev build --clean` still works (`dotnet clean` leaves `obj/project.assets.json`, so the up-front restore still covers the builds).
- The Ganda template needs no change (single build, no explicit restore).

## Checklist

- [x] Add `WithNoRestore()` to the per-project builds after the solution restore in `build-command.cs`
- [x] Confirm `dev build` still restores once up front and still builds in the existing project order
- [x] Note-only for the Ganda template unless it is updated to the same two-step pattern

## Acceptance criteria

- The commands `dev build` issues after the first restore include `--no-restore`.
- `dev build --clean` still works.

## Notes

- Related incrementality work (other repos): timewarp-terminal 040 and timewarp-flexbox 157 (`ContinuousIntegrationBuild` only in CI). Umbrella: timewarp-jenzi 011.
- Filed by Jenzi from cross-repo build-telemetry audit 2026-11-12.

## Results

`tools/dev-cli/endpoints/build-command.cs` now adds `.WithNoRestore()` to each per-project `DotNet.Build()` in the loop. Nothing else changed: the solution restore still runs first, the optional `--clean` still follows it, and the 8 projects build in the same order in `Release`. `dotnet clean` keeps `obj/project.assets.json`, so the up-front restore still covers the builds after `--clean`. No Ganda template change was made because that template has no explicit restore followed by more builds.

### Gates

- `./bin/dev build`: **pass** (exit 0, all 8 projects, "Build completed successfully!"). This ran after `dotnet run --file tools/dev-cli/dev.cs -- self-install` installed the edited dev CLI.
- `./bin/dev build --clean`: **pass** (exit 0)
- `./bin/dev test`: **pass** (exit 0). Multi-mode: 2185 total, 2179 passed, 6 skipped, 0 failed. Standalone: 0 failed.
- `ganda repo audit`: **pass** (exit 0, 43 passed, 1 advisory warning). The first run failed `kanban-landed` because task 573's live-claim kitchen had drifted from origin-home. I fixed it with `ganda kanban reconcile --task-id 573` (boy-scout fix) and reran the audit. The remaining warning is `vscode-window-icon` (`.vscode/settings.json` is missing). It is advisory and was not changed.

### How to validate

Smoke: `./bin/dev build --verbose 2>&1 | grep '^dotnet build'`

Expect: 8 `dotnet build <project> --configuration Release --verbosity normal --no-restore` lines, in this order: analyzers, build, nuru, mcphost, daemon, mcp, search, devcli. Only one restore happens: the "Restoring packages..." step on `timewarp-nuru.slnx`. `./bin/dev build --clean` exits 0.

