# check-version: warn when source is multiple versions ahead of last release

## Description

`dev check-version` (shipped from this repo as DevCli content:
`source/timewarp-nuru-devcli/content/any/endpoints/check-version-command.cs`) answers exactly
one question — "is the source version already released?" — and answers it correctly. But it
treats "1 ahead" and "5 ahead" identically, so a repo that keeps bumping while never actually
cutting releases gets a green ✓ every single time.

**Real incident (timewarp-architecture, 2026-07-29 → 2026-08-03):** five version bumps
(beta.10 → beta.14) were merged to master across five PRs. That repo's workflow only publishes
on the `release: published` event, and no GitHub Release was ever created, so nothing shipped —
the last published version stayed beta.9 the whole time. `check-version` printed
"✓ Version in source is new — safe to release" on every bump, truthfully, while four version
numbers were silently burned and the packages went stale for five days. The gap was found by a
human noticing the releases page, not by tooling.

The distance between source and latest release is information the command already has and
currently discards.

## Requirements

1. **Always report the distance**, both strategies (git-tag and nuget-search): after the
   existing "Version in source" / "Latest release tag" lines, state the relationship — e.g.
   `Source is 5 prerelease increments ahead of v2.0.0-beta.9`.
2. **Warn when distance > 1**: a distinct, visible warning naming the likely cause, e.g.
   "4 version(s) were bumped but never released — was a release step skipped?" Keep **exit code
   0** (this is advisory; deliberate jumps are legitimate). Consider an opt-in `--strict` that
   exits non-zero for CI use.
3. **Only compute distance where it is honest**: when major/minor/patch and the prerelease
   label match and only the prerelease number differs (beta.9 → beta.14 = 5). For any other
   shape (major/minor/patch change, different label, no prior release), print both versions and
   skip the distance line rather than inventing a metric.
4. Tests: distance 0 (already released → existing failure path unchanged), 1 (normal, no
   warning), >1 (warning, exit 0; `--strict` → non-zero), mismatched-shape (no distance line),
   no-prior-release (no distance line), both strategies.
5. Readme/doc note for consumers on what the warning means and the `--strict` option.

## Notes

- Consumers today: timewarp-architecture (`tools/dev-cli/dev.cs` — "Shared endpoints (clean,
  self-install, check-version) come from TimeWarp.Nuru.DevCli"); any other repo on DevCli picks
  the fix up on package update.
- Related consumer-side habit worth documenting there, not here: merging a bump to master is
  not a release; the release event is what publishes.
