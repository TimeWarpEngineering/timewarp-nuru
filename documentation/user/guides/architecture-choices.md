# Architecture Choices

Choose the right approach for each command in your CLI application: Direct, Mediator, or Mixed.

## The Three Approaches

### 🚀 Direct Approach
Maximum performance with minimal overhead.

**When to use:**
- Simple utility commands
- Performance-critical operations
- Stateless operations
- Quick scripts

**Benefits:**
- ~4KB memory footprint
- Zero framework overhead
- Inline logic
- Fastest execution

**Example:**
```csharp
NuruApp app = new NuruAppBuilder()
  .AddRoute("version", () => Console.WriteLine("v1.0.0"))
  .AddRoute("ping", () => Console.WriteLine("pong"))
  .AddRoute
  (
    "add {x:double} {y:double}",
    (double x, double y) => Console.WriteLine(x + y)
  )
  .Build();
```

### 🏗️ Mediator Approach
Enterprise patterns with full dependency injection.

**When to use:**
- Complex business logic
- Need testability
- Require dependency injection
- Team collaboration
- Long-term maintenance

**Benefits:**
- Testable command handlers
- Dependency injection throughout
- Separation of concerns
- Enterprise patterns (CQRS, Mediator)
- Better code organization

**Example:**
```csharp
NuruApp app = new NuruAppBuilder()
  .AddDependencyInjection()
  .ConfigureServices(services =>
  {
    services.AddSingleton<IDatabase, Database>();
    services.AddScoped<IAnalyzer, Analyzer>();
  })
  .AddRoute<QueryCommand>("query {sql}")
  .AddRoute<AnalyzeCommand>("analyze {*files}")
  .Build();
```

### ⚡ Mixed Approach (Recommended)
Use the right tool for each command.

**When to use:**
- Real-world applications
- Want optimal performance AND structure
- Commands vary in complexity

**Benefits:**
- Optimize per command
- Gradual adoption
- Performance where it matters
- Structure where needed

**Example:**
```csharp
NuruApp app = new NuruAppBuilder()
  .AddDependencyInjection()
  .ConfigureServices(services =>
  {
    services.AddScoped<IDeploymentService, DeploymentService>();
  })
  // Direct: Simple, fast commands
  .AddRoute("version", () => Console.WriteLine("v1.0"))
  .AddRoute("ping", () => Console.WriteLine("pong"))
  // Mediator: Complex commands with DI
  .AddRoute<DeployCommand>("deploy {env} --dry-run")
  .AddRoute<AnalyzeCommand>("analyze {*files}")
  .Build();
```

## Decision Guide

| Factor | Direct | Mediator | Mixed |
|--------|---------|----------|-------|
| Memory footprint | ~4KB | Moderate | Optimal per command |
| Execution speed | Fastest | Fast | Optimized |
| Testability | Basic | Excellent | Excellent (where used) |
| Dependency injection | No | Yes | Yes (where enabled) |
| Code organization | Inline | Structured | Flexible |
| Team size | 1-2 | 3+ | Any |
| Complexity | Simple | Complex | Varies |
| Learning curve | Easy | Moderate | Moderate |

## Detailed Comparison

### Performance

**Direct (JIT):**
- Memory: ~4KB
- 37 tests: 2.49s

**Mediator (JIT):**
- Memory: Moderate
- 37 tests: 6.52s (161% slower)

**Direct (AOT):**
- Memory: ~4KB
- 37 tests: 0.30s (88% faster than JIT)
- Binary: 3.3 MB

**Mediator (AOT):**
- Memory: Moderate
- 37 tests: 0.42s (93% faster than JIT)
- Binary: 4.8 MB

See [Performance Reference](../reference/performance.md) for detailed benchmarks.

### Code Organization

**Direct:**
```csharp
// All in one place
builder.AddRoute
(
  "deploy {env}",
  (string env) =>
  {
    ValidateEnvironment(env);
    DeploymentConfig config = LoadConfig(env);
    ExecuteDeployment(config);
    LogSuccess(env);
  }
);
```

**Mediator:**
```csharp
// Separated concerns
public sealed class DeployCommand : IRequest
{
    public string Environment { get; set; }

    public sealed class Handler(
      IValidator validator,
      IConfigService config,
      IDeploymentService deployment,
      ILogger logger) : IRequestHandler<DeployCommand>
    {
      public async Task Handle(DeployCommand cmd, CancellationToken ct)
      {
        await validator.ValidateAsync(cmd.Environment);
        DeploymentConfig cfg = await config.LoadAsync(cmd.Environment);
        await deployment.ExecuteAsync(cfg);
        logger.LogInformation("Deployed to {Env}", cmd.Environment);
      }
    }
}
```

### Testing

**Direct:**
```csharp
// Test by invoking the app
int result = await app.RunAsync(new[] { "deploy", "test" });
Assert.Equal(0, result);
```

**Mediator:**
```csharp
// Test handler in isolation
DeployCommand.Handler handler = new
(
  mockValidator,
  mockConfig,
  mockDeployment,
  mockLogger
);

DeployCommand command = new() { Environment = "test" };
await handler.Handle(command, CancellationToken.None);

mockDeployment.Verify(x => x.ExecuteAsync(It.IsAny<Config>()), Times.Once);
```

## Migration Paths

### Start Direct, Add Mediator Later

```csharp
// Phase 1: Start simple
NuruApp app = new NuruAppBuilder()
  .AddRoute("deploy {env}", (string env) => Deploy(env))
  .Build();

// Phase 2: Add DI for new complex command
NuruApp app2 = new NuruAppBuilder()
  .AddDependencyInjection()
  .ConfigureServices(services =>
  {
    services.AddScoped<IAnalyzer, Analyzer>();
  })
  .AddRoute("deploy {env}", (string env) => Deploy(env))  // Keep existing
  .AddRoute<AnalyzeCommand>("analyze {*files}")  // New complex command
  .Build();

// Phase 3: Migrate deploy to mediator if needed
// (Replace the direct route with mediator route)
```

### Start Mediator, Optimize Hot Paths

```csharp
NuruApp app = new NuruAppBuilder()
  .AddDependencyInjection()
  // Most commands use mediator
  .AddRoute<DeployCommand>("deploy {env}")
  .AddRoute<AnalyzeCommand>("analyze {*files}")
  // Optimize hot path with direct approach
  .AddRoute("ping", () => "pong")  // Called frequently, needs speed
  .Build();
```

## Real-World Scenarios

### Small Utility Tool (< 10 commands)

**Recommendation: Direct**

```csharp
NuruApp app = new NuruAppBuilder()
  .AddRoute("encode {text}", (string text) => Base64.Encode(text))
  .AddRoute("decode {text}", (string text) => Base64.Decode(text))
  .AddRoute("hash {file}", (string file) => ComputeHash(file))
  .Build();
```

### Enterprise CLI (100+ commands, team of 5+)

**Recommendation: Mediator**

```csharp
// Custom extension methods can use ConfigureServices for full fluent chain
NuruApp app = new NuruAppBuilder()
  .AddDependencyInjection()
  .ConfigureServices(services =>
  {
    services.AddDatabase();
    services.AddAuthentication();
    services.AddTelemetry();
  })
  // All commands use mediator
  .AddRoute<UserCreateCommand>("user create {email}")
  .AddRoute<ReportGenerateCommand>("report generate {type}")
  // ... 98 more commands
  .Build();
```

### DevOps Tool (mixed complexity)

**Recommendation: Mixed**

```csharp
NuruApp app = new NuruAppBuilder()
  .AddDependencyInjection()
  .ConfigureServices
  (
    services =>
    {
      services.AddDeploymentServices();
      services.AddMonitoring();
    }
  )
  // Simple direct commands
  .AddRoute("version", () => Console.WriteLine("v2.0"))
  .AddRoute("status", () => ShowStatus())
  // Complex commands with DI
  .AddRoute<DeployCommand>("deploy {env} --dry-run")
  .AddRoute<RollbackCommand>("rollback {env} --to {version}")
  .Build();
```

## Best Practices

### Start Simple

```csharp
// ✅ Start with direct approach
NuruApp app = new NuruAppBuilder()
  .AddRoute("greet {name}", (string name) => $"Hello, {name}!")
  .Build();

// ❌ Don't over-engineer from the start
NuruApp app = new NuruAppBuilder()
  .AddDependencyInjection()
  .ConfigureServices
  (
    services =>
    {
      services.AddScoped<IGreetingService, GreetingService>();
      services.AddScoped<IGreetingFormatter, GreetingFormatter>();
    }
  )
  .AddRoute<GreetCommand>("greet {name}")  // Overkill for simple greeting
  .Build();
```

### Add Structure When Needed

Migrate to mediator when:
- Command logic exceeds ~20 lines
- Need to inject services
- Multiple team members working on same command
- Need to write unit tests
- Command has complex business rules

### Mix Freely

```csharp
NuruApp app = new NuruAppBuilder()
  .AddDependencyInjection()
  .ConfigureServices(services => services.AddComplexServices();)
  // Direct for simple operations
  .AddRoute("ping", () => "pong")
  .AddRoute("version", () => "1.0")
  // Mediator for complex operations
  .AddRoute<ComplexCommand>("complex {arg}")
  // Both in same app - totally fine!
  .Build();
```

## Related Documentation

- **[Getting Started](../getting-started.md)** - Quick start examples
- **[Use Cases](../use-cases.md)** - Real-world scenarios
- **[Performance](../reference/performance.md)** - Detailed benchmarks
- **[Calculator Samples](../../../Samples/Calculator/)** - All three approaches demonstrated
