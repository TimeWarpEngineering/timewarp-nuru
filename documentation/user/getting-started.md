# Getting Started with TimeWarp.Nuru

Build your first command-line application in 5 minutes.

## Installation

```bash
dotnet new console -n MyCliApp
cd MyCliApp
dotnet add package TimeWarp.Nuru
```

## Two Ways to Define Commands

TimeWarp.Nuru provides two first-class patterns for defining commands. Choose based on your preference - both are fully supported.

### Approach 1: Fluent DSL

Define routes inline with a fluent builder pattern:

```csharp
using TimeWarp.Nuru;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("add {x:double} {y:double}")
    .WithHandler((double x, double y) => Console.WriteLine($"{x} + {y} = {x + y}"))
    .AsCommand()
    .Done()
  .Map("greet {name}")
    .WithHandler((string name) => Console.WriteLine($"Hello, {name}!"))
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);
```

### Approach 2: Attributed Routes

Define commands as classes with attributes - auto-discovered at build time:

**Program.cs:**
```csharp
using TimeWarp.Nuru;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
```

**AddCommand.cs:**
```csharp
using TimeWarp.Nuru;

[NuruRoute("add", Description = "Add two numbers")]
public sealed class AddCommand : ICommand<Unit>
{
  [Parameter(Description = "First number")]
  public double X { get; set; }

  [Parameter(Description = "Second number")]
  public double Y { get; set; }

  public sealed class Handler : ICommandHandler<AddCommand, Unit>
  {
    public ValueTask<Unit> Handle(AddCommand command, CancellationToken ct)
    {
      Console.WriteLine($"{command.X} + {command.Y} = {command.X + command.Y}");
      return default;
    }
  }
}
```

## Run It

```bash
dotnet run -- add 15 25
# Output: 15 + 25 = 40

dotnet run -- greet Alice
# Output: Hello, Alice!
```

## Choosing Your Approach

| Aspect | Fluent DSL | Attributed Routes |
|--------|------------|-------------------|
| Best for | Simple apps, scripts, quick prototypes | Larger apps, separation of concerns |
| Organization | Single file possible | Commands in separate files |
| Testability | Inline handlers | Handlers injected via DI |
| Discovery | Explicit `.Map()` calls | Auto-discovered via source generator |

Both approaches:
- Use the same route pattern syntax
- Support async handlers
- Work with pipeline behaviors
- Are fully AOT compatible

## Understanding Route Patterns

### Literals and Parameters
```
"greet {name}"         - "greet" is literal, {name} is a parameter
"add {x} {y}"          - Multiple parameters
```

### Typed Parameters
```
"{count:int}"          - Integer
"{amount:double}"      - Floating point
"{enabled:bool}"       - Boolean (true/false)
"{when:datetime}"      - DateTime
"{id:guid}"            - GUID
```

### Optional Parameters
```
"{name?}"              - Optional (nullable in handler)
"{count:int?}"         - Optional with type
```

### Options (Flags)
```
"deploy --force"       - Boolean flag
"deploy --env {env}"   - Option with value
"deploy -f --env {e}"  - Short and long forms
```

### Catch-All
```
"echo {*words}"        - Captures remaining args as string[]
```

## Commands vs Queries

- **Command** (`.AsCommand()`): Performs an action, may have side effects
- **Query** (`.AsQuery()`): Returns information, no side effects
- **IdempotentCommand** (`.AsIdempotentCommand()`): Safe to retry

## Adding Features

### Pipeline Behaviors

Add cross-cutting concerns like logging, telemetry, or authorization:

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .AddBehavior(typeof(LoggingBehavior))
  .AddBehavior(typeof(PerformanceBehavior))
  .Map("deploy {env}")
    .WithHandler((string env) => Deploy(env))
    .AsCommand()
    .Done()
  .Build();

public sealed class LoggingBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    Console.WriteLine($"[LOG] Handling {context.CommandName}");
    await proceed();
    Console.WriteLine($"[LOG] Completed {context.CommandName}");
  }
}
```

### Configuration

Options are automatically bound from configuration sections:

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("config show")
    .WithHandler((IOptions<DatabaseOptions> dbOptions) =>
    {
      Console.WriteLine($"Host: {dbOptions.Value.Host}");
      Console.WriteLine($"Port: {dbOptions.Value.Port}");
    })
    .AsQuery()
    .Done()
  .Build();

public class DatabaseOptions
{
  public string Host { get; set; } = "localhost";
  public int Port { get; set; } = 5432;
}
```

Convention: `DatabaseOptions` binds to the `"Database"` config section (strips "Options" suffix).

### REPL Mode

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("greet {name}")
    .WithHandler((string name) => Console.WriteLine($"Hello, {name}!"))
    .AsQuery()
    .Done()
  .AddRepl(options =>
  {
    options.Prompt = "myapp> ";
    options.WelcomeMessage = "Welcome! Type '--help' for commands.";
  })
  .Build();
```

Run with `-i` or `--interactive` to enter REPL mode.

## Next Steps

- **[Route Patterns](features/routing.md)** - Complete syntax reference
- **[Pipeline Behaviors](features/pipeline-behaviors.md)** - Middleware for commands
- **[Attributed Routes](features/attributed-routes.md)** - Deep dive on class-based commands
- **[Configuration](features/configuration.md)** - Settings and dependency injection
- **[REPL Mode](guides/using-repl-mode.md)** - Interactive shell
- **[Samples](../../samples/)** - Working examples

## Common Questions

### How is this different from other CLI frameworks?

TimeWarp.Nuru uses **compile-time source generation** for routing. Benefits:
- Zero runtime overhead
- Native AOT compatible
- Compile-time validation via Roslyn analyzer
- No reflection required

### Can I mix both approaches?

Yes! Use fluent DSL for simple commands and attributed routes for complex ones in the same app:

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("version")
    .WithHandler(() => Console.WriteLine("1.0.0"))
    .AsQuery()
    .Done()
  .DiscoverEndpoints()  // Also discovers attributed routes
  .Build();
```

### What about help text?

Help is automatic. Add descriptions with `.WithDescription()` or the `Description` property on `[NuruRoute]`:

```csharp
.Map("deploy {env|Target environment}")
  .WithDescription("Deploy the application to an environment")
  .WithHandler((string env) => Deploy(env))
  .AsCommand()
  .Done()
```

```bash
dotnet run -- --help
dotnet run -- deploy --help
```
