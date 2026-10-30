# DevCli: check-version and release query the package's real feed by convention, not always nuget.org

## Description

Replaces archived 525. Steven (2026-10-24): "we don't need a config directory nor want one." The
`sourceDirectory` idea is gone, because Roslynk moved to `source/` (Roslynk task 010). This task also adds **no
new setting**.

The bug: `source/timewarp-nuru-devcli/content/any/services/nuget-version-service.cs:72` always asks
`https://api.nuget.org/v3-flatcontainer/{id}/index.json`. TimeWarp publishes to two feeds: nuget.org for public
packages, and the org's private GitHub Packages feed (`https://nuget.pkg.github.com/TimeWarpEngineering/index.json`)
for timewarp-taratibu and `TimeWarp.Roslynk`. For a GitHub Packages package, every version looks unpublished. The
"already published" guards in check-version and `dev release` therefore never fire, and a version can be
re-released.

Repo visibility is **not** a usable convention, because Roslynk is public but publishes to GitHub Packages.

## Requirements

- **Check both org feeds, with no configuration.** A version counts as published if nuget.org **or** the org
  GitHub Packages feed lists it. These are the only two feeds TimeWarp publishes to. Hard-code them as DevCli
  constants beside the existing nuget.org one. Add no `.timewarp/dev.jsonc` key.
- **GitHub Packages auth:** use `GITHUB_TOKEN`, then `GH_TOKEN`, then `gh auth token`. Find the
  flat-container (`PackageBaseAddress/3.0.0`) resource from the service index instead of assuming nuget.org URL
  shapes.
- **Fail-safe:**
  - Found on either feed means published.
  - Not found on nuget.org, and GitHub Packages could not be checked (no token, 401/403, unreachable), means
    **refuse** with a clear message naming the feed and the fix (`gh auth refresh -s read:packages`, or
    `packages: read` in Actions). Never report "not published" in that case.
  - nuget.org unreachable also refuses, as it should today.
  - A 404 from GitHub Packages for an unknown package id is "not found", not an error.
- The **partially published** guard in `dev release` (`version PARTIALLY published, never tagged`) works across
  both feeds.
- nuget.org-only repos behave as before when GitHub Packages answers "not found".
- Tests:
  - found on nuget.org only
  - found on GitHub Packages only
  - found on neither
  - GitHub Packages auth failure, which refuses
  - GitHub Packages 404 for an unknown id, which counts as not found
  - nuget.org unreachable
  - a token without packages access refuses with the distinct message, not "already published"

  Use a fake HTTP handler; no live network in tests.
- **Rollout safety (added 2026-10-29, cockpit review):** the fail-safe refusal would break every release
  pipeline as it stands. The `release` job in `.github/workflows/workflow.yml` runs `dev workflow` (step 2 is
  check-version) with `GH_TOKEN: ${{ github.token }}` and explicit permissions `contents: read`,
  `id-token: write`, `actions: read`. Because the permissions are explicit, that token has **no** packages
  access: GitHub Packages answers 401, check-version refuses, and the pipeline aborts with "version already
  released". This hits every repo on the shared workflow, including nuget.org-only repos like Nuru.
  - Add `packages: read` to the `release` job permissions in this repo's `workflow.yml`, in this PR.
  - Any other job that runs check-version or `dev release` needs the same. Audit every job in `workflow.yml`.
  - Make the refusal for an unreadable GitHub Packages **distinct** from "already published". Name the feed, the
    HTTP status and the fix. The pipeline abort must not print "version already released" for it.
  - Results: list every consumer repo (at least timewarp-taratibu and Roslynk) that must add `packages: read`
    before it takes this DevCli. Say whether the ganda workflow convention or audit should carry it as a
    candidate follow-up. Do not change other repos here.
- **Verify the real GitHub Packages shapes before coding (live, read-only, with `gh auth token`):**
  - Read the service index at `https://nuget.pkg.github.com/TimeWarpEngineering/index.json` and use its
    `PackageBaseAddress/3.0.0` resource.
  - Look up `timewarp.roslynk` and a taratibu package. A cockpit probe of the guessed
    `.../download/timewarp.roslynk/index.json` returned **404** even though Roslynk publishes there. Find out
    whether the URL shape or the owner is wrong.
  - Record the observed status for an unknown id, a known id, no token (401 was observed), and a token without
    `read:packages`.
  - Mock exactly those responses in the tests. If a known package cannot be found live, stop and record it as a
    blocker instead of guessing.
- Docs: `documentation/developer/guides/releasing.md`. Include the `packages: read` requirement.
- Ship in a TimeWarp.Nuru.DevCli prerelease.
- Results note: timewarp-taratibu forks DevCli's `workflow` command locally
  (`tools/dev-cli/endpoints/workflow-command.cs`, a hard-coded GitHub Packages push target). Say whether DevCli
  could own that push target too, as a candidate follow-up. Do not change taratibu here.

## Goal (implementer restatement)

DevCli's publish gates (`check-version`, and `dev release` step 7) ask only nuget.org whether a version
exists. Packages on the org's GitHub Packages feed therefore always look unpublished, so the guards never
fire and a version can be released twice. Make the gates ask both org feeds, with no new setting.

Acceptance:

- A version is published when nuget.org **or** `https://nuget.pkg.github.com/TimeWarpEngineering/index.json`
  lists it. Both URLs are DevCli constants. The GitHub Packages flat container is the service index's
  `PackageBaseAddress/3.0.0` resource.
- Token order: `GITHUB_TOKEN`, `GH_TOKEN`, `gh auth token`.
- Fail-safe: found on either feed means published. Not on nuget.org and GitHub Packages unreadable (no token,
  401/403, unreachable, malformed) refuses with its own message (feed, HTTP status, fix), never "already
  published" and never "not published". nuget.org failure still refuses. A GitHub Packages 404 is not found.
- `check-version`, the `dev release` publish guard (all and partial states) and the `dev workflow` release
  pipeline use it. The pipeline abort names the feed problem, not "version already released".
- `packages: read` on every `workflow.yml` job that runs check-version or `dev release`.
- Fake-HTTP tests for the seven listed cases, mocked on live-observed responses. Live shapes recorded.
- Docs in `documentation/developer/guides/releasing.md` (with `packages: read`), DevCli readme.
- Results list consumer repos needing `packages: read`, and the follow-ups (ganda convention or audit,
  taratibu's local workflow fork, the DevCli beta release).

## Checklist

- [x] Both org feeds checked, with no setting
- [x] Token auth; an unverifiable GitHub Packages refuses; a 404 counts as not found
- [x] check-version and both release guards use it
- [x] Live GitHub Packages shapes verified and recorded (Roslynk lookup resolved)
- [x] `packages: read` added to the release job (and any other job that runs check-version)
- [x] Distinct refusal message for an unreadable GitHub Packages
- [x] Tests (fake HTTP)
- [x] Docs
- [ ] Released in a DevCli beta (after merge: the next 4.0.0-beta.8 release task; this repo bumps versions in release tasks, see 554)
- [x] Follow-ups noted: consumer repos needing `packages: read`; taratibu and Roslynk 008 bump DevCli; taratibu's local workflow fork

## Results

### Live GitHub Packages shapes (2026-10-29, read-only, `gh auth token` with `read:packages`)

| Request | Status | Body |
|---|---|---|
| Service index, token | 200 | `PackageBaseAddress/3.0.0` = `https://nuget.pkg.github.com/TimeWarpEngineering/download` (no trailing slash) |
| `download/timewarp.taratibu/index.json`, token | 200 | `{"versions":["1.0.0-beta.16","1.0.0-beta.15",...]}`, **newest first** (nuget.org lists oldest first) |
| `download/timewarp.taratibu.routines/index.json`, token | 200 | versions beta.16 to beta.10 |
| Unknown id `timewarp.does-not-exist-536`, token | 404 | `{"errors":[{"code":"NAME_UNKNOWN",...}]}` |
| No token: index, known id, unknown id | 401 | `WWW-Authenticate: Basic realm="GitHub Package Registry"` |
| Invalid token (Basic or Bearer) | index 200, unknown id 404, known id 401 | `"Your request could not be authenticated by the GitHub Packages service..."` |
| gh token without `read:packages`, known id | 403 | recorded in ganda task 363 (2026-10-08). The only gh account here has `read:packages`, so this was not re-probed |

Both Basic (`user:token`) and Bearer auth return 200. DevCli uses Basic, the scheme NuGet clients use.

**Roslynk 404, resolved.** The URL shape and the owner are both right. `TimeWarp.Roslynk` has **never been
published**: TimeWarpEngineering/Roslynk has 0 tags, 0 releases and 0 Actions runs, and its workflow only pushes
on a tag. The org package list has TimeWarp.Taratibu, TimeWarp.Taratibu.Routines, TimeWarp.Ganda, TimeWarp.Flexbox
and TimeWarp.Zana. So the cockpit's 404 was the correct "not found" answer. Taratibu stood in as the known package.

Two findings the probe changed in the design:

- "Latest NuGet version" is now the SemVer maximum across both feeds. `versions[^1]` would have picked the
  oldest GitHub Packages version.
- A bad token cannot hide a known package (it gets 401, so the gate refuses), and an unknown id still gets 404.

### What changed

- `nuget-version-service.cs`: constants `NuGetOrgFlatContainerUrl`, `GitHubPackagesServiceIndexUrl`,
  `PackageBaseAddressResourceType`. New `LookupAsync` (both feeds) and `GetGitHubPackagesVersionsAsync`. The
  service index is read once per service and the flat container comes from its `PackageBaseAddress/3.0.0`.
  `GetPackageVersionsAsync` (nuget.org) is unchanged. A test-seam constructor takes a credential provider.
- New `package-feed-lookup.cs`: `PackageFeedLookup` (`Check` returns Published, NotPublished or Unknown;
  `LatestVersion`; `FormatUnknownRefusal`) and `PackageFeedUnreadableException : HttpRequestException`.
- New `github-packages-credential.cs`: `GITHUB_TOKEN`, then `GH_TOKEN`, then `gh auth token`. `ToString` never
  prints the token.
- New `check-version-outcome.cs`: `CheckVersionOutcome` and `CheckVersionAbort.FormatAbortReason`.
- `check-version-command.cs`: uses `LookupAsync`, refuses on Unknown with the feed message, and records
  `Outcome` and `GitHubPackagesFailure`.
- `release-command.cs` step 7: uses `LookupAsync`, so the All ("already released") and Partial ("partially
  published but was never tagged") refusals count both feeds. Refuses on Unknown with the same feed message.
- `tools/dev-cli/endpoints/workflow-command.cs`: the Step 2/6 abort line comes from `FormatAbortReason`. An
  unreadable feed prints `Pipeline ABORTED — GitHub Packages feed https://nuget.pkg.github.com/TimeWarpEngineering/index.json unreadable (HTTP 401 Unauthorized) — add 'packages: read' to the job permissions`,
  never "version already released".
- `.github/workflows/workflow.yml`: `packages: read` on the `release` job. Audit of every job: `changes`,
  `stage` (`dev workflow --stage`), `ci` and `release` probe mode never run check-version or `dev release`.
  Only the `release` job's "Run release pipeline" step does. `dev release` runs on an operator machine, not in
  CI.
- Tests: new `check-version-09-two-feed-lookup.cs` (17 tests, multi-mode). `check-version-07-endpoint-fail-closed.cs`
  routes its stubs through both feeds and adds the endpoint-level 403 refusal and the GitHub-only
  already-released case. Both test props files compile the three new service files. InternalsVisibleTo lists
  were regenerated by `runfiles/generate-internals-visible-to.cs`.
- Docs: `documentation/developer/guides/releasing.md` gets a new "Feeds the publish gates read" section with
  the `packages: read` requirement, and Step 2/6 notes the distinct abort. The DevCli readme gets a migration
  note and the service table. `changelog.md` gets an entry under Unreleased / Fixed.

Required test cases, each on fake HTTP that mirrors the observed responses:

| Case | Test |
|---|---|
| found on nuget.org only | `Found_on_nuget_org_only_is_published` |
| found on GitHub Packages only | `Found_on_github_packages_only_is_published`, endpoint `Found_on_github_packages_only_is_already_released` |
| found on neither | `Found_on_neither_is_not_published`, endpoint `Not_found_for_every_package_reports_safe` |
| GitHub Packages auth failure refuses | `Github_packages_auth_failure_is_unknown`, `No_token_is_unknown_without_a_request` |
| GitHub Packages 404 is not found | `Github_packages_404_for_unknown_id_is_not_found` |
| nuget.org unreachable | `Nuget_org_unreachable_throws`, `Nuget_org_outage_throws_with_status`, endpoint `Nuget_outage_fails_closed_instead_of_reporting_safe` |
| token without packages access refuses with the distinct message | `Token_without_packages_access_is_unknown_not_published`, endpoint `Token_without_packages_access_refuses_with_the_feed_message` |

### Gates

- `dotnet build tools/dev-cli/dev.cs`: clean (warnings are errors).
- `ganda runfile cache --clear`, then `dotnet run tests/ci-tests/run-ci-tests.cs`: exit 0. Multi-mode and
  standalone both passed, 0 failed, with 09 in multi-mode and 07 in standalone.
- `ganda repo audit`: passes after `--fix` set the executable bit on `check-version-09-two-feed-lookup.cs`.
- Live smoke with real feeds: `dev check-version` on this repo reports 4.0.0-beta.7 already released (nuget.org
  read, GitHub Packages 404s). `--package TimeWarp.Taratibu` reports latest 1.0.0-beta.16 from GitHub Packages.
  `GITHUB_TOKEN=<invalid>` refuses with HTTP 401 and the fix lines.

### Follow-ups (not changed here)

- **Consumer repos that must add `packages: read` before taking this DevCli.** Each of these runs
  `dev workflow` release mode (check-version) from a job with explicit `permissions` and no packages scope:
  sovereign-intelligence-agency, timewarp-amina, timewarp-amuru, timewarp-architecture, timewarp-bayline,
  timewarp-flexbox, timewarp-health, timewarp-jaribu, timewarp-mediator, timewarp-quickbooks,
  timewarp-session-lake-tools, timewarp-source-generators, timewarp-state, timewarp-terminal. Already covered:
  timewarp-taratibu (`packages: write`, which includes read) and timewarp-ganda (`packages: read`). Roslynk's
  current workflow has `packages: write` and does not run check-version yet; Roslynk 008 must keep a packages
  scope when it adopts the dev-cli release flow. This list comes from the local master checkouts on
  2026-10-29. A nuget.org-only repo may still pass without the scope, because an Actions token probably gets
  404 for an unknown id the way an invalid token does. That was not observed, so add the scope anyway.
- **Candidate ganda follow-up:** the ganda workflow convention (template) and a `ganda repo audit` check
  should carry `packages: read` on any job that runs check-version, `dev release` or `dev workflow` release
  mode. File it in timewarp-ganda.
- **taratibu and Roslynk 008** bump TimeWarp.Nuru.DevCli to the release that carries this (4.0.0-beta.8).
- **taratibu's local workflow fork** (`tools/dev-cli/endpoints/workflow-command.cs`) hard-codes
  `GitHubPackagesSource` for the push and still aborts with "version already released" (line 243). DevCli
  could own the push target too: `NuGetVersionService.GitHubPackagesServiceIndexUrl` now exists, so a shared
  push step could pick nuget.org (OIDC key) or GitHub Packages (`GITHUB_TOKEN`) by convention. That is a
  candidate DevCli follow-up. Until then, taratibu should call `CheckVersionAbort.FormatAbortReason` at
  line 243.
- **Release:** "Ship in a DevCli prerelease" happens after merge. Version bumps here ride release tasks (554
  did beta.7), so this PR does not bump `source/Directory.Build.props`.
- Residual: a valid token without access to one specific private package could get 404 instead of 403, which
  would read as "not found". This was not observed. Org packages are org-scoped, so a `read:packages` token
  sees them all.

### How to validate

**Smoke:**

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/devcli/check-version-09-two-feed-lookup.cs
dotnet run tests/timewarp-nuru-tests/devcli/check-version-07-endpoint-fail-closed.cs
dotnet run --file tools/dev-cli/dev.cs -- check-version --package TimeWarp.Taratibu
GITHUB_TOKEN=ghp_invalid0000000000000000000000000000 dotnet run --file tools/dev-cli/dev.cs -- check-version --package TimeWarp.Taratibu
grep -n "packages: read" .github/workflows/workflow.yml
```

**Expect:**

- 09: 17 passed. 07: 9 passed.
- Taratibu check (needs `gh` logged in with `read:packages`): `Latest NuGet version: 1.0.0-beta.16`, and
  "safe to release" for the source version 4.0.0-beta.7.
- Invalid-token check: `Error: GitHub Packages lookup for 'TimeWarp.Taratibu' failed (HTTP 401 Unauthorized)`,
  then the feed URL and the two Fix lines. No "already released", exit 1.
- `workflow.yml`: one `packages: read` line, in the `release` job.

### Review

- Rounds: 1. Roster: general. Effort: 3 (by-diff, 1731 lines). Turn cap 200 (cursor uncapped).
- Counts: bug 0 open / 0 fixed / 0 wontfix; suggestion 0; nit 0. Final open count: 0.
- Disposition: clean. No wontfix and no escalation.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Session

- Created: Roslynk cockpit 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-10-24)
- 2026-10-29 implementer (claude, headless task work): live probe, implementation, tests, docs. `ganda repo audit`
  --fix set the executable bit on the new test runfile (runfile-executable); it then passes.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session bb03761c-eba3-48f9-ba58-9e8f7995b48f, max-turns 500, Done — 2026-10-29T13:45:39Z
- Review oracle: tw-implementation-review, effort 3, reviewer general (cursor, Grok 4.7), session not reported, turn cap 200 (cursor uncapped), round 1, disposition clean — 2026-10-29
- Review oracle: review by reviewer-cursor (cursor, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 200) — 2026-10-29T13:55:02Z

## Learnings

The restatement matches the task on both feeds, the fail-safe, the distinct abort, `packages: read`, the seven fake-HTTP cases, and the releasing guide. It adds two stricter items the code also does: a malformed GitHub Packages body counts as unreadable, and the DevCli readme carries the migration note.

Misread, with no gate change: the task says ship this in a TimeWarp.Nuru.DevCli prerelease. The restatement records that as a follow-up after merge. `source/Directory.Build.props` is still `4.0.0-beta.7`, and the checklist leaves the beta item open because version bumps ride release tasks (554 shipped beta.7).

## Review

- Round 1: Done

## Notes

- Consumers: timewarp-taratibu (live gap) and TimeWarpEngineering/Roslynk 008.
- 525 is archived; this task supersedes it.
