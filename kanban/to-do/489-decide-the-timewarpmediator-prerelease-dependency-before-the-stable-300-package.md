# Decide the TimeWarp.Mediator prerelease dependency before the stable 3.0.0 package

## Description

**3.0.0 launch gate #2.** `TimeWarp.Nuru` depends on `TimeWarp.Mediator.Contracts` and
`TimeWarp.Mediator.Generators` `14.0.0-beta.4` (`Directory.Packages.props:33-34`). The newest Mediator
release is `v14.0.0-beta.4` (2026-09-27); there is no stable 14.0.0. A stable `3.0.0` package with a
prerelease dependency triggers NuGet warning NU5104 at pack time and forces every consumer onto a beta
of Mediator, which also surfaces as "prerelease" badges and `--prerelease` requirements on their side.

This task is the decision and its execution, not a code fix in Nuru by itself.

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

- [ ] Decision made and recorded (maintainer call)
- [ ] Mediator side task filed / version bumped, or NU5104 handled, per the decision
- [ ] Release notes and migration guide say what consumers need to do
- [ ] `dotnet pack` of `source/timewarp-nuru` on the 3.0.0 version shows no NU5104 (or the documented suppression)
- [ ] PR merged

## Notes

- Found by task 487 (2026-10-09). The blog and thread copy currently say nothing about Mediator being prerelease.

## Session

- Created: claude 2412bd45 (2026-10-09)
