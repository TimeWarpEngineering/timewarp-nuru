# Decide the TimeWarp.Mediator prerelease dependency before the stable 3.0.0 package

## Description

**3.0.0 launch gate #2.** `TimeWarp.Nuru` depends on `TimeWarp.Mediator.Contracts` and
`TimeWarp.Mediator.Generators` `14.0.0-beta.4` (`Directory.Packages.props:33-34`). The newest Mediator
release is `v14.0.0-beta.4` (2026-09-27); there is no stable 14.0.0. A stable `3.0.0` package with a
prerelease dependency triggers NuGet warning NU5104 at pack time and forces every consumer onto a beta
of Mediator, which also surfaces as "prerelease" badges and `--prerelease` requirements on their side.

This task is the decision and its execution, not a code fix in Nuru by itself.

## Decision

**Option 1, with a dogfood step (maintainer, 2026-10-12):** TimeWarp.Mediator 14.0.0 ships *before* Nuru 3.0.0,
but Mediator is dogfooded on its current beta across the TimeWarp consumers first. Sequence:

1. Dogfood `TimeWarp.Mediator 14.0.0-beta.4` (or a newer beta if one is cut) in its consumers — Nuru is already on
   beta.4; identify the others from `Directory.Packages.props` across the TimeWarp repos — and fix what breaks.
2. Release `TimeWarp.Mediator 14.0.0` (task in timewarp-mediator; link it here).
3. Bump Nuru's `Directory.Packages.props` to `14.0.0`, re-run CI, then the Nuru `3.0.0` bump task.

No NU5104 suppression. The 3.0.0 release notes and migration guide must not need an "allow prerelease" caveat.

## Options

1. **Ship TimeWarp.Mediator 14.0.0 first**, then bump Nuru's reference and tag 3.0.0. Cleanest for consumers.
   Needs the Mediator repo's own release check (its 3.0-era contract types are what Nuru 3.0 exposes).
2. Accept the prerelease dependency for 3.0.0: suppress NU5104 (`<NoWarn>`), document in the changelog and
   migration guide that consumers must allow prerelease for Mediator. Fast, but it makes "stable" partly untrue.
3. Delay 3.0.0 until Mediator is stable (same as 1, but with no interim Nuru beta).

## Requirements

- Decision recorded in Results with the reason, and reflected in the 3.0.0 release notes draft
  (`kanban/done/487-*/release-notes.md`) and `documentation/user/guides/migrating-to-3.0.md`.
- If option 1: a kanban task in timewarp-mediator for the 14.0.0 bump/release, linked here; Nuru's
  `Directory.Packages.props` bumped once it ships.
- If option 2: NU5104 suppression scoped to the two Mediator references, with a comment pointing here.

## Checklist

- [x] Decision made and recorded (maintainer call): option 1 + dogfood, 2026-10-12
- [x] Mediator consumers enumerated (which TimeWarp repos reference `TimeWarp.Mediator.*`, at which versions)
- [x] Dogfood task(s) filed for consumers not yet on the current Mediator beta (one per repo, `ganda kanban reserve --repo <repo>`) — none needed: every consumer is already on beta.4
- [x] Release task filed in timewarp-mediator for `14.0.0`, linked here: timewarp-mediator kanban 014 (depends on 013)
- [x] After Mediator 14.0.0 ships: Nuru `Directory.Packages.props` → `14.0.0`, CI green
- [x] Release notes and migration guide say what consumers need to do
- [x] `dotnet pack` of `source/timewarp-nuru` at `Version=3.0.0` raises no NU5104 for `TimeWarp.Mediator.*`. The pack still errors NU5104 for `TimeWarp.Amuru` 2.0.0-beta.2 only (task 494). Not suppressed.
- [ ] PR merged

## Results

**Decision:** option 1 + dogfood (maintainer, 2026-10-12). TimeWarp.Mediator 14.0.0 ships before Nuru 3.0.0.
**Reason:** a stable Nuru 3.0.0 referencing `14.0.0-beta.4` raises NU5104 and forces every consumer onto a
Mediator beta. Suppressing it (option 2) would make "stable" partly untrue. Option 3 drops the interim Nuru betas
that are already dogfooding Mediator.

### Mediator consumers (origin master, 2026-10-12)

| Repo | Reference | Mediator version |
|------|-----------|------------------|
| timewarp-nuru | `TimeWarp.Mediator.Contracts`, `.Generators` (direct) | 14.0.0-beta.4 |
| timewarp-state | `.Contracts`, `.Generators`, `.Analyzers` (direct) | 14.0.0-beta.4 |
| timewarp-architecture | `.Contracts`, `.Generators`, `.Analyzers` (direct) | 14.0.0-beta.4 |
| timewarp-intel | via `TimeWarp.State` 12.0.0-beta.10 (nuspec: Contracts 14.0.0-beta.4) | 14.0.0-beta.4 |
| timewarp-amuru, -bayline, -flow, -ganda, -session-lake-tools, -taratibu, -terminal, -mediator (dev CLI) | via `TimeWarp.Nuru` 3.0.0-beta.79 (nuspec: Contracts + Generators 14.0.0-beta.4) | 14.0.0-beta.4 |

Every consumer already runs beta.4, so the dogfood is live and no per-repo bump task was filed.
Known dogfood breakage: timewarp-mediator#64 (internal request types → CS0051), documented in the guide.

### Filed

- timewarp-mediator kanban **014** "Release TimeWarp.Mediator 14.0.0 stable (gates Nuru 3.0.0)", published to
  origin master (`kanban/to-do/014-release-timewarpmediator-1400-stable-gates-nuru-300/`), depends on 013
  (repository review children; 013-004 public API is a before-stable gate).

### Docs updated

- `documentation/user/guides/migrating-to-3.0.md`: published betas through 3.0.0-beta.79 use
  14.0.0-beta.4; later Nuru builds, including stable 3.0.0, reference stable 14.0.0. No prerelease
  opt-in for Mediator. Move your own `TimeWarp.Mediator.*` pins to 14.0.0 with those Nuru versions.
- `kanban/to-do/487-*/release-notes.md`: states the stable Mediator dependency; the pre-paste check
  names the 14.0.0 pin.
- `kanban/to-do/487-*/task.md`: launch gate #2 is checked. The pin is 14.0.0.

### Bump (2026-10-13)

nuget.org flat index lists `14.0.0` for `TimeWarp.Mediator.Contracts` and `TimeWarp.Mediator.Generators`
(also `TimeWarp.Mediator`). `Directory.Packages.props` pins both Nuru packages at `14.0.0`. The
`TimeWarpMediatorAssembly` comment names 14.0.0. Changelog Unreleased records the move from beta.5.

`dotnet build timewarp-nuru.slnx -c Release`: 0 warnings, 0 errors. `./bin/dev verify-samples`: 64/64.
`dotnet tests/ci-tests/run-ci-tests.cs`: exit 0. Multi-mode total 1883, passed 1877, skipped 6, failed 0.
No TWM005–TWM011. `endpoint-hello-world` prints `Hello World`. `calculator add 2 3` prints `5`.
`ganda repo audit`: 30 passed, 0 failed.

The rebuilt `TimeWarp.Nuru.3.0.0-beta.80.nupkg` nuspec depends on `TimeWarp.Mediator.Contracts` and
`TimeWarp.Mediator.Generators` at `14.0.0` (`include="All"` on Generators).

### Pack at 3.0.0

`dotnet pack source/timewarp-nuru/timewarp-nuru.csproj -c Release -p:Version=3.0.0` fails with one
NU5104, promoted to an error:

`A stable release of a package should not have a prerelease dependency. Either modify the version spec of dependency "TimeWarp.Amuru [2.0.0-beta.2, )" or update the version field in the nuspec.`

Mediator is not named. No `NoWarn` was added. nuget.org has no `TimeWarp.Amuru` 2.0.0 (latest is
`2.0.0-beta.2`). That remaining warning is task 494's stable-Nuru gate, outside this id.

### How to validate

**Smoke:**

```bash
grep -n 'TimeWarp.Mediator' Directory.Packages.props
grep -n '14.0.0' documentation/user/guides/migrating-to-3.0.md | head -5
dotnet build source/timewarp-nuru/timewarp-nuru.csproj -c Release
unzip -p artifacts/packages/TimeWarp.Nuru.3.0.0-beta.80.nupkg TimeWarp.Nuru.nuspec | grep Mediator
dotnet pack source/timewarp-nuru/timewarp-nuru.csproj -c Release -p:Version=3.0.0 -o /tmp/nuru-489-pack
```

**Expect:**

- Both `PackageVersion` lines are `14.0.0`.
- The migration guide says later Nuru builds, including stable 3.0.0, reference stable `TimeWarp.Mediator` 14.0.0.
- The nuspec lists `TimeWarp.Mediator.Contracts` and `TimeWarp.Mediator.Generators` at `14.0.0`.
- Pack exits non-zero with one NU5104, and that line names `TimeWarp.Amuru` `2.0.0-beta.2`. It does not name Mediator.

## Notes

- Found by task 487 (2026-10-09). The blog and thread copy currently say nothing about Mediator being prerelease.

## Session

- Created: claude 2412bd45 (2026-10-09)
- Decision recorded for the maintainer: claude 2412bd45 (2026-10-12)
- Consumers enumerated, timewarp-mediator 014 filed, docs updated: claude implement oracle (2026-10-12)
- Mediator 14.0.0 release recorded, task unblocked: Claude (timewarp-mediator cockpit session) (2026-10-13)
- Pin moved to 14.0.0, CI green, pack checked: grok implement oracle (2026-10-13)

## Unblocked

2026-10-13: TimeWarp.Mediator `14.0.0` stable is released (timewarp-mediator kanban 017, which replaced archived 014).
Tag `v14.0.0` at `96e3e42`; publish run 38408409563 pushed Contracts, Mediator, Generators and Analyzers at 13:53 UTC;
https://github.com/TimeWarpEngineering/timewarp-mediator/releases/tag/v14.0.0. The stable is `14.0.0-beta.5` unchanged,
which Nuru dogfooded in task 498 with zero TWM diagnostics. The pin bump is in ### Bump above.

## Blocked (history)

2026-10-12: TimeWarp.Mediator 14.0.0 must ship (timewarp-mediator kanban 014) before Nuru's Directory.Packages.props can move to 14.0.0 and the NU5104-free pack can be checked https://github.com/TimeWarpEngineering/timewarp-mediator/releases

- https://github.com/TimeWarpEngineering/timewarp-mediator/releases
