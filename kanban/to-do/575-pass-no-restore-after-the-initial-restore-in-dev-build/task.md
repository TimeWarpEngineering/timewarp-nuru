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

## Checklist

- [ ] Add `WithNoRestore()` to the per-project builds after the solution restore in `build-command.cs`
- [ ] Confirm `dev build` still restores once up front and still builds in the existing project order
- [ ] Note-only for the Ganda template unless it is updated to the same two-step pattern

## Acceptance criteria

- The commands `dev build` issues after the first restore include `--no-restore`.
- `dev build --clean` still works.

## Notes

- Related incrementality work (other repos): timewarp-terminal 040 and timewarp-flexbox 157 (`ContinuousIntegrationBuild` only in CI). Umbrella: timewarp-jenzi 011.
- Filed by Jenzi from cross-repo build-telemetry audit 2026-11-12.
