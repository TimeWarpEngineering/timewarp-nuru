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
- [ ] After Mediator 14.0.0 ships: Nuru `Directory.Packages.props` → `14.0.0`, CI green
- [x] Release notes and migration guide say what consumers need to do
- [ ] `dotnet pack` of `source/timewarp-nuru` on the 3.0.0 version shows no NU5104 (or the documented suppression)
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

- `documentation/user/guides/migrating-to-3.0.md`: betas use 14.0.0-beta.4; stable 3.0.0 references stable
  14.0.0, no prerelease opt-in for Mediator; bump your own `TimeWarp.Mediator.*` pins with Nuru.
- `kanban/to-do/487-*/release-notes.md`: states the stable Mediator dependency; pre-paste check names gate #2.
- `kanban/to-do/487-*/task.md`: launch gate #2 records the decision.

### Remaining (blocked on Mediator 14.0.0 shipping)

- Bump `Directory.Packages.props:33-34` to `14.0.0`, CI green.
- `dotnet pack source/timewarp-nuru` on 3.0.0 shows no NU5104.

### How to validate

**Smoke:**

```bash
grep -n 'TimeWarp.Mediator' Directory.Packages.props
grep -n 'Mediator' documentation/user/guides/migrating-to-3.0.md | head -5
git -C ~/worktrees/github.com/TimeWarpEngineering/timewarp-mediator/master fetch -q && \
  git -C ~/worktrees/github.com/TimeWarpEngineering/timewarp-mediator/master show \
  origin/master:kanban/to-do/014-release-timewarpmediator-1400-stable-gates-nuru-300/task.md | head -5
# after Mediator 14.0.0 ships and the bump lands:
dotnet pack source/timewarp-nuru -c Release -p:Version=3.0.0 2>&1 | grep -c NU5104
```

**Expect:**

- Today: props still show `14.0.0-beta.4`; after the bump, `14.0.0` on both lines.
- The migration guide says stable 3.0.0 references stable `TimeWarp.Mediator` 14.0.0.
- The mediator 014 kitchen exists on origin master with the title above.
- After the bump, the pack grep prints `0`.

## Notes

- Found by task 487 (2026-10-09). The blog and thread copy currently say nothing about Mediator being prerelease.

## Session

- Created: claude 2412bd45 (2026-10-09)
- Decision recorded for the maintainer: claude 2412bd45 (2026-10-12)
- Consumers enumerated, timewarp-mediator 014 filed, docs updated: claude implement oracle (2026-10-12)

## Blocked

2026-10-12: TimeWarp.Mediator 14.0.0 must ship (timewarp-mediator kanban 014) before Nuru's Directory.Packages.props can move to 14.0.0 and the NU5104-free pack can be checked https://github.com/TimeWarpEngineering/timewarp-mediator/releases

- https://github.com/TimeWarpEngineering/timewarp-mediator/releases

## Results

**Decision: option 1.** TimeWarp.Mediator `14.0.0` stable shipped first; Nuru bumps to it and 3.0.0 carries no prerelease
dependency, so NU5104 never needs suppressing.

- Mediator side: timewarp-mediator kanban 017 (replaced archived 014). Tag `v14.0.0` at `96e3e42`, 2026-10-13.
  Release: https://github.com/TimeWarpEngineering/timewarp-mediator/releases/tag/v14.0.0. Publish run 38408409563 pushed
  Contracts, Mediator, Generators and Analyzers 14.0.0 at 13:53 UTC. The stable is `14.0.0-beta.5` unchanged (no source
  change after beta.5), which Nuru already dogfooded in task 498 with zero TWM diagnostics.
- Remaining here: bump `TimeWarp.Mediator.Contracts` and `TimeWarp.Mediator.Generators` in `Directory.Packages.props` from
  `14.0.0-beta.5` to `14.0.0` (confirm nuget.org flat index lists 14.0.0 first), update the 3.0.0 release notes draft and
  `migrating-to-3.0.md` to say the dependency is stable, and confirm `dotnet pack` of `source/timewarp-nuru` shows no NU5104.

Recorded by Claude (timewarp-mediator cockpit session) on 2026-10-13 after cutting the Mediator release.
