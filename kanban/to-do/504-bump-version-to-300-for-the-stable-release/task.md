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

- [ ] `source/Directory.Build.props`: `<Version>3.0.0-beta.80</Version>` → `<Version>3.0.0</Version>`. Grep the repo for any other `3.0.0-beta.80` literal that must move (props, package metadata, `#:package` lines in samples or docs that pin a beta) and update those too. Kanban kitchens and the changelog's historical sections stay as they are.
- [ ] `changelog.md`: rename `## [Unreleased]` to `## [3.0.0] - 2026-10-14` and add a fresh empty `## [Unreleased]` above it. Add a short lead paragraph under `[3.0.0]` that says this is the first stable 3.0 release, that it supersedes the `3.0.0-beta.*` line (last published beta.80, 2026-10-12), and that it is the first 3.0 package built in Release (the betas were Debug, see the Packaging entry). Keep the existing Changed/Fixed entries; do not rewrite them. If the file has a link-reference list for versions at the bottom, add `3.0.0` and keep `Unreleased` pointing at `v3.0.0...HEAD`.
- [ ] `documentation/user/guides/migrating-to-3.0.md`: the intro (lines ~6-10) says to install the prerelease until `3.0.0` is published. Change it to the stable install (`dotnet add package TimeWarp.Nuru`, no `--prerelease`). Leave the Mediator and Amuru sections as they are; they already describe the stable pins.
- [ ] `readme.md` and `documentation/user/**`: grep for `--prerelease`, `prerelease`, and `beta` and fix any instruction that tells a 3.0 user to opt into prereleases. The MCP note in `readme.md` (older prereleases of the frozen MCP package) is historical and stays.
- [ ] `dotnet pack source/timewarp-nuru/timewarp-nuru.csproj -c Release -o /tmp/504-pack` (or the repo's pack entry point) must succeed with **zero NU5104** warnings and the nuspec version must read `3.0.0`. Paste the nuspec `<version>` and `<dependency>` lines for TimeWarp.Mediator.* and TimeWarp.Amuru.* into Results.
- [ ] `dotnet build` of the solution is clean (warnings are errors). Do not run the full CI suite locally; CI on the PR does that.
- [ ] Do **not** tag, run `dev release`, publish, or edit kanban 487. The release is a human action after merge.

## Notes

- Releasing guide: `documentation/developer/guides/releasing.md`. The promotion pipeline ships the
  CI-built artifact byte-identical; never rebuild for release.
- Launch runbook with release notes and assets: `kanban/to-do/487-*/` (`release-notes.md` is pasted into
  the GitHub Release on release day; it is not part of this task).
- `3.0.0-beta.80` is the last published beta: tag at `ab5ccfb`, 2026-10-12.

## Results

## Session

- 2026-10-14: created and dispatched by claude 31dfb8d1 (cockpit) on the maintainer's "ok lets do it" after 487 merged (PR #318).
