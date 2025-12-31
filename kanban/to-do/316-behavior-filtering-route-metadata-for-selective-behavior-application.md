# Behavior Filtering - Route Metadata for Selective Behavior Application

## Summary

Add route-level metadata to enable behaviors to apply selectively based on route characteristics. This enables patterns like authorization (only on certain routes) and retry (only for idempotent operations).

## Background

### Problem

The current `INuruBehavior` pattern applies behaviors globally to all routes. The Mediator-based samples use **marker interfaces** on command classes (e.g., `IRequireAuthorization`, `IRetryable`) to selectively apply behavior logic:

```csharp
// Mediator pattern - behavior has access to command instance
if (message is IRequireAuthorization auth)
{
  // check auth.RequiredPermission
}
```

In TimeWarp.Nuru, behaviors receive `BehaviorContext` which only has:
- `CommandName` (route pattern string)
- `CommandTypeName` (type name string)
- `CorrelationId`, `Stopwatch`, `CancellationToken`

Behaviors **cannot** check marker interfaces because they don't have access to the command instance.

### Blocked Samples

The following samples are blocked waiting for this feature:
- `pipeline-middleware-authorization.cs` - Needs to check if route requires permission
- `pipeline-middleware-retry.cs` - Needs to check if route is retryable
- `pipeline-middleware.cs` - Combined example with all behaviors

## Proposed Solution

Add `RouteMetadata` to `BehaviorContext` that behaviors can inspect.

### Option A: DSL Method + Attribute

**DSL (for delegate routes):**
```csharp
.Map("admin {action}")
  .WithRouteMetadata("RequirePermission", "admin:execute")
  .WithRouteMetadata("MaxRetries", 3)
  .WithHandler(...)
```

**Attribute (for attributed routes):**
```csharp
[NuruRoute("admin")]
[RouteMetadata("RequirePermission", "admin:execute")]
public class AdminCommand : ICommand<Unit> { ... }
```

**BehaviorContext:**
```csharp
public class BehaviorContext
{
  // ... existing properties ...
  public IReadOnlyDictionary<string, object> RouteMetadata { get; init; }
}
```

**Behavior usage:**
```csharp
public ValueTask OnBeforeAsync(BehaviorContext context)
{
  if (context.RouteMetadata.TryGetValue("RequirePermission", out var permission))
  {
    // Check authorization for (string)permission
  }
  return ValueTask.CompletedTask;
}
```

### Option B: Typed Metadata Attributes

More type-safe but less flexible:

```csharp
[NuruRoute("admin")]
[RequirePermission("admin:execute")]  // Strongly typed
[Retryable(MaxRetries = 3)]           // Strongly typed
public class AdminCommand : ICommand<Unit> { ... }
```

Would need corresponding DSL methods:
```csharp
.Map("admin {action}")
  .RequirePermission("admin:execute")
  .Retryable(maxRetries: 3)
```

## Checklist

### Design
- [ ] Decide on metadata approach (Option A vs B vs hybrid)
- [ ] Design `BehaviorContext.RouteMetadata` property
- [ ] Design DSL method(s) for metadata
- [ ] Design attribute(s) for metadata

### Implementation - Core
- [ ] Add `RouteMetadata` property to `BehaviorContext`
- [ ] Add `WithRouteMetadata()` to endpoint builder DSL
- [ ] Add `[RouteMetadata]` attribute (if using Option A)

### Implementation - Generator
- [ ] Extract metadata from DSL calls
- [ ] Extract metadata from attributes
- [ ] Emit metadata dictionary initialization in generated code
- [ ] Pass metadata to BehaviorContext creation

### Implementation - Samples
- [ ] Convert `pipeline-middleware-authorization.cs` to INuruBehavior
- [ ] Convert `pipeline-middleware-retry.cs` to INuruBehavior
- [ ] Convert `pipeline-middleware.cs` (combined) to INuruBehavior

### Testing
- [ ] Unit tests for metadata extraction
- [ ] Integration tests for behavior filtering
- [ ] Sample verification tests

## Related Tasks

- #315 - Implement Pipeline Behavior Code Generation (parent task, partially complete)
- #265 - Epic: V2 Source Generator Implementation

## Notes

### Why not marker interfaces?

Marker interfaces work when behaviors have access to the command instance (like Mediator). In TimeWarp.Nuru's source-generated approach:
1. Behaviors are Singleton, not per-request
2. Behaviors receive `BehaviorContext`, not the command
3. The generator doesn't pass the command instance to behaviors

Route metadata is a cleaner solution that:
- Works with both delegate and attributed routes
- Is explicit and discoverable
- Can be extended without changing behavior interfaces
- Is compile-time verifiable (generator extracts it)
