# Route Registration API Design

## Overview

This document defines the **consumer-facing API** for route registration in TimeWarp.Nuru 3.0. The goal is an optimal, clean API — not constrained by backward compatibility.

## Design Principles

1. **Delegates are syntactic sugar** — All routes become Command/Handler pairs at compile time
2. **Consistency** — Same mental model whether you write a delegate or a Command class
3. **Discoverability** — API guides users toward correct usage
4. **AOT-first** — API designed for compile-time code generation

---

## Core Route Registration

### `Map(pattern, delegate)`

The simplest form — register a route with an inline handler.

```csharp
builder.Map("add {x:double} {y:double}", (double x, double y) =>
{
    Console.WriteLine($"{x} + {y} = {x + y}");
});
```

**Async variant:**

```csharp
builder.Map("fetch {url}", async (string url) =>
{
    var content = await httpClient.GetStringAsync(url);
    Console.WriteLine(content);
});
```

**With return value (exit code):**

```csharp
builder.Map("validate {file}", (string file) =>
{
    return File.Exists(file) ? 0 : 1;
});
```

---

### `Map<TCommand>(pattern)`

Register a route with an explicit Command class (Mediator pattern).

```csharp
builder.Map<AddCommand>("add {x:double} {y:double}");

public sealed class AddCommand : IRequest
{
    public double X { get; set; }
    public double Y { get; set; }
    
    public sealed class Handler : IRequestHandler<AddCommand>
    {
        public ValueTask<Unit> Handle(AddCommand request, CancellationToken ct)
        {
            Console.WriteLine($"{request.X} + {request.Y} = {request.X + request.Y}");
            return default;
        }
    }
}
```

---

### `MapDefault(delegate)` / `MapDefault<TCommand>()`

Register the default route — invoked when no other route matches.

**Simple delegate:**

```csharp
builder.MapDefault(() => Console.WriteLine("Usage: mycli <command>"));
```

**With Command class:**

```csharp
builder.MapDefault<DefaultCommand>();

public sealed class DefaultCommand : IRequest
{
    public sealed class Handler : IRequestHandler<DefaultCommand>
    {
        public ValueTask<Unit> Handle(DefaultCommand request, CancellationToken ct)
        {
            Console.WriteLine("Usage: mycli <command>");
            return default;
        }
    }
}
```

**With options pattern (optional):**

Default routes can still accept options — they just have no required positional parameters:

```csharp
// Delegate with options
builder.MapDefault("--verbose,-v --format {fmt?}", (bool verbose, string? format) =>
{
    ShowHelp(verbose, format);
});

// Command with options
builder.MapDefault<HelpCommand>("--verbose,-v --format {fmt?}");
```

This allows:
```bash
mycli                      # Matches default
mycli --verbose            # Matches default with option
mycli --format json        # Matches default with option
```

---

## Multiple Patterns (Aliases)

### `MapMultiple(patterns[], delegate)`

Register multiple patterns that invoke the same handler.

```csharp
builder.MapMultiple(["exit", "quit", "q"], () => Environment.Exit(0), "Exit the application");
```

**Use cases:**
- Command aliases (`exit`, `quit`, `q`)
- Abbreviations (`list`, `ls`)
- Legacy command names alongside new ones

**Behavior:**
- First pattern is "primary" (shown in help)
- All patterns share one Command/Handler
- Parameters must match across patterns (or be a subset)

---

## Grouped Routes

### `MapGroup(prefix)`

Create a group of routes sharing a common prefix and/or options.

```csharp
var docker = builder.MapGroup("docker")
    .WithDescription("Container management commands")
    .WithGroupOptions("--debug,-D --log-level {level?}");

docker.Map("run {image}", (string image, bool debug, string? logLevel) => { ... });
docker.Map("build {path}", (string path, bool debug, string? logLevel) => { ... });
```

**Resulting effective patterns:**
- `docker run {image} --debug,-D? --log-level {level?}`
- `docker build {path} --debug,-D? --log-level {level?}`

### Nested Groups

Groups can be nested, with options accumulating:

```csharp
var docker = builder.MapGroup("docker")
    .WithGroupOptions("--debug,-D");

var compose = docker.MapGroup("compose")
    .WithGroupOptions("--file,-f {path?}");

compose.Map("up", (bool debug, string? file) => { ... });
// Effective: docker compose up --debug,-D? --file,-f {path?}
```

### Fluent Chain Requirement

**API CONSTRAINT:** Group routes must be defined in a fluent chain:

```csharp
// SUPPORTED - fluent chain
builder.MapGroup("docker")
    .WithGroupOptions("--debug")
    .Map("run {image}", handler);

// SUPPORTED - variable but immediate Map calls
var docker = builder.MapGroup("docker").WithGroupOptions("--debug");
docker.Map("run {image}", handler);  // Same statement block, trackable
```

**Why:** Enables source generator to resolve group context without complex data flow analysis.

---

## Route Options

### Description

```csharp
builder.Map("add {x} {y}", handler, description: "Add two numbers");

// Or fluent
builder.Map("add {x} {y}", handler)
    .WithDescription("Add two numbers");
```

### Hidden Routes

Routes that work but don't appear in help:

```csharp
builder.Map("secret-debug", handler)
    .Hidden();
```

### Deprecation

```csharp
builder.Map("old-command", handler)
    .Deprecated("Use 'new-command' instead");
```

---

## Pattern Syntax

### Literals

```csharp
"add"                    // Single literal
"user create"            // Multi-word command
"docker compose up"      // Nested subcommands
```

### Parameters

```csharp
"{name}"                 // Required string parameter
"{count:int}"            // Typed parameter
"{value:double}"         // Double parameter
"{enabled:bool}"         // Boolean parameter
"{name?}"                // Optional parameter
"{items:string[]*}"      // Catch-all array
```

### Options (Flags)

```csharp
"--verbose"              // Boolean flag
"--verbose,-v"           // Flag with alias
"--output {path}"        // Option with value
"--count {n:int}"        // Typed option value
"--format {fmt?}"        // Optional option value
```

### Combined

```csharp
"deploy {env} --force,-f --replicas {count:int?}"
```

---

## Dependency Injection in Handlers

### Delegate with DI Parameters

Parameters not in the pattern are resolved from DI:

```csharp
builder.Map("users list", (IUserService userService) =>
{
    foreach (var user in userService.GetAll())
        Console.WriteLine(user.Name);
});
```

**Convention:** Pattern parameters bind by name, remaining parameters come from DI.

### Explicit DI via Handler Class

```csharp
public sealed class ListUsersHandler : IRequestHandler<ListUsersCommand>
{
    private readonly IUserService _userService;
    
    public ListUsersHandler(IUserService userService)
    {
        _userService = userService;
    }
    
    public ValueTask<Unit> Handle(ListUsersCommand request, CancellationToken ct)
    {
        // Use _userService
    }
}
```

---

## Configuration

### App Builder

```csharp
var builder = NuruApp.CreateBuilder(args);

builder.ConfigureServices(services =>
{
    services.AddSingleton<IUserService, UserService>();
});

builder.Map("greet {name}", (string name) => Console.WriteLine($"Hello, {name}!"));

var app = builder.Build();
return await app.RunAsync(args);
```

---

## Open API Questions

1. **Should `MapMultiple` be renamed?** Alternatives: `MapAliases`, `MapWithAliases`

2. **Group options syntax** — Is `WithGroupOptions("--debug,-D --log-level {level?}")` the right API, or should it be:
   ```csharp
   .WithGroupOption("--debug,-D")
   .WithGroupOption("--log-level {level?}")
   ```

3. **Should hidden/deprecated be builder methods or attributes?**
   ```csharp
   // Builder method
   builder.Map("secret", handler).Hidden();
   
   // Or attribute on Command
   [Hidden]
   public class SecretCommand : IRequest { }
   ```

4. **Return value semantics** — Should `int` return always mean exit code? What about `string` returns (output)?

5. **CancellationToken** — Should it be implicitly available to all handlers, or explicitly requested as a parameter?
