# Attributed Routes

Attributed routes provide a class-based approach to defining CLI commands. The source generator discovers classes with `[NuruRoute]` at compile time and registers them automatically.

This is one of two first-class patterns for defining routes in TimeWarp.Nuru:
- **Attributed routes** (this document) - Class-based with attributes
- **Fluent DSL** - Inline with `Map()` calls (see [Builder API](../reference/builder-api.md))

Both patterns are fully supported and can be mixed in the same application.

## Overview

With attributed routes:
- Define commands as classes with `[NuruRoute]` attribute
- Parameters and options declared as properties with `[Parameter]` and `[Option]`
- Handlers are nested classes implementing handler interfaces
- Source generator discovers and registers routes at compile time

## Basic Example

```csharp
// Program.cs
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Build();

return await app.RunAsync(args);
```

```csharp
// GreetQuery.cs
[NuruRoute("greet", Description = "Greet someone by name")]
public sealed class GreetQuery : IQuery<Unit>
{
  [Parameter(Description = "Name of the person to greet")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler : IQueryHandler<GreetQuery, Unit>
  {
    public ValueTask<Unit> Handle(GreetQuery query, CancellationToken ct)
    {
      Console.WriteLine($"Hello, {query.Name}!");
      return default;
    }
  }
}
```

```bash
./myapp greet Alice    # Output: Hello, Alice!
```

## Attributes Reference

### [NuruRoute]

Applied to command/query classes to register the route pattern:

```csharp
[NuruRoute("pattern", Description = "Help text")]
public sealed class MyCommand : ICommand<Unit> { }
```

- **pattern** - The route pattern (literals only; parameters come from properties)
- **Description** - Optional help text displayed in auto-generated help

### [NuruRouteAlias]

Register multiple patterns for the same command:

```csharp
[NuruRoute("goodbye", Description = "Say goodbye and exit")]
[NuruRouteAlias("bye", "cya")]
public sealed class GoodbyeCommand : ICommand<Unit> { }
```

All three patterns (`goodbye`, `bye`, `cya`) invoke the same handler.

### [NuruRouteGroup]

Applied to base classes to provide a shared prefix for derived commands:

```csharp
[NuruRouteGroup("docker")]
public abstract class DockerGroupBase;

[NuruRoute("run", Description = "Run a container")]
public sealed class DockerRunCommand : DockerGroupBase, ICommand<Unit>
{
  [Parameter] public string Image { get; set; } = string.Empty;
}
// Effective pattern: docker run {image}
```

Group base classes can also define shared options using `[GroupOption]`.

### [Parameter]

Marks a property as a positional parameter:

```csharp
[Parameter(Description = "Target environment")]
public string Env { get; set; } = string.Empty;

[Parameter(Order = 1, Description = "Optional deployment tag")]
public string? Tag { get; set; }

[Parameter(IsCatchAll = true, Description = "Arguments to forward")]
public string[] Args { get; set; } = [];
```

| Property | Description |
|----------|-------------|
| `Order` | Position in the pattern (0-based, inferred from declaration order if omitted) |
| `Description` | Help text for the parameter |
| `IsCatchAll` | Captures all remaining arguments as `string[]` |

### [Option]

Defines a named option with long form (`--name`) and optional short form (`-n`):

```csharp
[Option("force", "f", Description = "Skip confirmation prompt")]
public bool Force { get; set; }

[Option("config", "c", Description = "Path to config file")]
public string? ConfigFile { get; set; }

[Option("replicas", "r", Description = "Number of replicas")]
public int Replicas { get; set; } = 1;
```

| Type | Behavior |
|------|----------|
| `bool` | Flag option (presence = true, absence = false) |
| Other types | Expects a value after the option |

### [GroupOption]

Same as `[Option]` but defined on a group base class and inherited by all derived commands:

```csharp
[NuruRouteGroup("docker")]
public abstract class DockerGroupBase
{
  [GroupOption("debug", "D", Description = "Enable debug output")]
  public bool Debug { get; set; }
}
```

## Interfaces

### ICommand\<TResult\>

Commands perform actions with side effects (create, update, delete):

```csharp
[NuruRoute("deploy")]
public sealed class DeployCommand : ICommand<Unit>
{
  [Parameter] public string Env { get; set; } = string.Empty;
  // ...
}
```

### IQuery\<TResult\>

Queries return data without side effects (safe to retry):

```csharp
[NuruRoute("status")]
public sealed class GetStatusQuery : IQuery<StatusResult>
{
  // ...
}
```

The distinction enables semantic clarity and potential optimizations (caching, retries).

## Handler Pattern

Handlers are nested classes implementing `ICommandHandler<T, TResult>` or `IQueryHandler<T, TResult>`:

```csharp
[NuruRoute("deploy")]
public sealed class DeployCommand : ICommand<Unit>
{
  [Parameter] public string Env { get; set; } = string.Empty;
  [Option("force", "f")] public bool Force { get; set; }

  public sealed class Handler(ILogger<DeployCommand> logger, IDeployService deploy)
    : ICommandHandler<DeployCommand, Unit>
  {
    public async ValueTask<Unit> Handle(DeployCommand cmd, CancellationToken ct)
    {
      logger.LogInformation("Deploying to {Env}", cmd.Env);
      await deploy.DeployAsync(cmd.Env, cmd.Force, ct);
      return Unit.Value;
    }
  }
}
```

Handlers support:
- **Constructor injection** - Receive services from the DI container
- **CancellationToken** - Handle cancellation for async operations
- **ValueTask\<T\>** - Efficient for both sync and async implementations

## Optionality

Parameter optionality is inferred from nullability:

```csharp
[Parameter] public string Env { get; set; } = string.Empty;     // Required
[Parameter] public string? Tag { get; set; }                     // Optional
```

Option optionality follows the same pattern:

```csharp
[Option("config", "c")] public string? ConfigFile { get; set; } // Optional
[Option("replicas", "r")] public int Replicas { get; set; } = 1; // Has default
```

## Mixing with Fluent DSL

Both patterns work together in the same application:

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("version")
    .WithHandler(() => Console.WriteLine("1.0.0"))
    .AsQuery()
    .Done()
  // Attributed routes are discovered automatically
  .Build();
```

Use whichever pattern fits each use case:
- **Fluent DSL** - Simple one-off routes, quick prototyping
- **Attributed routes** - Complex commands with many options, testable handlers with DI

## Complete Example

See [samples/03-attributed-routes/](../../../samples/03-attributed-routes/) for a complete working example demonstrating:

- Simple parameters (`greet {name}`)
- Options with short/long forms (`deploy --force,-f`)
- Route aliases (`goodbye`, `bye`, `cya`)
- Route groups (`docker run`, `docker build`)
- Catch-all parameters (`exec {*args}`)
- Command/Query separation
- Handler dependency injection

## See Also

- [Builder API](../reference/builder-api.md) - Fluent DSL alternative
- [Routing](routing.md) - Route pattern syntax reference
- [Auto-Help](auto-help.md) - Generating help from routes
