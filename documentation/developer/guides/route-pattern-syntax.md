# Route Pattern Syntax

This document describes the route pattern syntax used in TimeWarp.Nuru for defining CLI commands.

## Basic Syntax

### Literal Segments

Literal segments are plain text that must match exactly:

```csharp
.Map("status")
  .WithHandler(() => Console.WriteLine("OK"))
  .AsCommand()
  .Done()
.Map("git commit")
  .WithHandler(() => Console.WriteLine("Committing..."))
  .AsCommand()
  .Done()
```

### Parameters

Parameters are defined using curly braces `{}` and capture values from the command line:

```csharp
// Basic parameter
.Map("greet {name}")
  .WithHandler((string name) => Console.WriteLine($"Hello {name}"))
  .AsCommand()
  .Done()

// Multiple parameters
.Map("copy {source} {destination}")
  .WithHandler((string source, string dest) => ...)
  .AsCommand()
  .Done()
```

### Parameter Types

Parameters can have type constraints using a colon `:` followed by the type:

```csharp
.Map("delay {ms:int}")
  .WithHandler((int milliseconds) => ...)
  .AsCommand()
  .Done()
.Map("price {amount:double}")
  .WithHandler((double amount) => ...)
  .AsCommand()
  .Done()
.Map("schedule {date:DateTime}")
  .WithHandler((DateTime date) => ...)
  .AsCommand()
  .Done()
```

Supported types:
- `string` (default if no type specified)
- `int`
- `double`
- `bool`
- `DateTime`
- `Guid`
- `long`
- `float`
- `decimal`

### Optional Parameters

Parameters can be made optional by adding `?` after the name:

```csharp
.Map("deploy {env} {tag?}")
  .WithHandler((string env, string? tag) => ...)
  .AsCommand()
  .Done()
```

Optional parameters can also have type constraints:

```csharp
.Map("wait {seconds:int?}")
  .WithHandler((int? seconds) => ...)
  .AsCommand()
  .Done()
.Map("backup {source} {destination:string?}")
  .WithHandler((string source, string? destination) => ...)
  .AsCommand()
  .Done()
```

### Catch-all Parameters

Use `*` prefix for catch-all parameters that capture all remaining arguments:

```csharp
.Map("docker {*args}")
  .WithHandler((string[] args) => ...)
  .AsCommand()
  .Done()
```

## Options

Options start with `--` (long form) or `-` (short form):

```csharp
// Boolean option
.Map("build --verbose")
  .WithHandler((bool verbose) => ...)
  .AsCommand()
  .Done()

// Option with value
.Map("build --config {mode}")
  .WithHandler((string mode) => ...)
  .AsCommand()
  .Done()

// Short form
.Map("build -c {mode}")
  .WithHandler((string mode) => ...)
  .AsCommand()
  .Done()
```

## Descriptions

### Parameter Descriptions

Add descriptions to parameters using the pipe `|` character:

```csharp
.Map("deploy {env|Target environment (dev, staging, prod)}")
  .WithHandler((string env) => ...)
  .AsCommand()
  .Done()

.Map("copy {source|Source file path} {dest|Destination path}")
  .WithHandler((string source, string dest) => ...)
  .AsCommand()
  .Done()
```

### Option Descriptions

Options can have descriptions and short aliases:

```csharp
// Option with description
.Map("build --verbose|Show detailed output")
  .WithHandler((bool verbose) => ...)
  .AsCommand()
  .Done()

// Option with short alias and description
.Map("build --config,-c|Build configuration mode")
  .WithHandler((string config) => ...)
  .AsCommand()
  .Done()

// Option with parameter and descriptions
.Map("deploy {env} --version|Deploy specific version {ver|Version tag}")
  .WithHandler((string env, string ver) => ...)
  .AsCommand()
  .Done()
```

### Short Aliases

Use comma `,` to specify short aliases for options:

```csharp
.Map("test --verbose,-v")
  .WithHandler((bool verbose) => ...)
  .AsCommand()
  .Done()
.Map("build --output,-o {path}")
  .WithHandler((string path) => ...)
  .AsCommand()
  .Done()
```

## Complex Examples

### Multiple Options with Descriptions

```csharp
.Map("deploy {env|Environment name} " +
          "--dry-run,-d|Preview without deploying " +
          "--force,-f|Skip confirmations")
  .WithHandler((string env, bool dryRun, bool force) => ...)
  .AsCommand()
  .Done()
```

### Options with Parameters and Descriptions

```csharp
.Map("backup {source|Directory to backup} " +
          "--output,-o|Backup file location {path|Output path} " +
          "--compress,-c|Enable compression")
  .WithHandler((string source, string path, bool compress) => ...)
  .AsCommand()
  .Done()
```

## Route Descriptions

In addition to inline descriptions, you can provide an overall route description:

```csharp
.Map("deploy {env}")
  .WithHandler((string env) => ...)
  .WithDescription("Deploy application to specified environment")
  .AsCommand()
  .Done()
```

## Automatic Help Generation

Help is built in and enabled automatically for every app; no extra call is needed. Use `ConfigureHelp(...)` to customize it:

```csharp
NuruApp app = NuruApp.CreateBuilder()
    .Map(...)
    .Map(...)
    .Build();  // --help / -h work automatically
```

Help is available for:
- `--help` - Shows all available commands
- `command --help` - Shows help for every route whose leading literals equal `command`, most specific first. One match prints that route. `deploy --help` does not include `deploy status` or `deployment`.

## Best Practices

1. **Be consistent with descriptions**: Use sentence case and be concise
2. **Group related routes**: Keep similar commands together
3. **Use meaningful parameter names**: `{env}` is better than `{e}`
4. **Provide descriptions for complex parameters**: Especially for enums or specific formats
5. **Use short aliases sparingly**: Only for commonly used options
6. **Order matters**: More specific routes should come before generic ones

## Examples

### Complete Application Example

```csharp
NuruApp app = NuruApp.CreateBuilder()
    // Simple command
    .Map("version")
        .WithHandler(() => Console.WriteLine("1.0.0"))
        .WithDescription("Show version information")
        .AsQuery()
        .Done()

    // Command with parameters and descriptions
    .Map("deploy {env|Target environment (dev, staging, prod)} {tag?|Optional version tag}")
        .WithHandler((string env, string? tag) => DeployTo(env, tag))
        .WithDescription("Deploy application to environment")
        .AsCommand()
        .Done()

    // Command with options
    .Map("test {project|Project name} " +
              "--verbose,-v|Show detailed output " +
              "--filter,-f|Test name filter {pattern|Filter pattern}")
        .WithHandler((string project, bool verbose, string? pattern) => RunTests(project, verbose, pattern))
        .WithDescription("Run tests for specified project")
        .AsCommand()
        .Done()
    .Build();
```

This will generate help output like:

```
Available Routes:

--help                                  Show available commands
version                                 Show version information

Deploy Commands:
  deploy --help                         Show help for deploy command
  deploy {env} {tag?}                   Deploy application to environment

Test Commands:
  test --help                           Show help for test command
  test {project} --verbose,-v --filter,-f {pattern}  Run tests for specified project
```

And `deploy --help` will show:

```
Usage patterns for 'deploy':

  deploy {env} {tag?}
    Deploy application to environment

Arguments:
  env                  (Required)   Type: string    Target environment (dev, staging, prod)
  tag                  (Optional)   Type: string    Optional version tag
```