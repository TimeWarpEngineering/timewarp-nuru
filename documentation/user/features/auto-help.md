# Automatic Help Generation

TimeWarp.Nuru can automatically generate help documentation for your CLI commands using the route patterns and inline descriptions.

## Enabling Auto-Help

Help is built in. `NuruApp.CreateBuilder()` registers it automatically, so there is nothing to enable:

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .Map("deploy {env}")
    .WithHandler((string env) => Deploy(env))
    .AsCommand()
    .Done()
  .Map("backup {source}")
    .WithHandler((string source) => Backup(source))
    .AsCommand()
    .Done()
  .Build();
```

This creates:
- `--help` or `-h` - Root help: `Version:`, `Usage:`, `Options:`, then `Commands:` (the first literal of each route)
- `<command> --help` - Per-route help. When several routes share its literal prefix, each of those routes is listed.

## Basic Help

```bash
./myapp --help
```

Root help uses the entry assembly name when the builder has no app name. This transcript uses `myapp`. The default version text is `v1.0.0`. Command rows are the first literal only, and the description is empty until you call `.WithDescription`.

```
Version:
  v1.0.0


Usage:
  myapp [command] [options]

Options:
  --help, -h                                 Show this help message
  --version                                  Show version information
  --capabilities                             Show capabilities for AI tools
  --capabilities --group-filter <group>      Filter capabilities by group prefix
  --capabilities --search <query>            Search capabilities using nuru
  --json-args <value>                        Bind arguments from -, @path, or an inline JSON object

Commands:
  deploy
  backup
```

## Adding Descriptions

Use the pipe (`|`) syntax to add descriptions to parameters and options:

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .Map("deploy {env|Target environment} {tag?|Optional version tag}")
    .WithHandler((string env, string? tag) => Deploy(env, tag))
    .AsCommand()
    .Done()
  .Map("backup {source|Source directory} --compress,-c|Enable compression")
    .WithHandler((string source, bool compress) => Backup(source, compress))
    .AsCommand()
    .Done()
  .Build();
```

### Command-Level Help

```bash
./myapp deploy --help
```

Per-route help prints the pattern, then an indented description when `.WithDescription` was set, then tables. A required parameter is `{name}`. An optional parameter is `[name]`. This route has no options and no examples, so those sections are omitted. The page ends with the JSON-args line.

```
deploy {env} [tag]

Parameters:
  Name    Required    Type      Description
  env     Yes         string    Target environment
  tag     No          string    Optional version tag

Values may come from --json-args.
```

### Several routes with the same literal prefix

When more than one route has the same leading literal segments, `<command> --help` prints every one of those routes. Each block uses the per-route layout above (pattern, description, parameters, options, and examples). The most specific route is printed first. A single matching route prints that route alone.

```csharp
.Map("deploy").WithDescription("Simple deploy").Done()
.Map("deploy {env}").WithDescription("Deploy to environment").Done()
```

`deploy --help` prints `deploy {env}` and then `deploy`.

Only routes whose leading literals equal the words typed are included. `deploy --help` does not include `deploy status` or `deployment`. A route under another group prefix is separate, so `git deploy` is not listed by `deploy --help`. The same rule applies to fluent `.Map(...)` routes, `[NuruRoute]` endpoints, and routes inside `.WithGroupPrefix(...)`.

`worktree --help` prints the group summary when the subcommands add a further literal (`worktree add`, `worktree list`). That summary is the group listing, not this shared-prefix listing.

### With Options

```bash
./myapp backup --help
```

An option is wrapped in `[]` only when the pattern marks it optional with `?` (`--compress?`). This sample does not, so `--compress,-c` is shown as required. The options table writes `--long, -short` (a space after the comma). Built-in `--help` is not repeated on the route.

```
backup {source} --compress,-c

Parameters:
  Name      Required    Type      Description
  source    Yes         string    Source directory

Options:
  Option            Description
  --compress, -c    Enable compression

Values may come from --json-args.
```

## Complete Example

```csharp
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .Map("version")
    .WithHandler(() => Console.WriteLine("MyApp v1.0.0"))
    .WithDescription("Show application version")
    .AsCommand()
    .Done()
  .Map("deploy {env|Environment (prod/staging/dev)} {tag?|Version tag}")
    .WithHandler((string env, string? tag) => Deploy(env, tag))
    .WithDescription("Deploy to an environment")
    .WithExample("deploy prod", "Deploy to production")
    .WithExample("deploy staging v1.2.3", "Deploy a version tag")
    .AsCommand()
    .Done()
  .Map("backup {source|Source path} {dest?|Destination path} --compress,-c|Compress backup")
    .WithHandler((string source, string? dest, bool compress) => Backup(source, dest, compress))
    .WithDescription("Backup files")
    .AsCommand()
    .Done()
  .Map("logs {service|Service name} --tail,-t {lines:int|Number of lines}")
    .WithHandler((string service, int lines) => ShowLogs(service, lines))
    .WithDescription("View service logs")
    .AsCommand()
    .Done()
  .Build();

return await app.RunAsync(args);
```

A pipe after a command literal (`version|...`) is not a description. It is an invalid character (NURU_P005). Parameter pipes (`{env|...}`) and option pipes (`--compress,-c|...`) are descriptions. Command text comes from `.WithDescription`.

### Generated Help Output

```bash
./myapp --help
```

`Commands:` lists the first literal and the `.WithDescription` text. It does not list the full pattern, and there is no "Available commands" footer.

```
Version:
  v1.0.0


Usage:
  myapp [command] [options]

Options:
  --help, -h                                 Show this help message
  --version                                  Show version information
  --capabilities                             Show capabilities for AI tools
  --capabilities --group-filter <group>      Filter capabilities by group prefix
  --capabilities --search <query>            Search capabilities using nuru
  --json-args <value>                        Bind arguments from -, @path, or an inline JSON object

Commands:
  version    Show application version
  deploy     Deploy to an environment
  backup     Backup files
  logs       View service logs
```

```bash
./myapp deploy --help
```

Examples appear only because this route calls `.WithExample`. The example text is what the user types after the executable name.

```
deploy {env} [tag]

  Deploy to an environment

Parameters:
  Name    Required    Type      Description
  env     Yes         string    Environment (prod/staging/dev)
  tag     No          string    Version tag

Examples:
  deploy prod
    Deploy to production
  deploy staging v1.2.3
    Deploy a version tag

Values may come from --json-args.
```

## Description Syntax

### Parameter Descriptions

Format: `{name|description}`

```csharp
"{env|Target environment}"
"{count:int|Number of items}"
"{file?|Optional file path}"
```

### Option Descriptions

Format: `--option,-o|description`

```csharp
"--verbose,-v|Enable verbose output"
"--config {mode}|Configuration mode (Debug/Release)"
```

### Command Descriptions

Use `.WithDescription(...)` on the endpoint builder:

```csharp
.Map("version")
  .WithHandler(handler)
  .WithDescription("Show application version")
  .AsQuery()
  .Done()
```

### Command Examples

The "Examples:" section shown above (under [Generated Help Output](#generated-help-output))
comes from `[NuruRouteExample]` (Endpoint DSL) or `.WithExample()` (Fluent DSL), not from the
description syntax. Examples render as full-width lines rather than a table, since help tables
truncate cell text:

```csharp
// Endpoint DSL
[NuruRoute("deploy", Description = "Deploy to an environment")]
[NuruRouteExample("deploy prod", Description = "Deploy to production")]
public sealed class DeployCommand : ICommand<Unit> { }

// Fluent DSL
.Map("deploy {env}")
  .WithHandler((string env) => Deploy(env))
  .WithExample("deploy prod", "Deploy to production")
  .Done()
```

A route with no examples declared omits the "Examples:" section entirely.

## Customizing Help Output

### Custom Help Text

You can provide custom help handlers:

```csharp
builder.Map("--help")
  .WithHandler(() =>
  {
    Console.WriteLine("MyApp - Custom Help");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  deploy {env}     Deploy to environment");
    Console.WriteLine("  backup {source}  Backup files");
  })
  .AsQuery()
  .Done();
```

### Conditional Help

Show different help based on context:

```csharp
builder.Map("deploy --help")
  .WithHandler(() => ShowDeployHelp())
  .AsQuery()
  .Done();
builder.Map("backup --help")
  .WithHandler(() => ShowBackupHelp())
  .AsQuery()
  .Done();
```

## Best Practices

### Write Clear Descriptions

```csharp
// ❌ Vague
"{env|The environment}"

// ✅ Clear
"{env|Target environment (prod, staging, or dev)}"
```

### Include Examples

```csharp
// Add usage examples in descriptions
"{port:int|Server port number (default: 8080)}"
"{format|Output format (json, xml, or text)}"
```

### Consistent Formatting

```csharp
// Use consistent capitalization and punctuation
"{source|Source directory}"
"{dest|Destination directory}"
"--verbose,-v|Enable verbose output"
"--quiet,-q|Suppress output"
```

## Help for Complex Commands

### Subcommands

```csharp
builder.Map("git --help")
  .WithHandler(() => ShowGitHelp())
  .AsQuery()
  .Done();
builder.Map("git commit --help")
  .WithHandler(() => ShowGitCommitHelp())
  .AsQuery()
  .Done();
builder.Map("git push --help")
  .WithHandler(() => ShowGitPushHelp())
  .AsQuery()
  .Done();
```

### Option Groups

Group related options in help text:

```csharp
builder.Map
(
  "serve {port:int|Port number} " +
  "--host {addr|Host address} " +
  "--ssl|Enable SSL " +
  "--cert {path|Certificate path}"
)
  .WithHandler(handler)
  .AsCommand()
  .Done();
```

Help output shows options logically grouped.

## Related Documentation

- **[Routing](routing.md)** - Route pattern syntax
- **[Getting Started](../getting-started.md)** - Basic setup
- **[Use Cases](../use-cases.md)** - Real-world examples
