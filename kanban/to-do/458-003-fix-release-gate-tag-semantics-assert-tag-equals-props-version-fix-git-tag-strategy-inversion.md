# Fix release gate tag semantics: assert tag equals props version, fix git-tag strategy inversion

Parent: 458 (findings F3 + F4 in `458-*/review/findings.md`).

## Description

Two related defects in the release gate:

**F3 — missing assertion.** On `release: published`, nothing checks that the tag
equals `v{<Version>}` from `source/Directory.Build.props`. A mistyped tag publishes
packages whose version disagrees with the release page. Evidence this ledger
already drifted: git has tag `v3.0.0-beta.69` with no such version on NuGet, and
NuGet has `3.0.0-beta.70` with no git tag.

**F4 — inverted git-tag strategy (shared DevCli defect).**
`GitTagCheckService.CheckGitTagVersionAsync` treats `GITHUB_REF_NAME` as "latest
already-released tag." On a release event, `GITHUB_REF_NAME` **is the tag being
released**, so for any repo configured `checkVersionStrategy: git-tag`: a correct
release (tag == props) aborts as "already released," and a mismatched release
passes and publishes. Both branches backwards. It also compares only the single
latest tag rather than membership in all tags (same class as pre-454-022
nuget-search bug). Nuru is unaffected only because it uses nuget-search; every
DevCli consumer inherits this.

Target (convention.md rule 6): in release mode, hard-fail unless
`tag == "v" + propsVersion` and the tag commit is reachable from master. Outside
release context, git-tag strategy checks membership of the props version in **all**
tags, never just the newest, and never reads `GITHUB_REF_NAME` as prior history.

## Checklist

- [ ] Release mode: assert `GITHUB_REF_NAME == "v" + propsVersion`, clear failure message
- [ ] Release mode: assert tag commit is an ancestor of master (`git merge-base --is-ancestor`)
- [ ] `GitTagCheckService`: stop treating `GITHUB_REF_NAME` as latest release; check membership across all tags
- [ ] Tests: correct release passes; mismatched tag aborts; tag off master aborts; git-tag strategy with props version in older tag → flagged
- [ ] DevCli consumers note: behavior change for git-tag strategy repos (see repo-matrix.md F4 warning)
