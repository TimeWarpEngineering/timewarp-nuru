# Phase 1: Dev CLI Foundation & CI/CD Commands

## Description

Create the unified `dev` CLI tool that consolidates all CI/CD operations into a single AOT-compiled binary. This tool will replace the individual runfiles with attributed route commands and dramatically simplify the GitHub Actions workflow by leveraging NuGet Trusted Publishing (OIDC).

## Goals

1. **Unified CLI**: Single `dev` command for all CI/CD operations
2. **Simplified Workflow**: Reduce ci-cd.yml from ~118 lines to ~25 lines
3. **Trusted Publishing**: Migrate from API key secrets to OIDC-based publishing
4. **Local Parity**: Developers can run exact same commands locally as CI

## Command Structure

```bash
# Main CI command (auto-detects mode from GITHUB_EVENT_NAME)
dev ci                     # Auto-detect: pr, merge, or release
dev ci --mode pr           # build → verify-samples → test
dev ci --mode merge        # build → verify-samples → test
dev ci --mode release      # build → check-version → pack → push

# Individual commands for local development
dev build [--clean|-c] [--verbose|-v]
dev clean
dev test
dev verify-samples
dev check-version
```

## Checklist

### Cleanup & Project Setup
- [x] Delete duplicate `Program.cs` (keep lowercase `program.cs`)
- [x] Delete empty `Commands/` directory (keep lowercase `commands/`)
- [x] Create `timewarp-nuru-dev-cli.csproj` with AOT support
- [x] Create `global-usings.cs`
- [x] Create `Directory.Build.props` for analyzer suppressions
- [x] Add project to `timewarp-nuru.slnx`

### Command Implementation
- [x] Update `build-command.cs` - Full implementation from `runfiles/build.cs`
- [x] Create `clean-command.cs` - From `runfiles/clean.cs`
- [x] Create `test-command.cs` - From `runfiles/test.cs`
- [x] Create `verify-samples-command.cs` - From `runfiles/verify-samples.cs`
- [x] Create `check-version-command.cs` - From `runfiles/check-version.cs`
- [x] Create `ci-command.cs` - Orchestration with mode detection

### Testing
- [x] Test `dev build` locally
- [x] Test `dev clean` locally
- [x] Test `dev test` locally
- [x] Test `dev verify-samples` locally (2 pre-existing sample failures detected)
- [x] Test `dev check-version` locally
- [x] Test `dev ci --mode pr` locally
- [x] Test `dev ci` auto-detection locally (defaults to Pr when GITHUB_EVENT_NAME not set)

### NuGet Trusted Publishing Setup
- [ ] Create Trusted Publishing policy on nuget.org for each package:
  - [ ] TimeWarp.Nuru.Core
  - [ ] TimeWarp.Nuru.Logging
  - [ ] TimeWarp.Nuru.Completion
  - [ ] TimeWarp.Nuru.Telemetry
  - [ ] TimeWarp.Nuru.Repl
  - [ ] TimeWarp.Nuru
  - [ ] TimeWarp.Nuru.Analyzers
  - [ ] TimeWarp.Nuru.Mcp

### GitHub Actions Update
- [x] Update `ci-cd.yml` to use `dev ci`
- [x] Add `id-token: write` permission
- [x] Add `NuGet/login@v1` step for releases
- [x] Remove `NUGET_API_KEY` secret usage
- [ ] Test PR workflow (requires Trusted Publishing setup on nuget.org)
- [ ] Test release workflow (requires Trusted Publishing setup on nuget.org)

## Technical Details

### Project Structure
```
tools/dev-cli/
├── timewarp-nuru-dev-cli.csproj
├── program.cs
├── global-usings.cs
└── commands/
    ├── build-command.cs
    ├── clean-command.cs
    ├── test-command.cs
    ├── verify-samples-command.cs
    ├── check-version-command.cs
    └── ci-command.cs
```

### CI Mode Detection
```csharp
// Auto-detect from GitHub Actions environment
string? eventName = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME");
CiMode mode = eventName switch
{
    "pull_request" => CiMode.Pr,
    "push" => CiMode.Merge,
    "release" => CiMode.Release,
    "workflow_dispatch" => CiMode.Release,
    _ => CiMode.Pr  // Default for local dev
};
```

### Workflow Behaviors (Fail Fast)
| Mode    | Steps                                      |
| ------- | ------------------------------------------ |
| pr      | clean → build → verify-samples → test      |
| merge   | clean → build → verify-samples → test      |
| release | clean → build → check-version → pack → push |

### Simplified ci-cd.yml Target
```yaml
name: CI/CD

on:
  push:
    branches: [master]
  pull_request:
    branches: [master]
  release:
    types: [published]

jobs:
  ci:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      id-token: write

    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: NuGet login (OIDC)
        if: github.event_name == 'release'
        uses: NuGet/login@v1
        with:
          user: TimeWarp.Enterprises

      - name: Run CI
        run: dotnet run --project tools/dev-cli/timewarp-nuru-dev-cli.csproj -- ci
```

## Dependencies

- Task 150: Attributed Routes (Complete)
- Task 255: Epic - Dev CLI Unified Developer Tool (Parent)

## Notes

- NuGet org: https://www.nuget.org/profiles/TimeWarp.Enterprises
- Trusted Publishing reference: https://blog.verslu.is/nuget/trusted-publishing-easy-setup/
- Each package needs a separate Trusted Publishing policy on nuget.org
- No fallback to API key - Trusted Publishing only

## Implementation Notes

### TimeWarp.Amuru Update (1.0.0-beta.13 → 1.0.0-beta.17)
Updated during implementation. Breaking API changes fixed:
- `Shell.Run()` → `Shell.Builder()`
- `.ExecuteAsync()` → `.Build().RunAsync()`
- Fixed in: `clean.cs`, `clean-and-build.cs`, `format.cs`, `analyze.cs`

### Pre-existing Issues Discovered
2 samples fail to build (not related to this task):
- `samples/repl-demo/repl-prompt-fix-demo.cs`
- `samples/attributed-routes/attributed-routes.csproj`

### Workflow Reduction
- Before: 118 lines with complex bash scripts
- After: 62 lines (~48% reduction)
- Single command: `dotnet run --file tools/dev-cli/dev.cs -- ci`

## Results

**Completed: 2025-12-23**

### What was delivered
1. **Dev CLI as runfile** - `tools/dev-cli/dev.cs` with shebang for direct execution
2. **All CI/CD commands implemented**: build, clean, test, verify-samples, check-version, ci
3. **NuGet Trusted Publishing** - OIDC-based authentication via `--api-key` option
4. **Simplified workflow** - GitHub Actions reduced from 118 to 62 lines
5. **AOT publish script** - `runfiles/publish-dev.cs` outputs `./dev` binary

### Additional fixes during implementation
- Fixed 5 sample build failures (package versions, ITerminal injection)
- Bumped `TimeWarp.Jaribu` to `1.0.0-beta.8` (Terminal rename fix)
- Bumped `Microsoft.Extensions.Options.ConfigurationExtensions` to `10.0.1`

### Released
- **v3.0.0-beta.23**: All packages published to NuGet.org via Trusted Publishing
