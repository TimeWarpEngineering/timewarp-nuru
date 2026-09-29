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

1. **Always report the distance**: after the existing "Version in source" / "Latest NuGet version"
   lines, state the relationship — e.g.
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
   no-prior-release (no distance line).
5. Readme/doc note for consumers on what the warning means and the `--strict` option.

## Update 2026-09-28 (triage 477)

The `git-tag` strategy was removed in 3.0.0-beta.72 (`source/timewarp-nuru-devcli/readme.md`,
"git-tag strategy is gone": `GitTagCheckService` and `CheckVersionStrategy` no longer exist). There is one
methodology: NuGet version lookup via `NuGetVersionService` (fail-closed since 470-007), classified by
`PublishStateClassifier` / `PublishState`. Compute the distance from the NuGet version list (latest
published version vs source). The requirements above match that single methodology.

## Notes

- Consumers today: timewarp-architecture (`tools/dev-cli/dev.cs` — "Shared endpoints (clean,
  self-install, check-version) come from TimeWarp.Nuru.DevCli"); any other repo on DevCli picks
  the fix up on package update.
- Related consumer-side habit worth documenting there, not here: merging a bump to master is
  not a release; the release event is what publishes.

## Results

`check-version` prints an honest prerelease-increment distance after the source and latest NuGet
lines, and warns when that distance is greater than 1. The distance is defined only when
major.minor.patch and the prerelease label match and only the numeric identifier differs
(`2.0.0-beta.9` → `2.0.0-beta.14` is 5). The warning counts the intermediate bumps (distance
minus the source version being released): `4 version(s) were bumped but never released — was a
release step skipped?` Exit code stays 0. `--strict` exits 1 for that warning only. A core
change, a different label, a prerelease that is not `{label}.{number}`, or no published version
prints both versions and omits the distance line. Distance 0 keeps the existing already-released
failure (exit 1) and adds the distance line.

`PrereleaseDistance` is the pure function. `CheckVersionCommand` calls it after the version
lines. The release workflow calls `check-version` with `Strict` left false, so a deliberate jump
warns and the pipeline continues.

### Files changed

- `source/timewarp-nuru-devcli/content/any/services/prerelease-distance.cs` (new)
- `source/timewarp-nuru-devcli/content/any/endpoints/check-version-command.cs` (`--strict`, distance line, warning)
- `source/timewarp-nuru-devcli/readme.md` (consumer note)
- `tests/timewarp-nuru-tests/devcli/check-version-08-prerelease-distance.cs` (new)
- `tests/timewarp-nuru-tests/devcli/check-version-07-endpoint-fail-closed.cs` (handler wiring: distance 0/1/>1, `--strict`, mismatched shape, no prior release)
- `tests/timewarp-nuru-tests/devcli/Directory.Build.props` and `tests/ci-tests/Directory.Build.props` (compile the new service; exclude check-version-07 from multi-mode)
- `tests/ci-tests/run-ci-tests.cs` (run check-version-07 in the standalone phase)
- `source/timewarp-nuru/internals-visible-to.g.cs` and the parsing/mcp siblings (regenerated; also picks up stems that were already on disk)

### Test outcomes

- `check-version-08-prerelease-distance.cs`: 14 passed
- `check-version-07-endpoint-fail-closed.cs`: 7 passed (3 existing fail-closed + distance 0, 1, >1 with `--strict`, mismatched shape; no-prior-release asserted on the 404 case)
- `dotnet build tools/dev-cli/dev.cs`: succeeded
- `dotnet run tests/ci-tests/run-ci-tests.cs`: exit 0. Multi-mode grand total 1761 (1754 passed, 7 skipped, 0 failed), including `PrereleaseDistance` 14/14. Standalone `check-version-07` 7/7
- Live smoke against NuGet.org for `TimeWarp.Nuru`: source `3.0.0-beta.78`, latest `3.0.0-beta.76`, distance 2, warning `1 version(s) were bumped but never released — was a release step skipped?`, exit 0; the same command with `--strict` exits 1
- `ganda repo audit --fix`: 28 passed, 0 failed (`bin/dev` is gitignored local output)

### How to validate

**Smoke**

```bash
cd /home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/task-456-check-version-warn-when-source-is-multiple-version
dotnet run tests/timewarp-nuru-tests/devcli/check-version-08-prerelease-distance.cs
dotnet run tests/timewarp-nuru-tests/devcli/check-version-07-endpoint-fail-closed.cs
dotnet run tools/dev-cli/dev.cs -- check-version --package TimeWarp.Nuru; echo "exit=$?"
dotnet run tools/dev-cli/dev.cs -- check-version --package TimeWarp.Nuru --strict; echo "exit=$?"
```

**Expect**

- 08: 14 passed. `2.0.0-beta.14` vs `2.0.0-beta.9` is distance 5 and the warning `4 version(s) were bumped but never released — was a release step skipped?`. Distance 1 has no warning and is not a strict failure. Distance 0 has no warning. A major/minor/patch change, a different label, `1.2.0` vs `1.2.0`, and a null latest return no distance.
- 07: 7 passed. With the repo's source version published by the stub, the handler prints `was already released` and `Bump the version before releasing.`, plus `Source is 0 prerelease increments ahead`, and exits 1. One increment behind exits 0 with the distance line and no `never released` warning, including with `--strict`. Five increments behind prints the warning and `safe to release` and exits 0; `--strict` exits 1. Latest `1.0.0` prints both versions and no distance line, exit 0. A 404 prints `Latest NuGet version: (none)` and `safe to release`, and does not print `prerelease increment`, exit 0.
- Live `check-version --package TimeWarp.Nuru` (observed 2026-09-29): `Version in source: 3.0.0-beta.78`, `Latest NuGet version: 3.0.0-beta.76`, `Source is 2 prerelease increments ahead of v3.0.0-beta.76`, the one-version warning, `safe to release`, exit 0. `--strict` prints the same lines and exits 1. A later publish of beta.77 or beta.78 changes the numbers; the lines above are the shape.
