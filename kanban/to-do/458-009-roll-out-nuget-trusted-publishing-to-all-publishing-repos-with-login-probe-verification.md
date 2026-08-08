# Roll out NuGet trusted publishing to all publishing repos with login probe verification

Parent: 458. Cross-repo work tracked here (458 is the org consistency program).

## Description

All NuGet-publishing repos must use trusted publishing (OIDC via `nuget/login`),
no long-lived API keys. Policy model (verified against MS docs 2026-08-07): one
policy per (owner account, repo, workflow file[, environment]); a policy covers
**all packages owned by the account** — per repo, NOT per package.

Policies configured as of 2026-08-07 (13, per operator's NuGet.org review):
simple-icons, ganda, source-generators, flexbox, architecture, components,
options-validation, builder, state, amuru, jaribu, terminal, nuru.

Missing (8): heroicons, health, [redacted-private-repo] (workflows already
use `nuget/login` — releases fail until policy exists); build-tasks, mediator,
fixie, multiavatar, quickbooks (also still push with stored secrets — need
workflow migration before the policy matters).

**Verification:** NuGet has no public API to enumerate policies. The check is
behavioral: a `nuget/login` probe (OIDC exchange, no push) succeeds iff an active
policy matches the repo+workflow. Make it a `dev` command / reusable-workflow
canary step so "is TP configured" is answerable from CI on demand.

**Private-repo caveat (docs):** policies on private repos start "temporarily
active" for 7 days and lapse if no publish occurs; permanent only after first
successful publish. For ganda/health/[redacted-private-repo]: arm or re-arm
the policy just before the release, and expect the probe to report lapsed
policies as failures (that is correct behavior, not noise).

**Relation to the attestation model (458-010):**

- **Release-mode gate stack.** Release mode now opens with two cheap
  verifications before any build: the ganda **attestation check** on the tag's
  tree (458-010) and the TP **login probe** (this task). Both fail in seconds
  with precise messages; order them before build/pack/push alongside
  check-version.
- **TP state is probe-only, per-repo.** The OIDC exchange only works from
  inside the repo's own workflow run, so TP configuration can NOT be checked
  centrally — 458-010's optional detection-only sweep cannot cover TP policies
  by probing; it could at best track the roster list in this task. The
  authoritative TP check is always the in-repo probe.
- **Same trust separation.** TP and attestation follow the same principle:
  CI verifies evidence (OIDC identity / signature), never holds long-lived
  authority (stored keys / the private tool).
- **TimeWarp.Ganda public-NuGet decision** (owned by 458-010) — decided
  2026-08-08: **stop publishing**. Ganda drops off this task's roster: delete
  its existing TP policy and remove it from the configured-13 count (→ 12).

## Checklist

- [x] Add policies for the missing repos — **DONE (operator, 2026-08-08): TP
      policies exist on NuGet.org for all repos that actually publish
      packages.** Classification correction from the same report:
      **timewarp-health and [redacted-private-repo] do NOT currently
      publish NuGets** — their `nuget/login` workflows predate any actual
      publishing, so they are N/A for TP until they first publish (when they
      do: create the policy just before the first release — private-repo
      policies lapse after 7 days without a publish).
- [ ] Migrate secret-key workflows to `nuget/login` — **per-repo tasks filed and
      committed in each repo (2026-08-08); all six repos cloned locally**:
      state **077**, multiavatar **001**, quickbooks **009**, mediator **001**,
      fixie **001**, build-tasks **001** (kanban skeletons bootstrapped in
      mediator/fixie/build-tasks, which had none). Each task carries its
      repo-specific audit facts (state's draft-release trigger, multiavatar's
      ungated tag-push publish, etc.) and notes the full 458 conversion
      supersedes it if imminent. Commits are LOCAL — push each repo (or PR
      where master requires it) when starting the work.
- [ ] Login-probe step: `dev` command or reusable-workflow input that runs `nuget/login` without pushing; clear failure message "trusted publishing not configured/lapsed for this repo+workflow"
- [ ] Release mode runs the probe up front — fail fast before build, not at push; sequence it with the 458-010 attestation verify (both are seconds-cheap pre-build gates)
- [ ] After all repos flip: revoke every long-lived NuGet API key on nuget.org; delete `NUGET_API_KEY` / `PUBLISH_TO_NUGET_ORG` GitHub secrets
- [ ] Verify package ownership: every published package must have TimeWarp.Enterprises as owner (policies act on the owner account's packages)
- [ ] Orphans — decided 2026-08-08 (operator): **deprecate TimeWarp.AspNetCore.Blazor.Templates**; **leave TimeWarp.Cli alone for now** (revisit later)
- [ ] Ganda: delete its existing TP policy and remove its publish workflow (per 458-010 stop-publishing decision) — **tracked as timewarp-ganda kanban 201** (filed 2026-08-08)
- [ ] Record the TP roster in 458 `review/repo-matrix.md`

## Notes

Security property to remember: the 1-hour temp key covers ALL owner-account
packages — any trusted repo's workflow can push any TimeWarp package. Containment
is the shared reusable workflow + DevCli gate (458 architecture), and keeping the
policy list limited to repos that actually release. Policies deactivate if their
creating user leaves the org.
