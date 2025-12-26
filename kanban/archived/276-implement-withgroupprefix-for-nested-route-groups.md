# Implement WithGroupPrefix for Nested Route Groups

## Status: SUPERSEDED

**This task is superseded by #277 (Semantic DSL Interpreter).**

The runtime DSL builders (`GroupBuilder`, `GroupEndpointBuilder`) and `WithGroupPrefix()` method were implemented, but the generator still can't correctly extract nested group prefixes using syntax walking.

The solution is the semantic interpreter approach in #277, which will:
1. Mirror the DSL structure in IR builders
2. Use Roslyn's semantic model to "interpret" the DSL
3. Naturally handle prefix accumulation because the IR builders work the same way as the DSL builders

Once #277 is complete, this task's goal (nested groups working end-to-end) will be achieved.

---

## Original Description

Implement the `WithGroupPrefix()` method to enable nested route groups in the fluent DSL. This is a gap - the method is used in `dsl-example.cs` but doesn't exist in the builder.

## Parent

#272 V2 Generator Phase 6: Testing

## Superseded By

#277 Epic: Semantic DSL Interpreter with Mirrored IR Builders

## Blocks

#272 Commit 6.3 - Nested groups test

## Problem

The DSL design doc and `dsl-example.cs` show:

```csharp
.WithGroupPrefix("admin")
  .Map("restart")
    .WithHandler(() => "restarting...")
    .Done()
  .WithGroupPrefix("config")
    .Map("get {key}")
      .WithHandler((string key) => $"value-of-{key}")
      .Done()
    .Done() // end config group
  .Done() // end admin group
```

This produces routes:
- `admin restart`
- `admin config get {key}`
- `admin config set {key} {value}`

But `WithGroupPrefix()` doesn't exist on `NuruCoreAppBuilder` or `EndpointBuilder`.

## Design Decision: CRTP Pattern

Use the same CRTP fluent builder pattern as `EndpointBuilder<TBuilder>`. This is consistent with the codebase even though it's compile-time DSL.

**Key insight:** The recursion is natural - drill down into nested groups, then pop back up with `.Done()`.

## Implementation Plan

### Part 1: Create GroupBuilder Class

**File:** `source/timewarp-nuru-core/builders/group-builder.cs`

```csharp
namespace TimeWarp.Nuru;

/// <summary>
/// Fluent builder for nested route groups with shared prefixes.
/// </summary>
/// <typeparam name="TParent">The parent builder type to return to.</typeparam>
public sealed class GroupBuilder<TParent> : INestedBuilder<TParent>
  where TParent : class
{
  private readonly TParent _parent;
  private readonly string _prefix;
  private readonly Action<string, Endpoint> _registerEndpoint;

  internal GroupBuilder(TParent parent, string prefix, Action<string, Endpoint> registerEndpoint)
  {
    _parent = parent;
    _prefix = prefix;
    _registerEndpoint = registerEndpoint;
  }

  /// <summary>
  /// Returns to the parent builder.
  /// </summary>
  public TParent Done() => _parent;

  /// <summary>
  /// Adds a route within this group. Pattern will be prefixed automatically.
  /// </summary>
  public GroupEndpointBuilder<TParent> Map(string pattern)
  {
    string fullPattern = $"{_prefix} {pattern}";
    // Create endpoint and register with full pattern
    Endpoint endpoint = new()
    {
      RoutePattern = fullPattern,
      CompiledRoute = PatternParser.Parse(fullPattern)
    };
    _registerEndpoint(fullPattern, endpoint);
    return new GroupEndpointBuilder<TParent>(this, endpoint);
  }

  /// <summary>
  /// Creates a nested group with additional prefix.
  /// </summary>
  public GroupBuilder<GroupBuilder<TParent>> WithGroupPrefix(string prefix)
  {
    string nestedPrefix = $"{_prefix} {prefix}";
    return new GroupBuilder<GroupBuilder<TParent>>(this, nestedPrefix, _registerEndpoint);
  }
}
```

### Part 2: Create GroupEndpointBuilder Class

**File:** `source/timewarp-nuru-core/builders/group-endpoint-builder.cs`

```csharp
namespace TimeWarp.Nuru;

/// <summary>
/// Endpoint builder that returns to a GroupBuilder parent.
/// </summary>
public sealed class GroupEndpointBuilder<TGroupParent> : INestedBuilder<GroupBuilder<TGroupParent>>
  where TGroupParent : class
{
  private readonly GroupBuilder<TGroupParent> _parent;
  private readonly Endpoint _endpoint;

  internal GroupEndpointBuilder(GroupBuilder<TGroupParent> parent, Endpoint endpoint)
  {
    _parent = parent;
    _endpoint = endpoint;
  }

  public GroupBuilder<TGroupParent> Done() => _parent;

  public GroupEndpointBuilder<TGroupParent> WithHandler(Delegate handler)
  {
    _endpoint.Handler = handler;
    _endpoint.Method = handler.Method;
    return this;
  }

  public GroupEndpointBuilder<TGroupParent> WithDescription(string description)
  {
    _endpoint.Description = description;
    return this;
  }

  public GroupEndpointBuilder<TGroupParent> AsQuery()
  {
    _endpoint.MessageType = MessageType.Query;
    _endpoint.CompiledRoute.MessageType = MessageType.Query;
    return this;
  }

  public GroupEndpointBuilder<TGroupParent> AsCommand()
  {
    _endpoint.MessageType = MessageType.Command;
    _endpoint.CompiledRoute.MessageType = MessageType.Command;
    return this;
  }

  public GroupEndpointBuilder<TGroupParent> AsIdempotentCommand()
  {
    _endpoint.MessageType = MessageType.IdempotentCommand;
    _endpoint.CompiledRoute.MessageType = MessageType.IdempotentCommand;
    return this;
  }
}
```

### Part 3: Add WithGroupPrefix to NuruCoreAppBuilder

**File:** `source/timewarp-nuru-core/builders/nuru-core-app-builder/nuru-core-app-builder.routes.cs`

Add method:

```csharp
/// <summary>
/// Creates a route group with a shared prefix.
/// </summary>
/// <param name="prefix">The prefix for all routes in this group (e.g., "admin").</param>
/// <returns>A GroupBuilder for configuring nested routes.</returns>
public virtual GroupBuilder<TSelf> WithGroupPrefix(string prefix)
{
  ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
  return new GroupBuilder<TSelf>((TSelf)this, prefix, RegisterGroupEndpoint);
}

private void RegisterGroupEndpoint(string fullPattern, Endpoint endpoint)
{
  EndpointCollection.Add(endpoint);
}
```

### Part 4: Add WithGroupPrefix to EndpointBuilder

**File:** `source/timewarp-nuru-core/builders/endpoint-builder.cs`

Add method:

```csharp
/// <summary>
/// Creates a route group with a shared prefix (forwarded to app builder).
/// </summary>
public GroupBuilder<TBuilder> WithGroupPrefix(string prefix) =>
  _builder.WithGroupPrefix(prefix);
```

### Part 5: Update Generator to Extract Group Prefixes

**File:** `source/timewarp-nuru-analyzers/generators/locators/with-group-prefix-locator.cs` (new)
**File:** `source/timewarp-nuru-analyzers/generators/extractors/fluent-chain-extractor.cs` (modify)

The generator needs to:
1. Locate `.WithGroupPrefix()` calls
2. Track the prefix scope while walking the fluent chain
3. Prepend prefixes to route patterns in nested `.Map()` calls

## Checklist

### Runtime/Builder Side
- [ ] Create `GroupBuilder<TParent>` class
- [ ] Create `GroupEndpointBuilder<TGroupParent>` class
- [ ] Add `WithGroupPrefix()` to `NuruCoreAppBuilder`
- [ ] Add `WithGroupPrefix()` to `EndpointBuilder`
- [ ] Verify builds without errors

### Generator Side
- [ ] Create `with-group-prefix-locator.cs`
- [ ] Update `FluentChainExtractor` to track group scope
- [ ] Update route extraction to prepend group prefixes
- [ ] Add test for nested groups in `temp-minimal-intercept-test.cs`

## Files to Create

| File | Purpose |
| ---- | ------- |
| `builders/group-builder.cs` | GroupBuilder class |
| `builders/group-endpoint-builder.cs` | GroupEndpointBuilder class |
| `generators/locators/with-group-prefix-locator.cs` | Locate group calls |

## Files to Modify

| File | Change |
| ---- | ------ |
| `builders/nuru-core-app-builder/nuru-core-app-builder.routes.cs` | Add `WithGroupPrefix()` |
| `builders/endpoint-builder.cs` | Add `WithGroupPrefix()` |
| `generators/extractors/fluent-chain-extractor.cs` | Track group scope |

## Test Case

```csharp
public static async Task Should_match_nested_group_routes()
{
  // Arrange
  using TestTerminal terminal = new();

  NuruCoreApp app = NuruApp.CreateBuilder([])
    .UseTerminal(terminal)
    .WithGroupPrefix("admin")
      .Map("status")
        .WithHandler(() => "admin status")
        .AsQuery()
        .Done()
      .WithGroupPrefix("config")
        .Map("get {key}")
          .WithHandler((string key) => $"config value: {key}")
          .AsQuery()
          .Done()
        .Done() // end config group
      .Done() // end admin group
    .Build();

  // Act - test nested route: "admin config get debug"
  int exitCode = await app.RunAsync(["admin", "config", "get", "debug"]);

  // Assert
  exitCode.ShouldBe(0);
  terminal.OutputContains("config value: debug").ShouldBeTrue();

  await Task.CompletedTask;
}
```

## Notes

### Type Flow

The recursive type structure:
- `NuruCoreAppBuilder<TSelf>.WithGroupPrefix("admin")` → `GroupBuilder<TSelf>`
- `GroupBuilder<TSelf>.Map("status")` → `GroupEndpointBuilder<TSelf>`
- `GroupEndpointBuilder<TSelf>.Done()` → `GroupBuilder<TSelf>`
- `GroupBuilder<TSelf>.WithGroupPrefix("config")` → `GroupBuilder<GroupBuilder<TSelf>>`
- `GroupBuilder<GroupBuilder<TSelf>>.Done()` → `GroupBuilder<TSelf>`
- `GroupBuilder<TSelf>.Done()` → `TSelf`

This allows infinite nesting while maintaining type safety.

### References

- `tests/timewarp-nuru-core-tests/routing/dsl-example.cs` - Shows expected usage
- `.agent/workspace/2024-12-25T12-00-00_v2-fluent-dsl-design.md` - DSL spec lines 263-286
- `.agent/workspace/2024-12-25T14-00-00_v2-source-generator-architecture.md` - Extractor design
- `samples/attributed-routes/messages/docker/docker-group-base.cs` - Attributed grouping pattern
