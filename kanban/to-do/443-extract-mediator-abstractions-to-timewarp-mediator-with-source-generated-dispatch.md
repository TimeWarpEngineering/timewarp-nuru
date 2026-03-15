# 443 - Extract Mediator Abstractions to TimeWarp.Mediator with Source-Generated Dispatch

## Description

Three-package architecture for source-generated, AOT-friendly infrastructure. Extracts reusable concerns from Nuru into independent libraries that compose through shared abstractions.

### Package Architecture

```
TimeWarp.ServiceGen          (source-generated DI container)
├── Service registration API (AddSingleton<T>, AddTransient<T>, AddScoped<T>)
├── Source generator: scans registrations, emits factory methods
├── Constructor dependency resolution at compile time
├── Optional bridge to Microsoft.Extensions.DependencyInjection

TimeWarp.Mediator            (depends on ServiceGen)
├── ISender<TScope>, IPublisher<TScope>, INotification
├── Handler/message abstractions
├── Source generator: emits typed dispatch, uses ServiceGen for handler DI

TimeWarp.Nuru                (depends on Mediator, which brings in ServiceGen)
├── CLI routing, behaviors, NuruApp
├── Source generator: emits interceptors, route matching
├── Uses ServiceGen for all service resolution (replaces current ServiceResolverEmitter)
```

Depends on: task 444 (TimeWarp.ServiceGen)

## Requirements

### Phase 1: TimeWarp.Mediator Abstractions

- [ ] Extract from Nuru: IMessage, IIdempotent, IQuery<T>, ICommand<T>, IIdempotentCommand<T>, Unit
- [ ] Extract handler interfaces: IQueryHandler<,>, ICommandHandler<,>, IIdempotentCommandHandler<,>
- [ ] Add ISender<TScope> interface (Send for commands, queries, idempotent commands)
- [ ] Add unscoped ISender interface (default pipeline, no scope marker needed)
- [ ] Add INotification marker interface
- [ ] Add INotificationHandler<T> interface
- [ ] Add IPublisher<TScope> and unscoped IPublisher interfaces

### Phase 2: Source Generator for Dispatch

- [ ] Generator scans compilation for handler implementations
- [ ] Emit concrete Sender class per TScope with type-switched dispatch (no reflection)
- [ ] Emit concrete Publisher class per TScope with fan-out to all INotificationHandler<T> implementations
- [ ] Use Unsafe.As for generic return type bridging (AOT-safe, generator guarantees type match)
- [ ] Use TimeWarp.ServiceGen for handler constructor dependency resolution
- [ ] Optional Microsoft DI integration path (via ServiceGen bridge)

### Phase 3: Update Nuru

- [ ] Add TimeWarp.Mediator as package dependency (brings in ServiceGen transitively)
- [ ] Update Nuru generator to emit `global::TimeWarp.Mediator.*` type references
- [ ] Remove extracted abstractions from Nuru source (IMessage, IQuery, ICommand, handlers, Unit, IIdempotent)
- [ ] Replace Nuru's ServiceResolverEmitter with TimeWarp.ServiceGen
- [ ] Ensure Nuru's static DI and Microsoft DI paths both resolve ISender/IPublisher
- [ ] Verify all existing tests pass with abstractions sourced from TimeWarp.Mediator

## Design Decisions Needed

- **Handler discovery**: Assembly-wide scan vs explicit registration (explicit avoids cross-contamination in multi-project compilations)
- **Handler lifetime**: Singleton (like Nuru), Transient, or Scoped — ServiceGen handles this uniformly
- **Pipeline behaviors**: Should TimeWarp.Mediator have its own behavior pipeline (independent of Nuru's INuruBehavior)?
- **Typed/scoped pipelines**: Support `ISender<TScope>` so different contexts get isolated pipelines with their own handlers and behaviors. Solves the problem where a single shared IMediator (e.g., in TimeWarp.State Blazor apps) forces every behavior to filter for the types it cares about. With `ISender<TScope>`, the generator emits separate dispatch implementations per scope — zero runtime filtering (e.g., `ISender<ClientPipeline>`, `ISender<ServerPipeline>`).
- **Namespace**: `TimeWarp.Mediator` for all extracted types

## Notes

- TimeWarp.Mediator repo already exists as a fork of MediatR before it went commercial — will be rewritten with source-gen approach
- Three source generators (ServiceGen + Mediator + Nuru) coexist fine: each generator's inputs are regular compiled types (interfaces, attributes), not other generators' output
- Build order: ServiceGen first → Mediator on top → Nuru migration last
