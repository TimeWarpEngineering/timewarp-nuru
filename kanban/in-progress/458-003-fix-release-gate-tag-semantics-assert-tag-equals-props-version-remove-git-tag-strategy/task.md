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

- [x] Release mode: assert `GITHUB_REF_NAME == "v" + propsVersion`, clear failure message (release events only; explicit skip log for break-glass/local — D1)
- [x] Release mode: assert HEAD is an ancestor of master (all release modes; 4-state outcome: Ancestor/NotAncestor/MasterUnresolvable/GitError, each with distinct message; fail-closed default)
- [x] Remove `git-tag` strategy: `GitTagCheckService`, `--strategy`/`--tag`, `CheckVersionStrategy` enum, config key deleted; lingering `checkVersionStrategy` in dev.jsonc silently ignored by design (regression-tested)
- [ ] timewarp-architecture: remove `checkVersionStrategy: git-tag` from `.timewarp/dev.jsonc` — **cross-repo, deferred to org rollout** (its config keeps parsing and lands on nuget-search meanwhile)
- [x] Tests: 11 tag-assertion matrix tests + 3 legacy-config tests; gate simulations for mismatch-abort and break-glass-skip paths
- [x] DevCli consumers note: readme Migration Notes cover strategy removal, lingering-config behavior, new public TagAssertion types; convention.md shared-surface bullet reworded (rule 6 itself references no strategies)
- [x] Resurrect condition recorded in readme migration notes (tags-as-ledger only for versioned non-NuGet releases; membership-across-all-tags; never GITHUB_REF_NAME)

## Results

Implemented in commits `f002e65a` (implementation), `d0355613` (round-1 fixes),
`e4a7c9c3` (round-2 fail-closed default).

- **Release gate (Step 1/6 of release pipeline,** `tools/dev-cli/endpoints/workflow-command.cs`**):**
  on `release` events, `TagAssertion.Validate` (new pure DevCli content service,
  `services/tag-assertion.cs`) hard-fails unless `GITHUB_REF_NAME == v{<Version>}`
  (Ordinal); non-release release-mode runs log an explicit skip. HEAD
  ancestor-of-master asserted in ALL release modes via
  `git merge-base --is-ancestor` (origin/master with logged local-master
  fallback); four distinct outcomes with precise failure messages; every
  non-Ancestor path aborts with exit 1 before check-version.
- **git-tag strategy removed:** `git-tag-check-service.cs` and
  `check-version-strategy.cs` deleted; `--strategy`/`--tag` options gone;
  check-version is single-methodology (NuGet membership). Legacy
  `checkVersionStrategy` config keys deserialize silently to defaults
  (nuget-search) — proven by `RepoConfigService.Parse` regression tests.
  This repo's `.timewarp/dev.jsonc` dropped the key.
- **Tests:** `workflow-02-release-tag-assertion.cs` (11) +
  `check-version-02-legacy-strategy-config.cs` (3). Test props swap includes;
  TimeWarp.Amuru.Tools reference added (Git.FindRoot lives there — verified by
  decompile).
- **Review (Phase 4b):** 2 rounds, 1 sonnet reviewer, effort 1. 4 findings
  total (1 MED, 1 LOW, 2 INFO) — all resolved, no wontfix. Disposition:
  **clean** (`review/disposition.md`). Reviewer disproved the `+`-in-tag
  footgun (git accepts build-metadata tags).

### How to validate

Smoke:
1. `dotnet run tests/timewarp-nuru-tests/devcli/workflow-02-release-tag-assertion.cs`
   → Expect 11/11. `dotnet run tests/timewarp-nuru-tests/devcli/check-version-02-legacy-strategy-config.cs`
   → Expect 3/3.
2. `GITHUB_EVENT_NAME=release GITHUB_REF_NAME=v0.0.0 dotnet run --file tools/dev-cli/dev.cs -- workflow --mode release`
   → Expect abort at Step 1/6: tag `v0.0.0` does not match expected, exit 1.
3. `GITHUB_EVENT_NAME=workflow_dispatch dotnet run --file tools/dev-cli/dev.cs -- workflow --mode release`
   → Expect "Tag assertion skipped…", then (from a branch not on master) the
   distinct NotAncestor abort.
4. `dotnet run --file tools/dev-cli/dev.cs -- check-version` → Expect no
   "Strategy:" line; `--strategy git-tag` → unknown-option error.

Automated gate: `ganda runfile cache --clear && dotnet run tests/ci-tests/run-ci-tests.cs`
→ Expect 0 failed (last: 1431 total / 1424 passed / 7 skipped / 0 failed).

Depends on / not in scope: timewarp-architecture config flip (org rollout);
next real release-event run exercises the tag-pass path end-to-end.

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
