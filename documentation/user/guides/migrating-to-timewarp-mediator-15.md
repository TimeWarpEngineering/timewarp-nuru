# Migrating to TimeWarp.Mediator 15

TimeWarp.Nuru 4.0 depends on TimeWarp.Mediator 15 and TimeWarp.Contracts 2.0.0-beta.31. Mediator 15
removes the kind-specific handler interfaces and moves handlers to `ValueTask`. Every endpoint with a
nested `Handler`, and every handler for an `[Operation]` request, needs the edits below. Routes, the
message markers, `--help`, `--capabilities`, and the MCP tool list do not change.

The upstream guide is TimeWarp.Mediator's `documentation/migration.md` (section "TimeWarp.Nuru").

## Endpoint handlers

| Before (Mediator 14) | After (Mediator 15) |
|---|---|
| `Handler : ICommandHandler<X, T>` | `Handler : IRequestHandler<X, T>` |
| `Handler : IQueryHandler<X, T>` | `Handler : IRequestHandler<X, T>` |
| `Handler : IIdempotentCommandHandler<X, T>` | `Handler : IRequestHandler<X, T>` |
| `public Task<T> Handle(X x, CancellationToken ct)` | `public ValueTask<T> Handle(X x, CancellationToken ct)` |
| `public async Task<T> Handle(...)` | `public async ValueTask<T> Handle(...)`; the body stays |
| `return Unit.Task;` | `return Unit.ValueTask;` |
| `return Task.FromResult(x);` in a handler | `return ValueTask.FromResult(x);` |
| `return Task.CompletedTask;` in a `ValueTask` handler | `return ValueTask.CompletedTask;` |

The endpoint keeps its marker: `ICommand<T>`, `IQuery<T>`, `IIdempotentCommand<T>`, or `ICommand<T>` plus
`IIdempotent`. The marker, not the handler interface, sets the message kind, so the catalog,
`--capabilities`, and MCP annotations are the same as before.

```csharp
using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("greet", Description = "Say hello")]
public sealed class GreetQuery : IQuery<Unit>
{
  [Parameter] public string Name { get; set; } = "";

  public sealed class Handler(ITerminal terminal) : IRequestHandler<GreetQuery, Unit>
  {
    public ValueTask<Unit> Handle(GreetQuery query, CancellationToken ct)
    {
      terminal.WriteLine($"Hello, {query.Name}!");
      return Unit.ValueTask;
    }
  }
}
```

`ValueTask<T>` has no implicit conversion from `Task<T>`. Wrap a task with `new ValueTask<T>(task)`, or
make the method `async` and `await` it.

## Callers of `ISender`, `IMediator`, and `IPublisher`

`Send` and `Publish` return `ValueTask` / `ValueTask<T>`. `await sender.Send(x)` is unchanged. A method
that returns the call directly changes its return type:

```csharp
// Before
static Task<string> Ping(ISender sender) => sender.Send(new PingQuery());

// After
static ValueTask<string> Ping(ISender sender) => sender.Send(new PingQuery());
```

Pass `.AsTask()` to `Task.WhenAll`. `ISender.CreateStream` is gone.

## Pipeline behaviors (`IPipelineBehavior<,>`)

`RequestHandlerDelegate<TResponse>` is `MessageHandlerDelegate<TRequest, TResponse>`, and `next` takes the
request:

```csharp
public ValueTask<TResponse> Handle(
  TRequest request,
  MessageHandlerDelegate<TRequest, TResponse> next,
  CancellationToken cancellationToken) =>
  next(request, cancellationToken);
```

Nuru's own `INuruBehavior` is unchanged.

## Package references

- A project that references `TimeWarp.Nuru` needs no change. Nuru brings `TimeWarp.Mediator` 15 and
  `TimeWarp.Mediator.Generators` 15.
- A contracts library that referenced `TimeWarp.Mediator.Contracts` references `TimeWarp.Mediator`
  instead, without `PrivateAssets`. Keep `[assembly: TimeWarp.Mediator.MediatorAssembly]` in it so the
  app's generated mediator dispatches its handlers. Do not reference `TimeWarp.Mediator.Generators` from
  a library.
- `TimeWarp.Mediator.Analyzers` is no longer a package. TWM001 to TWM004 ship inside `TimeWarp.Mediator`
  and report only in a project that references it directly.
- A project that pins `TimeWarp.Contracts` moves to 2.0.0-beta.31 or later. Earlier betas depend on
  `TimeWarp.Mediator.Contracts` 14, whose types collide with Mediator 15.

## Build properties

- `TimeWarpMediatorProfile` is gone. Delete it.
- `TimeWarpMediatorAssembly=false` no longer stops the mediator generator. It only sets analyzer
  membership. Nuru detects the generated mediator from `TimeWarpMediatorGenerator`, which the generator
  package sets, and registers it from `TimeWarpMediatorNamespace` (default `TimeWarp.Mediator.Generated`).

## Handlers Nuru cannot see

Mediator 15 dispatches only handlers in its compile-time graph and registers concrete handler types,
not handler interfaces. A route whose handler was not visible at build time (NURU_H008) writes
`No handler is registered for <type>.` and exits 1. A handler added to the service collection at run time,
under `UseMicrosoftDependencyInjection()`, is not used.

## Checklist for a consumer repo

1. `TimeWarp.Contracts` to 2.0.0-beta.31 or later, if the repo pins it.
2. `TimeWarp.Mediator.Contracts` to `TimeWarp.Mediator` 15, if the repo references it directly.
3. Every `ICommandHandler<`, `IQueryHandler<`, `IIdempotentCommandHandler<` to `IRequestHandler<`.
4. Every handler `Task<T> Handle(` to `ValueTask<T> Handle(`.
5. Every `Unit.Task` to `Unit.ValueTask`.
6. `Task.FromResult` / `Task.CompletedTask` returned from a handler to the `ValueTask` form.
7. Every method that returns `ISender.Send(...)` or `IPublisher.Publish(...)` directly to a `ValueTask` return type.
8. Every `IPipelineBehavior` to `MessageHandlerDelegate<TRequest, TResponse>` and `next(request, ct)`.
9. Delete `TimeWarpMediatorProfile`. Re-read any `TimeWarpMediatorAssembly=false` that was meant to stop emission.
10. Build. CS0535 (interface member not implemented), CS0246 (type not found), and CS0029 (cannot convert
    `ValueTask` to `Task`) point at anything left.
