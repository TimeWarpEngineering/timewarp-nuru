# Routing

Web-style routing for CLI applications - bringing familiar web development patterns to the command line.

## Route Pattern Syntax

TimeWarp.Nuru supports intuitive route patterns:

| Pattern         | Example                 | Matches                                         |
| --------------- | ----------------------- | ----------------------------------------------- |
| Literal         | `status`                | `./cli status`                                  |
| Parameter       | `greet {name}`          | `./cli greet Alice`                             |
| Typed Parameter | `delay {ms:int}`        | `./cli delay 1000`                              |
| Optional        | `deploy {env} {tag?}`   | `./cli deploy prod` or `./cli deploy prod v1.2` |
| Options         | `build --config {mode}` | `./cli build --config Release`                  |
| Catch-all       | `docker {*args}`        | `./cli docker run -it ubuntu`                   |

## Type Safety

Parameters are automatically converted to the correct types:

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  // Supports common types out of the box
  .Map("wait {seconds:int}")
    .WithHandler((int seconds) => Thread.Sleep(seconds * 1000))
    .AsCommand()
    .Done()
  .Map("download {url:uri}")
    .WithHandler((Uri url) => Download(url))
    .AsCommand()
    .Done()
  .Map("verbose {enabled:bool}")
    .WithHandler((bool enabled) => SetVerbose(enabled))
    .AsCommand()
    .Done()
  .Map("process {date:datetime}")
    .WithHandler((DateTime date) => Process(date))
    .AsCommand()
    .Done()
  .Map("scale {factor:double}")
    .WithHandler((double factor) => Scale(factor))
    .AsCommand()
    .Done()
  .Build();
```

### Supported Types

TimeWarp.Nuru includes built-in type converters for:

| Type Syntax | C# Type | Example |
|-------------|---------|---------|
| `string` | `string` | `{name:string}` or `{name}` (default) |
| `int` | `int` (Int32) | `{count:int}` |
| `double` | `double` | `{factor:double}` |
| `bool` | `bool` | `{enabled:bool}` |
| `DateTime` | `DateTime` | `{date:DateTime}` |
| `Guid` | `Guid` | `{id:Guid}` |
| `long` | `long` (Int64) | `{value:long}` |
| `decimal` | `decimal` | `{price:decimal}` |
| `TimeSpan` | `TimeSpan` | `{duration:TimeSpan}` |
| `uri` | `Uri` | `{url:uri}` |

See [Supported Types Reference](../reference/supported-types.md) for complete list and custom type converters.

## Default Route (MapDefault)

The `MapDefault` method registers a handler that executes when no arguments are provided:

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .MapDefault()
    .WithHandler(() => Console.WriteLine("Usage: myapp <command>"))
    .AsCommand()
    .Done()
  .Map("greet {name}")
    .WithHandler((string name) => Console.WriteLine($"Hello, {name}!"))
    .AsCommand()
    .Done()
  .Build();
```

```bash
./myapp              # Prints: Usage: myapp <command>
./myapp greet Alice  # Prints: Hello, Alice!
```

### Common Use Case: Show Help When No Args

A typical pattern is to display help information when users run your CLI without arguments:

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .MapDefault()
    .WithHandler(() =>
    {
      Console.WriteLine("myapp - A sample CLI application");
      Console.WriteLine();
      Console.WriteLine("Commands:");
      Console.WriteLine("  greet {name}    Greet someone by name");
      Console.WriteLine("  version         Show version info");
      Console.WriteLine("  help            Show detailed help");
    })
    .AsCommand()
    .Done()
  .Map("greet {name}")
    .WithHandler((string name) => Console.WriteLine($"Hello, {name}!"))
    .AsCommand()
    .Done()
  .Map("version")
    .WithHandler(() => Console.WriteLine("v1.0.0"))
    .AsCommand()
    .Done()
  .Build();
```

### MapDefault vs Catch-All `{*args}`

While both can handle "fallback" scenarios, they serve different purposes:

| Feature | `MapDefault` | Catch-all `{*args}` |
|---------|--------------|---------------------|
| **Matches** | Empty input only (no arguments) | Any unmatched input |
| **Use case** | Show usage/help when CLI invoked alone | Forward unknown commands elsewhere |
| **Handler receives** | Nothing | All arguments as `string[]` |
| **Specificity** | Most specific (exact empty match) | Least specific (matches anything) |

```csharp
NuruApp app = NuruApp.CreateBuilder(args)
  .MapDefault()
    .WithHandler(() => Console.WriteLine("No command provided. Try 'help'."))
    .AsCommand()
    .Done()
  .Map("help")
    .WithHandler(() => Console.WriteLine("Available: greet, version"))
    .AsCommand()
    .Done()
  .Map("{*args}")
    .WithHandler((string[] args) => Console.WriteLine($"Unknown: {string.Join(" ", args)}"))
    .AsCommand()
    .Done()
  .Build();
```

```bash
./myapp                    # "No command provided. Try 'help'."
./myapp help               # "Available: greet, version"
./myapp unknown command    # "Unknown: unknown command"
```

## Literal Segments

Literal segments must match exactly:

```csharp
builder.Map("status")
  .WithHandler(() => ShowStatus())
  .AsCommand()
  .Done();
builder.Map("version")
  .WithHandler(() => ShowVersion())
  .AsCommand()
  .Done();
builder.Map("git status")  // Multi-word literal
  .WithHandler(() => GitStatus())
  .AsCommand()
  .Done();
```

```bash
./cli status        # Matches
./cli version       # Matches
./cli git status    # Matches
./cli stat          # No match
```

## Parameters

Parameters capture values from command-line arguments:

```csharp
builder.Map("greet {name}")
  .WithHandler((string name) => Console.WriteLine($"Hello, {name}!"))
  .AsCommand()
  .Done();

builder.Map("add {x:double} {y:double}")
  .WithHandler((double x, double y) => Console.WriteLine($"{x} + {y} = {x + y}"))
  .AsCommand()
  .Done();
```

```bash
./cli greet Alice              # name = "Alice"
./cli add 10 20                # x = 10.0, y = 20.0
./cli add 3.14 2.86            # x = 3.14, y = 2.86
```

### Parameter Names

- Must be valid C# identifiers
- Automatically match handler delegate parameters by name
- Case-sensitive in the route pattern
- Must be unique within a route

## Optional Parameters

Parameters marked with `?` are optional:

```csharp
builder.Map("deploy {env} {tag?}")
  .WithHandler((string env, string? tag) =>
  {
    Console.WriteLine($"Deploying to {env}");
    if (tag != null)
      Console.WriteLine($"Version: {tag}");
  })
  .AsCommand()
  .Done();
```

```bash
./cli deploy prod              # env = "prod", tag = null
./cli deploy prod v1.2.3       # env = "prod", tag = "v1.2.3"
```

**Rules:**
- Optional parameters must appear after all required parameters
- Use nullable types in handler (`string?`, `int?`, etc.)
- Multiple consecutive optional parameters create ambiguity (analyzer error NURU_S002)

## Options (Flags)

Options provide named arguments with `--` or `-` prefixes:

```csharp
// Long form
builder.Map("build --verbose")
  .WithHandler(() => BuildVerbose())
  .AsCommand()
  .Done();

// Short form
builder.Map("list -l")
  .WithHandler(() => ListDetailed())
  .AsCommand()
  .Done();

// With values
builder.Map("serve --port {port:int}")
  .WithHandler((int port) => StartServer(port))
  .AsCommand()
  .Done();

// Optional options
builder.Map("build --config? {mode?}")
  .WithHandler((string? mode) => Build(mode ?? "Release"))
  .AsCommand()
  .Done();
```

```bash
./cli build --verbose
./cli list -l
./cli serve --port 8080
./cli build --config Debug
./cli build                    # mode = null, defaults to "Release"
```

### Option Aliases

Options can have both long and short forms:

```csharp
builder.Map("backup {source} --compress,-c")
  .WithHandler((string source, bool compress) => Backup(source, compress))
  .AsCommand()
  .Done();
```

```bash
./cli backup ./data --compress   # compress = true
./cli backup ./data -c           # compress = true (same)
./cli backup ./data              # compress = false
```

## Catch-All Parameters

Catch-all parameters capture all remaining arguments:

```csharp
builder.Map("echo {*words}")
  .WithHandler((string[] words) => Console.WriteLine(string.Join(" ", words)))
  .AsCommand()
  .Done();

builder.Map("git add {*files}")
  .WithHandler((string[] files) => StageFiles(files))
  .AsCommand()
  .Done();
```

```bash
./cli echo Hello World from Nuru
# words = ["Hello", "World", "from", "Nuru"]

./cli git add file1.cs file2.cs file3.cs
# files = ["file1.cs", "file2.cs", "file3.cs"]
```

**Rules:**
- Must be the last positional parameter
- Cannot be combined with optional parameters (analyzer error NURU_S004)
- Handler parameter must be `string[]`

## Complex Scenarios

### Git-Style Sub-Commands

Build hierarchical command structures:

```csharp
// Repository commands
builder.Map("git init")
  .WithHandler(() => GitInit())
  .AsCommand()
  .Done();
builder.Map("git clone {url}")
  .WithHandler((string url) => GitClone(url))
  .AsCommand()
  .Done();
builder.Map("git status")
  .WithHandler(() => GitStatus())
  .AsCommand()
  .Done();

// Branch commands
builder.Map("git branch")
  .WithHandler(() => ListBranches())
  .AsCommand()
  .Done();
builder.Map("git branch {name}")
  .WithHandler((string name) => CreateBranch(name))
  .AsCommand()
  .Done();
builder.Map("git checkout {branch}")
  .WithHandler((string branch) => Checkout(branch))
  .AsCommand()
  .Done();

// Commit commands
builder.Map("git add {*files}")
  .WithHandler((string[] files) => GitAdd(files))
  .AsCommand()
  .Done();
builder.Map("git commit -m {message}")
  .WithHandler((string message) => GitCommit(message))
  .AsCommand()
  .Done();
builder.Map("git push")
  .WithHandler(() => GitPush())
  .AsCommand()
  .Done();
builder.Map("git push --force")
  .WithHandler(() => GitPushForce())
  .AsCommand()
  .Done();
```

### Docker-Style Options

Complex option combinations:

```csharp
builder.Map("run {image}")
  .WithHandler((string image) => Docker.Run(image))
  .AsCommand()
  .Done();

builder.Map("run {image} --port {port:int}")
  .WithHandler((string image, int port) => Docker.Run(image, port: port))
  .AsCommand()
  .Done();

builder.Map("run {image} --port {port:int} --detach")
  .WithHandler((string image, int port) => Docker.Run(image, port: port, detached: true))
  .AsCommand()
  .Done();

builder.Map("run {image} --env {*vars}")
  .WithHandler((string image, string[] vars) => Docker.Run(image, envVars: vars))
  .AsCommand()
  .Done();
```

### Conditional Routing Based on Options

Different handlers for different option combinations:

```csharp
// Dry run
builder.Map("deploy {app} --env {environment} --dry-run")
  .WithHandler((string app, string environment) => DeployDryRun(app, environment))
  .AsCommand()
  .Done();

// Actual deployment
builder.Map("deploy {app} --env {environment}")
  .WithHandler((string app, string environment) => DeployReal(app, environment))
  .AsCommand()
  .Done();

// Force deployment
builder.Map("deploy {app} --env {environment} --force")
  .WithHandler((string app, string environment) => DeployForce(app, environment))
  .AsCommand()
  .Done();
```

## Route Specificity and Matching

When multiple routes could match, Nuru uses specificity rules:

1. **Most specific wins**: Routes with more literals are more specific
2. **Parameters vs catch-all**: Regular parameters are more specific than catch-all
3. **Options matter**: Routes with specific options are more specific

```csharp
builder.Map("deploy prod")                                       // Most specific
  .WithHandler(() => DeployProduction())
  .AsCommand()
  .Done();
builder.Map("deploy {env}")                                      // Less specific
  .WithHandler((string env) => DeployEnv(env))
  .AsCommand()
  .Done();
builder.Map("{*args}")                                           // Least specific
  .WithHandler((string[] args) => HandleGeneric(args))
  .AsCommand()
  .Done();
```

```bash
./cli deploy prod    # Matches first route (most specific)
./cli deploy dev     # Matches second route
./cli anything else  # Matches third route
```

## Best Practices

### Self-Contained Routes

Design routes to minimize the number of route definitions:

```csharp
// ❌ Factorial explosion with optional parameters
builder.Map("deploy {env}").WithHandler(handler).AsCommand().Done();
builder.Map("deploy {env} {version}").WithHandler(handler).AsCommand().Done();
builder.Map("deploy {env} {version} {region}").WithHandler(handler).AsCommand().Done();
// Creates 3 routes for one concept

// ✅ Use optional parameters
builder.Map("deploy {env} {version?} {region?}").WithHandler(handler).AsCommand().Done();
// One route, same flexibility
```

### Clear Parameter Names

Use descriptive names that indicate purpose:

```csharp
// ❌ Unclear
builder.Map("copy {arg1} {arg2}").WithHandler(handler).AsCommand().Done();

// ✅ Clear
builder.Map("copy {source} {destination}").WithHandler(handler).AsCommand().Done();
```

### Consistent Option Naming

Follow CLI conventions:

```csharp
// ✅ Standard conventions
builder.Map("build --verbose").WithHandler(handler).AsCommand().Done();      // Long form
builder.Map("build -v").WithHandler(handler).AsCommand().Done();             // Short form
builder.Map("build --verbose,-v").WithHandler(handler).AsCommand().Done();   // Both (preferred)
```

## Related Documentation

- **[Roslyn Analyzer](analyzer.md)** - Compile-time route validation
- **[Supported Types](../reference/supported-types.md)** - Complete type reference
- **[Auto-Help](auto-help.md)** - Generating help from routes
- **[Developer Guide: Route Pattern Syntax](../../developer/guides/route-pattern-syntax.md)** - Implementation details
