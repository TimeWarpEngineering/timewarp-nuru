# DevCli: check-version and release query the package's real feed, not always nuget.org

## Description

Filed from Roslynk task 008 (fork issue TimeWarpEngineering/Roslynk#13). Rescoped 2026-10-23, after Steven
said: "we have our structure this is Pete's structure... do we really want to add configuration? TimeWarp is
going for consistency." This task originally also added a `sourceDirectory` setting. That part is **dropped**:
Roslynk will move to the standard `source/` layout instead (Roslynk task after 009), so DevCli keeps
hard-coding `source/`.

What remains is a real bug. `nuget-version-service.cs:72` always queries `api.nuget.org`. TimeWarp packages
that are published only to the org's private GitHub Packages feed
(`https://nuget.pkg.github.com/TimeWarpEngineering/index.json`) therefore look unpublished at every version,
so the "already published" guards in check-version and release do nothing. **timewarp-taratibu has this gap
today**: nuget.org returns 404 for its package. Roslynk (`TimeWarp.Roslynk`) will have it too.

## Requirements

- check-version and release ask the feed the package is actually published to.
  - **Prefer convention over a new setting.** Find the feed from something the repo already declares. Options:
    the push target in the release workflow DevCli generates, a `nuget.config` package source, or a single
    org-wide rule such as "a repo that releases to GitHub Packages". Pick the source of truth that already
    exists. Add a `.timewarp/dev.jsonc` key only if nothing in the repo determines the feed, and give the
    reason in Results.
  - For a non-nuget.org feed, authenticate with `GITHUB_TOKEN`, then `GH_TOKEN`, then `gh auth token`. Look up
    the flat-container/registration resource from the service index instead of assuming nuget.org URL shapes.
  - **401, 403 or an unreachable feed means the check failed.** Refuse with a clear message; never report "not
    published".
- nuget.org repos behave exactly as they do today.
- If the release workflow template or `dev workflow` output has the same assumption, apply the same fix there.
- Tests: nuget.org stays unchanged; a GitHub Packages package found and not found; an auth failure refuses.
- Docs: `documentation/developer/guides/releasing.md`.
- Ship in a TimeWarp.Nuru.DevCli prerelease.

## Checklist

- [ ] Feed derived from the repo's existing declaration (or one justified setting)
- [ ] Token auth; an auth failure or unreachable feed refuses
- [ ] Tests
- [ ] Docs
- [ ] Released in a DevCli beta
- [ ] Follow-ups noted: taratibu and Roslynk 008 bump DevCli

## Session

- Created: Roslynk cockpit 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-10-23)
- Rescoped (sourceDirectory dropped): same session, 2026-10-23

## Notes

- Consumers: timewarp-taratibu (live gap), TimeWarpEngineering/Roslynk 008.
- The kitchen folder slug still says "configurable-source-directory". Only the title changed; the id is the
  identity.
