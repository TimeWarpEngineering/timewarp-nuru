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

## Notes

### Implementation plan (Phase 2, 2026-08-08) — key decisions

- **D1:** tag-equality assertion runs only when `GITHUB_EVENT_NAME == "release"`
  (on dispatch, `GITHUB_REF_NAME` is a branch; locally it's unset) with an
  explicit "skipped" log line; ancestor-of-master assertion runs in ALL release
  modes (release event, break-glass, local) — the invariant is "published code
  is on master." Master ref: `origin/master` with local `master` fallback.
- **D2:** removed `checkVersionStrategy` key is silently ignored on
  deserialize (STJ default) — architecture's legacy config keeps parsing and
  lands on nuget-search, the intended outcome; regression test proves it.
- **D4:** pure `TagAssertion.Validate(refName, propsVersion)` in new DevCli
  content `services/tag-assertion.cs` (unit-tested matrix); git ancestor check
  stays a thin `Shell.Builder("git")` wrapper in workflow-command.cs.
- Deletions in one commit unit: `git-tag-check-service.cs`,
  `check-version-strategy.cs`, strategy property + JSON context registration,
  `--strategy`/`--tag` options, `HandleGitTagAsync`, DI registration; both
  test props updated (remove strategy include, add tag-assertion +
  repo-config-service + irepo-config-service).
- New tests: `workflow-02-release-tag-assertion.cs` (tag matrix incl.
  branch-name and case-sensitivity), `check-version-02-legacy-strategy-config.cs`
  (legacy jsonc parses; packages preserved; empty → defaults).
- Release pipeline banners renumber to 6 steps with gate first; shared
  `AbortPipeline` helper; `ReadPropsVersion` extracted and reused by push.
- Docs: DevCli readme services/config/migration-notes updates (+ two stale
  cells found by plan agent); convention.md line-91 shared-surface reword;
  this repo's `.timewarp/dev.jsonc` drops the strategy key.
- Not unit-testable (verified via gate simulations + next real release):
  env reading, git ancestor wrapper, abort wiring.
