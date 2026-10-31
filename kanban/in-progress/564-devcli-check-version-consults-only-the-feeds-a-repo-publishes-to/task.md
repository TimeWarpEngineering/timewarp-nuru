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
- [ ] `checkVersionConfig.feeds` parsed and validated; default `nuget.org`
- [ ] `LookupAsync` contacts only declared feeds; no credential resolution otherwise
- [ ] `Unknown` only from a declared feed; 536 refusal text unchanged for that case
- [ ] Tests for nuget.org-only, both, unknown name, absent key
- [ ] Repos publishing to GitHub Packages identified in Results (which need the explicit setting)
- [ ] `ganda repo audit --fix` applied and committed; CI equivalent green
### Documentation
- [ ] DevCli readme Configuration section updated
- [ ] Design region in `nuget-version-service.cs` and `package-feed-lookup.cs` updated
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

## Implementation Notes

-

## Session

- Created: 04be23b7-afaf-4196-b07d-39320558a224 (2026-10-31)
