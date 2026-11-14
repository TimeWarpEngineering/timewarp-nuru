# Use Cases

TimeWarp.Nuru excels in two primary scenarios: building new CLI applications from scratch and enhancing existing command-line tools.

## 🆕 Greenfield CLI Applications

Build modern command-line tools from scratch with clean architecture and progressive complexity.

### Simple Utility Tools

**Use Case**: Quick developer tools with straightforward commands

```csharp
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .Map("version")
    .WithHandler(() => Console.WriteLine("MyTool v1.0.0"))
    .AsQuery()
    .Done()
  .Map("status")
    .WithHandler(() => ShowSystemStatus())
    .AsQuery()
    .Done()
  .Map("config get {key}")
    .WithHandler((string key) => Console.WriteLine(GetConfig(key)))
    .AsCommand()
    .Done()
  .Map("config set {key} {value}")
    .WithHandler((string key, string value) => SetConfig(key, value))
    .AsCommand()
    .Done()
  .Build();

return await app.RunAsync(args);
```

**Benefits**:
- Zero boilerplate
- Type-safe parameters
- Clear route definitions
- Fast execution (~4KB memory)

### Enterprise CLI Applications

**Use Case**: Complex business tools requiring DI, logging, and testability

```csharp
using TimeWarp.Nuru;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services =>
  {
    services.AddSingleton<IDatabaseService, DatabaseService>();
    services.AddSingleton<IAuthService, AuthService>();
    services.AddScoped<IReportGenerator, ReportGenerator>();
  })
  // Simple commands remain direct
  .Map("version")
    .WithHandler(() => Console.WriteLine("v1.0.0"))
    .AsQuery()
    .Done()
  .Map("ping")
    .WithHandler(() => Console.WriteLine("pong"))
    .AsQuery()
    .Done()
  // Complex commands use mediator pattern  (routes declared via [NuruRoute] on each class)
  .Map<QueryCommand>()
  .Map<DeployCommand>()
  .Map<GenerateReportCommand>()
  .Build();

return await app.RunAsync(args);
```

**Benefits**:
- Testable command handlers
- Dependency injection throughout
- Separation of concerns
- Enterprise patterns (CQRS, Mediator)

### Multi-Command Developer Tools

**Use Case**: Git-like tools with subcommands and complex options

```csharp
NuruApp app = NuruApp.CreateBuilder()
  // Repository management
  .Map("repo init {name} --bare")
    .WithHandler((string name, bool bare) => InitializeRepository(name, bare))
    .AsCommand()
    .Done()
  .Map("repo clone {url} {path?}")
    .WithHandler((string url, string? path) => CloneRepository(url, path))
    .AsCommand()
    .Done()
  .Map("repo list")
    .WithHandler(() => ListRepositories())
    .AsQuery()
    .Done()
  // Branch operations
  .Map("branch create {name}")
    .WithHandler((string name) => CreateBranch(name))
    .AsCommand()
    .Done()
  .Map("branch delete {name} --force")
    .WithHandler((string name, bool force) => DeleteBranch(name, force))
    .AsCommand()
    .Done()
  .Map("branch list --all")
    .WithHandler((bool all) => ListBranches(all))
    .AsQuery()
    .Done()
  // File operations
  .Map("add {*files}")
    .WithHandler((string[] files) => StageFiles(files))
    .AsCommand()
    .Done()
  .Map("commit -m {message}")
    .WithHandler((string message) => Commit(message))
    .AsCommand()
    .Done()
  .Map("push --force")
    .WithHandler((bool force) => Push(force))
    .AsCommand()
    .Done()
  .Build();

return await app.RunAsync(args);
```

**Benefits**:
- Hierarchical command structure
- Flexible option combinations
- Catch-all for file lists
- Self-documenting routes

## 🔄 Progressive Enhancement of Existing CLIs

Wrap existing command-line tools to add authentication, logging, validation, or routing logic.

In these examples `RunTool(tool, args)` stands for your own process helper (for example, a
TimeWarp.Amuru call) that runs the wrapped tool and returns its exit code. Pass that code on with
`Environment.ExitCode`: a handler's returned `int` is printed to stdout, not used as the exit code.

### Adding Authentication Layer

**Use Case**: Wrap an existing deployment CLI with access control

```csharp
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  // Intercept production deployments for auth check
  .Map("deploy prod {*args}")
    .WithHandler(async (string[] args) =>
    {
      if (!await ValidateProductionAccess())
      {
        Console.Error.WriteLine("❌ Production access denied");
        Environment.ExitCode = 1;  // a returned int is printed, not the exit code
        return;
      }

      Console.WriteLine("✅ Access granted, deploying to production...");
      Environment.ExitCode = await RunTool("existing-deploy-tool", ["deploy", "prod", ..args]);
    })
    .AsCommand()
    .Done()
  // Other environments pass through
  .Map("deploy {env} {*args}")
    .WithHandler(async (string env, string[] args) =>
    {
      Environment.ExitCode = await RunTool("existing-deploy-tool", ["deploy", env, ..args]);
    })
    .AsCommand()
    .Done()
  .Build();

return await app.RunAsync(args);
```

**Benefits**:
- Add security without modifying original tool
- Gradual migration strategy
- Audit logging insertion point
- Zero disruption to existing workflows

### Adding Validation and Dry-Run Mode

**Use Case**: Add safety checks to dangerous operations

```csharp
// One route: --confirm and --dry-run are optional flags on the same command
builder.Map("delete database {name} --confirm --dry-run")
  .WithHandler(async (string name, bool confirm, bool dryRun) =>
  {
    // Add dry-run capability not in original tool
    if (dryRun)
    {
      Console.WriteLine($"[DRY RUN] Would delete database: {name}");
      return;
    }

    if (!confirm)
    {
      Console.Error.WriteLine("❌ Must use --confirm flag to delete database");
      Environment.ExitCode = 1;
      return;
    }

    // Validate database name against pattern
    if (!ValidateDatabaseName(name))
    {
      Console.Error.WriteLine($"❌ Invalid database name: {name}");
      Environment.ExitCode = 1;
      return;
    }

    // Log before execution
    await LogAuditEvent("database.delete", name);

    Environment.ExitCode = await RunTool("original-cli", ["delete", "database", name]);
  })
  .AsCommand()
  .Done();
```

**Benefits**:
- Add safety nets to dangerous operations
- Implement dry-run mode
- Validation layer
- Audit trail

### Command Routing and Transformation

**Use Case**: Modernize CLI interface while maintaining backward compatibility

```csharp
// New simplified interface; --debug and --quiet are optional flags
builder.Map("build {project} --debug --quiet")
  .WithHandler(async (string project, bool debug, bool quiet) =>
  {
    // Transform to old CLI syntax
    List<string> args = ["--project", project, "--config", debug ? "Debug" : "Release"];
    if (!quiet) args.Add("--verbose");

    Environment.ExitCode = await RunTool("legacy-tool", [.. args]);
  })
  .AsCommand()
  .Done();

// Pass through for advanced users needing old syntax
builder.Map("{*args}")
  .WithHandler(async (string[] args) =>
  {
    Environment.ExitCode = await RunTool("legacy-tool", args);
  })
  .AsCommand()
  .Done();
```

**Benefits**:
- Modernize interface gradually
- Backward compatibility maintained
- Simplified common operations
- Power users keep full access

### Monitoring and Instrumentation

**Use Case**: Add telemetry to existing tools

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services =>
  {
    services.AddSingleton<ITelemetryService, TelemetryService>();
  })
  .Map("{*args}")
    .WithHandler(async (string[] args, ITelemetryService telemetry) =>
    {
      Stopwatch sw = Stopwatch.StartNew();
      string command = string.Join(" ", args);

      try
      {
        int result = await RunTool("original-tool", args);
        sw.Stop();

        await telemetry.TrackCommandAsync(new CommandMetrics
        {
          Command = command,
          Duration = sw.Elapsed,
          Success = result == 0,
          Timestamp = DateTime.UtcNow
        });

        Environment.ExitCode = result;
      }
      catch (Exception ex)
      {
        await telemetry.TrackErrorAsync(command, ex);
        throw;
      }
    })
    .AsCommand().Done()
  .Build();
```

**Benefits**:
- Add telemetry without changing original tool
- Performance monitoring
- Error tracking
- Usage analytics

## Real-World Scenarios

### DevOps Deployment Tool

Combine both patterns:

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services =>
  {
    services.AddSingleton<IDeploymentService, DeploymentService>();
    services.AddSingleton<IConfigurationService, ConfigurationService>();
  })
  // Simple commands - Direct
  .Map("version")
    .WithHandler(() => Console.WriteLine("DeployTool v2.0"))
    .AsQuery()
    .Done()
  .Map("ping {host}")
    .WithHandler((string host) => PingHost(host))
    .AsQuery()
    .Done()
  // Complex commands - Mediator
  .Map<DeployCommand>()
  .Map<RollbackCommand>()
  .Map<ValidateCommand>()
  // Wrap existing tool for migration
  .Map("legacy {*args}")
    .WithHandler(async (string[] args) => { Environment.ExitCode = await RunTool("old-deploy-tool", args); })
    .AsCommand()
    .Done()
  .Build();
```

### Database Management CLI

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services =>
  {
    services.AddSingleton<IDatabaseService, DatabaseService>();
  })
  // Direct for queries
  .Map("db list")
    .WithHandler(() => ListDatabases())
    .AsQuery()
    .Done()
  .Map("db status {name}")
    .WithHandler((string name) => ShowDatabaseStatus(name))
    .AsQuery()
    .Done()
  // Mediator for complex operations
  .Map<BackupCommand>()
  .Map<RestoreCommand>()
  .Map<MigrateCommand>()
  .Build();
```

## Choosing Your Approach

| Scenario | Recommended Approach | Rationale |
|----------|---------------------|-----------|
| Simple utility (< 10 commands) | Direct only | Maximum performance, minimal code |
| Enterprise tool (complex business logic) | Mixed (Direct + Mediator) | Optimize by command complexity |
| Existing tool wrapper | Direct | Minimal overhead for pass-through |
| Testability required | Mediator | Better separation, DI support |
| Team > 5 developers | Mediator or Mixed | Better code organization |
| Performance critical | Direct | ~4KB memory, fastest execution |

## Next Steps

- **[Architecture Choices](guides/architecture-choices.md)** - Detailed comparison of approaches
- **[Deployment Guide](guides/deployment.md)** - Native AOT and distribution
- **[Routing Features](features/routing.md)** - Complete route pattern syntax
- **[Examples](../../samples/)** - Working code you can run
