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
NuruApp app = new NuruAppBuilder()
  // Supports common types out of the box
  .Map("wait {seconds:int}", (int s) => Thread.Sleep(s * 1000))
  .Map("download {url:uri}", (Uri url) => Download(url))
  .Map("verbose {enabled:bool}", (bool v) => SetVerbose(v))
  .Map("process {date:datetime}", (DateTime d) => Process(d))
  .Map("scale {factor:double}", (double f) => Scale(f))
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
NuruApp app = new NuruAppBuilder()
  .MapDefault(() => Console.WriteLine("Usage: myapp <command>"))
  .Map("greet {name}", (string name) => Console.WriteLine($"Hello, {name}!"))
  .Build();
```

```bash
./myapp              # Prints: Usage: myapp <command>
./myapp greet Alice  # Prints: Hello, Alice!
```

### Common Use Case: Show Help When No Args

A typical pattern is to display help information when users run your CLI without arguments:

```csharp
NuruApp app = new NuruAppBuilder()
  .MapDefault(() =>
  {
    Console.WriteLine("myapp - A sample CLI application");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  greet {name}    Greet someone by name");
    Console.WriteLine("  version         Show version info");
    Console.WriteLine("  help            Show detailed help");
  })
  .Map("greet {name}", (string name) => Console.WriteLine($"Hello, {name}!"))
  .Map("version", () => Console.WriteLine("v1.0.0"))
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
NuruApp app = new NuruAppBuilder()
  .MapDefault(() => Console.WriteLine("No command provided. Try 'help'."))
  .Map("help", () => Console.WriteLine("Available: greet, version"))
  .Map("{*args}", (string[] args) => Console.WriteLine($"Unknown: {string.Join(" ", args)}"))
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
builder.Map("status", () => ShowStatus());
builder.Map("version", () => ShowVersion());
builder.Map("git status", () => GitStatus());  // Multi-word literal
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
builder.Map
(
  "greet {name}",
  (string name) => Console.WriteLine($"Hello, {name}!")
);

builder.Map
(
  "add {x:double} {y:double}",
  (double x, double y) => Console.WriteLine($"{x} + {y} = {x + y}")
);
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
builder.Map
(
  "deploy {env} {tag?}",
  (string env, string? tag) =>
  {
    Console.WriteLine($"Deploying to {env}");
    if (tag != null)
      Console.WriteLine($"Version: {tag}");
  }
);
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
builder.Map("build --verbose", () => BuildVerbose());

// Short form
builder.Map("list -l", () => ListDetailed());

// With values
builder.Map("serve --port {port:int}", (int port) => StartServer(port));

// Optional options
builder.Map
(
  "build --config? {mode?}",
  (string? mode) => Build(mode ?? "Release")
);
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
builder.Map
(
  "backup {source} --compress,-c",
  (string source, bool compress) => Backup(source, compress)
);
```

```bash
./cli backup ./data --compress   # compress = true
./cli backup ./data -c           # compress = true (same)
./cli backup ./data              # compress = false
```

## Catch-All Parameters

Catch-all parameters capture all remaining arguments:

```csharp
builder.Map
(
  "echo {*words}",
  (string[] words) => Console.WriteLine(string.Join(" ", words))
);

builder.Map("git add {*files}", (string[] files) => StageFiles(files));
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
builder.Map("git init", () => GitInit());
builder.Map("git clone {url}", (string url) => GitClone(url));
builder.Map("git status", () => GitStatus());

// Branch commands
builder.Map("git branch", () => ListBranches());
builder.Map("git branch {name}", (string name) => CreateBranch(name));
builder.Map("git checkout {branch}", (string branch) => Checkout(branch));

// Commit commands
builder.Map("git add {*files}", (string[] files) => GitAdd(files));
builder.Map("git commit -m {message}", (string message) => GitCommit(message));
builder.Map("git push", () => GitPush());
builder.Map("git push --force", () => GitPushForce());
```

### Docker-Style Options

Complex option combinations:

```csharp
builder.Map("run {image}", (string image) => Docker.Run(image));

builder.Map
(
  "run {image} --port {port:int}",
  (string image, int port) => Docker.Run(image, port: port)
);

builder.Map
(
  "run {image} --port {port:int} --detach",
  (string image, int port) => Docker.Run(image, port: port, detached: true)
);

builder.Map
(
  "run {image} --env {*vars}",
  (string image, string[] vars) => Docker.Run(image, envVars: vars)
);
```

### Conditional Routing Based on Options

Different handlers for different option combinations:

```csharp
// Dry run
builder.Map
(
  "deploy {app} --env {environment} --dry-run",
  (string app, string env) => DeployDryRun(app, env)
);

// Actual deployment
builder.Map
(
  "deploy {app} --env {environment}",
  (string app, string env) => DeployReal(app, env)
);

// Force deployment
builder.Map
(
  "deploy {app} --env {environment} --force",
  (string app, string env) => DeployForce(app, env)
);
```

## Route Specificity and Matching

When multiple routes could match, Nuru uses specificity rules:

1. **Most specific wins**: Routes with more literals are more specific
2. **Parameters vs catch-all**: Regular parameters are more specific than catch-all
3. **Options matter**: Routes with specific options are more specific

```csharp
builder.Map("deploy prod", () => DeployProduction());           // Most specific
builder.Map("deploy {env}", (string env) => DeployEnv(env));    // Less specific
builder.Map("{*args}", (string[] args) => HandleGeneric(args)); // Least specific
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
builder.Map("deploy {env}", handler);
builder.Map("deploy {env} {version}", handler);
builder.Map("deploy {env} {version} {region}", handler);
// Creates 3 routes for one concept

// ✅ Use optional parameters
builder.Map("deploy {env} {version?} {region?}", handler);
// One route, same flexibility
```

### Clear Parameter Names

Use descriptive names that indicate purpose:

```csharp
// ❌ Unclear
builder.Map("copy {arg1} {arg2}", handler);

// ✅ Clear
builder.Map("copy {source} {destination}", handler);
```

### Consistent Option Naming

Follow CLI conventions:

```csharp
// ✅ Standard conventions
builder.Map("build --verbose", handler);      // Long form
builder.Map("build -v", handler);             // Short form
builder.Map("build --verbose,-v", handler);   // Both (preferred)
```

## Related Documentation

- **[Roslyn Analyzer](analyzer.md)** - Compile-time route validation
- **[Supported Types](../reference/supported-types.md)** - Complete type reference
- **[Auto-Help](auto-help.md)** - Generating help from routes
- **[Developer Guide: Route Pattern Syntax](../../developer/guides/route-pattern-syntax.md)** - Implementation details
