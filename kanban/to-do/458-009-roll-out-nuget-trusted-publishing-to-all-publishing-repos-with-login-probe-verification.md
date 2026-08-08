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

- [x] **INCIDENT 2026-08-08: all TP policies accidentally deleted on
      NuGet.org** (mistaken for API keys — UX) — **RESOLVED same day:
      operator recreated all 18 uniformly with `workflow.yml`.** Original
      recreate spec kept below for the record. **Recreate ALL 18 uniformly —
      one canonical workflow name, per operator ruling: policies encode the
      convention (`workflow.yml`), never legacy filenames; repos conform to
      policy.** Owner TimeWarp.Enterprises, repo owner TimeWarpEngineering,
      env blank, workflow `workflow.yml` for every row: amuru, architecture,
      build-tasks, builder, components, fixie, flexbox, heroicons, jaribu,
      mediator, multiavatar, nuru, options-validation, quickbooks,
      simple-icons, source-generators, state, terminal.
      Consequence: the six legacy repos' policies stay dormant until their
      publish workflow IS `workflow.yml` — their migration tasks must
      produce that rename/conversion (noted in each). Do NOT recreate:
      ganda (stop-publishing — ganda 201's policy-delete item thereby
      done), health + the redacted repo (don't publish). Verify no
      long-lived API keys were deleted alongside (legacy repos publish on
      those until migrated).
- [x] ~~Add policies for the missing repos~~ — was done (operator,
      2026-08-08) before the deletion incident above; superseded by the
      recreate item. Classification correction from the same report:
      **timewarp-health and [redacted-private-repo] do NOT currently
      publish NuGets** — their `nuget/login` workflows predate any actual
      publishing, so they are N/A for TP until they first publish (when they
      do: create the policy just before the first release — private-repo
      policies lapse after 7 days without a publish).
- [x] Migrate secret-key workflows to `nuget/login` — **DONE 2026-08-08, executed
      by the agent fleet within minutes of the tasks being filed** (state 077,
      multiavatar 001, quickbooks 009, mediator 001, fixie 001, build-tasks
      001 — all marked done with plans/reviews/results in their repos).
      Verified from this session: all six use `nuget/login`, zero residual
      `PUBLISH_TO_NUGET_ORG`/`NUGET_API_KEY` references.
- [ ] **ORG-WIDE single-workflow consolidation (operator ruling 2026-08-08:
      ONE workflow.yml for ALL CI/CD, params passed in; nuru verified as the
      clean reference).** 21 deviating repos identified from the audit data;
      tasks filed in every one:
      - Six migration repos — rename tasks **broadened in place to full
        consolidation** before the fleet grabbed them: state 078, fixie 002,
        multiavatar 002 (3 files → 1), mediator 002 (+4 .disabled + sync),
        quickbooks 010 (+claude.yml decision), build-tasks 002.
      - Five cloned publishers/apps with cruft: amuru **108** (stray sync
        md), architecture **173** (fold skill-lint + template-smoke or record
        exception), options-validation **003** (delete .bak), timewarp-ai
        **001** (publish-extension → workflow.yml; claude decisions),
        timewarp-software **024** (fold rebuild.yml).
      - Ten formerly-uncloned repos: **all cloned + tasks filed/committed
        2026-08-08** (crunchit, gambit, kahini, kivinjari, amina, financial,
        os, uml-mcp via background agent; `.github` and the github.io site
        filed directly — note ganda clone normalizes their dotted names to
        worktree dirs `hub/` and `timewarpengineeringhub.io/`).
      **EXECUTION 2026-08-08: the 11 substantial consolidations + ganda
      200/201 were run by four Grok Build delegates and ALL VERIFIED** —
      every repo now has exactly one workflow.yml (plus deliberately-kept
      claude*.yml in quickbooks/timewarp-ai), YAML valid, nuget/login
      preserved everywhere, state's draft-release trigger fixed to
      published, multiavatar's tag-push publish removed, architecture's
      skill-lint/template-smoke folded as jobs.
      **COMPLETE 2026-08-08:** the nine cruft tasks executed directly (8 sync
      deletions + gambit rename [pre-existing invalid YAML noted]; github.io
      task withdrawn to architecture 174; hub duplicate archived). Rollout
      pushes: 6 migration repos' defaults (incl. state PRs 572-574) + 15
      clean fast-forward repos + ganda dev. Eight repos with DIVERGED
      defaults (components, flexbox, jaribu, options-validation,
      source-generators, software, architecture, terminal) keep their
      committed work riding each repo's normal merge flow — concurrent
      activity on their mainlines makes merging them not this program's
      call.
- [x] **Login probe BUILT and rolled out to all 18 repos (2026-08-08):**
      `workflow_dispatch` `mode=probe` runs only the nuget/login OIDC exchange
      and stops (nuru reference implementation + 17 repos via per-repo
      micro-tasks; four workflow shapes handled). Release-event runs already
      hit login before the pipeline (step order), satisfying the fail-fast
      intent.
- [x] **ALL 18 REPOS PROBED LIVE (2026-08-08). Scoreboard: 16 PASS / 2 FAIL.**
      PASS: nuru, amuru, architecture, build-tasks, fixie, flexbox, heroicons,
      jaribu, mediator, multiavatar, options-validation, quickbooks,
      simple-icons, source-generators, state (after fixing its leaky
      release-job gating — PRs #572/573/574), terminal.
      **FAIL — policies missing on NuGet.org: timewarp-builder,
      timewarp-components** ("No matching trust policy owned by user
      'TimeWarp.Enterprises' was found") — two rows from the recreate were
      skipped or misconfigured; the probe caught them exactly as designed.
- [x] Operator fixed the builder + components policies; both re-probed
      2026-08-08: **SUCCESS. Final scoreboard: 18/18 — every publishing repo's
      trusted publishing policy verified live via the probe.**
- [x] Revoke long-lived NuGet API keys — **DONE (operator, 2026-08-08): all
      keys killed.** Safe because every publisher is consolidated onto OIDC;
      no fallback remains, so each repo's next release is its live
      verification. Residual cosmetic cleanup: dead `NUGET_API_KEY` /
      `PUBLISH_TO_NUGET_ORG` entries may linger in repos' GitHub secrets
      settings — nothing references them; delete at leisure.
- [x] Package ownership — verified to the limit of public API (2026-08-08):
      owner search shows 18 TimeWarp.Enterprises-owned packages (all
      search-visible ones). Unlisted packages (prerelease-only lines like
      Jaribu/Nuru.Analyzers) cannot be enumerated publicly; their functional
      proof is the 18/18 probe pass (policies owned by TimeWarp.Enterprises
      exchanged tokens successfully) and each repo's next publish is the
      per-package confirmation.
- [x] Orphans — **TimeWarp.AspNetCore.Blazor.Templates deprecated (operator, 2026-08-08)**; TimeWarp.Cli left alone (revisit later)
- [x] Ganda: fully off public NuGet — publish workflow removed (grok), versions **unlisted + all previous deprecated (operator)**, TP policy deleted and not recreated. timewarp-ganda 201 DONE.
- [x] TP roster recorded in 458 `review/repo-matrix.md` (updated through the incident + recreate + 18/18 verification)

## Results

**End state (2026-08-08): the TimeWarp org publishes NuGet packages with ZERO
stored credentials.** Every publisher authenticates via OIDC trusted
publishing against a uniform `workflow.yml` policy; every policy verified
LIVE (18/18 probe pass); all long-lived API keys revoked and the dead
org-level secrets (`NUGET_API_KEY`, `PUBLISH_TO_NUGET_ORG`) deleted.

Delivered along the way: six legacy publishers migrated to OIDC (fleet) and
consolidated to single canonical workflow.ymls (grok, verified); 21-repo
single-workflow consolidation completed org-wide (incl. nine cruft cleanups
executed directly); probe mode built into all 18 publishers; policy-deletion
incident same-day resolved with the probe catching two silently-missed rows
(builder, components — fixed, re-verified); ganda fully off public NuGet;
Blazor Templates deprecated.

### How to validate

Smoke (any publisher repo): `gh workflow run workflow.yml --repo
TimeWarpEngineering/<repo> -f mode=probe` → run succeeds with "Trusted
publishing OK" probe-result step; a login-step failure = policy problem on
NuGet.org, nothing else.
Org check: `gh api orgs/TimeWarpEngineering/actions/secrets --jq
'.secrets[].name'` → no NUGET/PUBLISH secrets remain.
Full evidence trail: this checklist + 458 `review/repo-matrix.md` TP section.

Depends on / not in scope: per-package ownership final confirmation happens
at each repo's next publish; eight diverged-default repos merge their
committed work via their own flow; warn→require attestation flips and the
reusable-workflow conversion are 458-010 / Layer-1 follow-ons.

## Notes

Security property to remember: the 1-hour temp key covers ALL owner-account
packages — any trusted repo's workflow can push any TimeWarp package. Containment
is the shared reusable workflow + DevCli gate (458 architecture), and keeping the
policy list limited to repos that actually release. Policies deactivate if their
creating user leaves the org.
