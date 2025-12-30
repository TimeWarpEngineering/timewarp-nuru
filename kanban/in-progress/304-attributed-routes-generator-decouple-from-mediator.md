# Attributed Routes Generator + Decouple from Mediator

## Summary

Implement auto-discovery of `[NuruRoute]` attributed classes with TimeWarp.Nuru's own message interfaces, removing the dependency on the Mediator library. Remove redundant `Map<T>()` overloads from the DSL.

## Background

During planning, we determined:

1. **`Map<T>("pattern")` is redundant** - The pattern is already specified in `[NuruRoute("pattern")]` attribute. No need for two ways to do the same thing.

2. **Mediator library is unnecessary** - We don't actually use the mediator pattern. The command/handler pattern with nested `Handler` classes works without any external library.

3. **Own interfaces are cleaner** - Creating `TimeWarp.Nuru.IQuery<T>`, `ICommand<T>`, `IIdempotentCommand<T>` decouples us from external dependencies.

4. **We generate everything** - The source generator will emit all code that Mediator would have done, including pipeline pre/post processors.

## Interface Hierarchy

```
IMessage (root marker)
    |
    +-- IIdempotent : IMessage (safe to retry)
    |       |
    |       +-- IQuery<TResult> : IIdempotent (read-only, returns data)
    |       |
    |       +-- IIdempotentCommand<TResult> : IIdempotent (mutates, but safe to retry)
    |
    +-- ICommand<TResult> : IMessage (mutates, NOT safe to retry)
```

| Interface               | Reads | Writes | Safe to Retry | HTTP Analogy |
|-------------------------|-------|--------|---------------|--------------|
| `IQuery<T>`             | Yes   | No     | Yes           | GET          |
| `ICommand<T>`           | No    | Yes    | No            | POST         |
| `IIdempotentCommand<T>` | No    | Yes    | Yes           | PUT/DELETE   |

## Execution Plan (Serial)

### Phase 1: Create Message Interfaces (DONE)

Create `source/timewarp-nuru-core/abstractions/message-interfaces.cs`:
- `IMessage` - root marker
- `IIdempotent : IMessage` - safe to retry marker
- `IQuery<TResult> : IIdempotent` - read-only queries
- `ICommand<TResult> : IMessage` - mutating commands (not idempotent)
- `IIdempotentCommand<TResult> : IIdempotent` - mutating but safe to retry
- `Unit` - void equivalent struct

### Phase 2: Create Handler Interfaces (DONE)

Create `source/timewarp-nuru-core/abstractions/handler-interfaces.cs`:
- `IQueryHandler<TQuery, TResult>`
- `ICommandHandler<TCommand, TResult>`
- `IIdempotentCommandHandler<TCommand, TResult>`

### Phase 3: Remove `Map<T>()` from DSL Builder (DONE)

Modify `source/timewarp-nuru-core/builders/nuru-core-app-builder/nuru-core-app-builder.routes.cs`:
- Delete `Map<TCommand>(string pattern)` method
- Delete `Map<TCommand, TResponse>(string pattern)` method

### Phase 4: Remove `Map<T>()` from Endpoint Builder (DONE)

Modify `source/timewarp-nuru-core/builders/endpoint-builder.cs`:
- Delete `Map<TCommand>()` forwarding methods

### Phase 5: Enhance Attributed Route Extractor

The extractor file already exists at `source/timewarp-nuru-analyzers/generators/extractors/attributed-route-extractor.cs`.
Need to enhance it to find nested `Handler` class and extract constructor dependencies.

#### Phase 5 Detailed Tasks

**5.1: Remove `HandlerKind.Mediator` from enum** (`handler-definition.cs`)
- Delete `Mediator` from `HandlerKind` enum
- This kind assumed `app.Mediator.Send()` which requires Mediator library

**5.2: Remove `ForMediator()` factory method** (`handler-definition.cs`)
- Delete `HandlerDefinition.ForMediator()` static method

**5.3: Remove `EmitMediatorInvocation()` from emitter** (`handler-invoker-emitter.cs`)
- Delete `case HandlerKind.Mediator:` branch
- Delete `EmitMediatorInvocation()` method
- Build will temporarily break until Phase 8 adds `Command` handling

**5.4: Add `HandlerKind.Command` to enum** (`handler-definition.cs`)
- New kind for nested `Handler` class pattern (no Mediator library)

```csharp
public enum HandlerKind
{
  Delegate,
  Method,
  Command  // NEW: Nested Handler class pattern
}
```

**5.5: Add new properties to `HandlerDefinition`** (`handler-definition.cs`)
- `NestedHandlerTypeName` (string?) - e.g., `global::MyApp.GreetQuery.Handler`
- `ConstructorDependencies` (ImmutableArray<ParameterBinding>) - handler constructor params

**5.6: Add `ForCommand()` factory method** (`handler-definition.cs`)

```csharp
public static HandlerDefinition ForCommand(
  string commandTypeName,                          // e.g., "global::MyApp.GreetQuery"
  string nestedHandlerTypeName,                    // e.g., "global::MyApp.GreetQuery.Handler"
  ImmutableArray<ParameterBinding> propertyBindings,        // Route → command properties
  ImmutableArray<ParameterBinding> constructorDependencies, // Handler constructor deps
  HandlerReturnType returnType)
```

**5.7: Implement `ExtractConstructorDependencies()`** (`attributed-route-extractor.cs`)

```csharp
private static ImmutableArray<ParameterBinding> ExtractConstructorDependencies(INamedTypeSymbol handlerClass)
{
  ImmutableArray<ParameterBinding>.Builder deps = ImmutableArray.CreateBuilder<ParameterBinding>();
  
  // Find the constructor (or primary constructor)
  IMethodSymbol? constructor = handlerClass.InstanceConstructors
    .FirstOrDefault(c => c.DeclaredAccessibility == Accessibility.Public);
  
  if (constructor is null)
    return deps.ToImmutable();
  
  foreach (IParameterSymbol param in constructor.Parameters)
  {
    string typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    
    // All constructor params are services (resolved via static instantiation per task #292)
    deps.Add(ParameterBinding.FromService(
      parameterName: param.Name,
      serviceTypeName: typeName));
  }
  
  return deps.ToImmutable();
}
```

**5.8: Modify `ExtractHandler()` to find nested Handler** (`attributed-route-extractor.cs`)

Change from returning `ForMediator()` to:

```csharp
// 1. Find nested Handler class
INamedTypeSymbol? handlerClass = classSymbol.GetTypeMembers("Handler").FirstOrDefault();
if (handlerClass is null)
{
  // No nested Handler class found - skip this route
  return null;
}

// 2. Extract constructor dependencies
ImmutableArray<ParameterBinding> constructorDeps = ExtractConstructorDependencies(handlerClass);

// 3. Return Command handler definition
return HandlerDefinition.ForCommand(
  commandTypeName: fullTypeName,
  nestedHandlerTypeName: handlerClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
  propertyBindings: parameters.ToImmutable(),
  constructorDependencies: constructorDeps,
  returnType: returnType);
```

#### Phase 5 Files to Modify

| File | Action | Description |
|------|--------|-------------|
| `generators/models/handler-definition.cs` | Modify | Remove `Mediator`, add `Command`, add properties, add `ForCommand()` |
| `generators/extractors/attributed-route-extractor.cs` | Modify | Find nested `Handler`, extract constructor deps |
| `generators/emitters/handler-invoker-emitter.cs` | Modify | Remove `EmitMediatorInvocation()` |

#### Phase 5 Generated Code Target (for reference - Phase 8 emits this)

For `GreetQuery` with nested `Handler(ITerminal terminal)`:

```csharp
// Route: greet {name}
if (args is ["greet", var __name_0])
{
  string Name = __name_0;
  
  // Create command instance with bound properties
  global::MyApp.GreetQuery __command = new() 
  { 
    Name = Name 
  };
  
  // Resolve handler constructor dependencies (same as delegate service injection per task #292)
  global::TimeWarp.Terminal.ITerminal __terminal = app.Terminal;
  
  // Create handler and invoke
  global::MyApp.GreetQuery.Handler __handler = new(__terminal);
  await __handler.Handle(__command, cancellationToken);
  
  return 0;
}
```

### Phase 6: Integrate Attributed Routes into Generator (DONE - already integrated)

The `nuru-generator.cs` already calls `AttributedRouteExtractor.Extract()` and merges results.

### Phase 7: Update Handler Definition Model

Covered by Phase 5 detailed tasks above.

### Phase 8: Implement Command Handler Invocation

Modify `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs`:
- Add `case HandlerKind.Command:` branch
- Add `EmitCommandInvocation()` method:
  1. Create command instance with property bindings
  2. Resolve handler constructor dependencies using existing `ServiceResolverEmitter` (task #292)
  3. Create handler instance with `new Handler(dep1, dep2, ...)`
  4. Call `await handler.Handle(command, cancellationToken)`
  5. Handle result if not `Unit`

### Phase 9: Update Calculator Commands Sample

Rename `samples/02-calculator/02-calc-mediator.cs` to `02-calc-commands.cs`:
- Convert to attributed runfile style (classes in same file)
- Use `TimeWarp.Nuru` interfaces
- Demonstrates command/handler pattern introduction

### Phase 10: Update Calculator Mixed Sample

Modify `samples/02-calculator/03-calc-mixed.cs`:
- Add missing `.Done()` calls
- Update command classes to use `TimeWarp.Nuru` interfaces
- Keep delegate portions as-is
- Demonstrates mixing delegates + attributed commands

### Phase 11: Update Attributed Routes Sample Entry Point

Modify `samples/attributed-routes/attributed-routes.cs`:
- Remove `#:package Mediator.*` directives
- Remove `services.AddMediator()` call

### Phase 12: Update Attributed Routes Message Classes

Modify all files in `samples/attributed-routes/messages/**/*.cs`:
- Change `using Mediator;` to `using TimeWarp.Nuru;`
- Update interface references (`ICommand<Unit>`, `IQuery<Unit>`, etc.)
- Update handler interfaces (`ICommandHandler<T, Unit>`, etc.)

### Phase 13: Update This Task

Move to done, document results.

### Phase 14: Test Everything

- Run `01-calc-delegate.cs` (should still work - delegates unchanged)
- Run `02-calc-commands.cs` (new attributed style)
- Run `03-calc-mixed.cs` (mixed delegates + commands)
- Run `attributed-routes/attributed-routes.cs`
- Run existing generator tests

## Files Summary

| File | Action | Description |
|------|--------|-------------|
| `source/timewarp-nuru-core/abstractions/message-interfaces.cs` | Create | Message type interfaces |
| `source/timewarp-nuru-core/abstractions/handler-interfaces.cs` | Create | Handler interfaces |
| `source/timewarp-nuru-core/builders/nuru-core-app-builder.routes.cs` | Modify | Remove `Map<T>()` |
| `source/timewarp-nuru-core/builders/endpoint-builder.cs` | Modify | Remove `Map<T>()` |
| `source/timewarp-nuru-analyzers/generators/extractors/attributed-route-extractor.cs` | Modify | Find nested `Handler`, extract deps |
| `source/timewarp-nuru-analyzers/generators/models/handler-definition.cs` | Modify | Remove `Mediator`, add `Command` |
| `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs` | Modify | Remove `Mediator`, add `Command` invocation |
| `samples/02-calculator/02-calc-mediator.cs` | Rename/Rewrite | -> `02-calc-commands.cs` |
| `samples/02-calculator/03-calc-mixed.cs` | Modify | Fix and update |
| `samples/attributed-routes/attributed-routes.cs` | Modify | Remove Mediator |
| `samples/attributed-routes/messages/**/*.cs` | Modify | Use TimeWarp.Nuru interfaces |

## Design Decisions

1. **Handler discovery**: Same assembly only. Routes in NuGet packages don't make sense for source-gen.

2. **Handler convention**: Nested `Handler` class inside command type (e.g., `AddCommand.Handler`). **Required** - no fallback to Mediator pattern.

3. **Property binding**: Case-insensitive match between route params and command properties.

4. **Namespace**: Flat `TimeWarp.Nuru` for all interfaces.

5. **No `HandlerKind.Mediator`**: Removed entirely. We generate all handler invocation code ourselves, including pipeline pre/post processors.

6. **Service resolution**: Uses existing static instantiation approach from task #292 - `ITerminal` → `app.Terminal`, registered services → `new T()` or `Lazy<T>`.

## Checklist

- [x] Phase 1: Create `abstractions/message-interfaces.cs`
- [x] Phase 2: Create `abstractions/handler-interfaces.cs`
- [x] Phase 3: Remove `Map<T>()` from `nuru-core-app-builder.routes.cs`
- [x] Phase 4: Remove `Map<T>()` from `endpoint-builder.cs`
- [x] Phase 5.1: Remove `HandlerKind.Mediator` from enum
- [x] Phase 5.2: Remove `ForMediator()` factory method
- [x] Phase 5.3: Remove `EmitMediatorInvocation()` from emitter
- [x] Phase 5.4: Add `HandlerKind.Command` to enum
- [x] Phase 5.5: Add `NestedHandlerTypeName` and `ConstructorDependencies` properties
- [x] Phase 5.6: Add `ForCommand()` factory method
- [x] Phase 5.7: Implement `ExtractConstructorDependencies()` in extractor
- [x] Phase 5.8: Modify `ExtractHandler()` to find nested Handler and return `ForCommand()`
- [x] Phase 6: Integrate attributed routes into `nuru-generator.cs` (already done)
- [x] Phase 7: (covered by Phase 5)
- [x] Phase 8: Implement command handler invocation in `handler-invoker-emitter.cs`
- [x] Phase 9: Rename/rewrite `02-calc-mediator.cs` -> `02-calc-commands.cs`
- [x] Phase 10: Fix `03-calc-mixed.cs` (unblocked by #308)
- [x] Phase 11: Update `attributed-routes/attributed-routes.cs` (entry point updated)
- [ ] Phase 12: Update `attributed-routes/messages/**/*.cs` **BLOCKED by #309, #310, #311**
- [ ] Phase 13: Update this task
- [ ] Phase 14: Test everything

## Notes

This task evolved from "support `Map<T>()`" to "implement attributed routes and remove `Map<T>()`" after realizing:
- Attributed routes are the "Minimal API" equivalent - cleaner DX
- `Map<T>()` is redundant when pattern is in the attribute
- Own interfaces decouple from Mediator library dependency
- We generate everything Mediator would have done

### Session 2025-12-30 Progress

Completed Phase 5 and 8 implementation:

1. **Handler Definition Model (`handler-definition.cs`):**
   - Removed `HandlerKind.Mediator` enum value
   - Added `HandlerKind.Command` enum value
   - Added `NestedHandlerTypeName` and `ConstructorDependencies` properties
   - Replaced `ForMediator()` with `ForCommand()` factory method

2. **Handler Invoker Emitter (`handler-invoker-emitter.cs`):**
   - Replaced `EmitMediatorInvocation()` with `EmitCommandInvocation()`
   - Added `ResolveServiceExpression()` and `ResolveRegisteredService()` for service resolution
   - Fixed `EmitResultOutput()` to handle value types correctly (no null check for int, double, etc.)

3. **Attributed Route Extractor (`attributed-route-extractor.cs`):**
   - Modified `ExtractHandler()` to find nested `Handler` class using `GetTypeMembers("Handler")`
   - Added `ExtractConstructorDependencies()` to extract handler constructor parameters
   - Returns `null` if no nested Handler class found (skips route)

4. **Additional Fixes:**
   - Updated `TelemetryBehavior` to use `TimeWarp.Nuru.IMessage` instead of `Mediator.IMessage`
   - Updated `samples/timewarp-nuru-sample` to remove Mediator dependency and use delegate handlers

**Remaining:** Phases 9-14 (update remaining samples and attributed-routes sample)

### Session 2025-12-31 Progress

**Phase 9 Complete:** `02-calc-commands.cs` already working from previous session.

**Phase 10 In Progress - BLOCKED:**

Converted `03-calc-mixed.cs` to use attributed routes pattern:
- Removed Mediator package directives
- Removed `using Mediator;`
- Removed all `Map<T>()` calls (routes auto-discovered via `[NuruRoute]`)
- Added missing `.Done()` calls to delegate routes
- Converted `FactorialCommand`, `PrimeCheckCommand`, `FibonacciCommand`, `StatsCommand` to:
  - Use `[NuruRoute]` attribute with `[Parameter]` attributes
  - Use `ICommand<Unit>` / `ICommand<StatsResponse>` from `TimeWarp.Nuru`
  - Use `ICommandHandler<T, TResult>` from `TimeWarp.Nuru`
- Changed `StatsCommand.Values` from `string` to `string[]` (catch-all is always array)
- Added `#pragma warning disable NURU_H002` for object initializer false positive (see #307)

**Issues Discovered (and resolved):**

1. **#307 - NURU_H002 false positive:** Analyzer incorrectly flags properties in object initializers as closures. Workaround: `#pragma warning disable NURU_H002` (task still open for proper fix)

2. **#308 - Method group not analyzed:** Fixed! `ServiceExtractor` now analyzes method group references.

**Phase 10 Complete:**
- All delegate routes work: `add`, `subtract`, `multiply`, `divide`, `compare`
- All attributed routes work: `factorial`, `isprime`, `fibonacci`, `stats`
- All 10 generator tests pass (no regressions)

**Phase 11 Complete:**
- Removed `services.AddMediator()` from entry point
- Removed `using Microsoft.Extensions.DependencyInjection;`
- Removed `.AddAutoHelp()` and `.WithMetadata()` (not recognized by DSL interpreter, not needed with CreateBuilder)

**Phase 12 In Progress - BLOCKED:**

Updated all 14 message files to remove `using Mediator;` and convert interfaces:
- Queries: Use `IQuery<Unit>` and `IQueryHandler<T, Unit>` (4 files)
- Commands: Use `ICommand<Unit>` and `ICommandHandler<T, Unit>` (5 files)
- Idempotent: Use `IIdempotentCommand<Unit>` and `IIdempotentCommandHandler<T, Unit>` (2 files)
- Unspecified: Converted `PingRequest` to `PingQuery` (1 file)
- Group bases: No changes needed (2 files)

**Generator bugs discovered blocking compilation:**

1. **#309 - Type conversion for typed options:** `DeployCommand.Replicas` is `int` but generator assigns `string` directly. Need to emit `int.Parse()`.

2. **#310 - Hyphenated option variable naming:** `DockerBuildCommand` has `--no-cache` option. Generator declares `noCache` but references `nocache` in property binding.

3. **#311 - Catch-all `Args` collision:** `ExecCommand.Args` generates `string[] args = args[1..]` which shadows the method parameter.

**Next:** Fix #309, #310, #311 to unblock Phase 12.
