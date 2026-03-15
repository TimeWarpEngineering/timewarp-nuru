# 444 - TimeWarp.ServiceGen: Source-Generated AOT-Friendly DI Container

## Description

Extract Nuru's compile-time service resolution (currently in `ServiceResolverEmitter`) into a standalone, reusable source-generated DI container. This mirrors the Microsoft.Extensions.DependencyInjection API surface but resolves everything at compile time — zero reflection, fully AOT-compatible.

This is the foundation layer for both TimeWarp.Mediator (task 443) and TimeWarp.Nuru.

### What Nuru Already Does Today

Nuru's `ServiceResolverEmitter` already implements the core of this:
- Analyzes constructor parameters at compile time
- Emits `new Service(dep1, dep2)` calls with resolved dependencies
- Supports `Lazy<T>` singletons (thread-safe, one-time init)
- Supports transient creation (new instance per use)
- Recognizes built-in services (ITerminal, IConfiguration, NuruApp, ILogger<T>)
- Has a separate Microsoft DI path (`UseMicrosoftDependencyInjection()`)

ServiceGen generalizes this into a reusable package.

## Requirements

### Abstractions

- [ ] Registration API: `AddSingleton<T>()`, `AddSingleton<TInterface, TImpl>()`, `AddTransient<T>()`, `AddScoped<T>()`
- [ ] Factory overloads: `AddSingleton<T>(Func<T> factory)`
- [ ] Instance overloads: `AddSingleton<T>(T instance)`

### Source Generator

- [ ] Scan service registrations in compilation (fluent API calls or attributes)
- [ ] Analyze constructor parameters for each registered service
- [ ] Emit factory methods with resolved dependencies (no reflection)
- [ ] Emit `Lazy<T>` fields for singletons
- [ ] Emit direct `new` calls for transients
- [ ] Scoped lifetime support (scope context tracking)
- [ ] Detect circular dependencies at compile time (emit diagnostic error)
- [ ] Detect missing registrations at compile time (emit diagnostic error)

### Microsoft DI Bridge

- [ ] Optional bridge: populate `IServiceCollection` from source-gen registrations
- [ ] Optional bridge: resolve from `IServiceProvider` when Microsoft DI is active
- [ ] Allow mixing static and runtime resolution (static for known types, runtime for externals)

### Diagnostics

- [ ] Analyzer: warn on unresolvable constructor parameters
- [ ] Analyzer: error on circular dependencies
- [ ] Analyzer: warn on missing registrations

## Design Decisions Needed

- **Registration style**: Fluent API (`builder.AddSingleton<T>()`) vs attribute-based (`[Singleton]` on class) vs both
- **Scope semantics**: What defines a scope boundary? (HTTP request, CLI command invocation, explicit `using` block)
- **Open generics**: Support `AddSingleton(typeof(IRepository<>), typeof(Repository<>))`? Complex for source gen but valuable
- **Keyed services**: Support keyed/named registrations (aligns with .NET 8 keyed services)?

## Notes

- Nuru's `ServiceResolverEmitter` is the starting point — extract and generalize
- Must work standalone (no Nuru or Mediator dependency)
- Three generators (ServiceGen + Mediator + Nuru) compose through shared compiled types, not generated output
- Build this first — Mediator and Nuru migration depend on it
