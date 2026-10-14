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
- [x] Nuru `Directory.Packages.props` -> Amuru 2.0.0, CI green
- [x] Release notes and migration guide say what consumers need to do
- [x] `dotnet pack source/timewarp-nuru -c Release -p:Version=3.0.0` exits 0 with no NU5104
- [ ] PR merged

## How to validate

```bash
grep -n 'TimeWarp.Amuru' Directory.Packages.props
curl -s https://api.nuget.org/v3-flatcontainer/timewarp.amuru/index.json
dotnet pack source/timewarp-nuru/timewarp-nuru.csproj -c Release -p:Version=3.0.0 2>&1 | grep -c NU5104   # expect 0
```

## Results

**Decision:** option 1 (maintainer, 2026-10-13). Ship TimeWarp.Amuru 2.0.0 first, then pin Nuru to it. Do not pin back to 1.1.1 and do not suppress NU5104.
**Reason:** a stable Nuru 3.0.0 referencing `TimeWarp.Amuru` 2.0.0-beta.2 raises NU5104 and forces every consumer onto an Amuru beta. Suppressing it (option 2) would make "stable" partly untrue. Pinning back (option 3) drops the 2.0 API Nuru already uses.

Amuru 2.0.0 shipped 2026-10-14 (timewarp-amuru kanban 129, release
[v2.0.0](https://github.com/TimeWarpEngineering/timewarp-amuru/releases/tag/v2.0.0)).
No public API changed between 2.0.0-beta.2 and 2.0.0, so this pin is a version bump only.

### Docs updated

- `changelog.md` Unreleased/Changed: `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools` moved from 2.0.0-beta.2 to stable 2.0.0 (502). Consumers do not need a prerelease opt-in. Published betas through 3.0.0-beta.80 depend on 2.0.0-beta.2.
- `documentation/user/guides/migrating-to-3.0.md`: stable 3.0.0 depends on `TimeWarp.Amuru` 2.0.0. Own pins of `TimeWarp.Amuru` should be 2.0.0 with that Nuru version. `TimeWarp.Amuru.Tools` 2.0.0 is this repo's dev CLI pin and does not flow from the Nuru package.
- `kanban/to-do/487-*/release-notes.md`: states the stable Amuru dependency. The pre-paste check names the 2.0.0 pin (gate #5).
- `kanban/to-do/487-*/task.md`: launch gate #5 is checked. The pin is 2.0.0.
- No csproj comment outside `kanban/done` still named `2.0.0-beta.2`. Historical mentions under `kanban/done` were left as the record of the beta pin.

### Bump (2026-10-14)

nuget.org flat index lists `2.0.0` for `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools`.
`Directory.Packages.props` pins both at `2.0.0`.

`ganda runfile cache --clear` cleared 1428 entries. `./bin/dev build`: each project 0 warnings, 0 errors.
`./bin/dev verify-samples`: 64/64. `./bin/dev test` (`tests/ci-tests/run-ci-tests.cs`): exit 0.
Multi-mode total 1883, passed 1877, skipped 6, failed 0. Standalone phase completed with no failures.
`ganda repo audit`: 30 passed, 0 failed.

### Pack at 3.0.0

`dotnet pack source/timewarp-nuru/timewarp-nuru.csproj -c Release -p:Version=3.0.0 -o /tmp/nuru-502-pack`
exited 0. `grep -c NU5104` on a second pack of the same command was 0. No other prerelease dependency
surfaced. The `TimeWarp.Nuru.3.0.0.nupkg` nuspec depends on `TimeWarp.Amuru` at `2.0.0`. Every other
dependency in that nuspec is a stable version (`TimeWarp.Mediator.Contracts` and
`TimeWarp.Mediator.Generators` stay at `14.0.0`). No `NoWarn` was added.

`TimeWarp.Nuru.Search` packed at `Version=3.0.0` also exited 0. It is a `DotnetTool` nuspec with no
dependency group; the search project still takes `TimeWarp.Amuru` from the same central pin.

### How to validate

**Smoke:**

```bash
grep -n 'TimeWarp.Amuru' Directory.Packages.props
grep -n 'TimeWarp.Amuru 2.0.0' documentation/user/guides/migrating-to-3.0.md changelog.md
dotnet pack source/timewarp-nuru/timewarp-nuru.csproj -c Release -p:Version=3.0.0 -o /tmp/nuru-502-pack
unzip -p /tmp/nuru-502-pack/TimeWarp.Nuru.3.0.0.nupkg TimeWarp.Nuru.nuspec | grep Amuru
```

**Expect:**

- Both `PackageVersion` lines are `2.0.0` (`TimeWarp.Amuru` and `TimeWarp.Amuru.Tools`).
- The migration guide and changelog say stable 3.0.0 depends on `TimeWarp.Amuru` 2.0.0, and that betas through 3.0.0-beta.80 depend on 2.0.0-beta.2.
- Pack exits 0. The nuspec lists `TimeWarp.Amuru` at `2.0.0`. The pack log does not contain `NU5104`.

### Review (round 1, 2026-10-14)

Effort 1, roster `general` (review oracle, Claude Opus 5.5). **Disposition: not reached. 2 findings are open.**

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |

- **M1 (bug):** The claim "betas through 3.0.0-beta.80 depend on Amuru 2.0.0-beta.2" is false. Only beta.80 does.
  beta.79 depends on `1.1.1`, beta.72–76 on `1.0.0`, and beta.58–71 on `1.0.0-beta.N`, per the nuget.org
  nuspecs. The claim appears in `changelog.md:13`, `migrating-to-3.0.md:48-49`, 487 `release-notes.md:54-55`,
  487 `task.md:66`, and the Results above. It came from this task's Scope step 4.
- **M2 (suggestion):** Nuru 2.0.0 has no Amuru dependency. The migration guide should say that 3.0 adds it.
- Fix on this id, then re-review in `review/round-2/`.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

## Notes

- Found by 489 (2026-10-13) while verifying the Mediator pin; recorded there under "Pack at 3.0.0".
- timewarp-amuru is at `<Version>2.0.0-beta.2</Version>` on origin master with no open release task on its board
  (to-do: 083, 119, 128 as of 2026-10-13).

## Session

- Created: Claude Fable 5.1, session 31dfb8d1 (2026-10-13), filed from the 489 merge
- Decision recorded (ship Amuru first) and task unblocked on Amuru 2.0.0: Claude Fable 5.1 (2026-10-14)
- Pin moved to 2.0.0, docs updated, CI green, pack checked: Grok implement oracle (2026-10-14)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-14T02:49:20Z
- Review oracle round 1: Claude Opus 5.5 (tw-implementation-review, effort 1, general). 1 bug and 1 suggestion are open. Blocked on the fix loop. 2026-10-14
