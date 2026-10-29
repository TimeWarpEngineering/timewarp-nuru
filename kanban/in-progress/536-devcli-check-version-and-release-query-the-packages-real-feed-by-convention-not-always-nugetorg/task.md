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

## Checklist

- [ ] Both org feeds checked, with no setting
- [ ] Token auth; an unverifiable GitHub Packages refuses; a 404 counts as not found
- [ ] check-version and both release guards use it
- [ ] Live GitHub Packages shapes verified and recorded (Roslynk lookup resolved)
- [ ] `packages: read` added to the release job (and any other job that runs check-version)
- [ ] Distinct refusal message for an unreadable GitHub Packages
- [ ] Tests (fake HTTP)
- [ ] Docs
- [ ] Released in a DevCli beta
- [ ] Follow-ups noted: consumer repos needing `packages: read`; taratibu and Roslynk 008 bump DevCli; taratibu's local workflow fork

## Session

- Created: Roslynk cockpit 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-10-24)

## Notes

- Consumers: timewarp-taratibu (live gap) and TimeWarpEngineering/Roslynk 008.
- 525 is archived; this task supersedes it.
