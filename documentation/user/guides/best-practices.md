# Best Practices

Patterns and conventions for building maintainable, performant CLI applications with TimeWarp.Nuru.

## Route Organization

### Self-Contained Routes

Minimize route explosion by using optional parameters:

```csharp
// ❌ Factorial explosion
builder.AddRoute("deploy {env}", handler);
builder.AddRoute("deploy {env} {version}", handler);
builder.AddRoute("deploy {env} {version} {region}", handler);
// 3 routes for variations

// ✅ Self-contained with optionals
builder.AddRoute("deploy {env} {version?} {region?}", handler);
// 1 route, same flexibility
```

### Hierarchical Commands

Group related commands with shared prefixes:

```csharp
// Git-style command groups
builder.AddRoute("user create {email}", CreateUser);
builder.AddRoute("user delete {id:Guid}", DeleteUser);
builder.AddRoute("user list", ListUsers);

builder.AddRoute("project create {name}", CreateProject);
builder.AddRoute("project delete {id:Guid}", DeleteProject);
builder.AddRoute("project list", ListProjects);
```

### Command Naming

Use consistent, clear naming:

```csharp
// ✅ Clear, consistent
builder.AddRoute("server start", StartServer);
builder.AddRoute("server stop", StopServer);
builder.AddRoute("server restart", RestartServer);

// ❌ Inconsistent
builder.AddRoute("start-server", StartServer);
builder.AddRoute("stopServer", StopServer);
builder.AddRoute("restart_server", RestartServer);
```

## Error Handling

### Return Exit Codes

```csharp
builder.AddRoute("validate {file}", (string file) =>
{
    if (!File.Exists(file))
    {
        Console.Error.WriteLine($"❌ File not found: {file}");
        return 1;
    }

    var errors = Validate(file);
    if (errors.Any())
    {
        Console.Error.WriteLine($"❌ {errors.Count} validation errors");
        return 1;
    }

    Console.Error.WriteLine("✅ Validation passed");
    return 0;
});
```

### User-Friendly Messages

```csharp
// ✅ Clear, actionable
Console.Error.WriteLine("❌ Database connection failed");
Console.Error.WriteLine("Check your connection string in appsettings.json");

// ❌ Technical jargon
Console.Error.WriteLine("SqlException: Connection timeout expired");
```

### Async Error Handling

```csharp
builder.AddRoute("deploy {env}", async (string env) =>
{
    try
    {
        await DeployAsync(env);
        return 0;
    }
    catch (DeploymentException ex)
    {
        Console.Error.WriteLine($"❌ Deployment failed: {ex.Message}");
        return 1;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Unexpected error: {ex.Message}");
        logger.LogError(ex, "Deployment failed");
        return 1;
    }
});
```

## Output Best Practices

### Separate Streams

```csharp
// ✅ Progress → stderr, data → stdout
builder.AddRoute("process {file}", (string file) =>
{
    Console.Error.WriteLine($"Processing {file}...");  // stderr
    var result = Process(file);
    Console.Error.WriteLine("Complete!");              // stderr
    return result;                                     // stdout (JSON)
});

// ❌ Mixed output
builder.AddRoute("process {file}", (string file) =>
{
    Console.WriteLine($"Processing {file}...");  // stdout (breaks piping)
    var result = Process(file);
    Console.WriteLine(JsonSerializer.Serialize(result));  // stdout
    return result;
});
```

### Structured Output

```csharp
// ✅ Return objects for JSON
builder.AddRoute("status", () => new {
    Version = "1.0.0",
    Uptime = GetUptime(),
    Status = "Running"
});

// ❌ Manual JSON construction
builder.AddRoute("status", () =>
{
    Console.WriteLine("{");
    Console.WriteLine($"  \"version\": \"{GetVersion()}\",");
    // Error-prone and hard to maintain
});
```

## Performance

### Choose Right Approach per Command

```csharp
NuruAppBuilder builder = new();

// Direct for hot paths
builder.AddRoute("ping", () => "pong");
builder.AddRoute("version", () => "1.0.0");

// Mediator for complex operations
builder.AddDependencyInjection();
builder.Services.AddScoped<IDeploymentService, DeploymentService>();
builder.AddRoute<DeployCommand>("deploy {env}");
```

### Minimize Allocations

```csharp
// ✅ Return value types
.AddRoute("calc {x:int} {y:int}", (int x, int y) => x + y)

// ❌ Unnecessary allocations
.AddRoute("calc {x:int} {y:int}", (int x, int y) => new { Result = x + y })
```

### Use AOT for Production

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <TrimMode>partial</TrimMode>
</PropertyGroup>
```

## Testing

### Test Command Handlers

```csharp
// Mediator approach enables unit testing
[Fact]
public async Task DeployCommand_ValidEnvironment_Succeeds()
{
    // Arrange
    var mockService = new Mock<IDeploymentService>();
    var handler = new DeployCommand.Handler(mockService.Object);
    var command = new DeployCommand { Environment = "test" };

    // Act
    await handler.Handle(command, CancellationToken.None);

    // Assert
    mockService.Verify(x => x.DeployAsync("test"), Times.Once);
}
```

### Integration Testing

```csharp
[Fact]
public async Task Application_DeployCommand_Works()
{
    var app = BuildApp();
    var result = await app.RunAsync(new[] { "deploy", "test" });
    Assert.Equal(0, result);
}
```

## Code Organization

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

### Nested Handlers

```csharp
public sealed class DeployCommand : IRequest
{
    public string Environment { get; set; }

    // Handler nested with command
    public sealed class Handler(IDeploymentService deployment)
        : IRequestHandler<DeployCommand>
    {
        public async Task Handle(DeployCommand cmd, CancellationToken ct)
        {
            await deployment.DeployAsync(cmd.Environment);
        }
    }
}
```

## Configuration

### Use Options Pattern

```csharp
public class AppOptions
{
    public string DatabaseConnection { get; set; }
    public int Timeout { get; set; }
}

builder.Services.Configure<AppOptions>(
    builder.Configuration.GetSection("App"));
```

### Environment-Specific Settings

```csharp
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{env}.json", optional: true)
    .AddEnvironmentVariables();
```

## Logging

### Structured Logging

```csharp
// ✅ Structured
logger.LogInformation("Deployed to {Environment} at {Time}",
    env, DateTime.UtcNow);

// ❌ String interpolation
logger.LogInformation($"Deployed to {env} at {DateTime.UtcNow}");
```

### Log Levels

```csharp
logger.LogTrace("Detailed debug info");      // Development only
logger.LogDebug("Debug information");         // Development
logger.LogInformation("General information"); // Always
logger.LogWarning("Warning message");         // Always
logger.LogError(ex, "Error occurred");        // Always
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
builder.AddRoute("deploy {env}", (string env) =>
{
    // Validate against allowed environments
    var allowed = new[] { "dev", "staging", "prod" };
    if (!allowed.Contains(env))
    {
        Console.Error.WriteLine($"❌ Invalid environment. Allowed: {string.Join(", ", allowed)}");
        return 1;
    }

    Deploy(env);
    return 0;
});
```

## Documentation

### Add Descriptions

```csharp
builder.AddRoute(
    "deploy {env|Target environment (dev/staging/prod)} {version?|Version tag}",
    handler);
```

### Include Help

```csharp
var app = new NuruAppBuilder()
    .AddRoute("deploy {env|Environment} {version?|Version}", handler)
    .AddAutoHelp()
    .Build();
```

### Provide Examples

```bash
# Show usage in README
dotnet run -- deploy prod v1.2.3
dotnet run -- deploy staging
dotnet run -- deploy --help
```

## Related Documentation

- **[Architecture Choices](architecture-choices.md)** - Choose the right approach
- **[Deployment](deployment.md)** - Production deployment
- **[Performance](../reference/performance.md)** - Optimization tips
- **[Use Cases](../use-cases.md)** - Real-world patterns
