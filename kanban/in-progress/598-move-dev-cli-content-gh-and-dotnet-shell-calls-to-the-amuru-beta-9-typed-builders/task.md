# Move DevCli content gh and dotnet Shell calls to the Amuru beta.9 typed builders

## Description

`TimeWarp.Nuru.DevCli` ships its endpoints and services as source (`content/any/…`) that
compile into each consumer's dev CLI. Those files still call `Shell.Builder("gh")` and
`Shell.Builder("dotnet")`. TimeWarp.Amuru 3.0.0-beta.9 adds the AMURU0001 analyzer, which
reports these calls as compile errors. Any repo that bumps Amuru to beta.9 cannot build its
dev CLI against DevCli 4.0.0-beta.15.

Found by timewarp-jaribu task 077 (the dev CLI self-install failed after the bump):

```
content/any/endpoints/release-command.cs(142,30): error AMURU0001: Use Gh.Auth().Status() … "auth status"
content/any/endpoints/release-command.guards.cs(266,45): error AMURU0001: Use Gh.Run().List() … "run list"
content/any/endpoints/release-command.guards.cs(287,49): error AMURU0001: Use Gh.Api() … "api"
content/any/services/github-packages-credential.cs(47,22): error AMURU0001: Use Gh.Auth().Token() … "auth token"
content/any/services/packable-project-service.cs(221,12): error AMURU0001: Use DotNet.MsBuild() … "msbuild"
```

`release-command.guards.cs:220` (`gh release create`) is also in the `Gh` table
(`Gh.Release().Create()`), so expect a sixth hit once the others are fixed.

## Requirements

- Bump `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools` to 3.0.0-beta.9 in `Directory.Packages.props`.
- Move every AMURU0001 site under `source/timewarp-nuru-devcli/content/` (and anywhere else
  in the repo) to the typed `Gh.*` / `DotNet.*` builder. No `#pragma`, no `.editorconfig`
  downgrade, no `ExcludeAssets="analyzers"`.
- Where a typed builder lacks an option the call needs, use the base `DotNet.Builder()`
  (same argument list) and file a task in timewarp-amuru naming the missing option.
- The DevCli content package must declare (or document) the Amuru.Tools dependency the
  typed builders need, so consumers compile the content.
- Release a new `TimeWarp.Nuru.DevCli` beta so consumers can bump.

## Checklist

- [ ] Amuru pins at 3.0.0-beta.9, zero AMURU0001 in the repo and in DevCli content
- [ ] DevCli content compiles in a consumer dev CLI against Amuru beta.9
- [ ] DevCli beta released

## Depends on

- None. Amuru 3.0.0-beta.9 is published.

## Session

- Created: 1894826 (2026-11-18)

## Notes

Blocks timewarp-jaribu 077 (Amuru beta.9 bump). The ganda sibling (ganda 670) likely hits
the same errors if it consumes Nuru.DevCli content.
