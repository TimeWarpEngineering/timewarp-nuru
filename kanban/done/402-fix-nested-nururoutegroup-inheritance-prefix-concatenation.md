# Fix nested NuruRouteGroup inheritance prefix concatenation

## Description

GitHub Issue #160: When using nested `[NuruRouteGroup]` attributes through class inheritance, the route prefixes are not properly concatenated. Only the immediate parent group's prefix is applied, not the full chain.

**Expected command:** `dev ccc1-demo queue peek`
**Actual command:** `dev queue peek`

### Example Hierarchy

```csharp
[NuruRouteGroup("ccc1-demo")]
public abstract class Ccc1DemoGroup { }

[NuruRouteGroup("queue")]
public abstract class QueueGroup : Ccc1DemoGroup { }

[NuruRoute("peek", Description = "View messages in queue")]
public sealed class PeekEndpoint : QueueGroup, IQuery<Unit> { ... }
```

## Checklist

- [ ] Locate the source generator that processes `[NuruRouteGroup]` attributes
- [ ] Identify where the inheritance chain is being walked (or where it should be walked)
- [ ] Modify the code to traverse the full inheritance hierarchy and concatenate all `[NuruRouteGroup]` prefixes
- [ ] Write/update unit tests to verify nested route group prefix concatenation
- [ ] Verify the fix works with the `crunchit` repository's dev-cli (if accessible)
- [ ] Ensure backward compatibility with non-nested route groups

## Notes

# Implementation Plan: Fix Nested NuruRouteGroup Inheritance Prefix Concatenation

## Problem Summary

**Issue**: GitHub #160 - When using nested `[NuruRouteGroup]` attributes through class inheritance, only the immediate parent's prefix is applied.

**Expected command**: `dev ccc1-demo queue peek`
**Actual command**: `dev queue peek`

### Current Broken Behavior

```csharp
[NuruRouteGroup("ccc1-demo")]
public abstract class Ccc1DemoGroup { }

[NuruRouteGroup("queue")]
public abstract class QueueGroup : Ccc1DemoGroup { }

[NuruRoute("peek", Description = "View messages in queue")]
public sealed class PeekEndpoint : QueueGroup, IQuery<Unit> { ... }

// Currently generates: "queue peek"
// Should generate: "ccc1-demo queue peek"
```

## Root Cause

In `source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs`, lines 147-177, the `ExtractGroupPrefix` method only checks the immediate parent's `BaseType`:

```csharp
// Check base type for [NuruRouteGroup] attribute (line 161-174)
foreach (AttributeData attribute in classSymbol.BaseType.GetAttributes())
{
  if (attributeName != NuruRouteGroupAttributeName) continue;
  if (attribute.ConstructorArguments[0].Value is string prefix)
    return prefix; // Early return - doesn't walk full chain!
}
return null;
```

## Implementation Details

### Change Location

**File**: `source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs`
**Method**: `ExtractGroupPrefix` (lines 147-177)

### Algorithm Requirements

1. **Walk inheritance chain**: Start from `classSymbol.BaseType` and follow `BaseType` until reaching `object` or `System.Type`
2. **Collect all prefixes**: Gather each `[NuruRouteGroup]` prefix found, from root (grandparent) to leaf (immediate parent)
3. **Concatenate with space**: Join prefixes as `"ccc1-demo queue"` (root-to-leaf order)
4. **Preserve backward compatibility**: Single-level inheritance continues to work as before

### Pseudocode for Fix

```csharp
private static string? ExtractGroupPrefix(...)
{
  List<string> prefixes = [];
  INamedTypeSymbol? current = classSymbol.BaseType;
  
  while (current != null && current.SpecialType != SpecialType.System_Object)
  {
    foreach (AttributeData attr in current.GetAttributes())
    {
      if (attr.AttributeClass?.Name == "NuruRouteGroupAttribute")
      {
        string? prefix = attr.ConstructorArguments[0].Value as string;
        if (!string.IsNullOrEmpty(prefix))
          prefixes.Insert(0, prefix); // Insert at front for root-to-leaf order
      }
    }
    current = current.BaseType;
  }
  
  return prefixes.Count > 0 ? string.Join(" ", prefixes) : null;
}
```

### Key Design Decisions

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| **Delimiter** | Space-separated | Matches verbal command structure |
| **Order** | Root-to-leaf (grandparent first) | Natural reading order |
| **Duplicate handling** | Allow duplicates | User may intentionally have same prefix |
| **Max depth** | No limit (use `BaseType` chain) | Follows .NET inheritance |
| **Edge cases** | No special handling | Assume valid input |

## Test Plan

### Test File Location

Create new test file or add to existing: `tests/timewarp-nuru-analyzers-tests/auto/endpoint-generator-01-basic.cs` (extend existing tests)

### Test Cases

1. **Two-level nesting** (current capability)
   ```csharp
   [NuruRouteGroup("parent")]
   public abstract class ParentGroup { }
   
   [NuruRoute("child")]
   public sealed class TwoLevelChild : ParentGroup { }
   // Expect: "parent child"
   ```

2. **Three-level nesting** (the bug scenario)
   ```csharp
   [NuruRouteGroup("ccc1-demo")]
   public abstract class Ccc1DemoGroup { }
   
   [NuruRouteGroup("queue")]
   public abstract class QueueGroup : Ccc1DemoGroup { }
   
   [NuruRoute("peek")]
   public sealed class PeekEndpoint : QueueGroup { }
   // Expect: "ccc1-demo queue peek"
   ```

3. **Four-level nesting**
   ```csharp
   [NuruRouteGroup("level1")]
   [NuruRouteGroup("level2")]
   [NuruRouteGroup("level3")]
   public abstract class Level3Group : Level2Group { }
   // Expect: "level1 level2 level3"
   ```

4. **Backward compatibility** (no regression)
   ```csharp
   [NuruRouteGroup("single")]
   public abstract class SingleGroup { }
   
   [NuruRoute("cmd")]
   public sealed class SingleChild : SingleGroup { }
   // Expect: "single cmd" (same as before)
   ```

5. **Mixed inheritance** (some parents have groups, some don't)
   ```csharp
   [NuruRouteGroup("a")]
   public abstract class A { }
   
   public abstract class B : A { } // No group on B
   
   [NuruRoute("c")]
   public sealed class C : B { }
   // Expect: "a c" (skips B since it has no group)
   ```

### Test Assertion Pattern

```csharp
RegisteredRoute? route = NuruRouteRegistry.RegisteredRoutes
  .FirstOrDefault(r => r.RequestType == typeof(PeekEndpoint));

string expectedPattern = "ccc1-demo queue peek";
route?.Pattern.ShouldBe(expectedPattern);
```

## Files Modified

| File | Change |
|------|--------|
| `source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs` | Modify `ExtractGroupPrefix` method |
| `tests/timewarp-nuru-analyzers-tests/auto/endpoint-generator-01-basic.cs` | Add nested inheritance tests |
| `samples/03-endpoints/messages/nested-groups/nested-group-example.cs` | New sample demonstrating nested groups |
| `samples/examples.json` | Add nested-route-groups entry |

## Build and Verification

```bash
# Run tests
dotnet run tests/timewarp-nuru-analyzers-tests/auto/endpoint-generator-01-basic.cs

# Run full CI tests (optional - catch regressions)
dotnet run tests/ci-tests/run-ci-tests.cs
```

## Risk Assessment

| Risk | Mitigation |
|------|------------|
| **Breaking change** | No - only fixes bugs; existing single-level works same |
| **Performance** | Minimal - walks same inheritance chain compiler already built |
| **Test coverage** | Add comprehensive nested tests |

## Open Questions

None - all requirements clarified with user:

- ✓ Delimiter: Space-separated
- ✓ Order: Root-to-leaf (grandparent first)  
- ✓ Duplicates: Allow
- ✓ Max depth: No limit
- ✓ Edge cases: Assume valid input

## Sample Added

Created `samples/03-endpoints/messages/nested-groups/nested-group-example.cs` demonstrating:
- 3-level nesting: `cloud azure storage upload`
- 2-level nesting: `cloud azure vm start`
- Added to `samples/examples.json` as "nested-route-groups" example

---

### Related Files (from issue)

- `tools/dev-cli/commands/ccc1-demo/Ccc1DemoGroup.cs` - Parent group with `[NuruRouteGroup("ccc1-demo")]`
- `tools/dev-cli/commands/ccc1-demo/queue/QueueGroup.cs` - Nested group with `[NuruRouteGroup("queue")]` inheriting from `Ccc1DemoGroup`
- `tools/dev-cli/commands/ccc1-demo/queue/PeekEndpoint.cs` - Endpoint with `[NuruRoute("peek")]` inheriting from `QueueGroup`

### Real-World Use Case

Building a `dev` CLI with grouped commands like:
```
dev ccc1-demo build      # Works - direct child of Ccc1DemoGroup
dev ccc1-demo queue peek # BROKEN - nested group
```

Similar to `docker compose up` or `kubectl get pods` patterns.

## Results

[Document outcomes after completion]