# Use Cases

TimeWarp.Nuru excels in two primary scenarios: building new CLI applications from scratch and enhancing existing command-line tools.

## 🆕 Greenfield CLI Applications

Build modern command-line tools from scratch with clean architecture and progressive complexity.

### Simple Utility Tools

**Use Case**: Quick developer tools with straightforward commands

```csharp
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder(args)
  .Map("version", () => Console.WriteLine("MyTool v1.0.0"))
  .Map("status", () => ShowSystemStatus())
  .Map("config get {key}", (string key) => Console.WriteLine(GetConfig(key)))
  .Map("config set {key} {value}", (string key, string value) => SetConfig(key, value))
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

NuruApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(services =>
  {
    services.AddSingleton<IDatabaseService, DatabaseService>();
    services.AddSingleton<IAuthService, AuthService>();
    services.AddScoped<IReportGenerator, ReportGenerator>();
  })
  // Simple commands remain direct
  .Map("version", () => Console.WriteLine("v1.0.0"))
  .Map("ping", () => Console.WriteLine("pong"))
  // Complex commands use mediator pattern
  .Map<QueryCommand>("query {sql}")
  .Map<DeployCommand>("deploy {env} --version {tag?} --dry-run")
  .Map<GenerateReportCommand>("report generate {type} --format {fmt}")
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
NuruApp app = NuruApp.CreateBuilder(args)
  // Repository management
  .Map
  (
    "repo init {name} --bare",
    (string name, bool bare) => InitializeRepository(name, bare)
  )
  .Map
  (
    "repo clone {url} {path?}",
    (string url, string? path) => CloneRepository(url, path)
  )
  .Map("repo list", () => ListRepositories())
  // Branch operations
  .Map("branch create {name}", (string name) => CreateBranch(name))
  .Map
  (
    "branch delete {name} --force",
    (string name, bool force) => DeleteBranch(name, force)
  )
  .Map("branch list --all", (bool all) => ListBranches(all))
  // File operations
  .Map("add {*files}", (string[] files) => StageFiles(files))
  .Map("commit -m {message}", (string message) => Commit(message))
  .Map("push --force", (bool force) => Push(force))
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

### Adding Authentication Layer

**Use Case**: Wrap an existing deployment CLI with access control

```csharp
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder(args)
  // Intercept production deployments for auth check
  .Map
  (
    "deploy prod {*args}",
    async (string[] args) =>
    {
      if (!await ValidateProductionAccess())
      {
        Console.Error.WriteLine("❌ Production access denied");
        return 1;
      }

      Console.WriteLine("✅ Access granted, deploying to production...");
      return await Shell.ExecuteAsync("existing-deploy-tool", ["deploy", "prod", ..args]);
    }
  )
  // Other environments pass through
  .Map
  (
    "deploy {env} {*args}",
    async (string env, string[] args) =>
    {
      return await Shell.ExecuteAsync("existing-deploy-tool", ["deploy", env, ..args]);
    }
  )
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
builder.Map
(
  "delete database {name} --confirm",
  async (string name, bool confirm) =>
  {
    if (!confirm)
    {
      Console.Error.WriteLine("❌ Must use --confirm flag to delete database");
      return 1;
    }

    // Validate database name against pattern
    if (!ValidateDatabaseName(name))
    {
      Console.Error.WriteLine($"❌ Invalid database name: {name}");
      return 1;
    }

    // Log before execution
    await LogAuditEvent("database.delete", name);

    return await Shell.ExecuteAsync("original-cli", "delete", "database", name);
  }
);

// Add dry-run capability not in original tool
builder.Map
(
  "delete database {name} --dry-run",
  (string name) =>
  {
    Console.WriteLine($"[DRY RUN] Would delete database: {name}");
    return 0;
  }
);
```

**Benefits**:
- Add safety nets to dangerous operations
- Implement dry-run mode
- Validation layer
- Audit trail

### Command Routing and Transformation

**Use Case**: Modernize CLI interface while maintaining backward compatibility

```csharp
// New simplified interface
builder.Map
(
  "build {project}",
  async (string project) =>
  {
    // Transform to old CLI syntax
    return await Shell.ExecuteAsync
    (
      "legacy-tool",
      "--project", project,
      "--config", "Release",
      "--verbose"
    );
  }
);

// New interface with options
builder.Map
(
  "build {project} --debug --quiet",
  async (string project, bool debug, bool quiet) =>
  {
    List<string> args = new() { "--project", project };
    args.Add("--config");
    args.Add(debug ? "Debug" : "Release");
    if (!quiet) args.Add("--verbose");

    return await Shell.ExecuteAsync("legacy-tool", args.ToArray());
  }
);

// Pass through for advanced users needing old syntax
builder.Map
(
  "{*args}",
  async (string[] args) =>
  {
    return await Shell.ExecuteAsync("legacy-tool", args);
  }
);
```

**Benefits**:
- Modernize interface gradually
- Backward compatibility maintained
- Simplified common operations
- Power users keep full access

### Monitoring and Instrumentation

**Use Case**: Add telemetry to existing tools

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
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
        int result = await Shell.ExecuteAsync("original-tool", args);
        sw.Stop();

        await telemetry.TrackCommandAsync(new CommandMetrics
        {
          Command = command,
          Duration = sw.Elapsed,
          Success = result == 0,
          Timestamp = DateTime.UtcNow
        });

        return result;
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
NuruApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(services =>
  {
    services.AddSingleton<IDeploymentService, DeploymentService>();
    services.AddSingleton<IConfigurationService, ConfigurationService>();
  })
  // Simple commands - Direct
  .Map("version", () => Console.WriteLine("DeployTool v2.0"))
  .Map("ping {host}", (string host) => PingHost(host))
  // Complex commands - Mediator
  .Map<DeployCommand>("deploy {env} --version {tag?} --dry-run")
  .Map<RollbackCommand>("rollback {env} --to {version}")
  .Map<ValidateCommand>("validate {env}")
  // Wrap existing tool for migration
  .Map
  (
    "legacy {*args}",
    async (string[] args) => await Shell.ExecuteAsync("old-deploy-tool", args)
  )
  .Build();
```

### Database Management CLI

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(services =>
  {
    services.AddSingleton<IDatabaseService, DatabaseService>();
  })
  // Direct for queries
  .Map("db list", () => ListDatabases())
  .Map("db status {name}", (string name) => ShowDatabaseStatus(name))
  // Mediator for complex operations
  .Map<BackupCommand>("db backup {name} --compress")
  .Map<RestoreCommand>("db restore {name} --from {file}")
  .Map<MigrateCommand>("db migrate {name} --version {ver?}")
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
