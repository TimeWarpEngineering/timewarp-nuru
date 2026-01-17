# NuruCoreApp vs NuruApp Duplication Analysis

**Date:** 2026-01-17  
**Author:** Claude Code Analysis

## Executive Summary

`NuruCoreApp` and `NuruApp` represent a confusing and largely redundant abstraction. **All actual usage in the codebase uses `NuruApp.CreateBuilder()`** exclusively. The only substantive difference is telemetry flushing, which could be implemented as a behavior or extension method. The `NuruCoreApp.CreateBuilder()` method referenced in documentation **does not exist** and was never implemented—it was documented as a planned "slim builder" feature that was abandoned (confirmed by kanban task #341).

## Scope

Analyzed the following files and patterns:
- `source/timewarp-nuru/nuru-core-app.cs` - Base application class (58 lines)
- `source/timewarp-nuru/nuru-app.cs` - Full-featured subclass (74 lines)
- Builder hierarchy: `NuruCoreAppBuilder<TSelf>`, `NuruAppBuilder`
- Options: `NuruCoreApplicationOptions`, `NuruAppOptions`
- All tests, samples, and documentation for usage patterns

## Methodology

- Read both application class implementations
- Analyzed builder hierarchy and factory methods
- Searched codebase for all `CreateBuilder` usage patterns
- Reviewed historical kanban tasks for context on architectural decisions
- Examined telemetry flushing mechanism

## Findings

### 1. Only `NuruApp.CreateBuilder()` Exists

The factory method analysis reveals:

```csharp
// Only CreateBuilder method found in codebase:
public static NuruAppBuilder CreateBuilder(string[] args, NuruCoreApplicationOptions? options = null)
public static NuruAppBuilder CreateBuilder(string[] args, NuruAppOptions? nuruAppOptions, NuruCoreApplicationOptions? coreOptions = null)
```

**`NuruCoreApp.CreateBuilder()` does not exist.** This was referenced in:
- Documentation comments in `nuru-app-extensions.cs` (line 187)
- Sample files: `_shell-completion-example/overview.md`, `_dynamic-completion-example/dynamic-completion-example.cs`
- Historical kanban task #341 confirms this was never implemented

### 2. All Usage Is Via `NuruApp.CreateBuilder()`

Search of 100+ matches shows **100% of actual usage** follows this pattern:

```csharp
// Tests (all use this pattern)
NuruCoreApp app = NuruApp.CreateBuilder([])
NuruCoreApp app = NuruApp.CreateBuilder(args, nuruAppOptions)

// Samples
NuruCoreApp app = NuruApp.CreateBuilder(args)
NuruCoreApp app = NuruApp.CreateBuilder(args, new NuruAppOptions { ... })

// Documentation
NuruCoreApp app = NuruApp.CreateBuilder(args)
```

### 3. Minimal Runtime Difference

**`NuruCoreApp`** (base class):
- Has `ITerminal`, `ReplOptions?`, `LoggerFactory?` properties
- `RunAsync(string[] args)` - throws (intercepted by source generator)
- `RunReplAsync(CancellationToken)` - throws (intercepted by source generator)

**`NuruApp`** (subclass):
- Inherits all properties/methods from `NuruCoreApp`
- **Only difference** - overrides `RunAsync()`:

```csharp
public new async Task<int> RunAsync(string[] args)
{
  int exitCode = await base.RunAsync(args).ConfigureAwait(false);
  await NuruTelemetryExtensions.FlushAsync(delayMs: 0).ConfigureAwait(false);
  return exitCode;
}
```

### 4. Telemetry Flushing Is Also Handled Elsewhere

The telemetry flushing also occurs in `TelemetryBehavior`:

```csharp
// source/timewarp-nuru/telemetry/telemetry-behavior.cs
await NuruTelemetryExtensions.FlushAsync().ConfigureAwait(false);
```

This means telemetry flush happens twice if:
1. A behavior is registered (common case)
2. Using `NuruApp` (calls FlushAsync again)

### 5. Builder Returns `NuruAppBuilder` Regardless

Both factory methods return `NuruAppBuilder`, not a "core" builder:

```csharp
public static NuruAppBuilder CreateBuilder(string[] args, ...) => new NuruAppBuilder(...);
```

### 6. NuruAppBuilder Inherits From NuruCoreAppBuilder

The builder hierarchy:
- `NuruCoreAppBuilder<TSelf>` - 112 lines, core features
- `NuruAppBuilder : NuruCoreAppBuilder<NuruAppBuilder>` - 130 lines, adds `IHostApplicationBuilder`

## Recommendations

### Priority 1: Consolidate to Single App Class

**Replace `NuruApp` with a telemetry-flushing behavior or extension method.**

Options:

**Option A: Remove `NuruApp` entirely, add `RunAsync` extension method**

```csharp
// New extension in NuruTelemetryExtensions
public static async Task<int> RunWithTelemetryFlushAsync(this NuruCoreApp app, string[] args)
{
  int exitCode = await app.RunAsync(args).ConfigureAwait(false);
  await FlushAsync(delayMs: 0).ConfigureAwait(false);
  return exitCode;
}

// Usage:
await NuruApp.CreateBuilder(args).Map(...).Build()
    .RunWithTelemetryFlushAsync(args);
```

**Option B: Merge telemetry flush into `NuruCoreApp.RunAsync()` via behavior**

If telemetry is configured, the behavior already flushes. The explicit flush in `NuruApp` may be redundant.

### Priority 2: Update Documentation

The following files contain references to non-existent `NuruCoreApp.CreateBuilder()`:

| File | Line | Issue |
|------|------|-------|
| `source/timewarp-nuru-repl-reference-only/nuru-app-extensions.cs` | 187 | Example shows `NuruCoreApp.CreateBuilder()` |
| `samples/_shell-completion-example/overview.md` | 22 | Code example uses `NuruCoreApp.CreateBuilder(args)` |
| `samples/_dynamic-completion-example/dynamic-completion-example.cs` | 26 | Code example uses `NuruCoreApp.CreateBuilder(args)` |

### Priority 3: Rename for Clarity

If keeping both classes, rename for clarity:

- `NuruCoreApp` → `NuruApp` (the actual entry point)
- `NuruApp` → `NuruAppWithTelemetry` or `NuruFullFeaturedApp`

However, this creates breaking changes and may not be worth the effort given Option A is cleaner.

### Priority 4: Remove Redundant Flush Call

If `TelemetryBehavior` already calls `FlushAsync()`, the explicit call in `NuruApp.RunAsync()` is redundant. Investigate whether both calls are necessary:

```csharp
// Current: NuruApp.RunAsync() calls FlushAsync
// TelemetryBehavior also calls FlushAsync

// This could cause double-flush or ordering issues
```

## Action Items

1. **Remove `NuruApp` class** - Consolidate into single app class with extension method for telemetry flushing
2. **Update documentation examples** - Replace `NuruCoreApp.CreateBuilder()` with `NuruApp.CreateBuilder()` in all doc comments
3. **Audit telemetry flushing** - Verify if double-flush occurs and eliminate redundancy
4. **Update source generator** - Ensure it handles single entry point correctly

## References

- `source/timewarp-nuru/nuru-core-app.cs` - Base class implementation
- `source/timewarp-nuru/nuru-app.cs` - Subclass implementation  
- `source/timewarp-nuru/telemetry/nuru-telemetry-extensions.cs` - Telemetry flushing
- `source/timewarp-nuru/telemetry/telemetry-behavior.cs` - Behavior-based telemetry
- `kanban/done/341-remove-all-createslimbuilder-and-createemptybuilder-references-from-codebase.md` - Historical context on abandoned slim builder concept
- `kanban/done/071-implement-static-factory-builder-api.md` - Original factory method design
