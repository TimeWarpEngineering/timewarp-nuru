# Task 564: DevCli check-version consults only the feeds a repo publishes to

## Description

Since task 536, `NuGetVersionService.LookupAsync` reads two hard-coded feeds, nuget.org and
the org GitHub Packages feed, and `PackageFeedLookup` returns `Unknown` when a version is not
on nuget.org and GitHub Packages cannot be read. The `check-version` and `dev release` gates
refuse on `Unknown`. That is correct for a repo that publishes to GitHub Packages. It is wrong
for a repo that does not: it adds a dependency on a feed the repo never uses, and a missing
`packages: read` token in that repo's CI blocks its release.

It bit timewarp-mediator on 2026-10-31. Mediator publishes to nuget.org only. Its `dev release`
for v15.0.0-beta.3 (run 41184701087) failed in `Run release pipeline` with:

```
Error: GitHub Packages lookup for 'TimeWarp.Mediator' failed (no HTTP response):
no GitHub token: GITHUB_TOKEN and GH_TOKEN are unset and 'gh auth token' returned none.
  TimeWarp.Mediator 15.0.0-beta.3 is not on nuget.org, and the org GitHub Packages feed
  (https://nuget.pkg.github.com/TimeWarpEngineering/index.json) could not be read,
  so whether it is published is unknown.
Pipeline FAILED — Check Version failed
```

beta.1 and beta.2 of the same repo released on DevCli beta.7, before task 536. Steven's
direction (2026-10-31): a repo must not depend on a feed it does not publish to. Mediator is
holding its beta.3 release for this fix rather than adding a token for a feed it does not use.

## Goal (implementer restatement)

A repo declares the feeds it publishes to, and the DevCli publish gates (`check-version` and
`dev release` step 7) read only those feeds. A nuget.org-only repo, which is the default when
nothing is declared, never contacts GitHub Packages and never looks for a GitHub token, so it
no longer needs `packages: read` to release.

Acceptance:

- `checkVersionConfig.feeds` in `.timewarp/dev.jsonc`: comma-separated, `nuget.org` and/or
  `github-packages` (case-insensitive, trimmed, same grammar as `packages`). Absent means
  `nuget.org`. An unknown name, or a value that names no feed, is a config error printed
  before any network call (exit 1).
- `NuGetVersionService.LookupAsync` takes the resolved feed set and reads only those feeds.
  GitHub Packages and the credential provider are not touched unless `github-packages` is
  declared. `Unknown` comes only from a declared, unreadable GitHub Packages feed, and with
  both feeds declared the 536 refusal text is unchanged.
- `check-version` and `dev release` resolve the feed set with the same resolver from the same
  config key.
- Tests cover: nuget.org-only never touches the GitHub handler or the credential provider;
  both feeds keep the 536 behavior; an unknown name fails before the handler is called;
  absent `feeds` equals `nuget.org`. No test reads the machine's tokens.
- DevCli readme Configuration section documents `feeds`, the default, and the
  `packages: read` sentence. Design regions explain why the feed set belongs to the repo.
- `ganda repo audit` clean, CI-equivalent tests green, `<Version>` stays `4.0.0-beta.10`.

## Requirements

- `checkVersionConfig` gains a declaration of the feeds the repo publishes to. Proposed shape
  in `.timewarp/dev.jsonc`:

  ```jsonc
  "checkVersionConfig": {
    "packages": "TimeWarp.Mediator,TimeWarp.Mediator.Generators",
    // feeds: nuget.org | github-packages, comma-separated. Default: nuget.org.
    "feeds": "nuget.org"
  }
  ```

  Keep the key name and value grammar consistent with `packages`. Record the final shape in
  the DevCli readme Configuration section.
- Default when `feeds` is absent: `nuget.org` only. The repos that publish to GitHub Packages
  (today, per the org package list: TimeWarp.Taratibu, TimeWarp.Taratibu.Routines,
  TimeWarp.Ganda, TimeWarp.Flexbox, TimeWarp.Zana; and this repo if it does) declare
  `nuget.org,github-packages` explicitly. State the reasoning in the Design region of
  `nuget-version-service.cs`: the feed set is a property of the repo, not of the tool. If the
  implementer has evidence the default should stay "both" (for example an attestation or
  ganda rule that already requires GitHub Packages everywhere), record it in `## Results`
  and raise it as a blocker instead of choosing silently.
- `NuGetVersionService.LookupAsync` reads only the declared feeds. GitHub Packages is not
  contacted at all, and no credential is resolved, when it is not declared. The `Unknown`
  verdict can then arise only from a declared feed, and the refusal text stays as task 536
  wrote it for that case.
- A feed name that is not one of the two known values is a configuration error reported
  before any network call, in the same style as the other `dev.jsonc` validation.
- `dev release` and `check-version` share the resolved feed set; no separate switch.
- Tests: extend `check-version-09-two-feed-lookup.cs` (or a sibling file in the same guard
  style) with: nuget.org-only config never touches the GitHub Packages handler and never
  resolves a credential; both-feeds config keeps the 536 behavior; an unknown feed name
  fails before the handler is called; absent `feeds` equals `nuget.org`. No test reads the
  machine's tokens.
- `source/timewarp-nuru-devcli/readme.md` Configuration section documents `feeds`, the
  default, and the sentence "a repo that does not publish to GitHub Packages never needs
  `packages: read` for check-version".
- `ganda repo audit --fix` applied and committed; the repo's own `dev workflow` (or the
  documented CI equivalent) green.
- `<Version>` stays `4.0.0-beta.10` in this task unless Steven asks for the bump here; the
  consumer unblock is a DevCli `4.0.0-beta.11` release, which is a follow-up bump once this
  merges. Note that in `## Results` so the cockpit can chain it.

## Checklist

### Implementation
- [x] `checkVersionConfig.feeds` parsed and validated; default `nuget.org`
- [x] `LookupAsync` contacts only declared feeds; no credential resolution otherwise
- [x] `Unknown` only from a declared feed; 536 refusal text unchanged for that case
- [x] Tests for nuget.org-only, both, unknown name, absent key
- [x] Repos publishing to GitHub Packages identified in Results (which need the explicit setting)
- [x] `ganda repo audit --fix` applied and committed; CI equivalent green
### Documentation
- [x] DevCli readme Configuration section updated
- [x] Design region in `nuget-version-service.cs` and `package-feed-lookup.cs` updated
### Review
- [ ] Code Review
- [ ] PR with merge commit

## Notes

- Origin: timewarp-mediator release v15.0.0-beta.3, 2026-10-31, run
  https://github.com/TimeWarpEngineering/timewarp-mediator/actions/runs/41184701087. The
  tag exists at 1bcba1d with nothing published; Mediator re-cuts after a DevCli release with
  this fix lands and its pin moves.
- Task 536 introduced the two-feed lookup ("two feeds, no setting"); task 470-007 the
  fail-closed nuget.org lookup. This task keeps both behaviors for repos that declare both
  feeds.
- Mediator's current `.timewarp/dev.jsonc` already has `checkVersionConfig.packages`; it will
  add `"feeds": "nuget.org"` (or rely on the default) once this ships.

## Results

### What changed

- `checkVersionConfig.feeds` (`check-version-config.cs`): comma-separated `nuget.org` /
  `github-packages`, case-insensitive, trimmed, empty entries dropped (the `packages`
  grammar). `PackageFeedsResolver.Resolve` maps absent to `nuget.org`, and an unknown name or
  a value naming no feed (`""`, `","`) to an error that names the valid values. New types:
  `PackageFeeds` (flags), `PackageFeedsResolution`, `PackageFeedsResolver`. They live in
  `check-version-config.cs`, so consumers that compile DevCli files one by one need no new
  `Compile` entry.
- `NuGetVersionService.LookupAsync(packageId, PackageFeeds feeds, ct)` reads only the declared
  feeds. Without `GitHubPackages`, no request goes to GitHub Packages and the credential
  provider is never called. Without `NuGetOrg`, nuget.org is not called. An empty or
  out-of-range feed set throws `ArgumentException` before any request.
- `PackageFeedLookup` records `Feeds`. `Unknown` can come only from a declared, unreadable
  GitHub Packages feed. With both feeds declared, `FormatUnknownRefusal` is the 536 text
  word for word (a test pins all four lines). A GitHub-Packages-only repo gets the same lines
  without the false "not on nuget.org" clause.
- `check-version` resolves the feeds right after it loads config. `dev release` resolves them in
  step 1, before `gh auth status`, `git fetch` or any feed call, and uses them in step 7. Both
  use the same resolver on the same key, with no separate switch. A config error prints
  `Error: checkVersionConfig.feeds …` and exits 1 (check-version `Outcome` = `InvalidInput`).
  `dev workflow` runs the check-version handler, so the release pipeline inherits the feed set.
- Design regions updated in `nuget-version-service.cs`, `package-feed-lookup.cs`,
  `check-version-config.cs`, `check-version-command.cs` and `release-command.cs`. They say the
  feed set belongs to the repo, not the tool, and record the Mediator incident and Steven's
  2026-10-31 direction.
- Docs: the DevCli readme has a new "Feeds (`checkVersionConfig.feeds`)" subsection under
  Configuration, with the table, the default, the error rule, and the sentence "A repo that
  does not publish to GitHub Packages never needs `packages: read` for check-version." It also
  has a migration note for the next release, and the services table is updated.
  `documentation/developer/guides/releasing.md` "Feeds the publish gates read" is rewritten.
- This repo publishes to nuget.org only (its `nuget.config` and `dotnet nuget push` both use
  nuget.org), so it relies on the default. I removed the `packages: read` permission that 536
  added to the `release` job in `workflow.yml`, since the gate no longer needs it there. The
  release job builds dev-cli from source (`tools/dev-cli` compiles `content/any/**`), so a tag
  that contains this change runs the new gate. `.timewarp/dev.jsonc` gained a comment
  explaining `feeds`.

### Default stays nuget.org (evidence check)

I found no attestation or ganda rule that requires GitHub Packages everywhere: the audit has no
such check, and the 536 code said only "TimeWarp publishes to two feeds". Restores here use
nuget.org only. Nothing argues for keeping "both" as the default, so this is not a blocker.

### Repos that need the explicit setting (when they take the DevCli release with this change)

I checked nuget.org flat-container and the org GitHub Packages list on 2026-10-31:

| Package | nuget.org | GitHub Packages | Declare |
|---|---|---|---|
| TimeWarp.Taratibu, TimeWarp.Taratibu.Routines | 404 | yes | `"feeds": "github-packages"` (or both) |
| TimeWarp.Zana | 404 | yes | `"feeds": "github-packages"` (or both) |
| TimeWarp.Ganda | yes | yes | `"feeds": "nuget.org,github-packages"` |
| TimeWarp.Flexbox | yes | yes | `"feeds": "nuget.org,github-packages"` |
| TimeWarp.Mediator, TimeWarp.Nuru.* | yes | no | nothing (default) |

Without the key, those GitHub Packages repos would see their GitHub-only versions as
unpublished. Each needs the key in the same change that moves its DevCli pin.

### Follow-up for the cockpit

- `<Version>` stays `4.0.0-beta.10`. The consumer unblock is a DevCli `4.0.0-beta.11` release
  (version-bump task) once this merges. After that, Mediator moves its pin and re-cuts
  v15.0.0-beta.3 (the tag exists at 1bcba1d with nothing published). Taratibu, Zana, Ganda and
  Flexbox add `feeds` when they take beta.11.

### Gates run

- `dotnet run tests/timewarp-nuru-tests/devcli/check-version-09-two-feed-lookup.cs`: 26/26
  (10 new tests for 564).
- `dotnet run tests/timewarp-nuru-tests/devcli/check-version-07-endpoint-fail-closed.cs`:
  11/11 (2 new endpoint tests; the 536 endpoint tests now use a fixed both-feeds config).
- `check-version-02/04/05`: green.
- `ganda runfile cache --clear` then `dotnet run tests/ci-tests/run-ci-tests.cs`: exit 0,
  multi-mode and standalone (0 failed).
- `tests/devcli-consumer-check` `dotnet build` (net11 consumer): 0 warnings, 0 errors.
- `dotnet run --file tools/dev-cli/dev.cs -- workflow --stage build`: Pipeline SUCCEEDED.
- `ganda repo audit`: passes all checks. `--fix` changed nothing.

### How to validate

Smoke:

```bash
# nuget.org-only repo (this one, no feeds key), with no GitHub token in the environment:
env -u GITHUB_TOKEN -u GH_TOKEN dotnet run --file tools/dev-cli/dev.cs -- check-version
dotnet run tests/timewarp-nuru-tests/devcli/check-version-09-two-feed-lookup.cs
dotnet run tests/timewarp-nuru-tests/devcli/check-version-07-endpoint-fail-closed.cs
# typo check: set "feeds": "nuget.org,github-package" under checkVersionConfig in
# .timewarp/dev.jsonc, rerun check-version, then revert the edit
```

Expect:

- check-version prints `Version in source` / `Latest NuGet version` from nuget.org (on this
  branch, "already released" for 4.0.0-beta.10) and no GitHub Packages error or token message.
- Both test files report all tests passed (26 and 11).
- With the typo, check-version prints `Error: checkVersionConfig.feeds in .timewarp/dev.jsonc
  has unknown feed(s): 'github-package'. Valid feeds: nuget.org, github-packages
  (comma-separated).` and exits 1 without a NuGet line.

## Implementation Notes

-

## Session

- Created: 04be23b7-afaf-4196-b07d-39320558a224 (2026-10-31)
- 2026-10-31: implement oracle (implementer-claude, headless ganda task work). Feeds setting, both gates wired, tests, docs, workflow permission removed; gates green.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session c4618c42-96d0-4fc0-9256-9fff24ecff93, max-turns 500, Done — 2026-10-31T11:04:41Z
