# Builder API Reference

Complete reference for the `NuruApp.CreateBuilder()` fluent API.

## Entry Point

All Nuru applications start with `NuruApp.CreateBuilder()`:

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
  // ... configuration
  .Build();

return await app.RunAsync(args);
```

The builder uses a fluent API pattern where each method returns the builder for chaining.

## Route Definition

Routes are defined using the `.Map()` method followed by configuration methods.

### Basic Pattern

```csharp
.Map("pattern {param}")
  .WithHandler((Type param) => { })
  .WithDescription("Help text")
  .AsCommand()  // or .AsQuery() or .AsIdempotentCommand()
  .Done()
```

### Route Definition Methods

| Method | Description |
|--------|-------------|
| `.Map(pattern)` | Define a route with a string pattern |
| `.Map<TEndpoint>()` | Include a specific attributed endpoint class |
| `.Map(configureRoute)` | Define route using fluent route builder |

### Endpoint Configuration Methods

These methods are called on the `EndpointBuilder` returned by `.Map()`:

| Method | Description |
|--------|-------------|
| `.WithHandler(delegate)` | Attach the handler (lambda, method group, or delegate) |
| `.WithDescription(text)` | Add help text for the route |
| `.AsQuery()` | Mark as read-only operation (safe to retry) |
| `.AsCommand()` | Mark as state-changing operation (default) |
| `.AsIdempotentCommand()` | Mark as repeatable state-changing operation |
| `.Implements<T>(configure)` | Declare interface implementation for filtered behaviors |
| `.Done()` | Complete route definition, return to builder |
| `.Build()` | Complete route and build app (shortcut) |

### Route Classification

Route classification informs AI agents and tools how to treat commands:

- **Query** (`.AsQuery()`): No state change. Safe to run freely and retry on failure.
  - Examples: `list`, `get`, `status`, `show`, `describe`

- **Command** (`.AsCommand()`): State change, not repeatable. Confirm before running.
  - Examples: `create`, `append`, `send`, `delete`

- **Idempotent Command** (`.AsIdempotentCommand()`): State change but repeatable. Safe to retry.
  - Examples: `set`, `enable`, `disable`, `upsert`, `update`

## Builder Methods

### Route Registration

```csharp
// String pattern
.Map("deploy {env}")
  .WithHandler((string env) => Deploy(env))
  .Done()

// Attributed endpoint class
.Map<DeployCommand>()

// Auto-discover all [NuruRoute] classes
.DiscoverEndpoints()
```

### Route Groups

```csharp
.WithGroupPrefix("admin")
  .Map("status")
    .WithHandler(() => "admin status")
    .Done()
  .WithGroupPrefix("config")  // Nested: "admin config"
    .Map("get {key}")         // Route: "admin config get {key}"
      .WithHandler((string key) => $"value: {key}")
      .Done()
    .Done()  // End config group
  .Done()    // End admin group
```

### Application Metadata

```csharp
.WithName("myapp")           // Application name for help display
.WithDescription("My CLI")   // Application description for help
```

### Configuration

```csharp
// Add standard .NET configuration sources
// (appsettings.json, environment variables, command line, user secrets)
.AddConfiguration()

// Or with explicit args
.AddConfiguration(args)
```

### Dependency Injection

```csharp
.ConfigureServices(services =>
{
  services.AddSingleton<IMyService, MyService>();
  services.AddLogging(config => config.AddConsole());
})

// With access to configuration
.ConfigureServices((services, config) =>
{
  services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(config?.GetConnectionString("Default")));
})
```

### Pipeline Behaviors

```csharp
// Register behaviors (execute in registration order)
.AddBehavior(typeof(LoggingBehavior))
.AddBehavior(typeof(ValidationBehavior))
.AddBehavior(typeof(RetryBehavior))
```

Behaviors wrap handler execution like middleware. See [Pipeline Behaviors](../features/pipeline-behaviors.md).

### Type Converters

```csharp
// Register custom type converter
.AddTypeConverter(new EnumTypeConverter<MyEnum>())
.AddTypeConverter(new CustomTypeConverter())
```

### REPL Mode

```csharp
// Enable REPL with defaults
.AddRepl()

// Enable REPL with options
.AddRepl(options =>
{
  options.Prompt = "myapp> ";
  options.WelcomeMessage = "Welcome!";
  options.GoodbyeMessage = "Goodbye!";
  options.PersistHistory = true;
})
```

### Shell Completion

```csharp
// Enable shell tab completion
.EnableCompletion()

// With custom completion sources
.EnableCompletion(configure: registry =>
{
  registry.RegisterForParameter("env", new MyEnvCompletionSource());
  registry.RegisterForType(typeof(MyEnum), new EnumCompletionSource<MyEnum>());
})
```

### Telemetry

```csharp
// Enable OpenTelemetry with OTLP export
.UseTelemetry()

// With options
.UseTelemetry(options =>
{
  options.ServiceName = "my-cli";
  options.EnableTracing = true;
  options.EnableMetrics = true;
})
```

### Help Configuration

```csharp
.ConfigureHelp(options =>
{
  options.ShowPerCommandHelpRoutes = false;
})
```

### Terminal

```csharp
// For testing: inject a test terminal
.UseTerminal(testTerminal)

// For custom logging
.UseLogging(loggerFactory)
```

### Build

```csharp
// Build the application
NuruCoreApp app = builder.Build();
```

## App Methods

Methods available on `NuruCoreApp` after building:

| Method | Description |
|--------|-------------|
| `.RunAsync(args)` | Run the app with command-line arguments |
| `.RunReplAsync(token)` | Run in REPL mode (requires `.AddRepl()`) |

### Exit Codes

- **0**: Success
- **Non-zero**: Failure (handler threw exception)

Handler return values are written to terminal output, not used as exit codes:

```csharp
// Outputs "42" to terminal, returns exit code 0
.Map("answer")
  .WithHandler(() => 42)
  .Done()

// To signal failure, throw an exception
.Map("fail")
  .WithHandler(() => throw new Exception("Something went wrong"))
  .Done()
```

## Handler Signatures

Handlers support various signatures:

### Synchronous

```csharp
() => { }                      // No return
() => "result"                 // Return value (written to output)
() => 0                        // Return int (written to output, NOT exit code)
(string p) => { }              // With parameter
(int x, int y) => x + y        // Multiple parameters
```

### Asynchronous

```csharp
async () => await DoAsync()                  // Async, no return
async () => await ComputeAsync()             // Async with return
async (string p) => await ProcessAsync(p)   // Async with parameter
```

### With Cancellation

```csharp
async (CancellationToken ct) => await LongRunningAsync(ct)
async (string p, CancellationToken ct) => await ProcessAsync(p, ct)
```

## Complete Example

```csharp
using TimeWarp.Nuru;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .WithName("myapp")
  .WithDescription("My awesome CLI application")
  
  // Configuration and DI
  .AddConfiguration()
  .ConfigureServices(services =>
  {
    services.AddSingleton<IDeployer, Deployer>();
  })
  
  // Pipeline behaviors
  .AddBehavior(typeof(LoggingBehavior))
  
  // Query command (safe to retry)
  .Map("status")
    .WithHandler(() => Console.WriteLine("All systems operational"))
    .WithDescription("Show system status")
    .AsQuery()
    .Done()
  
  // Command with required parameter
  .Map("greet {name}")
    .WithHandler((string name) => Console.WriteLine($"Hello, {name}!"))
    .WithDescription("Greet someone by name")
    .AsCommand()
    .Done()
  
  // Command with typed parameters
  .Map("add {x:int} {y:int}")
    .WithHandler((int x, int y) => Console.WriteLine($"{x} + {y} = {x + y}"))
    .WithDescription("Add two integers")
    .AsQuery()
    .Done()
  
  // Command with optional parameter
  .Map("deploy {env} {tag?}")
    .WithHandler((string env, string? tag) =>
    {
      string version = tag ?? "latest";
      Console.WriteLine($"Deploying {version} to {env}");
    })
    .WithDescription("Deploy to environment with optional tag")
    .AsCommand()
    .Done()
  
  // Command with boolean option
  .Map("build --release,-r")
    .WithHandler((bool release) =>
    {
      string mode = release ? "Release" : "Debug";
      Console.WriteLine($"Building in {mode} mode");
    })
    .WithDescription("Build the project")
    .AsCommand()
    .Done()
  
  // Command with option value
  .Map("search {query} --limit,-l {count:int?}")
    .WithHandler((string query, int? count) =>
    {
      int limit = count ?? 10;
      Console.WriteLine($"Searching for '{query}' (limit: {limit})");
    })
    .WithDescription("Search with optional result limit")
    .AsQuery()
    .Done()
  
  // Async command with injected service
  .Map("deploy-async {env}")
    .WithHandler(async (string env, IDeployer deployer) =>
    {
      await deployer.DeployAsync(env);
    })
    .WithDescription("Deploy asynchronously")
    .AsIdempotentCommand()
    .Done()
  
  // Catch-all parameter
  .Map("echo {*words}")
    .WithHandler((string[] words) => Console.WriteLine(string.Join(" ", words)))
    .WithDescription("Echo all arguments")
    .AsQuery()
    .Done()
  
  // REPL and completion
  .AddRepl(options => options.Prompt = "myapp> ")
  .EnableCompletion()
  
  .Build();

return await app.RunAsync(args);
```

## See Also

- [Route Pattern Syntax](../features/route-patterns.md)
- [Supported Types](supported-types.md)
- [NuruAppOptions](nuru-app-options.md)
- [Pipeline Behaviors](../features/pipeline-behaviors.md)
