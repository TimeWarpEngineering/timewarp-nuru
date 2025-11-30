# Publish All NuGet Packages

## Description

The CI/CD pipeline builds 8 NuGet packages but only publishes 4. Update the workflow and version check script to explicitly publish all packages from a defined array.

**Missing from publish:**
- TimeWarp.Nuru.Core
- TimeWarp.Nuru.Telemetry
- TimeWarp.Nuru.Repl
- TimeWarp.Nuru.Completion

**Analysis:** `.agent/workspace/2025-12-01T14-30-00_ci-cd-nuget-publishing-analysis.md`

## Requirements

- Use explicit package array (no globs) to prevent accidental publishing
- Update both `ci-cd.yml` and `check-version.cs` with same package list
- Maintain publish order: dependencies before dependents

## Checklist

### Implementation
- [ ] Update `.github/workflows/ci-cd.yml` publish step with all 8 packages
- [ ] Update `scripts/check-version.cs` package array with all 8 packages
- [ ] Verify package dependency order (Core before Completion/Repl/Telemetry)

### Verification
- [ ] Run `scripts/build.cs` to confirm all 8 .nupkg files created
- [ ] Run `scripts/check-version.cs` to verify all packages checked

## Notes

### Package List (dependency order)

```csharp
string[] packages = [
  "TimeWarp.Nuru.Core",        // Foundation - no Nuru dependencies
  "TimeWarp.Nuru.Logging",     // Depends on Core
  "TimeWarp.Nuru.Completion",  // Depends on Core
  "TimeWarp.Nuru.Telemetry",   // Depends on Core, Logging
  "TimeWarp.Nuru.Repl",        // Depends on Core, Completion
  "TimeWarp.Nuru",             // Depends on all above
  "TimeWarp.Nuru.Analyzers",   // Standalone
  "TimeWarp.Nuru.Mcp"          // Standalone tool
];
```

### Package Architecture

```
TimeWarp.Nuru (batteries-included)
├── TimeWarp.Nuru.Core
├── TimeWarp.Nuru.Logging
├── TimeWarp.Nuru.Telemetry
├── TimeWarp.Nuru.Repl
└── TimeWarp.Nuru.Completion

TimeWarp.Nuru.Analyzers (standalone)
TimeWarp.Nuru.Mcp (standalone tool)
```

### CI/CD Publish Template

```yaml
# Explicit package list - no globs
PACKAGES=(
  "TimeWarp.Nuru.Core"
  "TimeWarp.Nuru.Logging"
  "TimeWarp.Nuru.Completion"
  "TimeWarp.Nuru.Telemetry"
  "TimeWarp.Nuru.Repl"
  "TimeWarp.Nuru"
  "TimeWarp.Nuru.Analyzers"
  "TimeWarp.Nuru.Mcp"
)

for pkg in "${PACKAGES[@]}"; do
  dotnet nuget push "artifacts/packages/${pkg}.$VERSION.nupkg" \
    --api-key ${{ secrets.NUGET_API_KEY }} \
    --source https://api.nuget.org/v3/index.json \
    --skip-duplicate
done
```
