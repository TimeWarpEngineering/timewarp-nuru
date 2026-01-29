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

The source generator should walk the full inheritance chain and concatenate all `[NuruRouteGroup]` prefixes, not just the immediate parent's prefix.

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