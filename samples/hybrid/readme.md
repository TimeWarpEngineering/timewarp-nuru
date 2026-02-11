# Hybrid Samples ⚠️ EDGE CASES

> **Warning**: Mixing Fluent DSL and Endpoint DSL should be rare.
> Most applications should pick one paradigm and commit to it.

## When to Use Hybrid Patterns

Valid scenarios for mixing DSLs:

1. **Migration** (Temporary)
   - Gradually converting from Fluent to Endpoint
   - See `01-migration/` for 3-step migration path

2. **Performance** (Measured)
   - One critical path needs maximum performance
   - Document the reason and measurements

3. **Education** (Demonstration)
   - Showing unified pipeline behavior
   - See `02-unified-pipeline/`

## Samples

### 01-migration/

Three-step migration path from Fluent to Endpoint DSL:

| File | Step | Description |
|------|------|-------------|
| `hybrid-migration-start-fluent.cs` | 1 | Pure Fluent DSL implementation |
| `hybrid-migration-add-endpoint.cs` | 2 | Add Endpoint patterns gradually |
| `hybrid-migration-complete.cs` | 3 | Full Endpoint DSL conversion |

**Run the progression:**
```bash
# Step 1: Pure Fluent
dotnet run samples/hybrid/01-migration/hybrid-migration-start-fluent.cs -- add 5 3

# Step 2: Add Endpoints
dotnet run samples/hybrid/01-migration/hybrid-migration-add-endpoint.cs -- factorial 5

# Step 3: Complete
dotnet run samples/hybrid/01-migration/hybrid-migration-complete.cs -- factorial 5
```

### 02-unified-pipeline/

Demonstrates that behaviors apply to ALL routes:

| File | Description |
|------|-------------|
| `hybrid-unified-pipeline.cs` | One pipeline for Fluent AND Endpoint routes |

**Key insight**: `AddBehavior()` applies uniformly - there's no separate "delegate pipeline" vs "command pipeline".

```bash
# Shows same behaviors wrapping both types
dotnet run samples/hybrid/02-unified-pipeline/hybrid-unified-pipeline.cs -- add 5 3
dotnet run samples/hybrid/02-unified-pipeline/hybrid-unified-pipeline.cs -- echo hello
```

### 03-when-to-mix/

Decision guide for mixing DSLs:

| File | Description |
|------|-------------|
| `hybrid-decision-guide.md` | When (not) to mix, with decision tree |

**Read this first** if you're considering mixing patterns.

## Anti-Patterns

### ❌ Random Mixing
```csharp
// DON'T: Mix without clear reason
.Map("list")        // Fluent
.DiscoverEndpoints() // Endpoints
.Map("create")      // Back to Fluent
```

### ❌ Same Logic in Both
```csharp
// DON'T: Implement same logic twice
.Map("add", (x, y) => x + y)     // Fluent

[NuruRoute("add")]                 // Endpoint
public class AddCommand { /* same */ }
```

### ❌ Undocumented Mixing
Always document WHY you're mixing:
```csharp
#region Migration: Converting to Endpoints (TODO 2026-03-01)
// These Fluent routes will become [NuruRoute] classes
.Map("temp")
  .WithHandler(/*...*/)
  .Done()
#endregion

.DiscoverEndpoints() // New code uses Endpoints
```

## Decision Tree

```
Are you migrating?
├── YES → Use hybrid temporarily, then convert fully
│
└── NO → Is this for performance?
    ├── YES → Measure first, then document the reason
    │
    └── NO → Pick ONE paradigm (don't mix)
```

## Rule of Thumb

> If you can't explain why you're mixing in one sentence, don't mix.

Most applications should:
- Use **Endpoint DSL** for production systems
- Use **Fluent DSL** for simple scripts
- Use **Hybrid** only temporarily during migration

## See Also

- [Decision Guide](./03-when-to-mix/hybrid-decision-guide.md) - Detailed guidance
- [Fluent DSL](../fluent/) - Simple, functional approach
- [Endpoint DSL](../endpoints/) - Recommended approach
- [Root Samples README](../) - Overview of all samples
