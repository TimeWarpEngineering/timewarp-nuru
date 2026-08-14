# Ship 3.0.0-beta.75 with route examples in help and capabilities (464)

## Description

Release TimeWarp.Nuru 3.0.0-beta.75 carrying task 464: `[NuruRouteExample]` / `.WithExample()`
rendered in per-route `--help` and emitted in `--capabilities`, plus the fluent named-argument
fix and the diagnostics-swallowing generator fix (aborted Build() chains now fail the build
instead of compiling silently). Merged to master via PR #223 (merge 17ab7778).

Downstream consumer: timewarp-ganda task 211 (annotate `skills add` with examples) unblocks on
this release.

## Checklist

- [x] Bump `<Version>` to 3.0.0-beta.75 in source/Directory.Build.props (PR #224)
- [x] Merge bump PR; master push CI green (Packages artifact built)
- [x] Fix `nuget-package-urls` audit blocker: add `<PackageProjectUrl>` (PR #225)
- [x] `dev release --dry-run` from clean synced master worktree — 8 guards pass
- [x] `dev release` — tag v3.0.0-beta.75 + GitHub Release
- [x] Watch `release:published` run through verify → push (OIDC) — success
- [x] Confirm packages on NuGet (flatcontainer)

## Notes

- Release procedure per tw-release skill / documentation/developer/guides/releasing.md —
  version typed exactly once (the bump PR); pipeline promotes the CI-built artifact.
- Unplanned blocker: master's audit check-set (stricter than dev's) failed
  `nuget-package-urls` — `source/Directory.Build.props` lacked `<PackageProjectUrl>` — so
  ganda refused to attest master HEAD, which gates the release pipeline. Fixed via
  `ganda repo audit --fix` (placement tidied by hand) in PR #225; attestation then signed
  under the stricter check-set.

## Results

### What was done

- Version bumped to 3.0.0-beta.75 (PR #224, merge d36f6abc); `PackageProjectUrl` metadata fix
  (PR #225, merge 4ff8ca0c).
- Release cut from clean synced master at 4ff8ca0c: tag v3.0.0-beta.75, GitHub Release
  https://github.com/TimeWarpEngineering/timewarp-nuru/releases/tag/v3.0.0-beta.75
- `release:published` pipeline run 31760320231 completed successfully — promoted the CI-built
  Packages artifact from run 31759844187 and pushed via OIDC trusted publishing.
- TimeWarp.Nuru and TimeWarp.Nuru.DevCli 3.0.0-beta.75 confirmed indexed on NuGet
  flatcontainer.

### How to validate

Smoke:
```bash
curl -s https://api.nuget.org/v3-flatcontainer/timewarp.nuru/index.json | grep beta.75
gh release view v3.0.0-beta.75 --repo TimeWarpEngineering/timewarp-nuru
```
Expect: `3.0.0-beta.75` present in the versions list; release exists with generated notes.

Consumer check: in any project, `dotnet add package TimeWarp.Nuru --version 3.0.0-beta.75`,
annotate a route with `[NuruRouteExample("demo --verbose", Description = "Run verbosely")]`,
then `--help` on that route shows an `Examples:` section and `--capabilities` includes an
`examples` array.

Automated gate: release pipeline run 31760320231 (green); master CI run 31759844187 (green).

Depends on / Not in scope: consuming this in ganda `skills add` is timewarp-ganda task 211.

## Session

- Created: 0f730c83-90e5-4a4c-8bb2-3020fdd469d6 (2026-08-14)
- Release + results: 0f730c83-90e5-4a4c-8bb2-3020fdd469d6 (2026-08-14)
