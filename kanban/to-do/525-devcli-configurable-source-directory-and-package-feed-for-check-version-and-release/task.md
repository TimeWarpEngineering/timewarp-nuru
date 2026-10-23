# DevCli: configurable source directory and package feed for check-version and release

## Description

Filed from Roslynk task 008 (fork issue TimeWarpEngineering/Roslynk#13, walk blocked 2026-10-23). The Roslynk
fork wants the standard `dev check-version` / `dev release` flow, publishing to the private GitHub Packages
feed like timewarp-taratibu. Two hard-codings in `source/timewarp-nuru-devcli` stop it:

1. **Source directory.** `props-version-reader.cs:35` reads only `{repoRoot}/source/Directory.Build.props`, and
   `packable-project-service.cs:56` scans only `{repoRoot}/source`. The release workflow's tag check uses the
   same reader. Roslynk keeps upstream's capital `Source/` (renaming would break merges from Peter Morris's
   repo), and Linux paths are case-sensitive, so DevCli never finds it. `.timewarp/dev.jsonc` accepts only
   `checkVersionConfig.packages` and `attestation`.
2. **Feed.** `nuget-version-service.cs:72` always queries `api.nuget.org`. For a package published only to
   GitHub Packages, every version looks unpublished, so check-version and release's "already published" guards
   are meaningless. **timewarp-taratibu has this gap today** (nuget.org 404s for its package).

## Requirements

- `.timewarp/dev.jsonc`: `sourceDirectory` (string, default `"source"`), relative to the repo root. Used by the
  version reader, the release tag check, the packable-project scan, and anything else that assumes `source/`
  (grep for it). Error messages name the configured path.
- `checkVersionConfig.feed` (NuGet v3 service index URL, default nuget.org). For a non-nuget.org feed,
  authenticate with `GITHUB_TOKEN`, then `GH_TOKEN`, then `gh auth token`; resolve the flat-container/registration
  resource from the index rather than assuming nuget.org URL shapes. **401/403 or unreachable = the check failed**
  (refuse, with a clear message), never "not published".
- The release workflow template / `dev workflow` output, if it embeds these assumptions, honors both settings.
- Tests for both settings (default unchanged; `Source` casing; GitHub Packages feed with auth, and an auth
  failure refusing).
- Docs: `documentation/developer/guides/releasing.md` and the dev.jsonc reference.
- Ship in a TimeWarp.Nuru.DevCli prerelease.

## Checklist

- [ ] `sourceDirectory` setting wired through reader, tag check, project scan
- [ ] `checkVersionConfig.feed` with token auth; auth/unreachable refuses
- [ ] Tests
- [ ] Docs
- [ ] Released in a DevCli beta
- [ ] Follow-ups noted: Roslynk 008 bumps DevCli and sets `"sourceDirectory": "Source"` +
      feed `https://nuget.pkg.github.com/TimeWarpEngineering/index.json`; taratibu sets the feed

## Session

- Created: Roslynk cockpit 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-10-23)

## Notes

- Consumers: TimeWarpEngineering/Roslynk 008 (blocked on this), timewarp-taratibu (feed gap).
