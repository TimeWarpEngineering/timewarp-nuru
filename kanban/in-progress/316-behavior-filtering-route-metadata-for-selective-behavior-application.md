# Behavior Filtering via `.Implements<T>()` for Selective Behavior Application

## Summary

Enable behaviors to selectively apply logic by checking contract interfaces on command instances. Both delegate and attributed routes will have command instances available to behaviors via `BehaviorContext.Command`.

## Background

### Problem

The current `INuruBehavior` pattern applies behaviors globally to all routes. The Mediator-based samples use **marker/contract interfaces** on command classes (e.g., `IRequireAuthorization`, `IRetryable`) to selectively apply behavior logic:

```csharp
// Mediator pattern - behavior has access to command instance
if (message is IRequireAuthorization auth)
{
  CheckPermission(auth.RequiredPermission);  // Can call interface methods!
}
```

In TimeWarp.Nuru, behaviors receive `BehaviorContext` which currently only has:
- `CommandName` (route pattern string)
- `CommandTypeName` (type name string)
- `CorrelationId`, `Stopwatch`, `CancellationToken`

Behaviors cannot check interfaces because they don't have access to the command instance.

### Blocked Samples

The following samples are blocked waiting for this feature:
- `pipeline-middleware-authorization.cs` - Needs `IRequireAuthorization` interface check
- `pipeline-middleware-retry.cs` - Needs `IRetryable` interface check (deferred - see notes)
- `pipeline-middleware.cs` - Combined example with all behaviors

## Solution: `.Implements<T>(Expression<Action<T>>)`

### Design Principles

1. **Unified approach**: Both delegate and attributed routes get command instances
2. **Interface-based**: Use real C# interfaces, not stringly-typed metadata
3. **Type-safe**: Compile-time verification via expression trees
4. **Extensible**: Users define their own interfaces - no DSL additions needed

### DSL for Delegate Routes

```csharp
// Single interface with single property
.Map("admin {action}")
  .Implements<IRequireAuthorization>(x => x.RequiredPermission = "admin:execute")
  .WithHandler((string action) => { ... })
  .Done()

// Multiple interfaces (chained)
.Map("flaky {operation}")
  .Implements<IRequireAuthorization>(x => x.RequiredPermission = "flaky:execute")
  .Implements<IRetryable>(x => x.MaxRetries = 5)
  .WithHandler((string operation) => { ... })
  .Done()

// Interface with multiple members (block body)
.Map("complex {id}")
  .Implements<IComplexInterface>(x => {
    x.PropertyA = "value";
    x.PropertyB = 42;
  })
  .WithHandler((int id) => { ... })
  .Done()
```

### Attributed Routes - Just implement the interface

```csharp
[NuruRoute("admin")]
public class AdminCommand : ICommand<Unit>, IRequireAuthorization
{
  public string RequiredPermission => "admin:execute";
  
  [Parameter] public string Action { get; set; }
  
  public sealed class Handler : ICommandHandler<AdminCommand, Unit> { ... }
}
```

### DSL Method Signature

```csharp
public TSelf Implements<T>(Expression<Action<T>> configuration) where T : class
```

The `Expression<Action<T>>` parameter allows the source generator to:
1. Analyze the expression tree at compile time
2. Extract property assignments (e.g., `RequiredPermission = "admin:execute"`)
3. Generate command class implementing the interface with those values baked in

This is similar to how EF Core analyzes `Where(x => x.Name == "foo")`.

### Generator Output for Delegate Routes

**Input:**
```csharp
.Map("admin {action}")
  .Implements<IRequireAuthorization>(x => x.RequiredPermission = "admin:execute")
  .WithHandler((string action) => { ... })
```

**Generated:**
```csharp
// Generated command class implementing the interface
file sealed class Admin_GeneratedCommand : IRequireAuthorization
{
  public required string Action { get; init; }
  
  // Interface implementation from expression tree
  public string RequiredPermission => "admin:execute";
}
```

**For multiple interfaces:**
```csharp
file sealed class Flaky_GeneratedCommand : IRequireAuthorization, IRetryable
{
  public required string Operation { get; init; }
  
  public string RequiredPermission => "flaky:execute";
  public int MaxRetries => 5;
}
```

### BehaviorContext Changes

Add command instance property:

```csharp
public class BehaviorContext
{
  public required string CommandName { get; init; }
  public required string CommandTypeName { get; init; }
  public required CancellationToken CancellationToken { get; init; }
  public string CorrelationId { get; }
  public Stopwatch Stopwatch { get; }
  
  /// <summary>
  /// The command instance for this request.
  /// Can be cast to check/use interface implementations.
  /// </summary>
  public object? Command { get; init; }
}
```

### Behavior Usage

```csharp
public sealed class AuthorizationBehavior : INuruBehavior
{
  public ValueTask OnBeforeAsync(BehaviorContext context)
  {
    // Works for BOTH delegate and attributed routes!
    if (context.Command is IRequireAuthorization auth)
    {
      string permission = auth.RequiredPermission;
      if (!HasPermission(permission))
        throw new UnauthorizedAccessException($"Required permission: {permission}");
    }
    return ValueTask.CompletedTask;
  }

  public ValueTask OnAfterAsync(BehaviorContext context) => ValueTask.CompletedTask;
  public ValueTask OnErrorAsync(BehaviorContext context, Exception ex) => ValueTask.CompletedTask;
}
```

## Checklist

### Phase 1: Core Infrastructure
- [ ] Add `Command` property to `BehaviorContext`
- [ ] Update behavior emitter to pass command instance to context
- [ ] For attributed routes: command instance already created, just pass it
- [ ] For delegate routes without `.Implements<T>()`: create simple generated command with parameters only

### Phase 2: DSL for `.Implements<T>()`
- [ ] Add `Implements<T>(Expression<Action<T>>)` method to endpoint builder
- [ ] Support chaining multiple `.Implements<T>()` calls
- [ ] DSL interpreter extracts interface type from generic parameter
- [ ] DSL interpreter extracts property assignments from expression tree

### Phase 3: Generator Updates
- [ ] Generate command classes implementing specified interfaces
- [ ] Extract constant values from expression tree assignments
- [ ] Support single property assignment: `x => x.Prop = value`
- [ ] Support block body with multiple assignments: `x => { x.A = 1; x.B = 2; }`
- [ ] Emit interface implementations with extracted values

### Phase 4: Update Samples
- [ ] Convert `pipeline-middleware-authorization.cs` using `IRequireAuthorization`
- [ ] Update `pipeline-middleware.cs` (combined) - partial, authorization only
- [ ] `pipeline-middleware-retry.cs` - DEFERRED (see notes)

### Phase 5: Testing
- [ ] Unit tests for expression tree parsing
- [ ] Unit tests for command class generation with interfaces
- [ ] Integration tests for behavior filtering
- [ ] Sample verification tests

## Related Tasks

- #315 - Implement Pipeline Behavior Code Generation (parent task, partially complete)
- #265 - Epic: V2 Source Generator Implementation

## Notes

### Why `.Implements<T>()` over route metadata?

1. **Type-safe**: Real C# interfaces with compile-time checking
2. **Extensible**: Users define their own interfaces - no DSL modifications needed
3. **Familiar**: Same pattern as Mediator/MediatR
4. **Full interface support**: Not just markers - can have methods/properties that behaviors call

### Retry Behavior - Deferred

The `IRetryable` pattern is more complex because `OnErrorAsync` can only observe exceptions - it cannot retry the handler. Options for future consideration:

- Option A: Special-case `IRetryable` in generator to wrap handler in retry loop
- Option B: Add `OnExecuteAsync(context, Func<Task> next)` hook with control flow
- Option C: Different retry mechanism (e.g., Polly integration)

For now, focus on authorization which works cleanly with `OnBeforeAsync`.

### Expression Tree Parsing

The source generator will need to analyze `Expression<Action<T>>` to extract:
- Property names being assigned
- Constant values being assigned

This is feasible - Roslyn provides full access to the syntax tree. The generator will:
1. Find the lambda expression in the `Implements<T>()` call
2. Walk the expression body looking for assignment expressions
3. Extract property name (left side) and value (right side)
4. Emit these as property implementations in the generated command class

### Attributed Routes Already Have Command Instances

Looking at generated code for attributed routes:
```csharp
global::AttributedRoutes.Messages.DeployCommand __command = new()
{
  Env = env,
  Force = force,
  // ...
};
```

The command instance is already created - we just need to pass it to `BehaviorContext.Command`.
