# Decide the TimeWarp.Amuru prerelease dependency before the stable 3.0.0 package

## Description

**3.0.0 launch gate #5** (same shape as 489 for Mediator). `TimeWarp.Nuru` depends on `TimeWarp.Amuru`
`2.0.0-beta.2` (`Directory.Packages.props:26`, referenced by `source/timewarp-nuru/timewarp-nuru.csproj` and
`source/timewarp-nuru-search/timewarp-nuru-search.csproj`). nuget.org has no stable `TimeWarp.Amuru` 2.0.0; the
latest stable is `1.1.1` (2026-09-23) and the latest release is `v2.0.0-beta.2` (2026-10-06).

With Mediator now pinned at stable 14.0.0 (489, PR #315), Amuru is the **only** prerelease dependency left in the
shipped `TimeWarp.Nuru` nuspec (`TimeWarp.Jaribu` and `TimeWarp.SourceGenerators` are test/build-time only and do not
flow). Found by 489's pack check on 2026-10-13:

```
dotnet pack source/timewarp-nuru/timewarp-nuru.csproj -c Release -p:Version=3.0.0
error NU5104: A stable release of a package should not have a prerelease dependency.
Either modify the version spec of dependency "TimeWarp.Amuru [2.0.0-beta.2, )" or update the version field in the nuspec.
```

NU5104 is promoted to an error in this repo, so the 3.0.0 pack fails outright until this is resolved.

## Decision

**Option 1 (maintainer, 2026-10-13): ship TimeWarp.Amuru 2.0.0 first. Do not pin back.** Steven: "we need to ship
amuru first I do NOT want to go backwards."

**Unblocked 2026-10-14:** TimeWarp.Amuru `2.0.0` and `TimeWarp.Amuru.Tools` `2.0.0` are on nuget.org (release
https://github.com/TimeWarpEngineering/timewarp-amuru/releases/tag/v2.0.0, 2026-10-14 01:54 UTC).

## Scope for the implementer

1. `Directory.Packages.props:26-27`: `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools` `2.0.0-beta.2` -> `2.0.0`.
2. Fix any stale `2.0.0-beta.2` Amuru mentions outside `kanban/done` (csproj comments, docs); add a changelog
   line under Unreleased/Changed, same shape as the 489 Mediator line.
3. `dotnet pack source/timewarp-nuru/timewarp-nuru.csproj -c Release -p:Version=3.0.0` must exit 0 with **zero**
   NU5104. If any other prerelease dependency surfaces, record it in Results and stop; do not suppress.
4. Release notes draft (`kanban/to-do/487-*/release-notes.md`) and `documentation/user/guides/migrating-to-3.0.md`:
   state that 3.0.0 depends on stable Amuru 2.0.0 (betas through 3.0.0-beta.80 depend on 2.0.0-beta.2). Tick 487
   launch gate #5.
5. `bin/dev build`, 64 samples, and the full CI suite green (`ganda runfile cache --clear` first).

## Options

1. **Ship TimeWarp.Amuru 2.0.0 first**, then bump Nuru's pin to `2.0.0` and tag 3.0.0. Cleanest for consumers;
   consistent with the 489 decision for Mediator (no "allow prerelease" caveat on a stable package).
2. Accept the prerelease dependency: `<NoWarn>NU5104</NoWarn>` scoped to the Amuru reference, document in the
   changelog and migration guide that consumers must allow prerelease for Amuru. Fast, but "stable" is partly untrue.
3. Pin Nuru back to `TimeWarp.Amuru` 1.1.1 for 3.0.0. Only viable if Nuru does not use any 2.0 API; check
   `source/timewarp-nuru/global-usings.cs`, the capabilities emitter, and the search/devcli usages first.

## Requirements

- Decision recorded in Results with the reason; reflected in the 3.0.0 release-notes draft
  (`kanban/to-do/487-*/release-notes.md`) and `documentation/user/guides/migrating-to-3.0.md`.
- If option 1: a kanban task in timewarp-amuru for the 2.0.0 stable release, linked here; Nuru's
  `Directory.Packages.props:26-27` bumped once it ships (`TimeWarp.Amuru` and `TimeWarp.Amuru.Tools`).
- If option 2: NU5104 suppression scoped to the Amuru reference with a comment pointing here.
- The 487 launch-gates list gets a gate #5 bullet for this (added with this filing).

## Checklist

- [x] Decision made and recorded (maintainer call): option 1, ship Amuru 2.0.0 first (2026-10-13)
- [x] Amuru 2.0.0 shipped (2026-10-14)
- [ ] Nuru `Directory.Packages.props` -> Amuru 2.0.0, CI green
- [ ] Release notes and migration guide say what consumers need to do
- [ ] `dotnet pack source/timewarp-nuru -c Release -p:Version=3.0.0` exits 0 with no NU5104
- [ ] PR merged

## How to validate

```bash
grep -n 'TimeWarp.Amuru' Directory.Packages.props
curl -s https://api.nuget.org/v3-flatcontainer/timewarp.amuru/index.json
dotnet pack source/timewarp-nuru/timewarp-nuru.csproj -c Release -p:Version=3.0.0 2>&1 | grep -c NU5104   # expect 0
```

## Notes

- Found by 489 (2026-10-13) while verifying the Mediator pin; recorded there under "Pack at 3.0.0".
- timewarp-amuru is at `<Version>2.0.0-beta.2</Version>` on origin master with no open release task on its board
  (to-do: 083, 119, 128 as of 2026-10-13).

## Session

- Created: Claude Fable 5.1, session 31dfb8d1 (2026-10-13), filed from the 489 merge
- Decision recorded (ship Amuru first) and task unblocked on Amuru 2.0.0: Claude Fable 5.1 (2026-10-14)
