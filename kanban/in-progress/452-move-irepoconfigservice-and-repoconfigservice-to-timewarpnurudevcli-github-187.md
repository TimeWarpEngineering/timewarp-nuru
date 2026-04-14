# Move IRepoConfigService and RepoConfigService to TimeWarp.Nuru.DevCli (GitHub #187)

## Description

Move `IRepoConfigService` and `RepoConfigService` from TimeWarp.Amuru to TimeWarp.Nuru.DevCli. These services were removed from Amuru (see Amuru #79) because Amuru is a public library and shouldn't contain application-specific config or reference private "ganda" branding. Per-repo config is a dev-cli concern, not a library concern.

The `check-version-command.cs` in Nuru.DevCli's shared endpoint package still references `IRepoConfigService`, which no longer exists in Amuru.

## Checklist

- [ ] Create `IRepoConfigService` interface in `TimeWarp.Nuru.DevCli`
- [ ] Create `RepoConfigService` implementation in `TimeWarp.Nuru.DevCli`
- [ ] Rename config file path from `.timewarp/ganda.jsonc` to `.timewarp/dev.jsonc`
- [ ] Update `check-version-command.cs` to use the new Amuru API (`CheckGitTagVersionAsync` / `CheckNuGetVersionAsync`)
- [ ] Remove old `IRepoCheckVersionService.CheckAsync(strategy, package, tag)` references
- [ ] Register `IRepoConfigService` / `RepoConfigService` in dev-cli DI container
- [ ] Update any existing config files (e.g., `timewarp-builder/.timewarp/ganda.jsonc`)
- [ ] Test the check-version command works correctly with both git-tag and nuget-search strategies

## Config Data Structure

Current config format in `.timewarp/ganda.jsonc`:
```json
{
  "CheckVersion": {
    "Strategy": "nuget-search",
    "Packages": "TimeWarp.Builder"
  }
}
```

Two string values:
- `Strategy`: `"git-tag"` or `"nuget-search"`
- `Packages`: comma-separated NuGet package IDs

## Session

- Created: ses_27611b26affeFNOaiW6Kctdf6V (2026-04-14)

## Notes

- The config is dev-cli specific — only `check-version-command` consumes it
- Nuru.DevCli is where shared dev-cli endpoints live
- Keeps Amuru clean as a general-purpose library with no application-specific config
- Any dev-cli (ganda, local dev, CI) reads the same `.timewarp/dev.jsonc`
- Amuru PR #73 refactored `IRepoCheckVersionService` to two focused methods (`CheckGitTagVersionAsync` and `CheckNuGetVersionAsync`)
- References:
  - GitHub Issue: https://github.com/TimeWarpEngineering/timewarp-nuru/issues/187
  - Amuru task 079: removed `IRepoConfigService` from Amuru
  - Only existing config file: `timewarp-builder/.timewarp/ganda.jsonc`
