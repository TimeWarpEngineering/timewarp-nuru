# Fix Fluent API Breakage with IBuilder Pattern

## Description

Task 156 (MessageType metadata) changed `Map()` to return `RouteConfigurator` instead of `TBuilder`, breaking fluent API chaining with extension methods like `AddReplSupport()`, `EnableStaticCompletion()`, etc.

This task implements a consistent fluent builder pattern using `IBuilder<TParent>` from `TimeWarp.FluentBuilder` that can be reused across all TimeWarp libraries.

## Impact

- **Developer Experience**: Significant - fluent API is a core part of Nuru's usability
- **Consistency**: Establishes pattern for all TimeWarp libraries
- **Tooling**: Leverages `timewarp-fluent-builder-cli` which we control and can adjust

## Parent

148-nuru-3-unified-route-pipeline

## Background

**Before (worked):**
```csharp
new NuruAppBuilder()
    .Map("status", handler)
    .Map("version", handler)
    .AddReplSupport()
    .Build();
```

**After Task 156 (broken):**
```csharp
new NuruAppBuilder()
    .Map("status", handler)      // Returns RouteConfigurator, not builder
    .AddReplSupport()            // ERROR: RouteConfigurator doesn't have this
    .Build();
```

## Solution

Use `IBuilder<TParent>` pattern from `TimeWarp.FluentBuilder`:

```csharp
public interface IBuilder<out TParent> where TParent : class
{
    TParent Done();  // Returns to parent with correct type preserved
}

// RouteConfigurator implements IBuilder<TBuilder>
app.Map("status", handler)      // Returns RouteConfigurator<NuruAppBuilder>
   .AsQuery()                   // Returns RouteConfigurator<NuruAppBuilder>
   .Done()                      // Returns NuruAppBuilder (preserves derived type!)
   .AddReplSupport()            // Works!
   .Build();
```

## Checklist

### Analysis (Complete)
- [x] Document fluent API pattern options (see `.agent/workspace/2025-12-16T00-45-00_fluent-api-pattern-analysis.md`)
- [x] Evaluate `IBuilder<TParent>` pattern from `TimeWarp.FluentBuilder`
- [x] Test `fluent-builder` CLI with Factory pattern for `CompiledRoute`

### Implementation - Infrastructure
- [ ] Add `TimeWarp.FluentBuilder` project reference to `timewarp-nuru-core`
- [ ] Or generate standalone `IBuilder<TParent>` and `ScopeExtensions` with `--standalone`
- [ ] Decide: reference vs standalone (tradeoffs documented in analysis)

### Implementation - RouteConfigurator
- [ ] Make `RouteConfigurator` generic: `RouteConfigurator<TBuilder>`
- [ ] Implement `IBuilder<TBuilder>` on `RouteConfigurator<TBuilder>`
- [ ] Update `Map()` methods to return `RouteConfigurator<TBuilder>`
- [ ] Ensure `Done()` returns correctly-typed `TBuilder`
- [ ] Add `Also()` extension method for inline configuration (from `ScopeExtensions`)

### Implementation - CompiledRouteBuilder (Optional)
- [ ] Evaluate using Factory pattern from `fluent-builder` CLI
- [ ] If beneficial, generate base with `[FluentBuilder(Pattern = BuilderPattern.Factory)]`
- [ ] Keep custom methods (`WithLiteral`, `WithOption`, etc.) in partial class

### Remove Workarounds
- [ ] Remove `RouteConfigurator` overloads from `timewarp-nuru-repl/nuru-app-extensions.cs`
- [ ] Remove `RouteConfigurator` overloads from `timewarp-nuru-completion/nuru-app-builder-extensions.cs`
- [ ] Revert test changes that worked around the breakage

### Testing
- [ ] Verify fluent chaining works: `Map().Done().AddReplSupport().Build()`
- [ ] Verify `Also()` pattern works: `Map().Also(r => r.AsQuery()).AddReplSupport()`
- [ ] Run full test suite
- [ ] Update/add tests for `IBuilder<TParent>` pattern

### Documentation
- [ ] Update user documentation for `Done()` pattern
- [ ] Document `Also()` as alternative for inline configuration
- [ ] Add examples to samples

## Notes

### Analysis Document

Full analysis of 10 pattern options at:
`.agent/workspace/2025-12-16T00-45-00_fluent-api-pattern-analysis.md`

### FluentBuilder CLI

The `timewarp-fluent-builder-cli` tool now supports:
- `BuilderPattern.Mutating` (default) - mutates existing instance
- `BuilderPattern.Factory` - accumulates state, creates at `Build()`

Factory pattern generates:
- Private fields for each property
- `WithX()` methods
- `AddX()` methods for collections
- `Build()` with object initializer

### Key Files

- `TimeWarp.FluentBuilder.IBuilder<TParent>` - interface with `Done()`
- `TimeWarp.FluentBuilder.ScopeExtensions` - `Also()`, `Apply()`, `Let()`, `Run()`
- `TimeWarp.FluentBuilder.BaseBuilder<TSelf, TBuilt>` - for mutating pattern

### Test Results Before Fix

```
Total: 156/158 tests passed (98.7%)
Failed:
  - Repl/repl-35-interactive-route-execution.cs
  - Repl/repl-23-key-binding-profiles.cs
```
