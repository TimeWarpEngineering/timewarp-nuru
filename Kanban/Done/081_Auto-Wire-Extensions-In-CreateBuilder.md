# Auto-Wire Extensions in CreateBuilder

## Description

Restructure packages so `TimeWarp.Nuru` becomes the batteries-included package with `CreateBuilder()` that auto-wires all extensions, while `TimeWarp.Nuru.Core` provides the slim foundation.

**New package structure:**

```
TimeWarp.Nuru.Core          - Slim foundation, CreateSlimBuilder(), new NuruAppBuilder()
    ↑
    ├── TimeWarp.Nuru.Logging
    ├── TimeWarp.Nuru.Completion
    ├── TimeWarp.Nuru.Repl
    ├── TimeWarp.Nuru.Telemetry
    │       ↑
    │       └── TimeWarp.Nuru   - Full package, NuruFullApp.CreateBuilder(), references all extensions
```

**User experience:**

| Package | Use Case |
|---------|----------|
| `TimeWarp.Nuru` | Default choice - batteries included, enterprise ready |
| `TimeWarp.Nuru.Core` | Minimal CLI, AOT-optimized, full control |

This follows patterns like `Microsoft.Extensions.Logging` vs `Microsoft.Extensions.Logging.Abstractions`.

## Requirements

- Rename current `TimeWarp.Nuru` to `TimeWarp.Nuru.Core`
- Create new `TimeWarp.Nuru` package that references Core + all extensions
- `NuruFullApp.CreateBuilder(args)` in new package auto-wires: `UseTelemetry()`, `AddReplSupport()`
- `NuruApp.CreateSlimBuilder(args)` in Core remains unchanged (DI, Config, AutoHelp only)
- `new NuruAppBuilder()` in Core remains empty (total user control)
- Extension packages update their references from `TimeWarp.Nuru` to `TimeWarp.Nuru.Core`
- No circular dependencies

## Checklist

### Package Restructure
- [x] Rename `Source/TimeWarp.Nuru` to `Source/TimeWarp.Nuru.Core`
- [x] Update all extension packages to reference `TimeWarp.Nuru.Core`
- [x] Create new `Source/TimeWarp.Nuru` package
- [x] New package references: Core, Logging, Telemetry, Repl, Completion
- [x] Implement `NuruFullApp.CreateBuilder()` in new package
- [x] Update solution file

### Implementation
- [x] Implement `NuruFullApp.CreateBuilder(args)` that calls `UseAllExtensions()`
- [x] `UseAllExtensions()` calls `UseTelemetry()` and `AddReplSupport()`
- [x] Ensure no circular dependencies
- [x] Verify `CreateSlimBuilder` path has zero references to extension types

### Verification
- [x] Full solution builds successfully
- [x] Test apps (Delegates and Mediator) work correctly
- [x] Samples compile and run

## Implementation Notes

**API Design Decision:**
To avoid naming conflicts with Core's `NuruApp` class, the full package provides:
- `NuruFullApp.CreateBuilder(args)` - Full featured with all extensions
- `NuruFullApp.CreateSlimBuilder(args)` - Delegates to Core's slim builder
- `NuruFullApp.CreateEmptyBuilder(args)` - Delegates to Core's empty builder
- `UseAllExtensions()` extension method on `NuruAppBuilder`

**Files Created:**
- `Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj` - Meta-package referencing all extensions
- `Source/TimeWarp.Nuru/NuruFullApp.cs` - Factory methods for full-featured builders
- `Source/TimeWarp.Nuru/NuruAppBuilderExtensions.cs` - `UseAllExtensions()` method
- `Source/TimeWarp.Nuru/GlobalUsings.cs` - Required usings for extension methods

**Files Renamed:**
- `Source/TimeWarp.Nuru/` → `Source/TimeWarp.Nuru.Core/`
- `TimeWarp.Nuru.csproj` → `TimeWarp.Nuru.Core.csproj`

**Files Updated:**
- All extension packages to reference `TimeWarp.Nuru.Core`
- All test/sample projects to reference `TimeWarp.Nuru.Core`
- `TimeWarp.Nuru.slnx` to include both projects
