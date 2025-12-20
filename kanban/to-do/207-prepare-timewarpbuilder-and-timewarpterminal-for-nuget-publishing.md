# Prepare TimeWarp.Builder and TimeWarp.Terminal for NuGet publishing

## Description

Configure the new `TimeWarp.Builder` and `TimeWarp.Terminal` packages for NuGet publishing. Both packages were extracted as part of Task 159 but the CI/CD workflow and project files need updates before they can be published.

## Checklist

### 1. Update `timewarp-builder` project

- [ ] Update `source/timewarp-builder/timewarp-builder.csproj`:
  - Change `<RootNamespace>TimeWarp.Nuru</RootNamespace>` to `<RootNamespace>TimeWarp.Builder</RootNamespace>`

- [ ] Update source file namespaces (3 files):
  - [ ] `source/timewarp-builder/i-builder.cs`: Change `namespace TimeWarp.Nuru;` to `namespace TimeWarp.Builder;`
  - [ ] `source/timewarp-builder/i-nested-builder.cs`: Change `namespace TimeWarp.Nuru;` to `namespace TimeWarp.Builder;`
  - [ ] `source/timewarp-builder/scope-extensions.cs`: Change `namespace TimeWarp.Nuru;` to `namespace TimeWarp.Builder;`

### 2. Update `timewarp-terminal` project

- [ ] Update `source/timewarp-terminal/timewarp-terminal.csproj`:
  - Change `<RootNamespace>TimeWarp.Nuru</RootNamespace>` to `<RootNamespace>TimeWarp.Terminal</RootNamespace>`

- [ ] Update `source/timewarp-terminal/GlobalUsings.cs`:
  - Change `global using TimeWarp.Nuru;` to `global using TimeWarp.Builder;`

*(Source files already use `namespace TimeWarp.Terminal;` - no changes needed)*

### 3. Update dependent packages to import new namespace

- [ ] Update `source/timewarp-nuru-core/GlobalUsings.cs`:
  - Add `global using TimeWarp.Builder;`

- [ ] Update `source/timewarp-nuru-repl/GlobalUsings.cs`:
  - Add `global using TimeWarp.Builder;`

### 4. Update CI/CD Workflow

- [ ] Update `.github/workflows/ci-cd.yml`:
  - Add `TimeWarp.Builder` and `TimeWarp.Terminal` to the `PACKAGES` array at the beginning

### 5. Verification

- [ ] Build solution: `dotnet runfiles/build.cs`
- [ ] Verify packages are generated in `artifacts/packages/`:
  - `TimeWarp.Builder.*.nupkg`
  - `TimeWarp.Terminal.*.nupkg`
- [ ] Run tests: `dotnet tests/scripts/run-nuru-tests.cs`

## Notes

### Files Modified (Total: 9)

| File | Change Type |
|------|-------------|
| `source/timewarp-builder/timewarp-builder.csproj` | Edit RootNamespace |
| `source/timewarp-builder/i-builder.cs` | Edit namespace |
| `source/timewarp-builder/i-nested-builder.cs` | Edit namespace |
| `source/timewarp-builder/scope-extensions.cs` | Edit namespace |
| `source/timewarp-terminal/timewarp-terminal.csproj` | Edit RootNamespace |
| `source/timewarp-terminal/GlobalUsings.cs` | Edit using |
| `source/timewarp-nuru-core/GlobalUsings.cs` | Add using |
| `source/timewarp-nuru-repl/GlobalUsings.cs` | Add using |
| `.github/workflows/ci-cd.yml` | Edit PACKAGES array |

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
