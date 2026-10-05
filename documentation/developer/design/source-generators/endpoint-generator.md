# Endpoint Source Generator

Design documentation for endpoint support in the Nuru source generator (`NuruGenerator`), which turns `[NuruRoute]` classes into routes without explicit `Map()` calls.

## Overview

The generator's `EndpointExtractor` scans for classes decorated with `[NuruRoute]` and extracts, for each one:
1. The route definition (literal, group prefix, aliases, `[Parameter]` and `[Option]` properties)
2. The message type (`ICommand<T>`, `IQuery<T>`) inferred from the implemented interface
3. The nested `Handler` class that handles the request

The extracted route feeds the same route model as the fluent `Map("pattern")` DSL, so matching, help and completion code is emitted the same way for both styles. Endpoint classes are collected globally across the compilation; `.DiscoverEndpoints()` or `.Map<TEndpoint>()` selects which ones an app includes.

## Motivation

With the fluent DSL, every route is declared inline in the app builder:

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .Map("deploy {env}")
    .WithHandler((string env) => Console.WriteLine($"Deploying to {env}"))
    .AsCommand().Done()
  .Build();
```

With endpoints, the route lives on a request class and the app only opts in:

```csharp
[NuruRoute("deploy", Description = "Deploy to an environment")]
public sealed class DeployCommand : ICommand<Unit>
{
  [Parameter]
  public string Env { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<DeployCommand, Unit>
  {
    public Task<Unit> Handle(DeployCommand command, CancellationToken cancellationToken)
    {
      Console.WriteLine($"Deploying to {command.Env}");
      return Task.FromResult(default(Unit));
    }
  }
}

// Include every [NuruRoute] endpoint in the compilation
NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

// Or include specific endpoints only
NuruApp app2 = NuruApp.CreateBuilder()
  .Map<DeployCommand>()
  .Build();
```

The mediator contracts (`ICommand<T>`, `IQuery<T>`, `Unit`, handlers) come from `TimeWarp.Mediator`; handlers return `Task<T>`.

## Attributes

### `[NuruRoute]`

Applied to request classes. Specifies the route pattern: a single literal, or `""` for the default route. Multi-word patterns (e.g. `"deploy {env}"` or `"git status"`) are rejected with NURU_A001; use `[NuruRouteGroup]` on a base class for multi-word routes.

```csharp
[NuruRoute("deploy", Description = "Deploy to an environment")]
public sealed class DeployCommand : ICommand<Unit> { /* nested Handler omitted */ }
```

**Properties:**
- `Pattern` (constructor) - A single route literal. Use `""` for default route.
- `Description` - Help text for the route.

### `[NuruRouteAlias]`

Registers additional patterns for the same request type.

```csharp
[NuruRoute("goodbye")]
[NuruRouteAlias("bye", "cya")]
public sealed class GoodbyeRequest { }
```

Generates three routes: `goodbye`, `bye`, `cya` - all mapping to `GoodbyeRequest`.

### `[NuruRouteGroup]`

Applied to base classes. Provides shared prefix and options for derived requests.

```csharp
[NuruRouteGroup("docker")]
public abstract class DockerRequestBase
{
  [GroupOption("debug", "D")]
  public bool Debug { get; set; }
}

[NuruRoute("run")]
public sealed class DockerRunRequest : DockerRequestBase { }
// Generated pattern: "docker run --debug,-D"
```

### `[Parameter]`

Marks a property as a positional parameter.

```csharp
[Parameter(Description = "Target environment")]
public string Env { get; set; } = string.Empty;

[Parameter(IsCatchAll = true)]
public string[] Args { get; set; } = [];
```

**Properties:**
- `Name` - Override parameter name (defaults to property name in camelCase)
- `Order` - Position in the argument list (required when a command has multiple parameters)
- `Description` - Help text
- `IsCatchAll` - Captures all remaining arguments

**Optionality:** Inferred from nullability. `string?` = optional, `string` = required.

### `[Option]`

Marks a property as a command-line option.

```csharp
[Option("force", "f", Description = "Skip confirmation")]
public bool Force { get; set; }

[Option("config", "c")]
public string? ConfigFile { get; set; }

[Option("replicas", "r")]
public int Replicas { get; set; } = 1;
```

**Constructor:**
- `longForm` - Long option name (without `--`)
- `shortForm` - Short option name (without `-`), optional

**Properties:**
- `Description` - Help text
- `IsRepeated` - Option can be specified multiple times

**Type inference:**
- `bool` → Flag (no value expected)
- Other types → Valued option (`expectsValue: true`)
- Nullable types → Optional value (`parameterIsOptional: true`)

### `[GroupOption]`

Same as `[Option]` but defined on a group base class. Inherited by all derived requests.

```csharp
[NuruRouteGroup("docker")]
public abstract class DockerRequestBase
{
  [GroupOption("debug", "D")]
  public bool Debug { get; set; }
}
```

## How the Generator Uses Endpoints

Endpoint classes do not generate registration code of their own (there is no `[ModuleInitializer]` or runtime route registry). The route model they produce is merged into the app's intercepted `Build()` call and emitted alongside fluent routes.

### Simple Route

For a request like:

```csharp
[NuruRoute("deploy", Description = "Deploy to an environment")]
public sealed class DeployCommand : ICommand<Unit>
{
  [Parameter]
  public string Env { get; set; } = string.Empty;

  [Option("force", "f")]
  public bool Force { get; set; }

  public sealed class Handler : ICommandHandler<DeployCommand, Unit>
  {
    public Task<Unit> Handle(DeployCommand command, CancellationToken cancellationToken) =>
      Task.FromResult(default(Unit));
  }
}
```

The extractor builds the equivalent of the pattern `deploy {env} --force,-f` with the description "Deploy to an environment".

### Grouped Route

For a grouped request like:

```csharp
[NuruRouteGroup("docker")]
public abstract class DockerRequestBase
{
  [GroupOption("debug", "D", Description = "Enable debug mode")]
  public bool Debug { get; set; }
}

[NuruRoute("run", Description = "Run a container")]
public sealed class DockerRunCommand : DockerRequestBase, ICommand<Unit>
{
  [Parameter(Description = "Image name")]
  public string Image { get; set; } = string.Empty;

  [Option("detach", "d")]
  public bool Detach { get; set; }

  public sealed class Handler : ICommandHandler<DockerRunCommand, Unit>
  {
    public Task<Unit> Handle(DockerRunCommand command, CancellationToken cancellationToken) =>
      Task.FromResult(default(Unit));
  }
}
```

The extractor walks the inheritance chain, so the group prefix `docker` and the group option `--debug,-D` are inherited from `DockerRequestBase`, giving the pattern `docker run {image} --debug,-D --detach,-d`.

## Generation Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                        COMPILE TIME                                  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  1. Generator collects all [NuruRoute] classes in the compilation   │
│                     ↓                                                │
│  2. EndpointExtractor reads the attribute, [Parameter]/[Option]     │
│     properties, message type and nested Handler                     │
│                     ↓                                                │
│  3. Walks inheritance chain for [NuruRouteGroup]                    │
│                     ↓                                                │
│  4. Per app, .DiscoverEndpoints() / .Map<T>() select the endpoints  │
│                     ↓                                                │
│  5. Emitters generate matching, help and handler-invocation code    │
│     in the interceptor for the app's Build()/RunAsync()             │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

Nothing is registered at runtime: all routing code is generated at compile time, which keeps the result AOT-compatible.

## Design Decisions

### 1. Attributes on Properties, Not Constructor Parameters

**Decision:** Use properties with attributes, not primary constructors or record parameters.

**Rationale:**
- Primary constructors don't allow per-parameter attributes easily
- Properties are clearer for binding semantics
- Matches the Mediator pattern of request classes

### 2. No Dashes in Attribute Parameters

**Decision:** `[Option("force", "f")]` not `[Option("--force", "-f")]`

**Rationale:**
- The generator adds `--` and `-` prefixes when building the route
- Cleaner syntax - users don't type dashes that would just be stripped

### 3. Nullability-Based Optionality

**Decision:** `string?` = optional, `string` = required

**Rationale:**
- Consistent with C# nullability semantics
- No extra `IsOptional` property needed
- Compiler enforces handling of optional values

### 4. Inheritance-Based Grouping

**Decision:** `[NuruRouteGroup]` on base class, not a separate grouping construct

**Rationale:**
- Natural C# pattern - inheritance already conveys "is-a" relationship
- Group options inherited via normal property inheritance
- No special container classes needed

### 5. Aliases Expand to Full Patterns

**Decision:** Each `[NuruRouteAlias]` (and group alias) is expanded to a complete alias pattern that replaces the group prefix plus route literal

**Rationale:**
- Aliases can have different pattern strings in help
- The emitter does not need to re-match literal segments after an alias prefix

## Implementation Details

### Source Files

- `source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs` - Extracts route definitions from `[NuruRoute]` classes
- `source/timewarp-nuru-analyzers/generators/nuru-generator.cs` - Collects endpoints globally and selects them per app

### Key Concepts

- `EndpointExtractor.Extract()` - Semantic analysis of one `[NuruRoute]` class into a route definition (or diagnostics, e.g. NURU_A001)
- Endpoint scoping - All `[NuruRoute]` classes in a compilation are collected globally; `.DiscoverEndpoints()` includes all of them, `.Map<T>()` only that type
- Nested `Handler` - A class without a nested `Handler` class is skipped

## Sample Applications

Working examples are in [samples/endpoints/](../../../../samples/endpoints/).

Features demonstrated:
- Simple routes with parameters and options (`01-hello-world`, `02-calculator`, `03-syntax`)
- Async handlers and pipeline behaviors (`04-async`, `05-pipeline`)
- Grouped routes with shared options (`14-group-options`)

Run a sample:
```bash
dotnet run samples/endpoints/14-group-options/group-options.cs -- git status --verbose
```

## Testing

Endpoint tests are in `tests/timewarp-nuru-tests/generator/`, for example `generator-11-endpoints.cs`.

## Related Documentation

- [Route Pattern Anatomy](../parser/route-pattern-anatomy.md)
- [Syntax Rules](../parser/syntax-rules.md)
- [Error Handling](../cross-cutting/error-handling.md)
