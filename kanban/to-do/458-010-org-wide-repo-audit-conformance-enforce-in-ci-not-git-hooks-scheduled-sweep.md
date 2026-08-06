# Org-wide repo audit conformance: enforce in CI not git hooks, scheduled sweep

Parent: 458 (Layer 3 of the enforcement architecture in `review/repo-matrix.md`).

## Description

Not all repos pass `ganda repo audit` today, and enforcement is inconsistent:
some repos wire it into git commit hooks, others don't, and hooks are weak by
nature — client-side, skippable (`--no-verify`), not installed on fresh clones,
and invisible to CI. Conformance must be checked where it cannot be skipped.

Target:

1. **CI is the enforcement plane.** `ganda repo audit` (plus the convention
   checks from `review/convention.md` — props SSOT, canonical caller workflow,
   v-prefixed tags, OIDC not secrets) runs as a step in merge/PR mode of the
   shared reusable workflow. A non-conforming repo fails its own CI on every
   PR — same rule in every repo, zero per-repo wiring. Git hooks remain optional
   local convenience for fast feedback, never the mechanism of record.
2. **Scheduled org sweep.** One scheduled workflow (`.github` repo) or `ganda`
   command iterates all non-fork/non-archived repos, runs the audit checks, and
   publishes a conformance report (which repos fail, which checks) — the
   regenerable version of 458's hand-built deviation matrix. Failures surface as
   a report/issue, not silently.
3. **Baseline pass:** run the audit against all active repos once, record the
   current failure list here, and burn it down (or explicitly waive per repo
   with a reason).

## Checklist

- [ ] Decide audit surface split: what lives in `ganda repo audit` (generic repo hygiene) vs `dev audit-convention` (release-convention checks) — avoid two overlapping tools
- [ ] Baseline: run audit across all active repos; record failures in this task
- [ ] Add audit step to the reusable workflow's pr/merge mode (fails the build)
- [ ] Waiver mechanism: per-repo documented opt-out with reason (config in `.timewarp/dev.jsonc`), so N/A repos don't red forever
- [ ] Scheduled sweep in `.github` repo regenerating the conformance/deviation report
- [ ] Deprecate audit git hooks as enforcement where present (keep as optional local convenience); note in convention docs
- [ ] Convention doc updated: "enforcement = CI, hooks = convenience"

## Notes

Implementation lands mostly in ganda (audit command) and the `.github` repo
(reusable workflow + sweep); tracked here because 458 owns the org-consistency
program. The sweep replaces ever hand-rebuilding the 458 audit
(`review/audit-results-2026-08-07.json` is the manual baseline snapshot).
