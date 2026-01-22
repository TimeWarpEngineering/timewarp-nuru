# Best Practices

Patterns and conventions for building maintainable, performant CLI applications with TimeWarp.Nuru.

## Route Organization

### Self-Contained Routes

Minimize route explosion by using optional parameters:

```csharp
// ❌ Factorial explosion
.Map("deploy {env}").WithHandler(handler).Done()
.Map("deploy {env} {version}").WithHandler(handler).Done()
.Map("deploy {env} {version} {region}").WithHandler(handler).Done()
// 3 routes for variations

// ✅ Self-contained with optionals
.Map("deploy {env} {version?} {region?}").WithHandler(handler).Done()
// 1 route, same flexibility
```

### Hierarchical Commands with Groups

Use `WithGroup()` to organize related commands:

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .WithName("myapp")
  .WithGroup("user", group => group
    .Map("create {email}")
      .WithHandler((string email) => CreateUser(email))
      .Done()
    .Map("delete {id:Guid}")
      .WithHandler((Guid id) => DeleteUser(id))
      .Done()
    .Map("list")
      .WithHandler(() => ListUsers())
      .Done()
  )
  .WithGroup("project", group => group
    .Map("create {name}")
      .WithHandler((string name) => CreateProject(name))
      .Done()
    .Map("delete {id:Guid}")
      .WithHandler((Guid id) => DeleteProject(id))
      .Done()
    .Map("list")
      .WithHandler(() => ListProjects())
      .Done()
  )
  .Build();
```

This produces commands like `myapp user create foo@example.com` and `myapp project list`.

### Command Naming

Use consistent, clear naming:

```csharp
// ✅ Clear, consistent (verb-based)
.Map("start").WithHandler(StartServer).Done()
.Map("stop").WithHandler(StopServer).Done()
.Map("restart").WithHandler(RestartServer).Done()

// ❌ Inconsistent
.Map("start-server").WithHandler(StartServer).Done()
.Map("stopServer").WithHandler(StopServer).Done()
.Map("restart_server").WithHandler(RestartServer).Done()
```

## Terminal Output

### Use ITerminal Abstraction

Always use `ITerminal` instead of `Console` directly. This enables testability with `TestTerminal`:

```csharp
// ✅ Inject ITerminal for testability
.Map("greet {name}")
  .WithHandler((string name, ITerminal terminal) =>
  {
    terminal.WriteLine($"Hello, {name}!");
  })
  .Done()

// ❌ Don't use Console directly
.Map("greet {name}")
  .WithHandler((string name) =>
  {
    Console.WriteLine($"Hello, {name}!");  // Not testable!
  })
  .Done()
```

`ITerminal` is automatically registered and can be injected into any handler.

## Error Handling

### Throw Exceptions for Errors

TimeWarp.Nuru handles exceptions automatically - throwing results in exit code 1:

```csharp
.Map("validate {file}")
  .WithHandler((string file, ITerminal terminal) =>
  {
    if (!File.Exists(file))
    {
      throw new FileNotFoundException($"File not found: {file}");
    }

    List<ValidationError> errors = Validate(file);
    if (errors.Count > 0)
    {
      throw new ValidationException($"{errors.Count} validation errors found");
    }

    terminal.WriteLine("Validation passed");
  })
  .Done()
```

### Return Values for Output

Return values are for command output, not exit codes:

```csharp
// ✅ Return values for actual output
.Map("add {x:int} {y:int}")
  .WithHandler((int x, int y) => x + y)  // Returns the sum
  .Done()

.Map("greet {name}")
  .WithHandler((string name) => $"Hello, {name}!")  // Returns greeting
  .Done()

// The returned value is written to stdout
// myapp add 2 3  → outputs "5"
// myapp greet World  → outputs "Hello, World!"
```

### User-Friendly Error Messages

```csharp
// ✅ Clear, actionable
throw new InvalidOperationException(
  "Database connection failed. Check your connection string in appsettings.json");

// ❌ Technical jargon only
throw new SqlException("Connection timeout expired");
```

### Async Error Handling

```csharp
.Map("deploy {env}")
  .WithHandler(async (string env, ITerminal terminal) =>
  {
    try
    {
      await DeployAsync(env);
      terminal.WriteLine($"Deployed to {env}");
    }
    catch (DeploymentException ex)
    {
      // Re-throw with user-friendly message
      throw new InvalidOperationException($"Deployment failed: {ex.Message}", ex);
    }
  })
  .Done()
```

## Output Best Practices

### Separate Streams

Use `ITerminal.WriteLine()` for stdout and `ITerminal.WriteErrorLine()` for stderr:

```csharp
// ✅ Progress → stderr, data → stdout
.Map("process {file}")
  .WithHandler((string file, ITerminal terminal) =>
  {
    terminal.WriteErrorLine($"Processing {file}...");  // stderr
    var result = Process(file);
    terminal.WriteErrorLine("Complete!");              // stderr
    return result;                                     // stdout (the actual output)
  })
  .Done()

// ❌ Mixed output breaks piping
.Map("process {file}")
  .WithHandler((string file, ITerminal terminal) =>
  {
    terminal.WriteLine($"Processing {file}...");  // stdout (breaks piping!)
    var result = Process(file);
    terminal.WriteLine(JsonSerializer.Serialize(result));  // stdout
  })
  .Done()
```

### Structured Output

```csharp
// ✅ Return objects for JSON serialization
.Map("status")
  .WithHandler(() => new
  {
    Version = "1.0.0",
    Uptime = GetUptime(),
    Status = "Running"
  })
  .Done()

// ❌ Manual JSON construction (error-prone)
.Map("status")
  .WithHandler((ITerminal terminal) =>
  {
    terminal.WriteLine("{");
    terminal.WriteLine($"  \"version\": \"{GetVersion()}\",");
    // Error-prone and hard to maintain
  })
  .Done()
```

## Performance

### Use AOT for Production

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

### Minimize Allocations in Hot Paths

```csharp
// ✅ Return value types for simple calculations
.Map("calc {x:int} {y:int}")
  .WithHandler((int x, int y) => x + y)
  .Done()

// ❌ Unnecessary allocations
.Map("calc {x:int} {y:int}")
  .WithHandler((int x, int y) => new { Result = x + y })
  .Done()
```

## Testing

### Test with TestTerminal

Use `TestTerminal` to capture and verify output. Since handlers inject `ITerminal`, the `TestTerminal` captures all output:

```csharp
[Fact]
public async Task Greet_ReturnsGreeting()
{
  // Arrange
  using var terminal = new TestTerminal();
  TestTerminalContext.Current = terminal;

  NuruApp app = NuruApp.CreateBuilder()
    .WithName("test")
    .Map("greet {name}")
      .WithHandler((string name, ITerminal terminal) =>
        terminal.WriteLine($"Hello, {name}!"))
      .Done()
    .Build();

  // Act
  int exitCode = await app.RunAsync(["greet", "World"]);

  // Assert
  Assert.Equal(0, exitCode);
  Assert.Contains("Hello, World!", terminal.Output);
}
```

### Test Endpoint Handlers Directly

For Endpoint API, test the handler class:

```csharp
[Fact]
public async Task DeployHandler_ValidEnvironment_Succeeds()
{
  // Arrange
  var mockService = new Mock<IDeploymentService>();
  var terminal = new TestTerminal();
  var handler = new DeployCommand.Handler(mockService.Object, terminal);
  var command = new DeployCommand { Environment = "test" };

  // Act
  await handler.Handle(command, CancellationToken.None);

  // Assert
  mockService.Verify(x => x.DeployAsync("test"), Times.Once);
}
```

## Code Organization

### Fluent API for Simple CLIs

```csharp
// Good for small apps with few commands
NuruApp app = NuruApp.CreateBuilder()
  .WithName("myapp")
  .Map("ping").WithHandler(() => "pong").Done()
  .Map("version").WithHandler(() => "1.0.0").Done()
  .Build();
```

### Endpoint API for Complex CLIs

```csharp
// Good for larger apps - commands in separate files
NuruApp app = NuruApp.CreateBuilder()
  .WithName("myapp")
  .DiscoverEndpoints()  // Discovers [NuruRoute] classes
  .Build();
```

```csharp
// Commands/DeployCommand.cs
[NuruRoute("deploy {env}", Description = "Deploy to environment")]
public sealed class DeployCommand : ICommand<Unit>
{
  public required string Env { get; set; }

  public sealed class Handler(IDeploymentService deployment, ITerminal terminal)
    : ICommandHandler<DeployCommand, Unit>
  {
    public async ValueTask<Unit> Handle(DeployCommand cmd, CancellationToken ct)
    {
      await deployment.DeployAsync(cmd.Env);
      terminal.WriteLine($"Deployed to {cmd.Env}");
      return Unit.Value;
    }
  }
}
```

### Group by Feature

```
/Commands
  /User
    CreateUserCommand.cs
    DeleteUserCommand.cs
    ListUsersCommand.cs
  /Project
    CreateProjectCommand.cs
    DeleteProjectCommand.cs
```

## Configuration

### Use Options Pattern

```csharp
public class AppOptions
{
  public string DatabaseConnection { get; set; } = "";
  public int Timeout { get; set; } = 30;
}

NuruApp app = NuruApp.CreateBuilder()
  .WithName("myapp")
  .ConfigureServices(services =>
  {
    services.AddOptions<AppOptions>()
      .BindConfiguration("App");
  })
  .DiscoverEndpoints()
  .Build();
```

### Validate Configuration at Startup

```csharp
using System.ComponentModel.DataAnnotations;

public class DatabaseOptions
{
  [Required(ErrorMessage = "Connection string is required")]
  public string ConnectionString { get; set; } = "";

  [Range(1, 300, ErrorMessage = "Timeout must be between 1 and 300 seconds")]
  public int CommandTimeout { get; set; } = 30;
}

NuruApp app = NuruApp.CreateBuilder()
  .WithName("myapp")
  .ConfigureServices(services =>
  {
    services.AddOptions<DatabaseOptions>()
      .BindConfiguration("Database")
      .ValidateDataAnnotations()
      .ValidateOnStart();  // Fails fast during Build()
  })
  .DiscoverEndpoints()
  .Build();
```

## Logging

### Structured Logging

```csharp
// ✅ Structured - enables filtering and analysis
logger.LogInformation("Deployed to {Environment} at {Time}", env, DateTime.UtcNow);

// ❌ String interpolation - loses structure
logger.LogInformation($"Deployed to {env} at {DateTime.UtcNow}");
```

### Log Levels

```csharp
logger.LogTrace("Detailed debug info");      // Development only
logger.LogDebug("Debug information");         // Development
logger.LogInformation("General information"); // Production
logger.LogWarning("Warning message");         // Production
logger.LogError(ex, "Error occurred");        // Production
```

## Security

### Never Log Secrets

```csharp
// ❌ Don't log sensitive data
logger.LogInformation("Connecting with password: {Password}", password);

// ✅ Log safely
logger.LogInformation("Connecting to database");
```

### Validate Input

```csharp
.Map("deploy {env}")
  .WithHandler((string env) =>
  {
    string[] allowed = ["dev", "staging", "prod"];
    if (!allowed.Contains(env))
    {
      throw new ArgumentException(
        $"Invalid environment '{env}'. Allowed: {string.Join(", ", allowed)}");
    }
    Deploy(env);
  })
  .Done()
```

## Documentation

### Add Descriptions

```csharp
.Map("deploy {env|Target environment (dev/staging/prod)} {version?|Version tag}")
  .WithHandler(handler)
  .Done()
```

### Provide Examples in README

```bash
# Deploy to staging
myapp deploy staging

# Deploy specific version to production
myapp deploy prod v1.2.3

# Show help
myapp --help
myapp deploy --help
```

## Related Documentation

- **[Architecture Choices](architecture-choices.md)** - Choose Fluent API vs Endpoint API
- **[Deployment](deployment.md)** - Production deployment
- **[Performance](../reference/performance.md)** - Optimization tips
- **[Use Cases](../use-cases.md)** - Real-world patterns
