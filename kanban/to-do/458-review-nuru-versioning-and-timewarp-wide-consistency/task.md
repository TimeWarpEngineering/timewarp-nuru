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

- [ ] Re-read `source/Directory.Build.props`, `.github/workflows/workflow.yml`, `tools/dev-cli/endpoints/workflow-command.cs`, check-version + config
- [ ] Note how `workflow_dispatch` vs `release` map to release mode today
- [ ] Inventory TimeWarp publish repos (from org / known list / `ganda` workspace if useful)

### Analysis & recommendation

- [ ] Write `review/findings.md` — what works, what to change, what to leave alone (be opinionated; avoid laundry lists of theoretical footguns)
- [ ] Write `review/convention.md` — proposed TimeWarp versioning + release convention (copy-pasteable)
- [ ] Write `review/repo-matrix.md` — repo × current state × required change (if any)
- [ ] Call out **breaking process changes** (e.g. dispatch no longer publishes without confirm)

### Follow-through

- [ ] Create Nuru implementation child tasks (if any) via `ganda kanban create … --parent 458`
- [ ] Create or file tasks for other TimeWarp repos that must align (link from `review/repo-matrix.md`)
- [ ] `## Results` with How to validate (what a cold reader re-reads / re-runs)

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

## Session

- Created: grok session (2026-08-06) — folder task for Claude (or human) review of Nuru versioning; TimeWarp-wide consistency required if Nuru changes
