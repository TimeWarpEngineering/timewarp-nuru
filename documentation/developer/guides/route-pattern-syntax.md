# Route Pattern Syntax

This document describes the route pattern syntax used in TimeWarp.Nuru for defining CLI commands.

## Basic Syntax

### Literal Segments

Literal segments are plain text that must match exactly:

```csharp
.Map("status", () => Console.WriteLine("OK"))
.Map("git commit", () => Console.WriteLine("Committing..."))
```

### Parameters

Parameters are defined using curly braces `{}` and capture values from the command line:

```csharp
// Basic parameter
.Map("greet {name}", (string name) => Console.WriteLine($"Hello {name}"))

// Multiple parameters
.Map("copy {source} {destination}", (string source, string dest) => ...)
```

### Parameter Types

Parameters can have type constraints using a colon `:` followed by the type:

```csharp
.Map("delay {ms:int}", (int milliseconds) => ...)
.Map("price {amount:double}", (double amount) => ...)
.Map("schedule {date:DateTime}", (DateTime date) => ...)
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
.Map("deploy {env} {tag?}", (string env, string? tag) => ...)
```

Optional parameters can also have type constraints:

```csharp
.Map("wait {seconds:int?}", (int? seconds) => ...)
.Map("backup {source} {destination:string?}", (string source, string? destination) => ...)
```

### Catch-all Parameters

Use `*` prefix for catch-all parameters that capture all remaining arguments:

```csharp
.Map("docker {*args}", (string[] args) => ...)
```

## Options

Options start with `--` (long form) or `-` (short form):

```csharp
// Boolean option
.Map("build --verbose", (bool verbose) => ...)

// Option with value
.Map("build --config {mode}", (string mode) => ...)

// Short form
.Map("build -c {mode}", (string mode) => ...)
```

## Descriptions

### Parameter Descriptions

Add descriptions to parameters using the pipe `|` character:

```csharp
.Map("deploy {env|Target environment (dev, staging, prod)}", 
    (string env) => ...)

.Map("copy {source|Source file path} {dest|Destination path}", 
    (string source, string dest) => ...)
```

### Option Descriptions

Options can have descriptions and short aliases:

```csharp
// Option with description
.Map("build --verbose|Show detailed output", 
    (bool verbose) => ...)

// Option with short alias and description
.Map("build --config,-c|Build configuration mode", 
    (string config) => ...)

// Option with parameter and descriptions
.Map("deploy {env} --version|Deploy specific version {ver|Version tag}", 
    (string env, string ver) => ...)
```

### Short Aliases

Use comma `,` to specify short aliases for options:

```csharp
.Map("test --verbose,-v", (bool verbose) => ...)
.Map("build --output,-o {path}", (string path) => ...)
```

## Complex Examples

### Multiple Options with Descriptions

```csharp
.Map("deploy {env|Environment name} " +
          "--dry-run,-d|Preview without deploying " +
          "--force,-f|Skip confirmations",
    (string env, bool dryRun, bool force) => ...)
```

### Options with Parameters and Descriptions

```csharp
.Map("backup {source|Directory to backup} " +
          "--output,-o|Backup file location {path|Output path} " +
          "--compress,-c|Enable compression",
    (string source, string path, bool compress) => ...)
```

## Route Descriptions

In addition to inline descriptions, you can provide an overall route description:

```csharp
.Map("deploy {env}", 
    (string env) => ...,
    description: "Deploy application to specified environment")
```

## Automatic Help Generation

Enable automatic help generation for all routes:

```csharp
var app = new NuruAppBuilder()
    .Map(...)
    .Map(...)
    .AddAutoHelp()  // Generates --help routes automatically
    .Build();
```

This will automatically create help routes for:
- `--help` - Shows all available commands
- `command --help` - Shows help for specific command and its variations

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
var app = new NuruAppBuilder()
    // Simple command
    .Map("version", 
        () => Console.WriteLine("1.0.0"),
        description: "Show version information")
    
    // Command with parameters and descriptions
    .Map("deploy {env|Target environment (dev, staging, prod)} {tag?|Optional version tag}",
        (string env, string? tag) => DeployTo(env, tag),
        description: "Deploy application to environment")
    
    // Command with options
    .Map("test {project|Project name} " +
              "--verbose,-v|Show detailed output " +
              "--filter,-f|Test name filter {pattern|Filter pattern}",
        (string project, bool verbose, string? pattern) => RunTests(project, verbose, pattern),
        description: "Run tests for specified project")
    
    // Enable automatic help
    .AddAutoHelp()
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