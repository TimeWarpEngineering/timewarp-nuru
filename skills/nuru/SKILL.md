---
name: nuru
description: Build CLI applications with TimeWarp.Nuru - a .NET route-based CLI framework with web-style routing, source generators, and AOT support. Use when creating CLI tools, defining commands/queries, adding parameters or options, testing CLI output, or working with TimeWarp.Nuru.
---

# TimeWarp.Nuru

Route-based CLI framework for .NET. Define commands like web routes. Source-generated for AOT compatibility.

**Package:** `TimeWarp.Nuru`
**Repository:** https://github.com/TimeWarpEngineering/timewarp-nuru
**Depends on:** [TimeWarp.Terminal](https://github.com/TimeWarpEngineering/timewarp-terminal) - console abstractions (`IConsole`, `ITerminal`), widgets (panels, tables, rules), ANSI colors, and `TestTerminal` for testable output. Included transitively via the Nuru package. For full Terminal API docs (colors, widgets, builders), fetch the README at https://raw.githubusercontent.com/TimeWarpEngineering/timewarp-terminal/refs/heads/master/README.md

## Reference: Calculator CLI

For a complete working example of a multi-command Nuru CLI, see the [calculator sample](https://github.com/TimeWarpEngineering/timewarp-nuru/tree/master/samples/endpoints/02-calculator). Replicate this folder structure for new CLIs:

```
my-cli/
├── my-cli.cs             # Entry point runfile
├── Directory.Build.props # Includes endpoints/**/*.cs into compilation
├── behaviors/
│   └── *.cs              # Pipeline behaviors
├── endpoints/
│   └── *.cs              # One endpoint class per file
└── services/
    └── *.cs              # Injected services
```

**Key patterns:**
- Entry point runfile with `#:project` directive referencing the Nuru project
- `Directory.Build.props` that glob-includes `endpoints/**/*.cs` and excludes the entry point
- One endpoint class per file in `endpoints/`
- File-scoped namespace in each endpoint file
- `DiscoverEndpoints()` with `.AddRepl(options => { options.AutoStartWhenEmpty = true; })`

## Choose a DSL

Nuru offers two DSLs. Pick one based on your needs:

- **Endpoint DSL (Recommended)**: Class-based with `[NuruRoute]` attribute. Best for testable, structured apps with dependency injection. Scales to large CLIs.
- **Fluent DSL**: Inline lambdas with `Map().WithHandler().AsCommand().Done()`. Best for quick scripts and simple CLIs.

## Endpoint DSL (Recommended)

### Basics

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .AddRepl(options =>
  {
    options.AutoStartWhenEmpty = true;
  })
  .Build();

return await app.RunAsync(args);
```

`DiscoverEndpoints()` auto-discovers all classes with `[NuruRoute]`.
`.AddRepl()` enables interactive REPL mode. `AutoStartWhenEmpty = true` starts REPL when no args provided.

### Commands, Queries, and Idempotent Commands

- **ICommand<T>** / **ICommandHandler<TCommand, T>**: Actions with side effects, NOT safe to retry (deploy, build, delete)
- **IQuery<T>** / **IQueryHandler<TQuery, T>**: Read-only operations, safe to retry (status, greet, version)
- **IIdempotentCommand<T>** / **IIdempotentCommandHandler<TCommand, T>**: Mutating but safe to retry (set config, PUT/DELETE-style operations)
- Use `Unit` as return type when there's no meaningful return value

### Simple Endpoint

`[NuruRoute]` takes a single literal (e.g. `"greet"`, `"status"`) or empty string (`""` for default route). The analyzer enforces this - multiple words or parameters in `[NuruRoute]` will produce a compile error.

```csharp
[NuruRoute("greet", Description = "Greet someone")]
public sealed class GreetQuery : IQuery<Unit>
{
  [Parameter(Description = "Name of the person to greet")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler : IQueryHandler<GreetQuery, Unit>
  {
    public ValueTask<Unit> Handle(GreetQuery query, CancellationToken ct)
    {
      Console.WriteLine($"Hello {query.Name}");
      return default;
    }
  }
}
```

### Parameters

When a command has **multiple `[Parameter]` attributes**, each **must** specify an explicit `Order` value (enforced by analyzer NURU_A002). A single parameter does not need `Order`.

```csharp
[NuruRoute("deploy", Description = "Deploy to an environment")]
public sealed class DeployCommand : ICommand<Unit>
{
  [Parameter(Order = 0, Description = "Target environment")]
  public string Env { get; set; } = string.Empty;

  // Optional parameter: use nullable type (string?), NOT IsOptional=true
  [Parameter(Order = 1, Description = "Optional tag to deploy")]
  public string? Tag { get; set; }

  public sealed class Handler : ICommandHandler<DeployCommand, Unit>
  {
    public ValueTask<Unit> Handle(DeployCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Deploying to {command.Env}");
      return default;
    }
  }
}
```

### Options (flags and named values)

```csharp
[NuruRoute("build", Description = "Build a project")]
public sealed class BuildCommand : ICommand<Unit>
{
  [Parameter(Description = "Project to build")]
  public string Project { get; set; } = string.Empty;

  [Option("mode", "m", Description = "Build mode (Debug or Release)")]
  public string Mode { get; set; } = "Debug";

  [Option("verbose", "v", Description = "Verbose output")]
  public bool Verbose { get; set; }

  public sealed class Handler : ICommandHandler<BuildCommand, Unit>
  {
    public ValueTask<Unit> Handle(BuildCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Building {command.Project} ({command.Mode}, verbose: {command.Verbose})");
      return default;
    }
  }
}
```

Usage: `myapp build MyApp --mode Release --verbose` or `myapp build MyApp -m Release -v`

### Catch-all Parameters

```csharp
[NuruRoute("exec", Description = "Run with arbitrary arguments")]
public sealed class ExecCommand : ICommand<Unit>
{
  [Parameter(IsCatchAll = true, Description = "Arguments")]
  public string[] Args { get; set; } = [];

  public sealed class Handler : ICommandHandler<ExecCommand, Unit>
  {
    public ValueTask<Unit> Handle(ExecCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Args: {string.Join(" ", command.Args)}");
      return default;
    }
  }
}
```

### Route Groups (sub-command hierarchies)

The shell splits `myapp docker build .` so the app only receives `["docker", "build", "."]` as args. Since `[NuruRoute]` accepts only a single literal, sub-command hierarchies use `[NuruRouteGroup]` on an abstract base class. The group prefix is prepended to the child's `[NuruRoute]` literal. Groups can nest to any depth.

```csharp
// Base class defines the "docker" prefix
[NuruRouteGroup("docker")]
public abstract class DockerGroupBase;

// Matched by: myapp docker build . --tag foo
// App receives args: ["docker", "build", ".", "--tag", "foo"]
[NuruRoute("build", Description = "Build an image from a Dockerfile")]
public sealed class DockerBuildCommand : DockerGroupBase, ICommand<Unit>
{
  [Parameter(Description = "Path to Dockerfile or build context")]
  public string Path { get; set; } = string.Empty;

  [Option("tag", "t", Description = "Name and optionally a tag")]
  public string? Tag { get; set; }

  [Option("no-cache", null, Description = "Do not use cache")]
  public bool NoCache { get; set; }

  public sealed class Handler : ICommandHandler<DockerBuildCommand, Unit>
  {
    public ValueTask<Unit> Handle(DockerBuildCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Building image from: {command.Path}");
      return default;
    }
  }
}
```

Nested groups concatenate prefixes through inheritance:

```csharp
[NuruRouteGroup("cloud")]
public abstract class CloudGroupBase;

[NuruRouteGroup("azure")]
public abstract class AzureGroupBase : CloudGroupBase;

[NuruRouteGroup("storage")]
public abstract class AzureStorageGroupBase : AzureGroupBase;

// Matched by: myapp cloud azure storage upload myfile.txt
[NuruRoute("upload", Description = "Upload a file to Azure storage")]
public sealed class AzureStorageUploadCommand : AzureStorageGroupBase, ICommand<Unit>
{
  [Parameter(Description = "File to upload")]
  public string File { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<AzureStorageUploadCommand, Unit>
  {
    public ValueTask<Unit> Handle(AzureStorageUploadCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Uploading: {command.File}");
      return default;
    }
  }
}
```

Shared options across a group use `[GroupOption]` on the base class properties. All routes inheriting from the group automatically get these options. Do not include dashes in the attribute - the generator adds them.

```csharp
[NuruRouteGroup("docker")]
public abstract class DockerGroupBase
{
  [GroupOption("verbose", "v", Description = "Verbose output")]
  public bool Verbose { get; set; }
}

// Both DockerBuildCommand and DockerRunCommand inherit --verbose/-v from DockerGroupBase
```

### Subset Publishing (Group Filtering)

Create multiple specialized CLI editions from one codebase by filtering endpoints by group type. Parent prefixes above the matched group are stripped.

```csharp
// Full edition - all commands
NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

// Kanban-only edition - "ganda" prefix stripped, only kanban commands
NuruApp.CreateBuilder()
  .DiscoverEndpoints(typeof(KanbanGroup))
  .Build();

// Multiple groups - OR logic
NuruApp.CreateBuilder()
  .DiscoverEndpoints(typeof(KanbanGroup), typeof(GitGroup))
  .Build();
```

**Behavior:**
- Include endpoint if it inherits (directly or indirectly) from any specified group type
- Strip all group prefixes above the matched type
- Ungrouped endpoints excluded when filter is active
- Type-based (`typeof()`) for refactoring safety

**Project structure** for multi-edition CLIs using runfiles:

```
my-cli/
├── Directory.Build.props       # Includes ganda/endpoints/** for all editions
├── ganda/
│   ├── ganda.cs                # Full edition entry point
│   └── endpoints/              # Shared command files (one per file)
│       ├── ganda-group.cs
│       ├── kanban-group.cs
│       ├── kanban-add-command.cs
│       └── git-commit-command.cs
├── kanban/
│   └── kanban.cs               # .DiscoverEndpoints(typeof(KanbanGroup))
└── git/
    └── git.cs                  # .DiscoverEndpoints(typeof(GitGroup))
```

See `samples/editions/01-group-filtering/` for a complete working example.

### Endpoint Key Rules

- Endpoint classes must be `sealed` (can be `public sealed` or `internal sealed`)
- Handler must be a nested `sealed class Handler` (accessibility matches the endpoint class)
- `[NuruRoute]` takes only a single literal or `""` - the analyzer rejects anything else
- Use nullable types (`string?`) for optional parameters, NOT `IsOptional=true`
- Return `Unit` when no meaningful return value
- Use `ValueTask<T>` for handler return types

### Return Values vs Exit Codes

Handler return values and exit codes are **independent concerns**:

- **Return value** → written to terminal (stdout). Controls what the user sees.
- **`Environment.ExitCode`** → controls the process exit code returned by `RunAsync()`. Controls what scripts/CI see.

| Return Type | Terminal Output | Exit Code |
|-------------|-----------------|-----------|
| `Unit` | Nothing | 0 (default) |
| `string` | Raw string | 0 (default) |
| `int` | Raw number (printed, **not** used as exit code) | 0 (default) |
| Complex object | JSON serialized | 0 (default) |
| `DateTime` | ISO 8601 formatted | 0 (default) |

**To signal failure**, set `Environment.ExitCode` explicitly:

```csharp
public sealed class Handler : ICommandHandler<DeployCommand, Unit>
{
  public async ValueTask<Unit> Handle(DeployCommand command, CancellationToken ct)
  {
    if (failed)
    {
      Terminal.WriteErrorLine("Deployment failed");
      Environment.ExitCode = 1;
    }
    return Unit.Value;
  }
}
```

**Common mistake**: returning `int` thinking it sets the exit code. `.WithHandler(() => 42)` outputs `"42"` to the terminal but the exit code is still 0.

## REPL Mode

Add interactive REPL to any Nuru app with `.AddRepl()`:

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .AddRepl(options =>
  {
    options.AutoStartWhenEmpty = true;  // Start REPL when no args
    options.Prompt = "calc> ";
    options.WelcomeMessage = "Calculator REPL. Type commands or 'exit' to quit.";
    options.PersistHistory = true;
    options.HistoryFilePath = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      ".my_app_history"
    );
  })
  .Build();

return await app.RunAsync(args);
```

**Mode detection:**
- With args: runs as CLI (e.g. `myapp add 2 3`)
- Without args + `AutoStartWhenEmpty`: starts REPL
- With `--interactive` / `-i`: starts REPL explicitly

## Fluent DSL

### Basics

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .Map("greet {name}")
    .WithHandler((string name) => $"Hello, {name}!")
    .AsQuery()
    .Done()
  .Build();

await app.RunAsync(args);
```

### Route Pattern Syntax

In the Fluent DSL, the full route pattern including parameters, options, and sub-commands is expressed in the `Map()` string.

| Pattern | `Map()` Example | Description |
|---------|-----------------|-------------|
| Literal | `"status"`, `"docker build"` | Exact match (multi-word = sub-commands) |
| Parameter | `"greet {name}"` | Required parameter |
| Typed | `"delay {ms:int}"` | Type-constrained parameter |
| Optional | `"deploy {env} {tag?}"` | Nullable parameter (handler param must be nullable) |
| Catch-all | `"exec {*args}"` | Captures all remaining args (must be last) |
| Option | `"build --config {mode}"` | Named option with value |
| Flag | `"build --verbose"` | Boolean flag |
| Short option | `"build -m {mode}"` | Short alias |
| Option alias | `"build --config,-c {mode}"` | Long and short form |
| Repeated option | `"run --env {var}*"` | Collects multiple values into array |
| Description | `"{env\|Target environment}"` | Inline help text via `\|` |

## Dependency Injection

Nuru has two DI modes:

1. **Source-Generated DI (Default)**: Fast, AOT-compatible. Supports:
   - `AddTransient/AddScoped/AddSingleton<T>()` - basic registrations
   - `AddLogging()` - logging with Microsoft.Extensions.Logging
   - `AddHttpClient<TService, TImplementation>()` - typed HTTP clients
   - Services with constructor dependencies (resolved at compile time via `new T(dep1, dep2)`)
   - Transitive dependencies (service depending on service with its own deps)

2. **Runtime DI** (opt-in with `.UseMicrosoftDependencyInjection()`):
   - Use when you need complex DI features like:
     - Extension methods beyond the supported ones
     - Factory delegates with `sp => new Service()`
   - Slightly slower startup, but full MS DI capabilities

Example of AddHttpClient with source-gen DI:
```csharp
.ConfigureServices(services =>
{
  services.AddHttpClient<IWeatherService, WeatherService>(client =>
  {
    client.BaseAddress = new Uri("https://api.example.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
  });
})
.DiscoverEndpoints()
.Build();
```

Then in the handler:
```csharp
public sealed class Handler(IWeatherService weatherService) : IQueryHandler<WeatherQuery, string>
{
  public async ValueTask<string> Handle(WeatherQuery query, CancellationToken ct)
  {
    return await weatherService.GetWeatherAsync(query.City, ct);
  }
}
```

## Testing with TestTerminal

Use `TestTerminal` from `TimeWarp.Terminal` to capture and verify CLI output:

```csharp
using TimeWarp.Terminal;

// Create test terminal for output capture
using TestTerminal terminal = new();

NuruApp app = NuruApp.CreateBuilder()
  .UseTerminal(terminal)
  .Map("demo")
    .WithHandler((ITerminal t) =>
    {
      t.WriteLine("Hello from stdout!");
      t.WriteErrorLine("Warning: something happened");
    })
    .AsCommand()
    .Done()
  .Build();

await app.RunAsync(["demo"]);

// Verify output
terminal.OutputContains("Hello from stdout!").ShouldBeTrue();
terminal.ErrorContains("Warning").ShouldBeTrue();
string[] lines = terminal.GetOutputLines();
```

## Installation

```bash
# Add to a .csproj project (--prerelease gets the latest version)
dotnet add package TimeWarp.Nuru --prerelease
```

```csharp
// In a runfile
#:package TimeWarp.Nuru
```

## Multi-File Runfile Projects

For CLIs with multiple endpoint files, use a `Directory.Build.props` to include them. See `samples/endpoints/02-calculator/Directory.Build.props` for the reference pattern:

```xml
<Project>
  <Import Project="$(MSBuildThisFileDirectory)../../Directory.Build.props" />

  <PropertyGroup>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <RunfileGlobbingEnabled>true</RunfileGlobbingEnabled>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="**/*.cs" Exclude="obj/**/*;bin/**/*;calculator.cs" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../../source/timewarp-nuru/timewarp-nuru.csproj" />
  </ItemGroup>
</Project>
```

## General Rules

- `DiscoverEndpoints()` auto-discovers all `[NuruRoute]` classes
- All warnings treated as errors; no trailing whitespace
- One endpoint class per file in `endpoints/` directory
- Use namespaces in endpoint files
- Follow the calculator sample structure for new CLIs

## Samples

- `samples/endpoints/02-calculator/` - Complete calculator CLI reference pattern
- `samples/endpoints/05-httpclient/` - Typed HTTP client with source-gen DI example
