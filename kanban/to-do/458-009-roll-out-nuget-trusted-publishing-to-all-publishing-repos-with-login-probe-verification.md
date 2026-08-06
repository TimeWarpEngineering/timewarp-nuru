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

## Checklist

- [ ] Add policies for the 8 missing repos (owner TimeWarp.Enterprises; workflow file name only, e.g. `workflow.yml`)
- [ ] Migrate secret-key workflows to `nuget/login`: state (`PUBLISH_TO_NUGET_ORG`), mediator, fixie, multiavatar, quickbooks, build-tasks, quickbooks (`PUBLISH_TO_NUGET_ORG`)
- [ ] Login-probe step: `dev` command or reusable-workflow input that runs `nuget/login` without pushing; clear failure message "trusted publishing not configured/lapsed for this repo+workflow"
- [ ] Release mode runs the probe first — fail fast before build, not at push
- [ ] After all repos flip: revoke every long-lived NuGet API key on nuget.org; delete `NUGET_API_KEY` / `PUBLISH_TO_NUGET_ORG` GitHub secrets
- [ ] Verify package ownership: every published package must have TimeWarp.Enterprises as owner (policies act on the owner account's packages); also confirm intent for orphans TimeWarp.Cli and TimeWarp.AspNetCore.Blazor.Templates (deprecate or adopt)
- [ ] Record the TP roster in 458 `review/repo-matrix.md`

## Notes

Security property to remember: the 1-hour temp key covers ALL owner-account
packages — any trusted repo's workflow can push any TimeWarp package. Containment
is the shared reusable workflow + DevCli gate (458 architecture), and keeping the
policy list limited to repos that actually release. Policies deactivate if their
creating user leaves the org.
