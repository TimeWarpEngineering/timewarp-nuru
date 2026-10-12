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
- [ ] Mediator consumers enumerated (which TimeWarp repos reference `TimeWarp.Mediator.*`, at which versions)
- [ ] Dogfood task(s) filed for consumers not yet on the current Mediator beta (one per repo, `ganda kanban reserve --repo <repo>`)
- [ ] Release task filed in timewarp-mediator for `14.0.0`, linked here; blocked on the dogfood tasks
- [ ] After Mediator 14.0.0 ships: Nuru `Directory.Packages.props` → `14.0.0`, CI green
- [ ] Release notes and migration guide say what consumers need to do
- [ ] `dotnet pack` of `source/timewarp-nuru` on the 3.0.0 version shows no NU5104 (or the documented suppression)
- [ ] PR merged

## Notes

- Found by task 487 (2026-10-09). The blog and thread copy currently say nothing about Mediator being prerelease.

## Session

- Created: claude 2412bd45 (2026-10-09)
- Decision recorded for the maintainer: claude 2412bd45 (2026-10-12)

## Blocked

2026-10-12: maintainer must choose option 1 (ship TimeWarp.Mediator 14.0.0 first), option 2 (ship Nuru 3.0.0 on Mediator 14.0.0-beta.4 with NU5104 suppressed and documented) or option 3 (delay 3.0.0), and record it on task 489; only 14.0.0-beta.4 exists today https://github.com/TimeWarpEngineering/timewarp-mediator/releases

- https://github.com/TimeWarpEngineering/timewarp-mediator/releases
