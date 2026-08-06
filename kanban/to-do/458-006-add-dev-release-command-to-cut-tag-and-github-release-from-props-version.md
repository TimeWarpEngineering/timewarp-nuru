# Add dev release command to cut tag and GitHub Release from props version

Parent: 458 (finding F7 in `458-*/review/findings.md`).

## Description

Today a release requires two independent human acts that can disagree: merge the
props version bump, then hand-type a tag/Release on GitHub. That dual entry is the
root cause of tag↔package divergence (beta.69 tagged but never on NuGet; beta.70 on
NuGet but never tagged) and the timewarp-architecture burned-version incident
(task 456).

Target (convention.md rule 5): the tag is **derived, not typed**. A `dev release`
DevCli endpoint reads `<Version>` from `source/Directory.Build.props`, runs the
check-version gate, and creates tag `v{Version}` plus the GitHub Release on the
master head commit (via `gh release create`). Publishing then proceeds through the
existing `release: published` pipeline. Humans type a version exactly once — in the
props bump PR. The 458-003 tag==props assertion stays as defense-in-depth.

## Checklist

- [ ] New DevCli content endpoint `release-command.cs` (ships to all consumers)
- [ ] Reads props version; refuses if working tree dirty, not on master, or master not in sync with origin
- [ ] Runs check-version gate first; refuses on already-fully-published
- [ ] Creates annotated tag `v{Version}` and GitHub Release (`gh release create v{Version} --title v{Version} --generate-notes`)
- [ ] `--dry-run` flag printing what would be created
- [ ] Tests for the guard conditions (dirty tree, wrong branch, existing tag)
- [ ] Releasing guide (458-008): `dev release` becomes the documented way to cut a release
