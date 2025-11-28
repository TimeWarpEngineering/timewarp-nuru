# Auto-Wire Extensions in CreateBuilder

## Description

Update `NuruApp.CreateBuilder()` to automatically include all extension packages (Telemetry, Logging, Repl, Completion) following ASP.NET Core's pattern. Users who want the full enterprise experience get batteries-included defaults, while `CreateSlimBuilder()` and `new NuruAppBuilder()` remain lean for minimal CLIs.

The AOT trimmer will remove unused code paths, so users of `CreateSlimBuilder()` won't pay the binary size cost for features they don't use.

## Requirements

- `CreateBuilder(args)` auto-wires: `UseTelemetry()`, `UseRepl()`, `UseCompletion()`, `UseLogging()`
- `CreateSlimBuilder(args)` remains unchanged (DI, Config, AutoHelp only - no extensions)
- `new NuruAppBuilder()` remains empty (total user control)
- AOT trimming must work correctly - unused extensions should be trimmed from slim builds
- No reflection or assembly scanning - explicit code paths only
- Measure AOT binary sizes before/after to verify trimming works

## Checklist

### Implementation
- [ ] Add project references from core to extension packages (Telemetry, Logging, Repl, Completion)
- [ ] Update `InitializeForMode(BuilderMode.Full)` to call extension methods
- [ ] Ensure extension methods have `[RequiresUnreferencedCode]` attributes if needed for trim warnings
- [ ] Verify `CreateSlimBuilder` path has zero references to extension types

### Verification
- [ ] Build AOT binary using `CreateBuilder()` - measure size
- [ ] Build AOT binary using `CreateSlimBuilder()` - measure size (should be smaller)
- [ ] Verify telemetry works in CreateBuilder app
- [ ] Verify telemetry is absent in CreateSlimBuilder app

### Documentation
- [ ] Update CLAUDE.md with builder mode differences
- [ ] Update samples to show appropriate builder for use case

## Notes

This follows ASP.NET Core's pattern where:
- `WebApplication.CreateBuilder()` includes auth, routing, logging, etc.
- `WebApplication.CreateSlimBuilder()` is minimal
- `WebApplication.CreateEmptyBuilder()` is bare bones

The trimmer removes unreachable code, so slim builds stay slim even though all code exists in the same package.

Reference discussion: Telemetry adds ~2.5MB in dependencies, but AOT trimmer removes this if the code path isn't used.
