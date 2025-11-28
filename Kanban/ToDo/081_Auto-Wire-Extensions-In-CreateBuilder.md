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
    │       └── TimeWarp.Nuru   - Full package, CreateBuilder(), references all extensions
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
- `NuruApp.CreateBuilder(args)` in new package auto-wires: `UseTelemetry()`, `UseRepl()`, `UseCompletion()`
- `NuruApp.CreateSlimBuilder(args)` in Core remains unchanged (DI, Config, AutoHelp only)
- `new NuruAppBuilder()` in Core remains empty (total user control)
- Extension packages update their references from `TimeWarp.Nuru` to `TimeWarp.Nuru.Core`
- No circular dependencies
- Measure AOT binary sizes to verify trimming works

## Checklist

### Package Restructure
- [ ] Rename `Source/TimeWarp.Nuru` to `Source/TimeWarp.Nuru.Core`
- [ ] Update all extension packages to reference `TimeWarp.Nuru.Core`
- [ ] Create new `Source/TimeWarp.Nuru` package
- [ ] New package references: Core, Logging, Telemetry, Repl, Completion
- [ ] Move `CreateBuilder()` to new package, keep `CreateSlimBuilder()` in Core
- [ ] Update solution file and Directory.Build.props

### Implementation
- [ ] Implement `NuruApp.CreateBuilder(args)` in new package that calls all `Use*()` methods
- [ ] Ensure no circular dependencies
- [ ] Verify `CreateSlimBuilder` path has zero references to extension types

### Verification
- [ ] Build AOT binary using `CreateBuilder()` from `TimeWarp.Nuru` - measure size
- [ ] Build AOT binary using `CreateSlimBuilder()` from `TimeWarp.Nuru.Core` - measure size
- [ ] Verify telemetry works in CreateBuilder app
- [ ] Verify extensions are absent in Core-only app

### Documentation
- [ ] Update CLAUDE.md with new package structure
- [ ] Update README.md with package guidance
- [ ] Update samples to reference appropriate package

## Notes

This avoids circular dependencies by having the "full" package at the top of the dependency tree, referencing everything below it. Users naturally reach for `TimeWarp.Nuru` and get the full experience. Power users who want granular control use `TimeWarp.Nuru.Core`.
