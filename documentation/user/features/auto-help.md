# Automatic Help Generation

TimeWarp.Nuru can automatically generate help documentation for your CLI commands using the route patterns and inline descriptions.

## Enabling Auto-Help

Add `.AddAutoHelp()` to your application builder:

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("deploy {env}", (string env) => Deploy(env))
  .Map("backup {source}", (string source) => Backup(source))
  .AddAutoHelp()  // Enable automatic help
  .Build();
```

This automatically creates:
- `--help` or `-h` - Shows all available commands
- `<command> --help` - Shows usage for specific command

## Basic Help

```bash
./myapp --help
```

```
Available commands:
  deploy {env}
  backup {source}

Use '<command> --help' for detailed help on a specific command.
```

## Adding Descriptions

Use the pipe (`|`) syntax to add descriptions to parameters and options:

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map
  (
    "deploy {env|Target environment} {tag?|Optional version tag}",
    (string env, string? tag) => Deploy(env, tag)
  )
  .Map
  (
    "backup {source|Source directory} --compress,-c|Enable compression",
    (string source, bool compress) => Backup(source, compress)
  )
  .AddAutoHelp()
  .Build();
```

### Command-Level Help

```bash
./myapp deploy --help
```

```
Usage: myapp deploy {env} {tag?}

Parameters:
  env     Target environment (required)
  tag     Optional version tag (optional)

Options:
  --help, -h    Show this help message
```

### With Options

```bash
./myapp backup --help
```

```
Usage: myapp backup {source} [options]

Parameters:
  source    Source directory (required)

Options:
  --compress, -c    Enable compression
  --help, -h        Show this help message
```

## Complete Example

```csharp
using TimeWarp.Nuru;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map
  (
    "version|Show application version",
    () => Console.WriteLine("MyApp v1.0.0")
  )
  .Map
  (
    "deploy {env|Environment (prod/staging/dev)} {tag?|Version tag}",
    (string env, string? tag) => Deploy(env, tag)
  )
  .Map
  (
    "backup {source|Source path} {dest?|Destination path} --compress,-c|Compress backup",
    (string source, string? dest, bool compress) => Backup(source, dest, compress)
  )
  .Map
  (
    "logs {service|Service name} --tail,-t {lines:int|Number of lines}",
    (string service, int lines) => ShowLogs(service, lines)
  )
  .AddAutoHelp()
  .Build();

return await app.RunAsync(args);
```

### Generated Help Output

```bash
./myapp --help
```

```
MyApp - Command-line interface

Available commands:
  version                                Show application version
  deploy {env} {tag?}                    Deploy to specified environment
  backup {source} {dest?} [options]      Backup files
  logs {service} [options]               View service logs

Use '<command> --help' for detailed help on a specific command.
Use '--help' or '-h' after any command for usage information.
```

```bash
./myapp deploy --help
```

```
Usage: myapp deploy {env} {tag?}

Description:
  Deploy to specified environment

Parameters:
  env     Environment (prod/staging/dev) (required)
  tag     Version tag (optional)

Options:
  --help, -h    Show this help message

Examples:
  myapp deploy prod
  myapp deploy staging v1.2.3
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

Format: `"pattern|description"`

```csharp
.Map("version|Show application version", handler)
```

## Customizing Help Output

### Custom Help Text

You can provide custom help handlers:

```csharp
builder.Map("--help", () =>
{
    Console.WriteLine("MyApp - Custom Help");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  deploy {env}     Deploy to environment");
    Console.WriteLine("  backup {source}  Backup files");
});
```

### Conditional Help

Show different help based on context:

```csharp
builder.Map("deploy --help", () => ShowDeployHelp());
builder.Map("backup --help", () => ShowBackupHelp());
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
builder.Map("git --help", () => ShowGitHelp());
builder.Map("git commit --help", () => ShowGitCommitHelp());
builder.Map("git push --help", () => ShowGitPushHelp());
```

### Option Groups

Group related options in help text:

```csharp
builder.Map
(
  "serve {port:int|Port number} " +
  "--host {addr|Host address} " +
  "--ssl|Enable SSL " +
  "--cert {path|Certificate path}",
  handler
);
```

Help output shows options logically grouped.

## Related Documentation

- **[Routing](routing.md)** - Route pattern syntax
- **[Getting Started](../getting-started.md)** - Basic setup
- **[Use Cases](../use-cases.md)** - Real-world examples
