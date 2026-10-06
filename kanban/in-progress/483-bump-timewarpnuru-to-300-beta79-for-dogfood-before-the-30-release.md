# Bump TimeWarp.Nuru to 3.0.0-beta.79 for dogfood before the 3.0 release

## Description

The pre-3.0 review (482) and its fifteen fix-now children are merged to master. Before cutting
`3.0.0`, publish one more beta so every TimeWarp repo can consume it and report breakage.
beta.77 and beta.78 were never tagged or published; the last published beta is `3.0.0-beta.76`.

This task is the version bump PR only. The tag, GitHub Release, and NuGet publish derive from
`dev release` run on clean synced master after this merges and master CI is green
(`documentation/developer/guides/releasing.md`).

## Checklist

- [x] `source/Directory.Build.props` `<Version>` → `3.0.0-beta.79`
- [x] `changelog.md`: Unreleased → `[3.0.0-beta.79] - 2026-10-06`; beta.78 recorded as not published
- [x] `documentation/user/guides/migrating-to-3.0.md` references beta.79
- [ ] PR merged to master; master push CI green
- [ ] `dev release --dry-run` then `dev release` from master
- [ ] `release:published` run green; `TimeWarp.Nuru` 3.0.0-beta.79 visible on NuGet flatcontainer
- [ ] Dogfood: bump consumers across TimeWarp repos to beta.79 (separate tasks per repo)
- [ ] If dogfood is clean, open the `3.0.0` bump task

## Notes

- Parent review record: `kanban/done/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md`.
- Post-3.0 items from that review (R-2 stream change, R-8 obsolete shims, A-5, A-6, A-7, T-1) are not in this beta by design.

## Session

- Created: 169911 (2026-10-06)
- Bump + PR: claude 2412bd45 (2026-10-06)
