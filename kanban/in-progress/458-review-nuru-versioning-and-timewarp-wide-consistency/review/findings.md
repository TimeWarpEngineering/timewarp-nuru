# Findings — Nuru versioning and release model review

Reviewed: `source/Directory.Build.props`, `.github/workflows/workflow.yml`,
`tools/dev-cli/endpoints/workflow-command.cs`, DevCli content
(`check-version-command.cs`, `git-tag-check-service.cs`, `repo-config-service.cs`),
`.timewarp/dev.jsonc`, git tag history, NuGet published versions.

Posture per task: correctness first, pristine target model; migration cost explicitly
out of scope (operator instruction, 2026-08-06).

## Verdicts at a glance

| Area | Verdict |
|------|---------|
| Version SSOT in `source/Directory.Build.props` | **Keep** — correct choice |
| One shared version for all packages | **Keep** — correct for interdependent set |
| Publish only on GitHub Release published | **Keep** — correct trigger |
| `workflow_dispatch` → release mode | **Broken and incoherent — fix** |
| No tests in release pipeline | **Correctness gap — fix** |
| No tag ↔ props assertion | **Correctness gap — fix** (divergence already occurred) |
| git-tag strategy semantics in release context | **Defective — fix in DevCli** |
| Triplicated hand-maintained package lists | **Fragile — derive instead** |
| Partial-publish recovery | **Gate contradicts recovery — fix** |
| Perpetual `-beta.N` | **Undecided product policy — decide explicitly** |
| Canonical release docs | **Missing — write** |

## Hard evidence of divergence (not theoretical)

The tag history and NuGet already disagree **in both directions** in this repo:

- Git has tag `v3.0.0-beta.69`; NuGet has **no** `3.0.0-beta.69` for TimeWarp.Nuru.
  A release was cut on GitHub whose packages never arrived (pipeline failed or was
  never re-run after failure).
- NuGet has `3.0.0-beta.70`; git has **no** tag `v3.0.0-beta.70`. A version reached
  NuGet out-of-band, with no corresponding GitHub Release.

Plus the timewarp-architecture incident (task 456): five bumps merged, zero releases
cut, four version numbers burned, found by a human days later.

Conclusion: the model's individual pieces are fine, but nothing mechanically ties
**props version ↔ git tag ↔ published packages** together. Every failure above is a
missing assertion, not a missing philosophy.

## What works — keep, and say so in the convention

1. **SSOT = `<Version>` in `source/Directory.Build.props`.** Explicit, in git,
   reviewable in PRs, deterministic for local/AOT builds, no tag-archaeology
   (MinVer-style height computation was considered and rejected: it makes local
   builds non-deterministic relative to tags and moves the SSOT out of the reviewed
   file set). The version is typed by a human exactly once, in a PR.
2. **One shared version across the package set.** The five packages
   (Analyzers, Mcp, Nuru, Search, DevCli) are an interdependent release unit;
   lockstep versioning is correct-by-construction (no compatibility matrix).
   Burned numbers on unchanged packages are harmless. Keep — as an explicit policy,
   not an accident.
3. **Publishing only from a published GitHub Release.** Intentional, auditable,
   permission-gated. Keep as the only routine publish path.
4. **`check-version` as pre-push gate** (post 454-022 membership fix) plus the
   456 distance warning. Right idea; gaps below.
5. **`--skip-duplicate` on push.** Right instinct for idempotent re-runs — but see
   the partial-publish contradiction below.

## Defects and gaps (ranked)

### F1 — `workflow_dispatch` is treated as a release by DevCli but not by the YAML (incoherent, currently fails)

`workflow-command.cs` `DetermineMode` maps `workflow_dispatch` → `Release`. But
`workflow.yml` only runs `nuget/login` (OIDC) and passes `--api-key` when
`github.event_name == 'release'`. So a manual dispatch enters the **full release
pipeline** — check-version, clean, build, pack — then attempts an unauthenticated
NuGet push and dies. Two components disagree about what dispatch means. If
credentials ever leak into that path (env var, future refactor), dispatch becomes a
silent publish button. `3.0.0-beta.70` on NuGet with no tag shows out-of-band
publishes do happen.

**Fix:** dispatch defaults to build/test (Merge mode). Release mode requires the
`release` event, or an explicit dispatch input (`mode: release` + typed
confirmation) as break-glass — and the YAML must supply credentials if and only if
that same condition holds.

### F2 — No test gate is enforced anywhere between PR and publish

Release mode is `check-version → clean → build → pack → push`. No `verify-samples`,
no `test`. The rebuttal "master was already tested on merge" describes practice, not
mechanism — verified against the live repo settings (2026-08-07):

- **Master branch protection has no `required_status_checks`** (GitHub API,
  `branches/master/protection`): PR CI runs tests but a PR can merge red or with CI
  still running. Merge-time testing is advisory.
- **Push CI on master is post-hoc**: a failed merge build turns master red, but the
  release event never reads the commit's CI status — nothing stops tagging and
  publishing a red commit.
- **Path-filter gap**: `dev build` builds `timewarp-nuru.slnx`, but the workflow
  path filters omit `*.slnx` and `assets/**` — commits touching those trigger no CI
  at all.
- **The published binaries are never the tested binaries**: the release run
  rebuilds from scratch with a floating SDK (`setup-dotnet: '10.0.x'` resolves at
  run time). For a source-generator product, SDK/Roslyn drift between merge time
  and release time changes emitted code.

Historical discipline has held — all recent release tags are ancestors of master
(verified) — so this has not produced a bad release. But "guaranteed by discipline"
is exactly what a pristine model replaces with a mechanism.

**Fix — two coherent designs; pick one (the current rebuild-without-retest is the
only incoherent option):**

- **Design B, build-once / promote (recommended as pristine).** Master merge CI
  builds, tests, and uploads the `.nupkg` set (the workflow already uploads
  `Packages-{run_number}`; the version is already final because the props bump
  merged first). The release job does **not** rebuild: it locates the successful
  CI run for the tag's commit, downloads those artifacts, runs the check-version
  gate, and pushes. Published bits are byte-identical to tested bits; SDK drift is
  eliminated; no retest needed — which is exactly the "release what master tested"
  property. Requires: required status checks on master (green becomes enforced,
  closing the advisory-CI bullet), release job fails loudly if no successful run /
  artifact exists for that commit (re-run master CI to regenerate), artifact
  retention long enough for the bump→release window.
- **Design A, rebuild + retest (simpler fallback).** Keep the rebuild but insert
  `verify-samples → test` before pack/push. Tested-what-you-ship holds within the
  release run; costs minutes per release; simpler to propagate to other repos
  (no cross-run artifact plumbing).

Either design closes the gap; Design B is the stronger property. The path-filter
gap (slnx/assets) should be fixed regardless — one-line workflow change.

### F3 — Nothing asserts tag == props version

On `release: published`, the pipeline never checks that the tag the human typed
(`v3.0.0-beta.72`) equals the props `<Version>`. Typo the tag and you publish
packages whose version disagrees with the release page forever. The
beta.69/beta.70 divergence proves the ledger already drifted.

**Fix:** in release mode, hard-fail unless `tag == "v" + propsVersion`. Additionally
assert the tag commit is reachable from master (no releases from stray branches).
Better still, remove the second human entry entirely — see F7.

### F4 — git-tag strategy has inverted semantics in the release context (shared DevCli defect)

`GitTagCheckService` treats `GITHUB_REF_NAME` as "the latest already-released tag."
On a `release: published` event, `GITHUB_REF_NAME` **is the tag being released**. So
for any repo using `checkVersionStrategy: git-tag` in the release pipeline:

- Correct release (tag == props) → "already released" → **pipeline aborts**.
- Wrong release (tag != props) → "safe to release" → **publishes the mismatch**.

Exactly backwards, both branches. Nuru is unaffected only because it configures
`nuget-search`, but this ships to every DevCli consumer. Also: like pre-454-022
nuget-search, it compares only the single latest tag, not membership in all tags.

**Fix:** in release context the git-tag check must be the F3 equality assertion
(tag == props ⇒ proceed). Outside release context, check membership of props version
in **all** tags, never just the newest.

### F5 — Package set is hand-maintained in three places

The same five-package set lives in (1) `PackProjectsAsync` project paths,
(2) `PushPackagesAsync` package IDs, (3) `.timewarp/dev.jsonc` check-version
`packages`. Add or rename a package and you must update three lists; miss one and it
is silently not packed, not pushed, or not gated. MSBuild already knows the truth:
`IsPackable` (`timewarp-nuru-parsing` and `timewarp-nuru-build` correctly opt out).

**Fix:** derive once — pack the solution (packable projects only), push
`artifacts/packages/*.nupkg` matching the props version, and have check-version read
package IDs from the packable csprojs (or one generated manifest). Zero
hand-maintained lists.

### F6 — check-version gate contradicts partial-publish recovery

Push loop pushes five packages sequentially with `--skip-duplicate`. If package 3 of
5 fails, re-running the release should resume — `--skip-duplicate` exists for
exactly that. But check-version fails when **any** package already has the version,
so the re-run aborts at step 1. The recovery path for beta.69-style incidents
(tagged, partially or never pushed) requires manual pushes outside the pipeline —
which is how out-of-band artifacts like beta.70 happen.

**Fix:** gate distinguishes three states: none published → proceed; **all**
published → abort ("already released"); **some** published → proceed with a loud
resume warning (skip-duplicate makes it idempotent).

### F7 — The version is typed by humans twice (props and tag); the second entry should not exist

Root cause of F3 and the burned-numbers class: props bump and tag creation are two
independent manual acts that can disagree or drift apart in time. The pristine model
has exactly one human act: merge the props bump. The tag/Release should be
**derived** — a `dev release` command (or thin workflow) reads props, verifies the
gate, creates `v{Version}` tag + GitHub Release on master head. F3's assertion then
remains as defense-in-depth, not as the primary mechanism.

### F8 — Perpetual beta is an undecided policy, not a bug

`3.0.0-beta.71` — seventy-one mainline prereleases. SemVer ordering is fine and
NuGet handles it, but "beta" tells consumers "unstable, hidden without
-Prerelease" while the framework is treated as production elsewhere in the org.
This review's call: prerelease on mainline is **legitimate only as a declared
pre-GA state with written exit criteria**. Nuru must either ship `3.0.0` or write
down what blocks GA. That is a product decision — filed as a decision task, not
decided here.

### F9 — No canonical release doc; stale breadcrumbs

There is no "how we release" document anywhere in `documentation/` or the readme.
The only descriptions live in code comments and this kanban. Meanwhile
`workflow.yml:43` says `fetch-depth: 0  # Required for MinVer` — MinVer is not used
anywhere; the comment describes a system that doesn't exist. Small, but it is
exactly how the next maintainer gets the wrong mental model.

**Fix:** `documentation/developer/guides/releasing.md` as the canonical doc; fix or
drop the comment (keep `fetch-depth: 0` — the git-tag checks legitimately need full
tag history).

## Explicitly rejected alternatives

- **MinVer / tag-derived versioning** — moves SSOT out of reviewed files, makes
  local builds depend on tag state, and still needs every assertion above. Rejected.
- **Independent per-package versions** — buys nothing for an interdependent set,
  costs a compatibility matrix and five gates. Rejected.
- **Publishing from `push` to master (continuous deployment)** — removes the
  intentional-release property the org values. Rejected.

## Non-findings (looked at, fine as-is)

- `Environment.ExitCode` signaling between DevCli steps — inelegant but works;
  not worth churn.
- Thin YAML delegating to `dev workflow` — good pattern, keep as the shared shape.
- Shipping the frozen Mcp package in lockstep — harmless; revisit only if it blocks
  something.
