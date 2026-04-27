# Move IRepoConfigService and RepoConfigService to TimeWarp.Nuru.DevCli (GitHub #187)

## Description

Move `IRepoConfigService` and `RepoConfigService` from TimeWarp.Amuru to TimeWarp.Nuru.DevCli. These services were removed from Amuru (see Amuru #79) because Amuru is a public library and shouldn't contain application-specific config or reference private "ganda" branding. Per-repo config is a dev-cli concern, not a library concern.

The `check-version-command.cs` in Nuru.DevCli's shared endpoint package still references `IRepoConfigService`, which no longer exists in Amuru.

## Checklist

- [x] Create `IRepoConfigService` interface in `TimeWarp.Nuru.DevCli`
- [x] Create `RepoConfigService` implementation in `TimeWarp.Nuru.DevCli`
- [x] Add shared config models (`RepoConfig`, `CheckVersionConfig`, `CheckVersionStrategy`) in `TimeWarp.Nuru.DevCli`
- [x] Rename config file path from `.timewarp/ganda.jsonc` to `.timewarp/dev.jsonc`
- [x] Update `check-version-command.cs` to use the new Amuru API (`CheckGitTagVersionAsync` / `CheckNuGetVersionAsync`)
- [x] Remove old `IRepoCheckVersionService.CheckAsync(strategy, package, tag)` references
- [x] Register `IRepoConfigService` / `RepoConfigService` in dev-cli DI container
- [x] Update `TimeWarp.Nuru.DevCli.props` and local dev-cli includes to compile shared services
- [x] Remove local duplicate shared endpoints from `tools/dev-cli/endpoints/`
- [x] Flatten shared/local `DevCli.*` namespaces to `DevCli`
- [x] Update TimeWarp.Amuru to `1.0.0-beta.29`
- [x] Verify build / publish works (`dotnet build timewarp-nuru.slnx`, `dotnet build tools/dev-cli/dev.cs`, `dev self-install`)
- [ ] Update any existing config files (e.g., `timewarp-builder/.timewarp/ganda.jsonc`)
- [ ] Test the check-version command works correctly with both git-tag and nuget-search strategies against real config files

## Config Data Structure

Current config format in `.timewarp/dev.jsonc`:
```jsonc
{
  "checkVersionConfig": {
    "checkVersionStrategy": "nuget-search",
    "packages": "TimeWarp.Builder"
  }
}
```

Current values:
- `checkVersionStrategy`: `"git-tag"` or `"nuget-search"`
- `packages`: comma-separated NuGet package IDs

Implementation notes:
- C# property names intentionally match type names for clarity (`CheckVersionConfig`, `CheckVersionStrategy`)
- JSON config example is camelCase
- The enum uses `JsonStringEnumMemberName` so JSON values stay hyphenated while C# stays strongly typed

## Session

- Created: ses_27611b26affeFNOaiW6Kctdf6V (2026-04-14)

## Notes

- The config is dev-cli specific — only `check-version-command` consumes it
- Nuru.DevCli is where shared dev-cli endpoints live
- Keeps Amuru clean as a general-purpose library with no application-specific config
- Any dev-cli (ganda, local dev, CI) reads the same `.timewarp/dev.jsonc`
- Amuru PR #73 refactored `IRepoCheckVersionService` to two focused methods (`CheckGitTagVersionAsync` and `CheckNuGetVersionAsync`)
- Shared config/service files were added under `source/timewarp-nuru-devcli/content/any/services/`
- Local duplicate shared endpoints were removed from `tools/dev-cli/endpoints/`
- `tools/dev-cli` compiles the shared source-only content files directly via `Directory.Build.props`
- `dev self-install` completed successfully after the namespace/include fixes
- References:
  - GitHub Issue: https://github.com/TimeWarpEngineering/timewarp-nuru/issues/187
  - Amuru task 079: removed `IRepoConfigService` from Amuru
  - Only existing config file: `timewarp-builder/.timewarp/ganda.jsonc`
