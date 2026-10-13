# Update NuGet packages to latest (TimeWarp.Mediator 14.0.0-beta.5 dogfood)

## Description

TimeWarp.Mediator `14.0.0-beta.5` shipped 2026-10-13 (https://github.com/TimeWarpEngineering/timewarp-mediator/releases/tag/v14.0.0-beta.5).
It carries the 22 repository-review changes (mediator kanban 013, PR #95): net10.0-only runtime and Contracts,
smaller runtime public API, generator handler binding and type-switch fixes, incremental generator pipeline,
TWM005-TWM011 diagnostics replacing generated-code compile errors, trimming/AOT annotations on the reflection
path. Nuru consumes `TimeWarp.Mediator.Contracts` and `TimeWarp.Mediator.Generators` at `14.0.0-beta.4`
(`Directory.Packages.props:33-34`, task 475). This is the dogfood pass before Mediator `14.0.0` stable, which
is the dependency Nuru 3.0.0 waits on (task 489, option 1).

Also take every other pin in `Directory.Packages.props` to latest per the repo audit policy. At filing time
`ganda nuget outdated` listed 4 packages: the two Mediator pins (→ 14.0.0-beta.5), Microsoft.CodeAnalysis.CSharp
5.6.0 → 5.9.0, Microsoft.Build.Utilities.Core 18.9.6 → 18.10.1.

Dogfood results so far from the other consumers: timewarp-architecture 342 built and tested on beta.5 with zero
TWM diagnostics and no runtime problems; its verdict is "ready for 14.0.0 stable". timewarp-state 119 is the
same bump for State. Nuru is the heaviest generator consumer (Mcp, Search, samples, AOT), so its verdict matters most.

Filed by Claude (timewarp-mediator cockpit session) at Steven's request.

## Requirements

- Both TimeWarp.Mediator pins at `14.0.0-beta.5`; other pins at latest unless a bump breaks the build and the
  reason is recorded here. Microsoft.CodeAnalysis.CSharp: check whether 5.6.0 is a deliberate floor for Nuru's own
  generators/analyzers before bumping; a hold with the reason recorded is acceptable.
- Release build clean with zero new TWM005-TWM011 diagnostics, or each triaged: fixed here, or filed on
  timewarp-mediator with a repro when the generator is wrong.
- CS0436 count in `dotnet build tests/ci-tests/run-ci-tests.cs -c Release` recorded before and after (task 475
  recorded 4, caused by InternalsVisibleTo to `run-ci-tests`; expected unchanged).
- Full CI test gate green, samples build and run, AOT sample publishes. Any failure caused by the Mediator bump
  is filed on timewarp-mediator before this task closes.
- Changelog entry under Unreleased.
- `ganda repo audit` clean.

## Checklist

- [ ] `ganda nuget outdated` re-run at start
- [ ] Bump `Directory.Packages.props` (Mediator x2, CodeAnalysis.CSharp, Build.Utilities.Core, or holds recorded)
- [ ] Release build: no CS errors; zero TWM diagnostics or each triaged
- [ ] CS0436 before/after recorded
- [ ] Build, tests, samples, AOT publish pass
- [ ] Breakages from the Mediator bump filed on timewarp-mediator
- [ ] Changelog updated
- [ ] Results: before/after versions table, TWM findings, verdict for Mediator 14.0.0 stable readiness
- [ ] `ganda repo audit` clean; PR merged

## Session

- Created: 1480458 (2026-10-13)

## Notes

- Related: task 489 (Mediator prerelease dependency decision for 3.0.0). Mediator 14.0.0 stable will follow
  once this and timewarp-state 119 report clean; 489 then bumps to stable and 3.0.0 can ship without NU5104.
- Mediator release notes: documentation/release-notes/release-notes-v14.0.0.md in timewarp-mediator.
- Mediator diagnostics reference: documentation/diagnostics.md in timewarp-mediator (TWM005-TWM011).
