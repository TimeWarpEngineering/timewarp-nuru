# Fix source generator: endpoint handler-invoker references __command when no behaviors apply

## Description

Endpoint samples fail to compile with error: `CS0103: The name '__command' does not exist`

**Affected samples:**
- `samples/endpoints/05-pipeline/filtered-auth/` - Command path with no applicable behaviors
- `samples/endpoints/05-pipeline/retry/` - Command path with no applicable behaviors

## Root Cause Analysis

### Key Discovery: Fluent ≠ Endpoint Code Paths

The fluent DSL and endpoint DSL use **different code paths** for handler invocation:

**Fluent** (`Map()` + `.Implements<T>()`):
- Uses delegate handlers that inline lambdas directly
- Generated code: `void __handler_0(string message) => WriteLine(...); __handler_0(message);`
- Never references `__command`

**Endpoint** (`[NuruRoute]` attributes):
- Uses Command handlers that unconditionally emit `__handler.Handle(__command, ...)`
- Generated code references `__command` variable

This is NOT a bug — it's an architectural difference. The issue is that `__command` variable is not always created.

### Bug 1: `__command` not created when no behaviors apply

**Location:** `behavior-emitter.cs` lines 77-85

```csharp
// behavior-emitter.cs lines 77-85
if (applicableBehaviors.Length == 0)
{
  emitHandler();  // BUG: emits __handler.Handle(__command, ...) but __command doesn't exist!
  return;
}
```

**Problem:** When `FilterBehaviorsForRoute()` returns zero applicable behaviors, the code skips `EmitCommandCreation()` entirely and calls `emitHandler()` directly. But `handler-invoker-emitter.cs` unconditionally emits `__handler.Handle(__command, ...)`.

**Why fluent works:** Fluent delegate handlers inline the lambda directly and never reference `__command`.

### Bug 2: Option variable naming mismatch

**Location:** `behavior-emitter.cs` lines 182-202 (Command path)

The Command path in `EmitCommandCreation` only emits route parameters, not options. When options are present, the variable name doesn't match what was created by the route-matcher.

**Example from filtered-auth:**
```csharp
bool admin = false;      // route-matcher creates variable named "admin"
...
IsAdmin = isadmin,       // expects "isadmin" (camelCase of property), but gets "admin"
```

## Reproduction Commands

```bash
# Endpoint (fails):
dotnet build samples/endpoints/05-pipeline/filtered-auth/filtered-auth.cs
# Error: CS0103: The name '__command' does not exist

# Fluent (works):
dotnet build samples/fluent/05-pipeline/fluent-pipeline-filtered-auth.cs
```

## Generated Code Comparison

**Fluent `echo` route (no behaviors - compiles):**
```csharp
void __handler_0(string message) => WriteLine($"Echo: {message}");
__handler_0(message);
```

**Endpoint `list` route (no behaviors - broken):**
```csharp
global::...ListQuery.Handler __handler = new();
string[] result = await __handler.Handle(__command, ...);  // __command doesn't exist!
```

## Files to Modify

1. `source/timewarp-nuru-analyzers/generators/emitters/behavior-emitter.cs`
   - Fix Bug 1: Still emit `__command` for endpoint routes even when no behaviors apply
   - Fix Bug 2: Add option binding to Command path in `EmitCommandCreation`

2. `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs`
   - May need adjustment for consistency

## Fix Approach

### Fix 1: Always emit `__command` for endpoint routes

In `behavior-emitter.cs`, distinguish between:
- **Delegate handler** (fluent): No `__command` needed, inline the lambda
- **Command handler** (endpoint): Always emit `__command`, even with no behaviors

```csharp
// Pseudocode fix:
if (applicableBehaviors.Length == 0)
{
  if (isEndpointRoute)  // NEW: Check if this is an endpoint (Command) route
  {
    // Still need __command for endpoint routes
    EmitCommandCreation(route, options);
    emitHandler();
  }
  else
  {
    // Fluent route: inline delegate directly
    emitHandler();
  }
  return;
}
```

### Fix 2: Add option binding to Command path

Ensure `EmitCommandCreation` also emits option variable assignments:
- Match the variable names created by route-matcher (camelCase of property name)
- Or use consistent naming throughout

## Verification

After fix:
```bash
dotnet build samples/endpoints/05-pipeline/filtered-auth/filtered-auth.cs
# Should compile successfully

dotnet build samples/endpoints/05-pipeline/retry/retry.cs
# Should compile successfully
```

## Related Files

- `samples/endpoints/05-pipeline/filtered-auth/filtered-auth.cs` - Failing endpoint sample
- `samples/fluent/05-pipeline/fluent-pipeline-filtered-auth.cs` - Working fluent equivalent
- `samples/endpoints/05-pipeline/retry/retry.cs` - Failing endpoint sample

## Notes

This is a DiscoverEndpoints code path issue. The `Map()` fluent API doesn't have this problem because it uses a different code generation path that doesn't reference `__command`.

The fix must maintain the architectural difference between fluent and endpoint paths while ensuring endpoint routes always have their `__command` variable properly defined.

## Checklist

- [ ] Fix Bug 1: Emit `__command` for endpoint routes even when no behaviors apply
- [ ] Fix Bug 2: Add option variable binding to Command path
- [ ] Verify `samples/endpoints/05-pipeline/filtered-auth/filtered-auth.cs` compiles
- [ ] Verify `samples/endpoints/05-pipeline/retry/retry.cs` compiles
- [ ] Run full endpoint sample verification: `dev verify-samples --category endpoints`
- [ ] All 29 endpoint samples should pass
