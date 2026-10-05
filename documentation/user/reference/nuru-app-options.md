# Builder Configuration Options

`NuruApp.CreateBuilder()` takes no arguments. Each feature is configured with a fluent builder method that takes an options callback: `AddRepl`, `UseTelemetry`, `EnableCompletion`, and `ConfigureHelp`. This page lists the options each callback exposes.

## Overview

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .AddRepl(options => options.Prompt = "myapp> ")
  .UseTelemetry(options => options.ServiceName = "my-app")
  .ConfigureHelp(options => options.ExcludePatterns = ["*-debug"])
  .Map("greet {name}")
    .WithHandler((string name) => $"Hello, {name}!")
    .AsQuery()
    .Done()
  .Build();
```

## Builder Methods

| Method | Callback type | Description |
|--------|---------------|-------------|
| `AddRepl(Action<ReplOptions>)` | `ReplOptions` | Enable and configure REPL mode |
| `UseTelemetry(Action<NuruTelemetryOptions>)` | `NuruTelemetryOptions` | Enable and configure OpenTelemetry |
| `EnableCompletion(appName, Action<CompletionSourceRegistry>)` | `CompletionSourceRegistry` | Enable shell completion and register completion sources |
| `ConfigureHelp(Action<HelpOptions>)` | `HelpOptions` | Configure help output filtering |

## AddRepl

Customizes REPL (Read-Eval-Print Loop) behavior when users enter interactive mode.

```csharp
NuruApp.CreateBuilder()
  .AddRepl(options =>
  {
    options.Prompt = "myapp> ";
    options.ContinuationPrompt = "...... ";
    options.WelcomeMessage = "Welcome to MyApp! Type 'help' for commands.";
    options.GoodbyeMessage = "See you next time!";
    options.MaxHistorySize = 500;
    options.ShowTiming = true;
    options.KeyBindingProfileName = "Emacs";  // Or "Vi", "VSCode", "Default"
  });
```

### Available ReplOptions

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Prompt` | `string` | `"> "` | Prompt displayed before each command |
| `ContinuationPrompt` | `string?` | `">> "` | Prompt for multiline input (Shift+Enter) |
| `WelcomeMessage` | `string?` | Standard message | Message shown when REPL starts |
| `GoodbyeMessage` | `string?` | `"Goodbye!"` | Message shown when REPL exits |
| `PersistHistory` | `bool` | `true` | Save history across sessions |
| `HistoryFilePath` | `string?` | `null` | Custom path; default `~/.nuru/history/<app>` (Unix: dirs `0700`, file `0600`; Windows: profile ACLs) |
| `MaxHistorySize` | `int` | `1000` | Maximum commands in history |
| `ContinueOnError` | `bool` | `true` | Continue after command failures |
| `ShowExitCode` | `bool` | `false` | Display exit code after each command |
| `EnableColors` | `bool` | `true` | Enable colored output |
| `PromptColor` | `string` | `"\x1b[32m"` | ANSI color for prompt (default: green) |
| `ShowTiming` | `bool` | `true` | Show execution time |
| `EnableArrowHistory` | `bool` | `true` | Arrow key history navigation |
| `HistoryIgnorePatterns` | `IList<string>?` | Sensitive patterns | Best-effort patterns to exclude from history |
| `KeyBindingProfileName` | `string` | `"Default"` | Key binding profile name |
| `KeyBindingProfile` | `object?` | `null` | Custom key binding profile instance |
| `AutoStartWhenEmpty` | `bool` | `false` | Start the REPL when the app is run with no arguments |

See [REPL Key Bindings](../features/repl-key-bindings.md) for keyboard shortcuts and customization.

## UseTelemetry

Configures OpenTelemetry integration for tracing, metrics, and logging.

```csharp
NuruApp.CreateBuilder()
  .UseTelemetry(options =>
  {
    options.ServiceName = "my-cli-app";
    options.ServiceVersion = "1.0.0";
    options.EnableTracing = true;
    options.EnableMetrics = true;
    options.EnableLogging = true;
    options.OtlpEndpoint = "http://localhost:4317";
  });
```

### Available NuruTelemetryOptions

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ServiceName` | `string` | Entry assembly name | Service identifier in telemetry |
| `ServiceVersion` | `string?` | Entry assembly version | Version in telemetry |
| `EnableTracing` | `bool` | `true` | Enable Activity spans |
| `EnableMetrics` | `bool` | `true` | Enable metrics collection |
| `EnableLogging` | `bool` | `true` | Enable OpenTelemetry logging |
| `OtlpEndpoint` | `string?` | `null` | OTLP endpoint URL |

Telemetry respects environment variables:
- `OTEL_SERVICE_NAME` - Overrides `ServiceName`
- `OTEL_SERVICE_VERSION` - Overrides `ServiceVersion`
- `OTEL_EXPORTER_OTLP_ENDPOINT` - Fallback if `OtlpEndpoint` is not set

## EnableCompletion

Registers custom completion sources for shell tab-completion.

```csharp
NuruApp.CreateBuilder()
  .EnableCompletion(configure: registry =>
  {
    // Your type. The package does not ship a static-list completion source.
    registry.RegisterForParameter("env", new EnvironmentCompletionSource());

    // Built-in source. Candidates are the enum names (Trace, Debug, Information, ...).
    registry.RegisterForType(typeof(LogLevel), new EnumCompletionSource<LogLevel>());
  });

sealed class EnvironmentCompletionSource : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    _ = context;
    yield return new CompletionCandidate("dev", "Development", CompletionType.Parameter);
    yield return new CompletionCandidate("staging", "Staging", CompletionType.Parameter);
    yield return new CompletionCandidate("prod", "Production", CompletionType.Parameter);
  }
}
```

### Shell vs REPL Completion

There are two distinct completion systems:

| System | Context | How It Works |
|--------|---------|--------------|
| **Shell Completion** | External (bash, zsh, pwsh, fish) | Shell invokes CLI to get completions before execution |
| **REPL Completion** | In-process | Running REPL handles Tab keypresses internally |

`EnableCompletion` configures **shell completion** sources. REPL completion uses route pattern metadata automatically.

### CompletionSourceRegistry Methods

| Method | Description |
|--------|-------------|
| `RegisterForParameter(name, source)` | Register source for specific parameter name |
| `RegisterForType(type, source)` | Register source for all parameters of a type |

See [Shell Completion](../features/shell-completion.md) for generating completion scripts.

## ConfigureHelp

Customizes help output filtering and display.

```csharp
NuruApp.CreateBuilder()
  .ConfigureHelp(options =>
  {
    options.ShowPerCommandHelpRoutes = false;  // Hide "command --help?" routes
    options.ShowReplCommandsInCli = false;     // Hide exit, quit, clear from --help
    options.ShowCompletionRoutes = false;      // Hide __complete, --generate-completion
    options.ExcludePatterns = ["*-debug", "*-internal"];  // Custom exclusions
  });
```

### Available HelpOptions

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ShowPerCommandHelpRoutes` | `bool` | `false` | Show routes like `blog --help?` |
| `ShowReplCommandsInCli` | `bool` | `false` | Show REPL commands in CLI help |
| `ShowCompletionRoutes` | `bool` | `false` | Show shell completion infrastructure |
| `ExcludePatterns` | `IList<string>?` | `null` | Additional patterns to exclude (supports `*` wildcard) |

See [Auto-Help](../features/auto-help.md) for help generation details.

## Built-in Routes

`--help` and `-h` are built in. `--version` is built in and has no `-v` alias. `--interactive` and `-i` are registered when you call `AddRepl()`. `--check-updates` is added with `AddCheckUpdatesRoute()`.

```csharp
NuruApp.CreateBuilder()
  .AddCheckUpdatesRoute();
```

When enabled, the built-in `--check-updates` route:
- Queries GitHub releases for the latest version
- Compares against the current assembly version
- Displays colored output (green checkmark for up-to-date, yellow warning for updates)

Requires `RepositoryUrl` in your project file:

```xml
<PropertyGroup>
  <RepositoryUrl>https://github.com/your-org/your-repo</RepositoryUrl>
</PropertyGroup>
```

The built-in version route writes one line: the assembly informational version, or `AssemblyName.Version`, or `1.0.0`. When the app model has a name, that name is written before the version on the same line. It does not write a commit hash or a commit date.

```bash
$ myapp --version
1.2.3
```

## Complete Example

```csharp
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  // Customize REPL
  .AddRepl(options =>
  {
    options.Prompt = "calc> ";
    options.WelcomeMessage = "Calculator REPL - Type 'help' for commands";
    options.KeyBindingProfileName = "Emacs";
  })
  // Configure telemetry
  .UseTelemetry(options =>
  {
    options.ServiceName = "calculator";
    options.EnableMetrics = true;
  })
  // Register completions
  .EnableCompletion(configure: registry =>
  {
    registry.RegisterForParameter("operation", new OperationCompletionSource());
  })
  // Hide internal routes from help
  .ConfigureHelp(options =>
  {
    options.ExcludePatterns = ["*-debug"];
  })
  .Map("add {x:double} {y:double}")
    .WithHandler((double x, double y) => Console.WriteLine($"{x} + {y} = {x + y}"))
    .AsCommand()
    .Done()
  .Map("multiply {x:double} {y:double}")
    .WithHandler((double x, double y) => Console.WriteLine($"{x} * {y} = {x * y}"))
    .AsCommand()
    .Done()
  .Build();

return await app.RunAsync(args);

sealed class OperationCompletionSource : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    _ = context;
    yield return new CompletionCandidate("add", null, CompletionType.Parameter);
    yield return new CompletionCandidate("subtract", null, CompletionType.Parameter);
    yield return new CompletionCandidate("multiply", null, CompletionType.Parameter);
    yield return new CompletionCandidate("divide", null, CompletionType.Parameter);
  }
}
```

## Related Documentation

- **[Built-in Routes](../features/built-in-routes.md)** - Built-in routes
- **[REPL Key Bindings](../features/repl-key-bindings.md)** - Keyboard shortcuts and profiles
- **[Shell Completion](../features/shell-completion.md)** - Tab completion configuration
- **[Auto-Help](../features/auto-help.md)** - Help generation and filtering
- **[Architecture Choices](../guides/architecture-choices.md)** - Direct vs Mediator approaches
