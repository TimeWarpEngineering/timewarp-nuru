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

### Phase 1: Add Generic Behavior Interface ✅
- [x] Add `INuruBehavior<TFilter>` interface for filtered behaviors
- [x] Add `BehaviorContext<TFilter>` with strongly-typed Command property
- [x] Commit: `424804da`

### Phase 2: Update BehaviorDefinition Model ✅
- [x] Add `FilterTypeName` property to `BehaviorDefinition`
- [x] Add `ForFilter()` factory method
- [x] Add `IsFiltered` computed property
- [x] Commit: `2252d0dc`

### Phase 3: Extract Filter Type from Behavior Registration ✅
- [x] Add `ExtractBehaviorFilterType()` to DSL interpreter
- [x] Detect `INuruBehavior<TFilter>` implementations
- [x] Error at compile-time for multiple filter interfaces
- [x] Commit: `280cce81`

### Phase 4: Add `.Implements<T>()` DSL Method ✅
- [x] Add `Implements<T>(Expression<Action<T>>)` to `EndpointBuilder`
- [x] Add `InterfaceImplementationDefinition` and `PropertyAssignment` models
- [x] Add `Implements` property to `RouteDefinition`
- [x] Add `WithImplements()` to `RouteDefinitionBuilder`
- [x] Add `AddImplementation()` to `IIrRouteBuilder` interface
- [x] Commit: `b063bbd9`

### Phase 5: Expression Tree Extractor ✅
- [x] Create `ImplementsExtractor` to parse lambda expressions
- [x] Extract interface type from generic argument
- [x] Extract property assignments from expression body
- [x] Support both simple and block body lambdas
- [x] Add `DispatchImplements()` to DSL interpreter
- [x] Commit: `1ec3e690`

### Phase 6: Generate Command Classes for Delegate Routes ✅
- [x] Generate `__Route_N_Command` classes for all delegate routes
- [x] Include route parameters as properties
- [x] Implement interfaces from `.Implements<T>()` calls
- [x] Emit interface property implementations with extracted values
- [x] Filter behaviors based on `FilterTypeName` vs route's implemented interfaces
- [x] For filtered behaviors, create typed `BehaviorContext<TFilter>`
- [x] Pass command instance to context for all routes
- [x] Commit: (latest)

### Phase 7: Attributed Routes Interface Detection ✅
- [x] Extract implemented interfaces from attributed command classes
- [x] Add `ImplementedInterfaces` to attributed route extraction
- [x] Filter out Mediator interfaces (ICommand, IQuery, etc.)
- [x] Filter out common .NET interfaces (System.*, Microsoft.*)
- [x] Commit: `baae5347`

### Phase 8: Update Samples ✅
- [x] Created `04-pipeline-middleware-filtered-auth.cs` with `INuruBehavior<IRequireAuthorization>`
- [x] Tested with `.Implements<T>()` delegate routes - WORKING
- [x] Changed `.Implements<T>()` from `Expression<Action<T>>` to `Action<T>` (simpler, avoids expression tree limitations)
- [x] Fixed property type resolution in ImplementsExtractor
- [x] Updated CommandClassEmitter to emit `{ get; set; }` properties for interface compatibility
- [x] Commit: `71e82665`

### Phase 9: Testing ✅
- [x] Integration tests for behavior filtering (`behavior-filtering-01-implements-extraction.cs`)
  - Route without `.Implements<T>()` executes without authorization behavior
  - Unauthorized access is blocked on protected routes
  - Authorized access is allowed with proper credentials
  - Global behaviors still apply to all routes
  - Sample compiles and runs correctly
  - Behavior ordering is maintained (LoggingBehavior wraps AuthorizationBehavior)
- [x] Commit: `42c9b709`

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
