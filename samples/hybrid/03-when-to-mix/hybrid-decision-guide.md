# When to Mix DSLs: Decision Guide

> ⚠️ **Warning**: Mixing Fluent DSL and Endpoint DSL should be rare. Most applications should pick one paradigm and commit to it.

## Quick Decision Tree

```
┌─────────────────────────────────────────┐
│  Are you building a NEW application?    │
└─────────────────────────────────────────┘
         │
         ├── YES → Use Endpoint DSL (recommended)
         │         • Better testability
         │         • Dependency injection support
         │         • Cleaner architecture
         │
         └── NO → Continue reading...

┌─────────────────────────────────────────┐
│  Do you need UNIT TESTABLE handlers?  │
└─────────────────────────────────────────┘
         │
         ├── YES → Use Endpoint DSL
         │
         └── NO → Continue reading...

┌─────────────────────────────────────────┐
│  Is this a SIMPLE SCRIPT or TOOL?       │
└─────────────────────────────────────────┘
         │
         ├── YES → Use Fluent DSL
         │         • Less boilerplate
         │         • Faster to write
         │         • Good for one-offs
         │
         └── NO → Continue reading...

┌─────────────────────────────────────────┐
│  Do you need COMPLEX DI or SERVICES?  │
└─────────────────────────────────────────┘
         │
         ├── YES → Use Endpoint DSL
         │
         └── NO → Either DSL works
                   (Pick one and be consistent)
```

## Valid Scenarios for Mixing

### 1. Migration Path (Temporary)
**When**: Gradually converting from Fluent to Endpoint

```csharp
// Step 1: Keep existing Fluent routes
.Map("legacy-command")
  .WithHandler(() => { /* old code */ })
  .Done()

// Step 2: Add new features as Endpoints
.DiscoverEndpoints() // New commands use [NuruRoute]
```

**Timeline**: Migrate completely, then remove Fluent routes.

### 2. Performance-Sensitive Operations
**When**: One specific route needs maximum performance

```csharp
// Most routes: Endpoint (testable, DI)
.DiscoverEndpoints()

// One critical path: Fluent (zero overhead)
.Map("hot-path")
  .WithHandler((int id) => LookupCache(id))
  .AsQuery()
  .Done()
```

**Note**: Only mix if profiling shows Endpoint overhead matters.

### 3. Unified Pipeline Demonstration
**When**: Showing that behaviors apply to all routes

See: `hybrid/02-unified-pipeline/hybrid-unified-pipeline.cs`

This is primarily for educational purposes.

## Anti-Patterns (Don't Do This)

### ❌ Random Mixing
```csharp
// DON'T: Mix without clear reason
.Map("list")      // Fluent
.DiscoverEndpoints() // Endpoints for similar operations
.Map("create")    // Back to Fluent
```

**Problem**: Inconsistent architecture confuses maintainers.

### ❌ Same Logic in Both
```csharp
// DON'T: Implement same logic twice
.Map("add", (x, y) => x + y)          // Fluent

[NuruRoute("add")]                     // Endpoint
public class AddCommand { /* same */ }
```

**Problem**: Duplication leads to bugs when one is updated.

### ❌ Mixing in Same File Without Documentation
```csharp
// DON'T: Undocumented mixing
.Map("quick")
  .WithHandler(() => { })
  .Done()

[NuruRoute("complex")]
public class ComplexCommand { }
```

**Problem**: Future developers won't understand the decision.

## Recommended Patterns

### ✅ Migration Strategy
```csharp
#region Migration: Fluent routes will be converted to Endpoints
// TODO (2026-02-15): Migrate these to [NuruRoute] classes
.Map("temp-fluent")
  .WithHandler(/*...*/)
  .Done()
#endregion

// New code uses Endpoints
.DiscoverEndpoints()
```

### ✅ Performance Optimization
```csharp
#region Performance: Fluent for hot path (measured 10% faster)
.Map("cache-lookup")
  .WithHandler((int id) => _cache[id])
  .AsQuery()
  .Done()
#endregion

// Everything else: Endpoint
.DiscoverEndpoints()
```

### ✅ Unified Pipeline Demo
```csharp
// Document this is for demonstration
// See docs: unified-pipeline.md
.AddBehavior(typeof(LoggingBehavior))
.Map("fluent-example").WithHandler(/*...*/).Done()
.DiscoverEndpoints() // Includes endpoints with same pipeline
```

## Summary

| Scenario | Use | Example |
|----------|-----|---------|
| New app | Endpoint only | `endpoints/02-calculator/` |
| Simple script | Fluent only | `fluent/01-hello-world/` |
| Migration | Hybrid (temporary) | `hybrid/01-migration/` |
| Performance hot path | Mixed (documented) | See anti-pattern notes |
| Demo/Education | Mixed (documented) | `hybrid/02-unified-pipeline/` |

**Rule of Thumb**: If you can't explain why you're mixing in one sentence, don't mix.
