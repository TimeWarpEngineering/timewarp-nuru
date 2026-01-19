# #293-002: Convert GroupEndpointBuilder Methods to No-Ops

## Parent

#293 Make DSL Builder Methods No-Ops

## Description

Convert `GroupEndpointBuilder<TGroupParent>` methods from doing runtime work to no-ops. This is nearly identical to #293-001 but for the group-specific endpoint builder.

**Key change:** Remove the `_endpoint` field entirely.

## File

`source/timewarp-nuru-core/builders/group-endpoint-builder.cs`

## Checklist

### Constructor Changes
- [ ] Remove `Endpoint endpoint` parameter from constructor
- [ ] Remove `_endpoint` private field
- [ ] Keep only `_groupBuilder` reference for `Done()` navigation

### Method Conversions
- [ ] `WithHandler(Delegate)` → return `this` (line 45-51)
- [ ] `WithDescription(string)` → return `this` (line 58-62)
- [ ] `AsQuery()` → return `this` (line 68-73)
- [ ] `AsCommand()` → return `this` (line 79-84)
- [ ] `AsIdempotentCommand()` → return `this` (line 90-95)
- [ ] `Done()` → keep as-is (already just returns `_groupBuilder`)

### Cleanup
- [ ] Add comment explaining source gen handles actual work
- [ ] Remove unused `using` statements if any

## Current Implementation

```csharp
public class GroupEndpointBuilder<TGroupParent>(
  GroupBuilder<TGroupParent> groupBuilder,
  Endpoint endpoint)
  where TGroupParent : class
{
  private readonly GroupBuilder<TGroupParent> _groupBuilder = groupBuilder;
  private readonly Endpoint _endpoint = endpoint;

  public GroupEndpointBuilder<TGroupParent> WithHandler(Delegate handler)
  {
    ArgumentNullException.ThrowIfNull(handler);
    _endpoint.Handler = handler;
    _endpoint.Method = handler.Method;
    return this;
  }
  // ...
}
```

## Target Implementation

```csharp
/// <summary>
/// Fluent builder for configuring endpoints within a group.
/// This is a compile-time DSL shell - the source generator extracts configuration
/// at build time. Runtime methods are no-ops that maintain the fluent chain.
/// </summary>
public class GroupEndpointBuilder<TGroupParent>(GroupBuilder<TGroupParent> groupBuilder)
  where TGroupParent : class
{
  private readonly GroupBuilder<TGroupParent> _groupBuilder = groupBuilder;

  public GroupEndpointBuilder<TGroupParent> WithHandler(Delegate handler)
  {
    // Source generator extracts handler at compile time
    _ = handler;
    return this;
  }

  public GroupEndpointBuilder<TGroupParent> WithDescription(string description)
  {
    // Source generator extracts description at compile time
    _ = description;
    return this;
  }

  public GroupEndpointBuilder<TGroupParent> AsQuery() => this;
  public GroupEndpointBuilder<TGroupParent> AsCommand() => this;
  public GroupEndpointBuilder<TGroupParent> AsIdempotentCommand() => this;

  public GroupBuilder<TGroupParent> Done() => _groupBuilder;
}
```

## Breaking Change

Constructor signature changes from `GroupEndpointBuilder(GroupBuilder<T>, Endpoint)` to `GroupEndpointBuilder(GroupBuilder<T>)`.

**Impact:** Only affects `GroupBuilder.Map()` which creates `GroupEndpointBuilder` instances. Will be updated in #293-004.

**Workaround:** Same as #293-001 - temporary constructor overload.

## Notes

- Very similar to #293-001 - same pattern, different class
- Can potentially be done in parallel with #293-001
