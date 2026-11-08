# DevCli check-version: packable-project scan takes minutes and prints nothing

## Description

Filed from the Roslynk cockpit, 2026-11-09. Steven reported "`dev check-version` just hangs, never
returns" in TimeWarpEngineering/Roslynk.

It does not hang. On Roslynk master (Version 1.0.0-beta.1, DevCli 4.0.0-beta.11) it completes in
**114 s** with exit 0 and the correct answer, but prints **nothing** until the end, so it looks hung.

Cause: `source/timewarp-nuru-devcli/content/any/services/packable-project-service.cs`
`GetPackableProjectsAsync` enumerates every `*.csproj` under `source/` and runs
`dotnet msbuild <proj> -nologo -getProperty:IsPackable,PackageId` **sequentially**, one process per
project. The Design comment assumes ~0.4 s/project; observed ~4.5 s/project on WSL2. Roslynk has 24
csproj under `source/` (5 app projects + 19 test-fixture projects under `source/TestFixtures/`, which
carry their own `global.json` and are never packable). Only one project (`TimeWarp.Roslynk`) is packable.
`dev release` and `dev pack`/push paths share this service, so they pay the same cost.

## Requirements

- Make the scan fast. Candidates, pick with evidence (time each on Roslynk):
  - Evaluate projects in parallel with a bounded degree (e.g. `Environment.ProcessorCount`), keeping the
    deterministic `ValidateDerivedSet` ordering and every fail-loud guard (458-004 / 470 M23).
  - One MSBuild invocation for many projects (e.g. a generated traversal or `-getProperty` over a
    solution), if it gives the same per-project answers.
  - Do **not** skip projects by path heuristic (e.g. "TestFixtures") or parse XML — task 458-004 D1
    requires real MSBuild evaluation so no packable project is silently dropped.
- Show progress: say what it is doing before the scan (e.g. `Evaluating 24 projects for IsPackable…`)
  so the command never sits silent. Keep stdout stable for anything that parses it.
- Update the Purpose/Design region (the 0.4 s/project claim) to match reality.
- Tests: existing parse/validate unit tests still pass; add coverage for the parallel path's ordering and
  error propagation (a failing project still throws naming it).
- Ship in a TimeWarp.Nuru.DevCli prerelease; follow-up: Roslynk bumps DevCli.

## Checklist

- [ ] Reproduce timing on Roslynk (`bin/dev check-version`, ~114 s baseline)
- [ ] Faster evaluation, guards preserved
- [ ] Progress line before the scan
- [ ] Design comment updated
- [ ] Tests
- [ ] Released in a DevCli beta; Roslynk follow-up noted

## Session

- Created: Roslynk cockpit 93b68595-4648-49ee-a58b-6a1ec1f5dab9 (2026-11-09)

## Notes

- Related: 525 (feed lookup for check-version), 458-004 (MSBuild-derived packable set).
