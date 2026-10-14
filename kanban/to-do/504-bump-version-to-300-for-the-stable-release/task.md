# Bump version to 3.0.0 for the stable release

## Description

Set `<Version>` to `3.0.0` in `source/Directory.Build.props` and make the repo's own wording match a
stable release. This is the one place a human types the version (see
`documentation/developer/guides/releasing.md` §1). After this PR merges, the master push run builds and
uploads the `Packages-*` artifact, and the maintainer cuts the release with `dev release` from master.
The tag, GitHub Release, and NuGet publish all derive from this bump; **this task does not tag, release,
or publish anything.**

Every gate for 3.0.0 is closed on master (see kanban 487 and the Unreleased changelog): Mediator 14.0.0
and Amuru 2.0.0 stable pins (489, 502), package props enable interceptors (488), bash completion and
REPL stdin fixes (492, 493), and Release-only packing with CI config/integrity gates (503, PR #317).
`3.0.0-beta.80` (2026-10-12) is the last beta and is API-identical to what 3.0.0 ships.

## Checklist

- [x] `source/Directory.Build.props`: `<Version>3.0.0-beta.80</Version>` → `<Version>3.0.0</Version>`. Grep the repo for any other `3.0.0-beta.80` literal that must move (props, package metadata, `#:package` lines in samples or docs that pin a beta) and update those too. Kanban kitchens and the changelog's historical sections stay as they are.
- [x] `changelog.md`: rename `## [Unreleased]` to `## [3.0.0] - 2026-10-14` and add a fresh empty `## [Unreleased]` above it. Add a short lead paragraph under `[3.0.0]` that says this is the first stable 3.0 release, that it supersedes the `3.0.0-beta.*` line (last published beta.80, 2026-10-12), and that it is the first 3.0 package built in Release (the betas were Debug, see the Packaging entry). Keep the existing Changed/Fixed entries; do not rewrite them. If the file has a link-reference list for versions at the bottom, add `3.0.0` and keep `Unreleased` pointing at `v3.0.0...HEAD`.
- [x] `documentation/user/guides/migrating-to-3.0.md`: the intro (lines ~6-10) says to install the prerelease until `3.0.0` is published. Change it to the stable install (`dotnet add package TimeWarp.Nuru`, no `--prerelease`). Leave the Mediator and Amuru sections as they are; they already describe the stable pins.
- [x] `readme.md` and `documentation/user/**`: grep for `--prerelease`, `prerelease`, and `beta` and fix any instruction that tells a 3.0 user to opt into prereleases. The MCP note in `readme.md` (older prereleases of the frozen MCP package) is historical and stays.
- [x] `dotnet pack source/timewarp-nuru/timewarp-nuru.csproj -c Release -o /tmp/504-pack` (or the repo's pack entry point) must succeed with **zero NU5104** warnings and the nuspec version must read `3.0.0`. Paste the nuspec `<version>` and `<dependency>` lines for TimeWarp.Mediator.* and TimeWarp.Amuru.* into Results.
- [x] `dotnet build` of the solution is clean (warnings are errors). Do not run the full CI suite locally; CI on the PR does that.
- [x] Do **not** tag, run `dev release`, publish, or edit kanban 487. The release is a human action after merge.
- [x] Review M1: the migration guide says `changelog.md` has a section for each published 3.0 beta through beta.79, and that `## [3.0.0]` lists only the changes since beta.79 (including beta.80).
- [x] Review M2: the `[3.0.0]` lead opens with the range since beta.79 and names the first Packaging entry (503).

## Review

- Round 1: Blocked — open M1, M2
- Fix pass (2026-10-14): M1 and M2 marked fixed on `review/round-1/merged.md`. Host runs review round 2.
- Round 2: M1 and M2 re-verified fixed; no new findings. Disposition **clean** (`review/disposition.md`).

## Notes

- Releasing guide: `documentation/developer/guides/releasing.md`. The promotion pipeline ships the
  CI-built artifact byte-identical; never rebuild for release.
- Launch runbook with release notes and assets: `kanban/to-do/487-*/` (`release-notes.md` is pasted into
  the GitHub Release on release day; it is not part of this task).
- `3.0.0-beta.80` is the last published beta: tag at `ab5ccfb`, 2026-10-12.

## Results

`<Version>` in `source/Directory.Build.props` is `3.0.0`. No other product pin of `3.0.0-beta.80` had to move: samples and docs do not `#:package` that beta. Left in place: the Release-only packing comment in that props file, the historical beta.80 sentence now under `[3.0.0]` in `changelog.md`, the Mediator and Amuru history in the migration guide, and every kanban kitchen (487 was not edited).

`changelog.md` has an empty `## [Unreleased]` above `## [3.0.0] - 2026-10-14`. The lead paragraph under `[3.0.0]` states that this is the first stable 3.0 release, that it supersedes `3.0.0-beta.*` (last published beta.80, 2026-10-12), and that it is the first 3.0 package built in Release. Changed and Fixed entries are unchanged. The file has no version link-reference list, so none was added.

`documentation/user/guides/migrating-to-3.0.md` installs with `dotnet add package TimeWarp.Nuru`. The "What did not move" note now points at `## [3.0.0]` instead of the empty Unreleased section. Mediator and Amuru sections are unchanged. `readme.md` already installs without `--prerelease`. Remaining `--prerelease` / prerelease lines under `documentation/user/**` are the frozen MCP package (`readme.md`, `documentation/user/tools/mcp-server.md`, `documentation/user/tools/overview.md`) and the `--check-version` behavior note in `documentation/user/features/built-in-routes.md`. `skills/tw-nuru/SKILL.md` dropped `--prerelease` from its install block, because `readme.md` sends agents to that skill. Dated posts were left as history.

`dotnet pack source/timewarp-nuru/timewarp-nuru.csproj -c Release -o /tmp/504-pack` succeeded. The console log has no `NU5104` and no other warnings. Every nuspec dependency version is a stable number. The lines the checklist asks for:

```xml
<version>3.0.0</version>
<dependency id="TimeWarp.Amuru" version="2.0.0" exclude="Build,Analyzers" />
<dependency id="TimeWarp.Mediator.Contracts" version="14.0.0" exclude="Build,Analyzers" />
<dependency id="TimeWarp.Mediator.Generators" version="14.0.0" include="All" />
```

`dotnet build timewarp-nuru.slnx` succeeded: 0 Warning(s), 0 Error(s). Nothing was tagged, released, or published.

Review round 1 M1 and M2 are fixed on this id. `documentation/user/guides/migrating-to-3.0.md` no longer says per-beta notes for beta.20–beta.77 were dropped, and it no longer puts those changes under `## [3.0.0]`. It says `changelog.md` has a section for each published 3.0 beta through `3.0.0-beta.79`, that `## [3.0.0]` lists the changes since beta.79 (including those that shipped in beta.80), and that later work is under `## [Unreleased]`. The `[3.0.0]` lead in `changelog.md` opens with "Changes since `3.0.0-beta.79`; beta.80 has no separate section." and points at the first Packaging entry under Fixed (task 503). Changed and Fixed entries were not rewritten. Every local tag `v3.0.0-beta.*` through beta.79 appears in a changelog heading. Disposition waits for review round 2.

### How to validate

Smoke: from the task worktree, confirm the props version, the packed nuspec, and the two wording fixes.

```bash
grep -n '<Version>' source/Directory.Build.props
unzip -p /tmp/504-pack/TimeWarp.Nuru.3.0.0.nupkg TimeWarp.Nuru.nuspec \
  | grep -E '<version>|TimeWarp\.(Mediator|Amuru)'
grep -n -F -e 'were not kept' -e '3.0.0-beta.79' -e '## [Unreleased]' \
  documentation/user/guides/migrating-to-3.0.md
sed -n '10,13p' changelog.md
```

Expect: `<Version>3.0.0</Version>`, `<version>3.0.0</version>`, `TimeWarp.Amuru` version `2.0.0`, `TimeWarp.Mediator.Contracts` version `14.0.0`, and `TimeWarp.Mediator.Generators` version `14.0.0`. The pack log contains no `NU5104`. The migration guide has no "were not kept" line. It names `3.0.0-beta.79` and `## [Unreleased]`, and the "What did not move" paragraph says `## [3.0.0]` lists the changes since beta.79, including those that shipped in beta.80. The `[3.0.0]` lead starts with `Changes since `3.0.0-beta.79`; beta.80 has no separate section.` and names the first Packaging entry under Fixed, task 503.

### Review

Two rounds ran at effort 1 with one reviewer (roster: general). Round 1 found 1 bug (M1) and 1 nit (M2). The fix pass (b007143a) fixed both. Round 2 reviewed the fix delta `89a3351a..b007143a`, confirmed both fixes against the changelog headings and local tags, and found nothing new.

Final counts: bug 0 open / 1 fixed / 0 wontfix; suggestion 0 / 0 / 0; nit 0 open / 1 fixed / 0 wontfix.

**Disposition: clean.** No wontfix and no escalations.

- **M1 (bug, fixed):** the migration guide's changelog pointer now matches the per-beta sections through beta.79.
- **M2 (nit, fixed):** the `[3.0.0]` lead names its range since beta.79 and the first Packaging entry (503).

Files: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/round-2/general.md`, `review/round-2/merged.md` (final ledger), `review/disposition.md`.

## Session

- 2026-10-14: created and dispatched by claude 31dfb8d1 (cockpit) on the maintainer's "ok lets do it" after 487 merged (PR #318).
- 2026-10-14: implement oracle (grok 01a13b33). Version bump, changelog heading, stable install wording, Release pack, and solution build.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-14T16:23:19Z
- 2026-10-14: review oracle round 1 (claude opus-5.5, effort 1, general). 1 bug (M1) and 1 nit (M2) open; Blocked for a fix pass.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 80 — 2026-10-14T16:24:53Z
- 2026-10-14: implement-fix-2 oracle (grok 01a13b3b). Corrected the migration-guide changelog pointer (M1) and the `[3.0.0]` lead (M2).
- 2026-10-14: review oracle round 2 (claude opus-5.5, effort 1, general; session id not exposed to the headless run). M1 and M2 re-verified fixed, no new findings; disposition clean.
