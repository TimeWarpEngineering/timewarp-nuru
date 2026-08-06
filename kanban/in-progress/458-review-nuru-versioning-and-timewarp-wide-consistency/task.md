# Review Nuru versioning and TimeWarp-wide consistency

## Description

Review **TimeWarp.Nuru’s versioning + release model** as the reference pattern for TimeWarp repos. Decide what stays, what should change, and **propagate the same rules** to other TimeWarp repositories that publish packages (they must stay consistent with Nuru once this review lands).

This is a **design / process review**, not an implement-everything task. Outcomes should be:

1. Written recommendations under this folder (`review/`).
2. Explicit list of **follow-up implementation tasks** (Nuru first, then other repos).
3. A short **TimeWarp release/versioning convention** statement other repos can copy.

**Consumer note:** Roslynk (and similar non-TimeWarp or half-aligned tools) may copy this later; the hard requirement is **TimeWarp org consistency**, not third-party adoption.

## Context (current Nuru model — facts only)

Do not re-litigate every tradeoff from prior chat. Use this as the starting map:

| Piece | Current behavior |
|-------|------------------|
| Version SSOT | `source/Directory.Build.props` → `<Version>…</Version>` (shared across packable source packages) |
| Local / CI pack | Same number — no separate “type version in Actions” for pack identity |
| CI entry | `.github/workflows/workflow.yml` → `dev workflow` (thin YAML) |
| PR / push to master | Build/test path — **does not** push NuGet |
| Release path | `GITHUB_EVENT_NAME=release` (GitHub Release **published**) or `workflow_dispatch` → release mode: **check-version → clean → build → pack → push** |
| Gate | `dev check-version` — version in props must not already be published (NuGet and/or git-tag strategy per config) |
| Multi-package | One shared `<Version>`; hardcoded pack list in workflow command |

Related existing work: task **456** (check-version distance warning), task **454-022** (already-published version check), DevCli shared `check-version` / workflow conventions.

## Requirements

### Review scope (Nuru)

Evaluate and recommend on at least:

1. **SSOT** — Keep version in `Directory.Build.props` (or equivalent)? Any better single file?
2. **Release trigger** — GitHub Release published vs `workflow_dispatch` vs both; what should “release mode” require?
3. **check-version** — Enough as the only pre-push gate? Gaps worth fixing (e.g. tag/release title vs props alignment, multi-package partial push)?
4. **Shared monorepo version** — Keep one version for all Nuru packages, or allow independent package versions?
5. **Prerelease policy** — Long-running `X.Y.Z-beta.N` on mainline: intentional product policy or debt?
6. **Developer ergonomics** — Version bumps in feature PRs vs dedicated release PRs; any enforcement worth the cost?
7. **Docs** — Where the canonical “how we release” lives (readme / docs / skill); is it accurate?

### TimeWarp-wide consistency (mandatory if Nuru changes)

If the review changes Nuru’s convention:

- [ ] List **TimeWarp repos** that publish NuGet (or ship versioned artifacts) and must align
- [ ] For each: current version SSOT, CI trigger, check-version presence, gaps vs the new convention
- [ ] Prefer **one convention + DevCli/shared workflow pieces** over snowflakes
- [ ] Create follow-up tasks **per repo or batched** — do not leave “other repos later” as chat-only

Repos that do **not** publish packages may only need a short “N/A” note.

### Out of scope (unless review proves otherwise)

- Rewriting all of DevCli
- Roslynk / non-TimeWarp product migration (optional note only)
- Changing SemVer rules themselves

## Checklist

### Discovery

- [x] Re-read `source/Directory.Build.props`, `.github/workflows/workflow.yml`, `tools/dev-cli/endpoints/workflow-command.cs`, check-version + config
- [x] Note how `workflow_dispatch` vs `release` map to release mode today (DevCli says Release, YAML withholds credentials — incoherent; findings F1)
- [x] Inventory TimeWarp publish repos (known list from 454-022 downstream review; recorded in repo-matrix.md)

### Analysis & recommendation

- [x] Write `review/findings.md` — what works, what to change, what to leave alone (be opinionated; avoid laundry lists of theoretical footguns)
- [x] Write `review/convention.md` — proposed TimeWarp versioning + release convention (copy-pasteable)
- [x] Write `review/repo-matrix.md` — repo × current state × required change (per-repo audit deferred by operator instruction; population + F4 warning recorded)
- [x] Call out **breaking process changes** (convention.md rule 4 / 458-001: dispatch no longer publishes; 458-003: git-tag strategy behavior change for consumers)

### Follow-through

- [x] Create Nuru implementation child tasks via `ganda kanban create … --parent 458` (458-001 … 458-008)
- [x] ~~Create or file tasks for other TimeWarp repos that must align~~ — deferred by operator instruction (2026-08-06): "pristine convention first, do NOT worry about migration work"; repo-matrix.md records the population and the trigger for the future audit
- [x] `## Results` with How to validate (what a cold reader re-reads / re-runs)

## Notes

### Review posture for Claude (and humans)

Prior multi-agent discussion mixed useful points with overstated “footguns.” Prefer:

- **High signal:** SSOT in git, intentional publish, check-version, multi-repo consistency, release-trigger clarity  
- **Low signal / skip unless evidence:** scare scenarios without a concrete incident or repo constraint  
- **Product policy** (perpetual beta, single version for all packages): decide explicitly; don’t treat as accidental bugs  

### Suggested questions for the review (optional)

1. Should `workflow_dispatch` default to **non-release** (or require `--mode release` / a confirm input)?
2. Should publish **assert** GitHub release tag (when present) equals props `<Version>`?
3. Is one monorepo version still correct for Nuru’s package set in 2026?
4. What is the minimum shared surface other repos need (props pattern + workflow event matrix + `check-version` config)?

### Folder layout for artifacts

```
458-review-nuru-versioning-and-timewarp-wide-consistency/
  task.md
  review/
    findings.md      # required
    convention.md    # required
    repo-matrix.md   # required if multi-repo impact; else short N/A in findings
```

## Results

Review complete (2026-08-06). Deliverables under `review/`:

- **`review/findings.md`** — 9 findings (F1–F9). Keep: props SSOT, lockstep monorepo
  version, release-published-only publishing, check-version + 456 warning. Fix:
  dispatch/release incoherence (F1), no tests in release pipeline (F2), no tag==props
  assertion (F3), inverted git-tag strategy in DevCli (F4), triplicated package lists
  (F5), gate blocks partial-publish resume (F6), dual human version entry (F7),
  missing release docs + stale MinVer comment (F9). **F8 retracted 2026-08-07**:
  stable 2.0.0 serves default consumers; the 3.0 beta line is a correct
  next-major prerelease train — beta exit is defined by API commitment and is
  the maintainer's call (458-007 archived accordingly).
- **`review/convention.md`** — proposed 10-rule TimeWarp versioning + release
  convention with event→mode matrix (copy-pasteable) plus the enforcement
  architecture (reusable workflow + DevCli gate + automated drift audit).
- **`review/repo-matrix.md`** — full org audit (2026-08-07, 65 repos via live
  API): 21 publishers with per-repo deviations, org-plan constraint (Free — no
  branch protection on private repos), enforcement architecture, rollout order.
  Raw data in `review/audit-results-2026-08-07.json`.
- **Child tasks 458-001 … 458-008** — Nuru implementation follow-ups, one per
  actionable finding (458-002 reframed to promote-or-retest; 458-007 archived
  with F8's retraction).

Hard evidence anchoring the review: git tag `v3.0.0-beta.69` exists with no such
version on NuGet, while NuGet has `3.0.0-beta.70` with no git tag — tag↔package
divergence has already occurred in both directions.

### How to validate

Smoke (cold reader, ~10 min):

1. Read `review/findings.md`; for F1, confirm `tools/dev-cli/endpoints/workflow-command.cs`
   maps `workflow_dispatch` → `Release` (DetermineMode) while `.github/workflows/workflow.yml`
   gates `nuget/login` and `--api-key` on `github.event_name == 'release'`.
2. For F2, confirm `RunReleaseWorkflowAsync` has no verify-samples/test steps.
3. For the divergence evidence:
   `git tag | grep beta.69` → present; `git tag | grep beta.70` → absent;
   `curl -s https://api.nuget.org/v3-flatcontainer/timewarp.nuru/index.json | grep -o '3.0.0-beta.69\|3.0.0-beta.70'`
   → only beta.70.
4. Confirm children exist: `ls kanban/to-do/458-00*` → 8 tasks referencing findings F1–F9.

Expect: each finding traceable to a file/line or reproducible observation; no
finding relies on a hypothetical incident.

Task remains open until children 458-001…008 land (kanban parent/child propagation);
the review deliverable itself is complete.

## Session

- Created: grok session (2026-08-06) — folder task for Claude (or human) review of Nuru versioning; TimeWarp-wide consistency required if Nuru changes
- Review: Claude Code session (2026-08-06) — discovery, findings/convention/repo-matrix written, children 458-001…008 created; migration work waived by operator
