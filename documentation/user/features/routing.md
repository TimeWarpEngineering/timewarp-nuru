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
  .AddRoute("wait {seconds:int}", (int s) => Thread.Sleep(s * 1000))
  .AddRoute("download {url:uri}", (Uri url) => Download(url))
  .AddRoute("verbose {enabled:bool}", (bool v) => SetVerbose(v))
  .AddRoute("process {date:datetime}", (DateTime d) => Process(d))
  .AddRoute("scale {factor:double}", (double f) => Scale(f))
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

## Literal Segments

Literal segments must match exactly:

```csharp
builder.AddRoute("status", () => ShowStatus());
builder.AddRoute("version", () => ShowVersion());
builder.AddRoute("git status", () => GitStatus());  // Multi-word literal
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
builder.AddRoute
(
  "greet {name}",
  (string name) => Console.WriteLine($"Hello, {name}!")
);

builder.AddRoute
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
builder.AddRoute
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
builder.AddRoute("build --verbose", () => BuildVerbose());

// Short form
builder.AddRoute("list -l", () => ListDetailed());

// With values
builder.AddRoute("serve --port {port:int}", (int port) => StartServer(port));

// Optional options
builder.AddRoute
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
builder.AddRoute
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
builder.AddRoute
(
  "echo {*words}",
  (string[] words) => Console.WriteLine(string.Join(" ", words))
);

builder.AddRoute("git add {*files}", (string[] files) => StageFiles(files));
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
builder.AddRoute("git init", () => GitInit());
builder.AddRoute("git clone {url}", (string url) => GitClone(url));
builder.AddRoute("git status", () => GitStatus());

// Branch commands
builder.AddRoute("git branch", () => ListBranches());
builder.AddRoute("git branch {name}", (string name) => CreateBranch(name));
builder.AddRoute("git checkout {branch}", (string branch) => Checkout(branch));

// Commit commands
builder.AddRoute("git add {*files}", (string[] files) => GitAdd(files));
builder.AddRoute("git commit -m {message}", (string message) => GitCommit(message));
builder.AddRoute("git push", () => GitPush());
builder.AddRoute("git push --force", () => GitPushForce());
```

### Docker-Style Options

Complex option combinations:

```csharp
builder.AddRoute("run {image}", (string image) => Docker.Run(image));

builder.AddRoute
(
  "run {image} --port {port:int}",
  (string image, int port) => Docker.Run(image, port: port)
);

builder.AddRoute
(
  "run {image} --port {port:int} --detach",
  (string image, int port) => Docker.Run(image, port: port, detached: true)
);

builder.AddRoute
(
  "run {image} --env {*vars}",
  (string image, string[] vars) => Docker.Run(image, envVars: vars)
);
```

### Conditional Routing Based on Options

Different handlers for different option combinations:

```csharp
// Dry run
builder.AddRoute
(
  "deploy {app} --env {environment} --dry-run",
  (string app, string env) => DeployDryRun(app, env)
);

// Actual deployment
builder.AddRoute
(
  "deploy {app} --env {environment}",
  (string app, string env) => DeployReal(app, env)
);

// Force deployment
builder.AddRoute
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
builder.AddRoute("deploy prod", () => DeployProduction());           // Most specific
builder.AddRoute("deploy {env}", (string env) => DeployEnv(env));    // Less specific
builder.AddRoute("{*args}", (string[] args) => HandleGeneric(args)); // Least specific
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
builder.AddRoute("deploy {env}", handler);
builder.AddRoute("deploy {env} {version}", handler);
builder.AddRoute("deploy {env} {version} {region}", handler);
// Creates 3 routes for one concept

// ✅ Use optional parameters
builder.AddRoute("deploy {env} {version?} {region?}", handler);
// One route, same flexibility
```

### Clear Parameter Names

Use descriptive names that indicate purpose:

```csharp
// ❌ Unclear
builder.AddRoute("copy {arg1} {arg2}", handler);

// ✅ Clear
builder.AddRoute("copy {source} {destination}", handler);
```

### Consistent Option Naming

Follow CLI conventions:

```csharp
// ✅ Standard conventions
builder.AddRoute("build --verbose", handler);      // Long form
builder.AddRoute("build -v", handler);             // Short form
builder.AddRoute("build --verbose,-v", handler);   // Both (preferred)
```

## Related Documentation

- **[Roslyn Analyzer](analyzer.md)** - Compile-time route validation
- **[Supported Types](../reference/supported-types.md)** - Complete type reference
- **[Auto-Help](auto-help.md)** - Generating help from routes
- **[Developer Guide: Route Pattern Syntax](../../developer/guides/route-pattern-syntax.md)** - Implementation details
