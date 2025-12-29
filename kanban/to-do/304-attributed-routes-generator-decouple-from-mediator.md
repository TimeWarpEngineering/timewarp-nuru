# Attributed Routes Generator + Decouple from Mediator

## Summary

Implement auto-discovery of `[NuruRoute]` attributed classes with TimeWarp.Nuru's own message interfaces, removing the dependency on the Mediator library. Remove redundant `Map<T>()` overloads from the DSL.

## Background

During planning, we determined:

1. **`Map<T>("pattern")` is redundant** - The pattern is already specified in `[NuruRoute("pattern")]` attribute. No need for two ways to do the same thing.

2. **Mediator library is unnecessary** - We don't actually use the mediator pattern. The command/handler pattern with nested `Handler` classes works without any external library.

3. **Own interfaces are cleaner** - Creating `TimeWarp.Nuru.IQuery<T>`, `ICommand<T>`, `IIdempotentCommand<T>` decouples us from external dependencies.

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

### Phase 1: Create Message Interfaces

Create `source/timewarp-nuru-core/abstractions/message-interfaces.cs`:
- `IMessage` - root marker
- `IIdempotent : IMessage` - safe to retry marker
- `IQuery<TResult> : IIdempotent` - read-only queries
- `ICommand<TResult> : IMessage` - mutating commands (not idempotent)
- `IIdempotentCommand<TResult> : IIdempotent` - mutating but safe to retry
- `Unit` - void equivalent struct

### Phase 2: Create Handler Interfaces

Create `source/timewarp-nuru-core/abstractions/handler-interfaces.cs`:
- `IQueryHandler<TQuery, TResult>`
- `ICommandHandler<TCommand, TResult>`
- `IIdempotentCommandHandler<TCommand, TResult>`

### Phase 3: Remove `Map<T>()` from DSL Builder

Modify `source/timewarp-nuru-core/builders/nuru-core-app-builder/nuru-core-app-builder.routes.cs`:
- Delete `Map<TCommand>(string pattern)` method
- Delete `Map<TCommand, TResponse>(string pattern)` method

### Phase 4: Remove `Map<T>()` from Endpoint Builder

Modify `source/timewarp-nuru-core/builders/endpoint-builder.cs`:
- Delete `Map<TCommand>()` forwarding methods

### Phase 5: Create Attributed Route Extractor

Create `source/timewarp-nuru-analyzers/generators/extractors/attributed-route-extractor.cs`:
- Scan for classes with `[NuruRoute]` attribute
- Extract pattern from attribute
- Extract parameters from `[Parameter]` properties
- Extract options from `[Option]` properties
- Extract group prefix from `[NuruRouteGroup]` base class
- Infer message type from implemented interface
- Find nested `Handler` class
- Extract handler constructor dependencies

### Phase 6: Integrate Attributed Routes into Generator

Modify `source/timewarp-nuru-analyzers/generators/nuru-generator.cs`:
- Add pipeline stage to discover `[NuruRoute]` classes in same assembly
- Convert to `RouteDefinition` model (same as DSL routes)
- Merge attributed routes into `AppModel.Routes`

### Phase 7: Update Handler Definition Model

Modify `source/timewarp-nuru-analyzers/generators/models/handler-definition.cs`:
- Add `HandlerKind.Command` for attributed handlers
- Add `CommandTypeName` - fully qualified command type
- Add `HandlerTypeName` - fully qualified handler type (e.g., `Command.Handler`)
- Add `PropertyBindings` - map route params to command properties
- Add `ConstructorDependencies` - services needed by handler

### Phase 8: Implement Command Handler Invocation

Modify `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs`:
- Add `EmitCommandHandlerInvocation()` method:
  1. Create command instance with property bindings
  2. Resolve handler constructor dependencies from registered services
  3. Create handler instance
  4. Call `handler.Handle(command, cancellationToken)`
  5. Handle result if present

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
| `source/timewarp-nuru-analyzers/generators/extractors/attributed-route-extractor.cs` | Create | Extract `[NuruRoute]` info |
| `source/timewarp-nuru-analyzers/generators/nuru-generator.cs` | Modify | Discover attributed routes |
| `source/timewarp-nuru-analyzers/generators/models/handler-definition.cs` | Modify | Command handler support |
| `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs` | Modify | Command handler invocation |
| `samples/02-calculator/02-calc-mediator.cs` | Rename/Rewrite | -> `02-calc-commands.cs` |
| `samples/02-calculator/03-calc-mixed.cs` | Modify | Fix and update |
| `samples/attributed-routes/attributed-routes.cs` | Modify | Remove Mediator |
| `samples/attributed-routes/messages/**/*.cs` | Modify | Use TimeWarp.Nuru interfaces |

## Design Decisions

1. **Handler discovery**: Same assembly only. Routes in NuGet packages don't make sense for source-gen.

2. **Handler convention**: Nested `Handler` class inside command type (e.g., `AddCommand.Handler`).

3. **Property binding**: Case-insensitive match between route params and command properties.

4. **Namespace**: Flat `TimeWarp.Nuru` for all interfaces.

## Checklist

- [ ] Phase 1: Create `abstractions/message-interfaces.cs`
- [ ] Phase 2: Create `abstractions/handler-interfaces.cs`
- [ ] Phase 3: Remove `Map<T>()` from `nuru-core-app-builder.routes.cs`
- [ ] Phase 4: Remove `Map<T>()` from `endpoint-builder.cs`
- [ ] Phase 5: Create `attributed-route-extractor.cs`
- [ ] Phase 6: Integrate attributed routes into `nuru-generator.cs`
- [ ] Phase 7: Update `handler-definition.cs`
- [ ] Phase 8: Implement command handler invocation in `handler-invoker-emitter.cs`
- [ ] Phase 9: Rename/rewrite `02-calc-mediator.cs` -> `02-calc-commands.cs`
- [ ] Phase 10: Fix `03-calc-mixed.cs`
- [ ] Phase 11: Update `attributed-routes/attributed-routes.cs`
- [ ] Phase 12: Update `attributed-routes/messages/**/*.cs`
- [ ] Phase 13: Update this task
- [ ] Phase 14: Test everything

## Notes

This task evolved from "support `Map<T>()`" to "implement attributed routes and remove `Map<T>()`" after realizing:
- Attributed routes are the "Minimal API" equivalent - cleaner DX
- `Map<T>()` is redundant when pattern is in the attribute
- Own interfaces decouple from Mediator library dependency
