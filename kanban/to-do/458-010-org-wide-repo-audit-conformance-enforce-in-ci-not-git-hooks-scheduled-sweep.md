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

**Enforcement model (clarified 2026-08-07, operator direction):** a sweep only
*observes* — it guarantees nothing at PR or release time and prohibits nothing.
The mechanism is: **one-off remediation, then per-repo gates.**

1. **One-off remediation pass (the migration, not a process).** Operator runs
   `ganda repo audit` (with `--fix` where fixers exist) across all active repos
   locally and brings them compliant once. Record the before/after list here.
2. **PR-level gate, via DevCli (public).** The convention checks from
   `review/convention.md` — props SSOT present, canonical caller workflow
   shape, v-prefixed tags, OIDC not secrets — live in `dev audit-convention`
   in TimeWarp.Nuru.DevCli (public, NuGet-distributed, already compiled into
   each repo's `dev.cs`). Runs in PR/merge mode of the shared reusable
   workflow. **Prohibits** merges only where required status checks exist
   (public repos on the Free plan); on private repos it is loud-but-advisory.
   No ganda dependency, zero per-repo wiring. Git hooks remain optional local
   convenience, never the mechanism of record.
3. **Release-level gate = the universal prohibition.** The same
   `dev audit-convention` runs in release mode and hard-fails the publish.
   Plan-independent, visibility-independent — this is where non-compliance is
   actually blocked on every repo (consistent with the 458 Layer 2 principle).
4. **Optional scheduled sweep — detection only, out-of-band state only.** A
   scheduled job on PRIVATE infra (timewarp-flow or timewarp-ganda, org-scoped
   token) exists solely for what no in-repo gate can see: GitHub-side settings
   (branch protection), NuGet-side state (TP policies), and repos that have not
   adopted the workflow or never receive PRs. It reports; it never enforces.
   Nothing in 1–3 depends on it; drop it if not worth the upkeep.
5. **No duplication — check ownership, one home per check** (verified against
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
   - **Ganda composes both** (locally and in the optional sweep): runs its own
     checks natively and invokes each repo's `dev audit-convention` (ganda
     already requires `bin/dev` via its own check). Ganda never reimplements
     convention checks; DevCli never grows hygiene checks.

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

- [ ] **One-off remediation:** run `ganda repo audit` (`--fix` where available) across all active repos locally; record before/after compliance list in this task
- [ ] Implement `dev audit-convention` in DevCli content (checks per convention.md; no ganda dependency; check-only, no fixers)
- [ ] Add audit step to the reusable workflow's PR/merge mode
- [ ] Add audit step to release mode as a **hard gate** before pack/push (the universal prohibition)
- [ ] Required status checks on public repos so the PR gate actually prohibits merges (same enabler as 458-002 Design B)
- [ ] Waiver mechanism: per-repo documented opt-out with reason (config in `.timewarp/dev.jsonc`), so N/A repos don't red forever
- [ ] Decide: keep the optional detection-only sweep (out-of-band state: branch protection, TP policies, non-adopted repos) or drop it — nothing else depends on it
- [ ] Decide: keep publishing TimeWarp.Ganda to public nuget.org (unlisted = still downloadable/decompilable) or stop and install from private repo — record decision and align 458-009 TP roster
- [ ] Deprecate audit git hooks as enforcement where present (keep as optional local convenience)
- [ ] Convention doc updated: "remediate once; enforce at PR (where checks are required) and always at release; ganda = private superset; hooks = convenience"

## Notes

Implementation lands in DevCli (audit-convention) and the `.github` repo
(reusable workflow step); the one-off remediation is operator-driven with ganda
locally; tracked here because 458 owns the org consistency program.
`review/audit-results-2026-08-07.json` is the manual baseline snapshot the
remediation pass starts from.
