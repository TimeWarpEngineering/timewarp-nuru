# Ship 3.0.0-beta.75 with route examples in help and capabilities (464)

## Description

Release TimeWarp.Nuru 3.0.0-beta.75 carrying task 464: `[NuruRouteExample]` / `.WithExample()`
rendered in per-route `--help` and emitted in `--capabilities`, plus the fluent named-argument
fix and the diagnostics-swallowing generator fix (aborted Build() chains now fail the build
instead of compiling silently). Merged to master via PR #223 (merge 17ab7778).

Downstream consumer: timewarp-ganda task 211 (annotate `skills add` with examples) unblocks on
this release.

## Checklist

- [ ] Bump `<Version>` to 3.0.0-beta.75 in source/Directory.Build.props (PR to master)
- [ ] Merge bump PR; master push CI green (Packages artifact built)
- [ ] `dev release --dry-run` from clean synced master worktree — 8 guards pass
- [ ] `dev release` — tag v3.0.0-beta.75 + GitHub Release
- [ ] Watch `release:published` run through verify → push (OIDC)
- [ ] Confirm packages on NuGet (flatcontainer)

## Notes

- Release procedure per tw-release skill / documentation/developer/guides/releasing.md —
  version typed exactly once (the bump PR); pipeline promotes the CI-built artifact.

## Session

- Created: 0f730c83-90e5-4a4c-8bb2-3020fdd469d6 (2026-08-14)
