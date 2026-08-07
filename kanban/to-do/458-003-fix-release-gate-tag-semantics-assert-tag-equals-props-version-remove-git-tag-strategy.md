# Fix release gate tag semantics: assert tag equals props version, remove git-tag strategy

Parent: 458 (findings F3 + F4 in `458-*/review/findings.md`).

## Description

Two related defects in the release gate; resolution updated 2026-08-08 per
operator decision: **one check-version methodology everywhere** (nuget-search
membership). The git-tag strategy is removed, not fixed.

**F3 — missing assertion.** On `release: published`, nothing checks that the tag
equals `v{<Version>}` from `source/Directory.Build.props`. A mistyped tag publishes
packages whose version disagrees with the release page. Evidence this ledger
already drifted: git has tag `v3.0.0-beta.69` with no such version on NuGet, and
NuGet has `3.0.0-beta.70` with no git tag.

**F4 — inverted git-tag strategy (shared DevCli defect) → resolved by removal.**
`GitTagCheckService.CheckGitTagVersionAsync` treats `GITHUB_REF_NAME` as "latest
already-released tag"; on a release event that IS the tag being released, so a
correct release aborts and a mismatched one passes — both branches backwards.
Rather than fix it: **delete the strategy.** Rationale: for a NuGet-publishing
repo the only release ledger that matters is NuGet itself; tags are a proxy that
has already diverged in both directions (beta.69/beta.70 above; ganda tags at
beta.22 vs NuGet beta.15). What git-tag was reaching for is the F3 assertion —
a different check with different semantics — and once 458-006 derives tags from
props, tags are outputs, not a ledger to search.

**Org state (verified 2026-08-08):** timewarp-architecture is the ONLY repo
configured `git-tag`; 5 repos are explicitly `nuget-search`; 14 (including
ganda — no `.timewarp/dev.jsonc` on either branch) use the code default, which
is nuget-search. One methodology is a one-repo config flip plus code deletion.

Target (convention.md rule 6): in release mode, hard-fail unless
`tag == "v" + propsVersion` and the tag commit is reachable from master.
check-version has exactly one methodology: props-version membership in the
published NuGet versions (three-state per 458-005).

## Checklist

- [ ] Release mode: assert `GITHUB_REF_NAME == "v" + propsVersion`, clear failure message
- [ ] Release mode: assert tag commit is an ancestor of master (`git merge-base --is-ancestor`)
- [ ] Remove `git-tag` strategy: delete `GitTagCheckService`, the `--strategy`/`--tag` options, `CheckVersionStrategy` enum, and the `checkVersionStrategy` config key (or hard-error on `git-tag` with pointer to this task)
- [ ] timewarp-architecture: remove `checkVersionStrategy: git-tag` from `.timewarp/dev.jsonc` (falls to nuget-search default) — coordinate with that repo
- [ ] Tests: correct release passes; mismatched tag aborts; tag off master aborts; unknown strategy config → clear error
- [ ] DevCli consumers note: breaking change for git-tag strategy config (architecture is the only known user); update convention.md rule 6 wording if it references strategies
- [ ] Record resurrect condition: tags-as-ledger returns only if a repo ships versioned NON-NuGet releases (no such repo today); would be a deliberate re-add with membership-across-all-tags semantics, never `GITHUB_REF_NAME`
