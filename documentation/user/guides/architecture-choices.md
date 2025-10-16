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
var app = new NuruAppBuilder()
    .AddRoute("version", () => Console.WriteLine("v1.0.0"))
    .AddRoute("ping", () => Console.WriteLine("pong"))
    .AddRoute("add {x:double} {y:double}",
        (double x, double y) => Console.WriteLine(x + y))
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
NuruAppBuilder builder = new();
builder.AddDependencyInjection();

builder.Services.AddSingleton<IDatabase, Database>();
builder.Services.AddScoped<IAnalyzer, Analyzer>();

builder.AddRoute<QueryCommand>("query {sql}");
builder.AddRoute<AnalyzeCommand>("analyze {*files}");

var app = builder.Build();
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
NuruAppBuilder builder = new();

// Direct: Simple, fast commands
builder.AddRoute("version", () => Console.WriteLine("v1.0"));
builder.AddRoute("ping", () => Console.WriteLine("pong"));

// Enable DI
builder.AddDependencyInjection();
builder.Services.AddScoped<IDeploymentService, DeploymentService>();

// Mediator: Complex commands with DI
builder.AddRoute<DeployCommand>("deploy {env} --dry-run");
builder.AddRoute<AnalyzeCommand>("analyze {*files}");

var app = builder.Build();
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
builder.AddRoute("deploy {env}", (string env) =>
{
    ValidateEnvironment(env);
    var config = LoadConfig(env);
    ExecuteDeployment(config);
    LogSuccess(env);
});
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
            var cfg = await config.LoadAsync(cmd.Environment);
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
var result = await app.RunAsync(new[] { "deploy", "test" });
Assert.Equal(0, result);
```

**Mediator:**
```csharp
// Test handler in isolation
var handler = new DeployCommand.Handler(
    mockValidator,
    mockConfig,
    mockDeployment,
    mockLogger);

var command = new DeployCommand { Environment = "test" };
await handler.Handle(command, CancellationToken.None);

mockDeployment.Verify(x => x.ExecuteAsync(It.IsAny<Config>()), Times.Once);
```

## Migration Paths

### Start Direct, Add Mediator Later

```csharp
// Phase 1: Start simple
var app = new NuruAppBuilder()
    .AddRoute("deploy {env}", (string env) => Deploy(env))
    .Build();

// Phase 2: Add DI for new complex command
NuruAppBuilder builder = new();
builder.AddRoute("deploy {env}", (string env) => Deploy(env));  // Keep existing

builder.AddDependencyInjection();
builder.Services.AddScoped<IAnalyzer, Analyzer>();
builder.AddRoute<AnalyzeCommand>("analyze {*files}");  // New complex command

var app = builder.Build();

// Phase 3: Migrate deploy to mediator if needed
builder.AddRoute<DeployCommand>("deploy {env}");  // Now uses DI/mediator
```

### Start Mediator, Optimize Hot Paths

```csharp
NuruAppBuilder builder = new();
builder.AddDependencyInjection();

// Most commands use mediator
builder.AddRoute<DeployCommand>("deploy {env}");
builder.AddRoute<AnalyzeCommand>("analyze {*files}");

// Optimize hot path with direct approach
builder.AddRoute("ping", () => "pong");  // Called frequently, needs speed

var app = builder.Build();
```

## Real-World Scenarios

### Small Utility Tool (< 10 commands)

**Recommendation: Direct**

```csharp
var app = new NuruAppBuilder()
    .AddRoute("encode {text}", (string text) => Base64.Encode(text))
    .AddRoute("decode {text}", (string text) => Base64.Decode(text))
    .AddRoute("hash {file}", (string file) => ComputeHash(file))
    .Build();
```

### Enterprise CLI (100+ commands, team of 5+)

**Recommendation: Mediator**

```csharp
NuruAppBuilder builder = new();
builder.AddDependencyInjection();

// Register all services
builder.Services.AddDatabase();
builder.Services.AddAuthentication();
builder.Services.AddTelemetry();

// All commands use mediator
builder.AddRoute<UserCreateCommand>("user create {email}");
builder.AddRoute<ReportGenerateCommand>("report generate {type}");
// ... 98 more commands

var app = builder.Build();
```

### DevOps Tool (mixed complexity)

**Recommendation: Mixed**

```csharp
NuruAppBuilder builder = new();

// Simple direct commands
builder.AddRoute("version", () => Console.WriteLine("v2.0"));
builder.AddRoute("status", () => ShowStatus());

// Complex commands with DI
builder.AddDependencyInjection();
builder.Services.AddDeploymentServices();
builder.Services.AddMonitoring();

builder.AddRoute<DeployCommand>("deploy {env} --dry-run");
builder.AddRoute<RollbackCommand>("rollback {env} --to {version}");

var app = builder.Build();
```

## Best Practices

### Start Simple

```csharp
// ✅ Start with direct approach
var app = new NuruAppBuilder()
    .AddRoute("greet {name}", (string name) => $"Hello, {name}!")
    .Build();

// ❌ Don't over-engineer from the start
NuruAppBuilder builder = new();
builder.AddDependencyInjection();
builder.Services.AddScoped<IGreetingService, GreetingService>();
builder.Services.AddScoped<IGreetingFormatter, GreetingFormatter>();
builder.AddRoute<GreetCommand>("greet {name}");  // Overkill for simple greeting
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
NuruAppBuilder builder = new();

// Direct for simple operations
builder.AddRoute("ping", () => "pong");
builder.AddRoute("version", () => "1.0");

// Mediator for complex operations
builder.AddDependencyInjection();
builder.Services.AddComplexServices();
builder.AddRoute<ComplexCommand>("complex {arg}");

// Both in same app - totally fine!
var app = builder.Build();
```

## Related Documentation

- **[Getting Started](../getting-started.md)** - Quick start examples
- **[Use Cases](../use-cases.md)** - Real-world scenarios
- **[Performance](../reference/performance.md)** - Detailed benchmarks
- **[Calculator Samples](../../../Samples/Calculator/)** - All three approaches demonstrated
