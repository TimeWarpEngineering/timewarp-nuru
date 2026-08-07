# Org-wide repo audit conformance: enforce in CI not git hooks, scheduled sweep

Parent: 458 (Layer 3 of the enforcement architecture in `review/repo-matrix.md`).

## Description

Not all repos pass `ganda repo audit` today, and enforcement is inconsistent:
some repos wire it into git commit hooks, others don't, and hooks are weak by
nature — client-side, skippable (`--no-verify`), not installed on fresh clones,
and invisible to CI. Conformance must be checked where it cannot be skipped.

Target:

**Constraint (decided 2026-08-07):** ganda is private by design (operator's
internal tooling; not to be made public or API-stabilized for outside use).
Public repos' CI runners have neither the ganda executable nor repo access.
Therefore CI enforcement cannot depend on ganda. Note: TimeWarp.Ganda currently
exists on public nuget.org (unlisted, beta.15 vs repo tags at beta.22+) —
unlisted is downloadable, not private; whether to keep publishing it publicly is
an explicit decision item below.

1. **CI is the enforcement plane, via DevCli (public).** The convention checks
   from `review/convention.md` — props SSOT present, canonical caller workflow
   shape, v-prefixed tags, OIDC not secrets — live in `dev audit-convention` in
   TimeWarp.Nuru.DevCli (public, NuGet-distributed, already compiled into each
   repo's `dev.cs`). Runs in merge/PR mode of the shared reusable workflow; a
   non-conforming repo fails its own CI on every PR, no ganda dependency, zero
   per-repo wiring. Git hooks remain optional local convenience for fast
   feedback, never the mechanism of record.
2. **Scheduled org sweep runs centrally, where ganda lives.** A scheduled job in
   a PRIVATE repo (timewarp-flow or timewarp-ganda) with an org-scoped token
   walks all non-fork/non-archived repos via the API, runs the full
   `ganda repo audit` checks, and publishes a conformance report (which repos
   fail, which checks) — the regenerable version of 458's hand-built deviation
   matrix. Ganda never leaves private infrastructure; public repos never see it.
3. **No duplication — check ownership, one home per check** (verified against
   `ganda repo audit --list-checks`, 23 checks, 2026-08-07):
   - **Hygiene → ganda only** (all current checks: directory structure, kebab
     names, slnx, CPM, banned APIs, bin/dev, envrc, icon, runfile shebangs,
     regions, nuru-latest, …). Private, evolves freely.
   - **Release-convention invariants → DevCli only** (~5 checks: `<Version>`
     SSOT in source props, caller workflow CONTENT matches canonical shape,
     v-prefixed tags, OIDC not stored secrets, dev.jsonc valid). Small and
     stable because the convention is.
   - Actual overlap today is only two existence checks (`workflow-file`,
     `source-directory-build-props`) — existence stays hygiene/ganda; content
     assertions are convention/DevCli.
   - **Ganda's sweep composes both**: runs its own checks natively and invokes
     each repo's `dev audit-convention` (ganda already requires `bin/dev` via
     its own check). Ganda never reimplements convention checks; DevCli never
     grows hygiene checks.
4. **Baseline pass:** run the audit against all active repos once, record the
   current failure list here, and burn it down (or explicitly waive per repo
   with a reason).

### Considered and rejected: running ganda itself in public-repo CI (2026-08-07)

Feasible: org-level secret with fine-grained PAT or GitHub App token
(`contents:read` on timewarp-ganda) lets any repo's workflow clone-and-build
ganda or download a prebuilt private release asset / GitHub Packages binary.
Rejected because:

1. **It imposes the stability contract ganda is private to avoid** — 20 repos'
   CI invoking ganda on every PR means any ganda change can break org CI;
   that is a de facto public API with worse debugging.
2. **Exposure**: the private binary lands on every public runner; any
   compromised workflow/third-party action with org-secret access can
   exfiltrate the token and the binary. Today ganda's blast surface is
   operator machines; this would make it every CI environment in the org.

**Fixes stance:** CI fails, never fixes — an enforcement gate that rewrites
code is out. `--fix` is a local, human-triggered activity where ganda already
exists. Convention violations are mostly one-time migration acts (458 rollout);
afterward the CI check is a drift tripwire. If a convention check later earns a
fixer, the fixer lives in ganda while the authoritative check stays in DevCli.

Revisit trigger: convention check count grows past ~10 or recurring-fix demand
appears — then re-evaluate single-tool-via-App-token against this record.

## Checklist

- [ ] Implement `dev audit-convention` in DevCli content (checks per convention.md; no ganda dependency)
- [ ] Add audit step to the reusable workflow's pr/merge mode (fails the build)
- [ ] Waiver mechanism: per-repo documented opt-out with reason (config in `.timewarp/dev.jsonc`), so N/A repos don't red forever
- [ ] Central sweep: scheduled job in private repo (timewarp-flow or timewarp-ganda) with org-read token; regenerates conformance/deviation report via `ganda repo audit`
- [ ] Baseline: run sweep across all active repos; record failures in this task
- [ ] Decide: keep publishing TimeWarp.Ganda to public nuget.org (unlisted = still downloadable/decompilable) or stop and install from private repo — record decision and align 458-009 TP roster
- [ ] Deprecate audit git hooks as enforcement where present (keep as optional local convenience)
- [ ] Convention doc updated: "enforcement = CI via DevCli; ganda = private superset + central sweep; hooks = convenience"

## Notes

Implementation lands in DevCli (audit-convention), ganda (sweep), and the
`.github` repo (reusable workflow step); tracked here because 458 owns the org
consistency program. The sweep replaces ever hand-rebuilding the 458 audit
(`review/audit-results-2026-08-07.json` is the manual baseline snapshot).
