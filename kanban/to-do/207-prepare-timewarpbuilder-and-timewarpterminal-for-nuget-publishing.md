# Prepare TimeWarp.Builder and TimeWarp.Terminal for NuGet publishing

## Description

Configure the new `TimeWarp.Builder` and `TimeWarp.Terminal` packages for NuGet publishing. Both packages were extracted as part of Task 159 but the CI/CD workflow and project files need updates before they can be published.

## Checklist

### Project File Updates

- [ ] Update `source/timewarp-builder/timewarp-builder.csproj`:
  - Change `RootNamespace` from `TimeWarp.Nuru` to `TimeWarp.Builder`
  
- [ ] Update `source/timewarp-terminal/timewarp-terminal.csproj`:
  - Change `RootNamespace` from `TimeWarp.Nuru` to `TimeWarp.Terminal`

### CI/CD Workflow

- [ ] Update `.github/workflows/ci-cd.yml`:
  - Add `TimeWarp.Builder` and `TimeWarp.Terminal` to the `PACKAGES` array
  - Ensure correct dependency order:
    1. TimeWarp.Builder (no dependencies)
    2. TimeWarp.Terminal (depends on TimeWarp.Builder)
    3. TimeWarp.Nuru.Core (depends on TimeWarp.Terminal)
    4. ... rest of packages

### Verification

- [ ] Build solution and verify packages are generated in `artifacts/packages/`
- [ ] Verify package metadata (description, tags, license, icon) is correct
- [ ] Run tests to ensure nothing broke

## Notes

### Current PACKAGES array in ci-cd.yml

```bash
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
```

### Updated PACKAGES array needed

```bash
PACKAGES=(
  "TimeWarp.Builder"
  "TimeWarp.Terminal"
  "TimeWarp.Nuru.Core"
  "TimeWarp.Nuru.Logging"
  "TimeWarp.Nuru.Completion"
  "TimeWarp.Nuru.Telemetry"
  "TimeWarp.Nuru.Repl"
  "TimeWarp.Nuru"
  "TimeWarp.Nuru.Analyzers"
  "TimeWarp.Nuru.Mcp"
)
```

### Dependency Order

```
TimeWarp.Builder
    ^
    |
TimeWarp.Terminal
    ^
    |
TimeWarp.Nuru.Core --> TimeWarp.Nuru.Logging
    ^                       |
    |                       v
TimeWarp.Nuru.Completion    |
    ^                       |
    |                       v
TimeWarp.Nuru.Telemetry     |
    ^                       |
    |                       v
TimeWarp.Nuru.Repl          |
    ^                       |
    +-----------------------+
    |
TimeWarp.Nuru
    ^
    |
TimeWarp.Nuru.Analyzers
TimeWarp.Nuru.Mcp
```
