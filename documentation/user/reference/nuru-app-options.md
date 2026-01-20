# NuruAppOptions Configuration

`NuruAppOptions` configures all auto-wired extensions when using `NuruApp.CreateBuilder()`. It provides a single place to customize REPL behavior, telemetry, shell completion, help output, and built-in routes.

## Overview

When you use `NuruApp.CreateBuilder()`, it automatically wires up several extensions with sensible defaults. `NuruAppOptions` lets you customize these defaults without manually wiring each extension.

```csharp
NuruApp.CreateBuilder(args, new NuruAppOptions
{
    ConfigureRepl = options => options.Prompt = "myapp> ",
    ConfigureTelemetry = options => options.ServiceName = "my-app",
    DisableVersionRoute = true
});
```

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConfigureRepl` | `Action<ReplOptions>?` | `null` | Configure REPL options |
| `ConfigureTelemetry` | `Action<NuruTelemetryOptions>?` | `null` | Configure telemetry options |
| `ConfigureCompletion` | `Action<CompletionSourceRegistry>?` | `null` | Configure shell completion sources |
| `ConfigureHelp` | `Action<HelpOptions>?` | `null` | Configure help output filtering |
| `InteractiveRoutePatterns` | `string` | `"--interactive,-i"` | Comma-separated patterns that trigger REPL mode |
| `DisableVersionRoute` | `bool` | `false` | Disable `--version, -v` route |
| `DisableCheckUpdatesRoute` | `bool` | `false` | Disable `--check-updates` route |

## ConfigureRepl

Customizes REPL (Read-Eval-Print Loop) behavior when users enter interactive mode.

```csharp
NuruApp.CreateBuilder(args, new NuruAppOptions
{
    ConfigureRepl = options =>
    {
        options.Prompt = "myapp> ";
        options.ContinuationPrompt = "...... ";
        options.WelcomeMessage = "Welcome to MyApp! Type 'help' for commands.";
        options.GoodbyeMessage = "See you next time!";
        options.MaxHistorySize = 500;
        options.ShowTiming = true;
        options.KeyBindingProfileName = "Emacs";  // Or "Vi", "VSCode", "Default"
    }
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
| `HistoryFilePath` | `string?` | `null` | Custom history file location |
| `MaxHistorySize` | `int` | `1000` | Maximum commands in history |
| `ContinueOnError` | `bool` | `true` | Continue after command failures |
| `ShowExitCode` | `bool` | `false` | Display exit code after each command |
| `EnableColors` | `bool` | `true` | Enable colored output |
| `PromptColor` | `string` | `"\x1b[32m"` | ANSI color for prompt (default: green) |
| `ShowTiming` | `bool` | `true` | Show execution time |
| `EnableArrowHistory` | `bool` | `true` | Arrow key history navigation |
| `HistoryIgnorePatterns` | `IList<string>?` | Sensitive patterns | Patterns to exclude from history |
| `KeyBindingProfileName` | `string` | `"Default"` | Key binding profile name |
| `KeyBindingProfile` | `object?` | `null` | Custom key binding profile instance |

See [REPL Key Bindings](../features/repl-key-bindings.md) for keyboard shortcuts and customization.

## ConfigureTelemetry

Configures OpenTelemetry integration for tracing, metrics, and logging.

```csharp
NuruApp.CreateBuilder(args, new NuruAppOptions
{
    ConfigureTelemetry = options =>
    {
        options.ServiceName = "my-cli-app";
        options.ServiceVersion = "1.0.0";
        options.EnableTracing = true;
        options.EnableMetrics = true;
        options.EnableLogging = true;
        options.OtlpEndpoint = "http://localhost:4317";
    }
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

## ConfigureCompletion

Registers custom completion sources for shell tab-completion.

```csharp
NuruApp.CreateBuilder(args, new NuruAppOptions
{
    ConfigureCompletion = registry =>
    {
        // Static completions for a parameter name
        registry.RegisterForParameter("env", 
            new StaticCompletionSource("dev", "staging", "prod"));
        
        // Completions for all parameters of a type
        registry.RegisterForType(typeof(LogLevel), 
            new StaticCompletionSource("debug", "info", "warning", "error"));
    }
});
```

### Shell vs REPL Completion

There are two distinct completion systems:

| System | Context | How It Works |
|--------|---------|--------------|
| **Shell Completion** | External (bash, zsh, pwsh, fish) | Shell invokes CLI to get completions before execution |
| **REPL Completion** | In-process | Running REPL handles Tab keypresses internally |

`ConfigureCompletion` configures **shell completion** sources. REPL completion uses route pattern metadata automatically.

### CompletionSourceRegistry Methods

| Method | Description |
|--------|-------------|
| `RegisterForParameter(name, source)` | Register source for specific parameter name |
| `RegisterForType(type, source)` | Register source for all parameters of a type |

See [Shell Completion](../features/shell-completion.md) for generating completion scripts.

## ConfigureHelp

Customizes help output filtering and display.

```csharp
NuruApp.CreateBuilder(args, new NuruAppOptions
{
    ConfigureHelp = options =>
    {
        options.ShowPerCommandHelpRoutes = false;  // Hide "command --help?" routes
        options.ShowReplCommandsInCli = false;     // Hide exit, quit, clear from --help
        options.ShowCompletionRoutes = false;      // Hide __complete, --generate-completion
        options.ExcludePatterns = ["*-debug", "*-internal"];  // Custom exclusions
    }
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

## InteractiveRoutePatterns

Comma-separated route patterns that trigger REPL mode.

```csharp
// Add --repl as another way to enter interactive mode
NuruApp.CreateBuilder(args, new NuruAppOptions
{
    InteractiveRoutePatterns = "--interactive,-i,--repl"
});
```

```bash
# All these now enter REPL mode
myapp --interactive
myapp -i
myapp --repl
```

## DisableVersionRoute

When `true`, disables the automatic `--version, -v` route.

```csharp
// Implement custom version output
NuruApp.CreateBuilder(args, new NuruAppOptions
{
    DisableVersionRoute = true
})
.Map("--version,-v", () => Console.WriteLine("MyApp v1.0 (custom format)"));
```

### Default Version Output

When enabled, the built-in version route displays:
- Assembly informational version (or simple version as fallback)
- Git commit hash (if available from TimeWarp.Build.Tasks)
- Commit date (if available)

```bash
$ myapp --version
1.2.3
Commit: abc1234567890def1234567890abcdef12345678
Date: 2024-01-15T10:30:00Z
```

## DisableCheckUpdatesRoute

When `true`, disables the automatic `--check-updates` route.

```csharp
NuruApp.CreateBuilder(args, new NuruAppOptions
{
    DisableCheckUpdatesRoute = true  // App not hosted on GitHub
});
```

### Default Check Updates Behavior

When enabled, the built-in route:
- Queries GitHub releases for the latest version
- Compares against the current assembly version
- Displays colored output (green checkmark for up-to-date, yellow warning for updates)

Requires `RepositoryUrl` in your project file:

```xml
<PropertyGroup>
  <RepositoryUrl>https://github.com/your-org/your-repo</RepositoryUrl>
</PropertyGroup>
```

## Complete Example

```csharp
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder(args, new NuruAppOptions
{
    // Customize REPL
    ConfigureRepl = options =>
    {
        options.Prompt = "calc> ";
        options.WelcomeMessage = "Calculator REPL - Type 'help' for commands";
        options.KeyBindingProfileName = "Emacs";
    },
    
    // Configure telemetry
    ConfigureTelemetry = options =>
    {
        options.ServiceName = "calculator";
        options.EnableMetrics = true;
    },
    
    // Register completions
    ConfigureCompletion = registry =>
    {
        registry.RegisterForParameter("operation", 
            new StaticCompletionSource("add", "subtract", "multiply", "divide"));
    },
    
    // Hide internal routes from help
    ConfigureHelp = options =>
    {
        options.ExcludePatterns = ["*-debug"];
    },
    
    // Add --repl alias
    InteractiveRoutePatterns = "--interactive,-i,--repl"
})
.Map("add {x:double} {y:double}", (double x, double y) => 
    Console.WriteLine($"{x} + {y} = {x + y}"))
.Map("multiply {x:double} {y:double}", (double x, double y) => 
    Console.WriteLine($"{x} * {y} = {x * y}"))
.Build();

return await app.RunAsync();
```

## Manual Feature Configuration

You can manually configure features using the builder methods:

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
    .Map("greet {name}", (string name) => Console.WriteLine($"Hello, {name}!"))
    .AddVersionRoute()      // Manually add version
    .EnableStaticCompletion()  // Manually add completion
    .Build();
```

See [Architecture Choices](../guides/architecture-choices.md) for detailed guidance.

## Related Documentation

- **[Built-in Routes](../features/built-in-routes.md)** - Routes auto-registered by CreateBuilder
- **[REPL Key Bindings](../features/repl-key-bindings.md)** - Keyboard shortcuts and profiles
- **[Shell Completion](../features/shell-completion.md)** - Tab completion configuration
- **[Auto-Help](../features/auto-help.md)** - Help generation and filtering
- **[Architecture Choices](../guides/architecture-choices.md)** - CreateBuilder vs SlimBuilder
