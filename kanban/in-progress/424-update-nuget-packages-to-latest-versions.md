# Update NuGet packages to latest versions

## Description

Update NuGet packages across all projects to latest STABLE versions. Keep TimeWarp.Terminal on prerelease (beta).

## Checklist

- [ ] Update TimeWarp.Nuru (net10.0) packages
  - [ ] Microsoft.CodeAnalysis.NetAnalyzers: 10.0.102 → 10.0.103 (STABLE)
  - [ ] Microsoft.Extensions.Configuration: 10.0.2 → 10.0.3 (STABLE)
  - [ ] Microsoft.Extensions.Configuration.Binder: 10.0.2 → 10.0.3 (STABLE)
  - [ ] Microsoft.Extensions.Configuration.CommandLine: 10.0.2 → 10.0.3 (STABLE)
  - [ ] Microsoft.Extensions.Configuration.EnvironmentVariables: 10.0.2 → 10.0.3 (STABLE)
  - [ ] Microsoft.Extensions.Configuration.Json: 10.0.2 → 10.0.3 (STABLE)
  - [ ] Microsoft.Extensions.Configuration.UserSecrets: 10.0.2 → 10.0.3 (STABLE)
  - [ ] Microsoft.Extensions.DependencyInjection.Abstractions: 10.0.2 → 10.0.3 (STABLE)
  - [ ] Microsoft.Extensions.Logging: 10.0.2 → 10.0.3 (STABLE)
  - [ ] Microsoft.Extensions.Logging.Abstractions: 10.0.2 → 10.0.3 (STABLE)
  - [ ] Microsoft.Extensions.Logging.Console: 10.0.2 → 10.0.3 (STABLE)
  - [ ] Microsoft.Extensions.Options: 10.0.2 → 10.0.3 (STABLE)
  - [ ] TimeWarp.Terminal: 1.0.0-beta.4 → 1.0.0-beta.5 (KEEP PRERELEASE)
- [ ] Update TimeWarp.Nuru.Analyzers (net10.0) packages
  - [ ] Microsoft.CodeAnalysis.NetAnalyzers: 10.0.102 → 10.0.103 (STABLE)
  - [ ] Microsoft.Extensions.Logging.Abstractions: 10.0.2 → 10.0.3 (STABLE)
- [ ] Update TimeWarp.Nuru.Build (net10.0) packages
  - [ ] Microsoft.Build.Utilities.Core: 18.0.2 → 18.3.3 (STABLE)
  - [ ] Microsoft.CodeAnalysis.NetAnalyzers: 10.0.102 → 10.0.103 (STABLE)
- [ ] Update TimeWarp.Nuru.Mcp (net10.0) packages
  - [ ] Microsoft.CodeAnalysis.NetAnalyzers: 10.0.102 → 10.0.103 (STABLE)
  - [ ] Microsoft.Extensions.Hosting: 10.0.2 → 10.0.3 (STABLE)
- [ ] Update TimeWarp.Nuru.Parsing (net10.0) packages
  - [ ] Microsoft.CodeAnalysis.NetAnalyzers: 10.0.102 → 10.0.103 (STABLE)
  - [ ] Microsoft.Extensions.Logging.Abstractions: 10.0.2 → 10.0.3 (STABLE)
- [ ] Run build to verify updates work
- [ ] Run tests to verify nothing broke

## Notes

## Implementation Plan

All packages are managed centrally in a single `Directory.Packages.props` file.

### Step 1: Update Directory.Packages.props
**File:** `/Directory.Packages.props`

Update the following version numbers:

| Package | Current | New |
|---------|---------|-----|
| Microsoft.CodeAnalysis.NetAnalyzers | 10.0.102 | 10.0.103 |
| Microsoft.Extensions.Configuration | 10.0.2 | 10.0.3 |
| Microsoft.Extensions.Configuration.Binder | 10.0.2 | 10.0.3 |
| Microsoft.Extensions.Configuration.CommandLine | 10.0.2 | 10.0.3 |
| Microsoft.Extensions.Configuration.EnvironmentVariables | 10.0.2 | 10.0.3 |
| Microsoft.Extensions.Configuration.Json | 10.0.2 | 10.0.3 |
| Microsoft.Extensions.Configuration.UserSecrets | 10.0.2 | 10.0.3 |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.2 | 10.0.3 |
| Microsoft.Extensions.Hosting | 10.0.2 | 10.0.3 |
| Microsoft.Extensions.Logging | 10.0.2 | 10.0.3 |
| Microsoft.Extensions.Logging.Abstractions | 10.0.2 | 10.0.3 |
| Microsoft.Extensions.Logging.Console | 10.0.2 | 10.0.3 |
| Microsoft.Extensions.Options | 10.0.2 | 10.0.3 |
| Microsoft.Build.Utilities.Core | 18.0.2 | 18.3.3 |
| TimeWarp.Terminal | 1.0.0-beta.4 | 1.0.0-beta.5 |

### Step 2: Verify Build
- Run `dotnet restore`
- Run `dotnet build`

### Step 3: Run Tests
- Run `dotnet run tests/ci-tests/run-ci-tests.cs`
