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
NuruApp app = NuruApp.CreateBuilder()
  .Map("version")
    .WithHandler(() => Console.WriteLine("v1.0.0"))
    .AsQuery()
    .Done()
  .Map("ping")
    .WithHandler(() => Console.WriteLine("pong"))
    .AsQuery()
    .Done()
  .Map("add {x:double} {y:double}")
    .WithHandler((double x, double y) => Console.WriteLine(x + y))
    .AsCommand()
    .Done()
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
NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services =>
  {
    services.AddSingleton<IDatabase, Database>();
    services.AddScoped<IAnalyzer, Analyzer>();
  })
  .Map<QueryCommand>()
  .Map<AnalyzeCommand>()
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
NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services =>
  {
    services.AddScoped<IDeploymentService, DeploymentService>();
  })
  // Direct: Simple, fast commands
  .Map("version")
    .WithHandler(() => Console.WriteLine("v1.0"))
    .AsQuery()
    .Done()
  .Map("ping")
    .WithHandler(() => Console.WriteLine("pong"))
    .AsQuery()
    .Done()
  // Mediator: Complex commands with DI
  .Map<DeployCommand>()
  .Map<AnalyzeCommand>()
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
builder.Map("deploy {env}")
  .WithHandler((string env) =>
  {
    ValidateEnvironment(env);
    DeploymentConfig config = LoadConfig(env);
    ExecuteDeployment(config);
    LogSuccess(env);
  })
  .AsCommand()
  .Done();
```

**With DI:**
```csharp
// Separated concerns via dependency injection
builder.Map("deploy {env}")
  .WithHandler(async (
    string env,
    IValidator validator,
    IConfigService config,
    IDeploymentService deployment,
    ILogger logger) =>
  {
    await validator.ValidateAsync(env);
    DeploymentConfig cfg = await config.LoadAsync(env);
    await deployment.ExecuteAsync(cfg);
    logger.LogInformation("Deployed to {Env}", env);
  })
  .AsCommand().Done();
```

### Testing

**Direct:**
```csharp
// Test by invoking the app
int result = await app.RunAsync(new[] { "deploy", "test" });
Assert.Equal(0, result);
```

**With DI:**
```csharp
// Test via TestTerminal
using TestTerminal terminal = new();
NuruApp app = NuruApp.CreateBuilder()
  .UseTerminal(terminal)
  .ConfigureServices(services =>
  {
    services.AddSingleton(mockValidator);
    services.AddSingleton(mockDeployment);
  })
  .Map("deploy {env}")
    .WithHandler(async (string env, IValidator validator, IDeploymentService deployment) =>
    {
      await validator.ValidateAsync(env);
      await deployment.ExecuteAsync(env);
    })
    .AsCommand().Done()
  .Build();

await app.RunAsync(["deploy", "test"]);
mockDeployment.Verify(x => x.ExecuteAsync("test"), Times.Once);
```

## Migration Paths

### Start Direct, Add Mediator Later

```csharp
// Phase 1: Start simple
NuruApp app = NuruApp.CreateBuilder()
  .Map("deploy {env}")
    .WithHandler((string env) => Deploy(env))
    .AsCommand()
    .Done()
  .Build();

// Phase 2: Add DI for new complex command
NuruApp app2 = NuruApp.CreateBuilder()
  .ConfigureServices(services =>
  {
    services.AddScoped<IAnalyzer, Analyzer>();
  })
  .Map("deploy {env}")
    .WithHandler((string env) => Deploy(env))
    .AsCommand()
    .Done()  // Keep existing
  .Map<AnalyzeCommand>()  // New complex command
  .Build();

// Phase 3: Migrate deploy to mediator if needed
// (Replace the direct route with mediator route)
```

### Start Mediator, Optimize Hot Paths

```csharp
NuruApp app = NuruApp.CreateBuilder()
  // Most commands use mediator
  .Map<DeployCommand>()
  .Map<AnalyzeCommand>()
  // Optimize hot path with direct approach
  .Map("ping")
    .WithHandler(() => "pong")
    .AsQuery()
    .Done()  // Called frequently, needs speed
  .Build();
```

## Real-World Scenarios

### Small Utility Tool (< 10 commands)

**Recommendation: Direct**

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .Map("encode {text}")
    .WithHandler((string text) => Base64.Encode(text))
    .AsQuery()
    .Done()
  .Map("decode {text}")
    .WithHandler((string text) => Base64.Decode(text))
    .AsQuery()
    .Done()
  .Map("hash {file}")
    .WithHandler((string file) => ComputeHash(file))
    .AsQuery()
    .Done()
  .Build();
```

### Enterprise CLI (100+ commands, team of 5+)

**Recommendation: Mediator**

```csharp
// Custom extension methods can use ConfigureServices for full fluent chain
NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services =>
  {
    services.AddDatabase();
    services.AddAuthentication();
    services.AddTelemetry();
  })
  // All commands use mediator
  .Map<UserCreateCommand>()
  .Map<ReportGenerateCommand>()
  // ... 98 more commands
  .Build();
```

### DevOps Tool (mixed complexity)

**Recommendation: Mixed**

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices
  (
    services =>
    {
      services.AddDeploymentServices();
      services.AddMonitoring();
    }
  )
  // Simple direct commands
  .Map("version")
    .WithHandler(() => Console.WriteLine("v2.0"))
    .AsQuery()
    .Done()
  .Map("status")
    .WithHandler(() => ShowStatus())
    .AsQuery()
    .Done()
  // Complex commands with DI
  .Map<DeployCommand>()
  .Map<RollbackCommand>()
  .Build();
```

## Best Practices

### Start Simple

```csharp
// ✅ Start with direct approach
NuruApp app = NuruApp.CreateBuilder()
  .Map("greet {name}")
    .WithHandler((string name) => $"Hello, {name}!")
    .AsQuery()
    .Done()
  .Build();

// ❌ Don't over-engineer from the start
NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices
  (
    services =>
    {
      services.AddScoped<IGreetingService, GreetingService>();
      services.AddScoped<IGreetingFormatter, GreetingFormatter>();
    }
  )
  .Map<GreetCommand>()  // Overkill for simple greeting
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
NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services => services.AddComplexServices();)
  // Direct for simple operations
  .Map("ping")
    .WithHandler(() => "pong")
    .AsQuery()
    .Done()
  .Map("version")
    .WithHandler(() => "1.0")
    .AsQuery()
    .Done()
  // Mediator for complex operations
  .Map<ComplexCommand>()
  // Both in same app - totally fine!
  .Build();
```

## Related Documentation

- **[Getting Started](../getting-started.md)** - Quick start examples
- **[Use Cases](../use-cases.md)** - Real-world scenarios
- **[Performance](../reference/performance.md)** - Detailed benchmarks
- **[Calculator Samples](../../../samples/calculator/)** - All three approaches demonstrated
