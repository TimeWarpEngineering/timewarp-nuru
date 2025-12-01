# Hard Deprecate Public NuruAppBuilder Constructors

## Description

Change the public constructors of `NuruAppBuilder` and `NuruCoreAppBuilder` to `internal`. This prevents users and AI agents from using `new NuruAppBuilder()` directly, forcing them to use the factory methods (`NuruApp.CreateBuilder()`, `NuruCoreApp.CreateSlimBuilder()`).

## Parent

MCP Builder Pattern Guidance Analysis - standardizing samples to prevent AI confusion

## Requirements

- Change `public NuruAppBuilder()` to `internal NuruAppBuilder()`
- Change `public NuruCoreAppBuilder()` to `internal NuruCoreAppBuilder()`
- Verify tests still work via `InternalsVisibleTo`
- Ensure factory methods remain the only public API

## Checklist

### Implementation
- [ ] Update `source/timewarp-nuru/nuru-app-builder.cs` line 18: Change `public NuruAppBuilder()` to `internal NuruAppBuilder()`
- [ ] Update `source/timewarp-nuru-core/nuru-core-app-builder.factory.cs` line 40: Change `public NuruCoreAppBuilder()` to `internal NuruCoreAppBuilder()`
- [ ] Verify `InternalsVisibleTo` is configured for test assemblies (check `timewarp-nuru-repl/internals-visible-to.g.cs`)
- [ ] Run tests to ensure they still work with internal constructors
- [ ] Update any documentation that references direct constructor usage
- [ ] Verify samples compile (they should be updated first via tasks 203-210)

## Notes

**Breaking Change Warning:** This is a breaking change for users who directly instantiate builders.

**Why this is safe:**
- Tests already have `InternalsVisibleTo` configured (100+ test assemblies listed in `timewarp-nuru-repl/internals-visible-to.g.cs`)
- Factory methods provide all necessary functionality
- This aligns with ASP.NET Core patterns (`WebApplication.CreateBuilder()`)

**Migration path for users:**
```csharp
// OLD (will no longer compile):
var builder = new NuruAppBuilder();

// NEW (use factory methods):
var builder = NuruApp.CreateBuilder(args);        // Full features
var builder = NuruCoreApp.CreateSlimBuilder(args); // Minimal features
```

Files to update:
- `source/timewarp-nuru/nuru-app-builder.cs` (line 18)
- `source/timewarp-nuru-core/nuru-core-app-builder.factory.cs` (line 40)

Reference analysis: `.agent/workspace/2025-12-01T21-15-00_mcp-builder-pattern-guidance-analysis.md`
