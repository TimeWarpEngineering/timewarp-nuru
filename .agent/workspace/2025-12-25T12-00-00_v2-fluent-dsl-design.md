# V2 Fluent DSL Design Document

**Date:** 2024-12-25  
**Status:** Design Complete  
**Reference Implementation:** `tests/timewarp-nuru-core-tests/routing/dsl-example.cs`  
**Related:** `.agent/workspace/2024-12-25T01-00-00_v2-generator-architecture.md`

## Executive Summary

This document defines the Fluent DSL for Nuru V2 — a compile-time source generator approach that eliminates runtime reflection and enables full Native AOT support. The generator intercepts `RunAsync()` calls and replaces them with generated dispatch code.

## Core Principle

> **Generate ALL deterministic code at compile-time via source generators**

**Deterministic (Compile-time):**
- Route patterns and matching logic
- Handler invocation code
- Parameter binding and type conversion
- Service injection points
- Help text generation
- Capabilities metadata

**Non-deterministic (Runtime only):**
- `args` passed at runtime
- Environment variables
- Configuration file values

---

## Architecture Overview

### NuGet Boundary Challenge

```
┌─────────────────────────┐     ┌─────────────────────────┐
│  NuGet: TimeWarp.Nuru   │     │  Consumer's Console App │
│                         │     │                         │
│  - NuruApp              │     │  - Program.cs (DSL)     │
│  - NuruCoreApp          │     │  - Handler lambdas      │
│  - Builder classes      │     │  - [Generated Code]     │
│  - RunAsync() base      │     │  - RunAsync interceptor │
└─────────────────────────┘     └─────────────────────────┘
```

The source generator runs in the consumer's assembly and has access to both:
1. The NuGet library types (for the builder API)
2. The consumer's handler lambdas and service types

### Interception Strategy: `RunAsync()`

Using C# 12 **interceptors**, the generator replaces `app.RunAsync(args)` with generated dispatch code.

**Important:** The generator intercepts the `RunAsync()` call regardless of what arguments are passed. It identifies the call site by method signature and receiver type, not by the argument values.

```csharp
// Production code - consumer writes:
return await app.RunAsync(args);

// Generator emits an interceptor for this call site:
[InterceptsLocation("Program.cs", line: 15, column: 16)]
public static Task<int> RunAsync_Generated(this NuruCoreApp app, string[] args)
{
    // Generated routing and dispatch logic
    // 'args' contains whatever the user passed at runtime
}
```

The generator:
1. Finds `app.RunAsync(...)` call site (any args)
2. Traces `app` back to its builder chain
3. Parses the fluent DSL to extract routes
4. Emits an interceptor with generated dispatch code

---

## Fluent DSL Specification

### Complete Production Example

```csharp
// Program.cs - What a consumer writes in production
NuruCoreApp app = NuruApp.CreateBuilder(args)
    .AddConfiguration()
    .ConfigureServices(services => services
        .AddLogging(builder => builder.AddConsole())
        .AddSingleton<MyService>()
    )
    .AddBehavior(typeof(TelemetryBehavior<,>))
    .UseTerminal(new SystemTerminal())
    .AddHelp()
    .AddRepl()
    .WithName("my-cli")
    .WithDescription("My awesome CLI application")
    .Map("status")
        .WithHandler(() => "healthy")
        .WithDescription("Check application status")
        .AsQuery()
        .Done()
    .Map("greet {name}")
        .WithHandler((string name) => $"Hello, {name}!")
        .WithDescription("Greet a user by name")
        .AsQuery()
        .Done()
    .Build();

// Production entry point - args come from command line
return await app.RunAsync(args);
```

### Entry Point

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
    // ... fluent configuration
    .Build();
```

### Configuration

```csharp
.AddConfiguration()
```

Enables configuration from:
- `appsettings.json`
- Environment variables  
- Command-line args (already passed to `CreateBuilder`)

### Service Registration

```csharp
.ConfigureServices(services => services
    .AddLogging(builder => builder.AddConsole())
    .AddSingleton<MyService>()
)
```

Uses familiar `IServiceCollection` pattern. The generator analyzes registrations to:
- Understand service lifetimes
- Generate service resolution code
- Inject services into handlers

### Behavior/Middleware Pipeline

```csharp
.AddBehavior(typeof(TelemetryBehavior<,>))
.AddBehavior(typeof(ValidationBehavior<,>))
```

**Order matters.** Behaviors wrap handler execution in registration order.

### Terminal

```csharp
.UseTerminal(terminal)
```

Stores terminal reference on `NuruCoreApp` for output operations.

### Help System

```csharp
.AddHelp()  // defaults
.AddHelp(options => { 
    options.ShowPerCommandHelpRoutes = false;
    options.ShowReplCommandsInCli = false;
})
```

Generator creates help routes and formats help text from route metadata.

### REPL Support

```csharp
.AddRepl()  // defaults
.AddRepl(options => { 
    options.Prompt = "my-app> ";
    options.ContinuationPrompt = ">> ";
    options.WelcomeMessage = "Welcome!";
})
```

### Metadata

```csharp
.WithName("my app")           // Overrides assembly name
.WithDescription("Does Cool Things")
.WithAiPrompt("Use queries before commands.")
```

| Method            | Purpose                                      | Used In                  |
| ----------------- | -------------------------------------------- | ------------------------ |
| `WithName`        | Application name (defaults to assembly name) | `--help`, `--capabilities` |
| `WithDescription` | Application description                      | `--help`, `--capabilities` |
| `WithAiPrompt`    | Guidance for AI agents                       | `--capabilities`           |

### Route Mapping

#### Basic Route

```csharp
.Map("status")
    .WithHandler(() => "healthy")
    .WithDescription("Check application status")
    .AsQuery()
    .Done()
```

#### Route with Parameters

```csharp
.Map("get {key}")
    .WithHandler((string key) => $"value-of-{key}")
    .WithDescription("Get value by key")
    .AsQuery()
    .Done()
```

Route parameters `{name}` bind to handler parameters by name.

#### Route with Multiple Parameters

```csharp
.Map("set {key} {value}")
    .WithHandler((string key, string value) => $"set {key} to {value}")
    .AsIdempotentCommand()
    .Done()
```

#### Route with Service Injection

```csharp
.Map("status")
    .WithHandler((ILogger<StatusHandler> logger) => { 
        logger.LogInformation("Status checked");
        return "healthy"; 
    })
    .AsQuery()
    .Done()
```

The generator detects `ILogger<T>` (and other services) and generates injection code:

```csharp
// Generated
var logger = loggerFactory?.CreateLogger<StatusHandler>() 
    ?? NullLogger<StatusHandler>.Instance;
```

#### Route Aliases

```csharp
.Map("my-command")
    .WithAlias("my-cmd")
    .WithHandler(() => "result")
    .AsCommand()
    .Done()
```

#### Nested Route Groups

```csharp
.WithGroupPrefix("admin")
    .Map("restart")
        .WithHandler(() => "restarting...")
        .AsCommand()
        .Done()
    .WithGroupPrefix("config")
        .Map("get {key}")
            .WithHandler((string key) => $"value-of-{key}")
            .AsQuery()
            .Done()
        .Map("set {key} {value}")
            .WithHandler((string key, string value) => $"set {key}={value}")
            .AsIdempotentCommand()
            .Done()
        .Done()  // end config group
    .Done()      // end admin group
```

Results in routes:
- `admin restart`
- `admin config get {key}`
- `admin config set {key} {value}`

### Message Types (Route Classification)

| Method                | Message Type         | AI Safety              |
| --------------------- | -------------------- | ---------------------- |
| `.AsQuery()`            | `query`                | Safe to run freely     |
| `.AsCommand()`          | `command`              | Confirm before running |
| `.AsIdempotentCommand()` | `idempotent-command`   | Safe to retry          |
| (none specified)      | `unspecified`          | Treated as command     |

Used by `--capabilities` output for AI tool discovery.

---

## Handler Parameter Types

The generator must handle different parameter sources:

| Parameter Type  | Source                    | Example                       |
| --------------- | ------------------------- | ----------------------------- |
| Route parameter | Extracted from args       | `{key}` → `string key`          |
| Service         | Resolved from DI          | `ILogger<T>`, `MyService`       |
| Options         | Parsed from `--flag` args | `[Option] bool verbose`       |

### Parameter Binding Rules

1. **Route parameters first** — Match `{name}` to handler param by name
2. **Services next** — Anything registered in `ConfigureServices`
3. **Special types** — `ILogger<T>` recognized automatically
4. **Fallback** — Unknown types cause analyzer error

---

## Generated Code Structure

### RunAsync Interceptor

The generator produces an interceptor that handles all routing at the call site:

```csharp
[InterceptsLocation("Program.cs", line: 15, column: 16)]
internal static async Task<int> RunAsync_Generated(this NuruCoreApp app, string[] args)
{
    // Built-in flags
    if (args is ["--help"]) { /* generated help output */ return 0; }
    if (args is ["--version"]) { /* version output */ return 0; }
    if (args is ["--capabilities"]) { /* JSON capabilities */ return 0; }
    
    // Route matching - args come from runtime (command line)
    if (args is ["status"])
    {
        var logger = app.LoggerFactory?.CreateLogger<StatusHandler>() 
            ?? NullLogger<StatusHandler>.Instance;
        var result = /* handler lambda */(logger);
        app.Terminal.WriteLine(result);
        return 0;
    }
    
    if (args is ["admin", "restart"])
    {
        var result = "restarting...";
        app.Terminal.WriteLine(result);
        return 0;
    }
    
    if (args is ["admin", "config", "get", var key])
    {
        var result = $"value-of-{key}";
        app.Terminal.WriteLine(result);
        return 0;
    }
    
    // No match
    app.Terminal.WriteLine("Unknown command. Use --help for usage.");
    return 1;
}
```

### Capabilities Response Generation

Generator also emits a static capabilities response:

```csharp
internal static readonly CapabilitiesResponse Capabilities = new()
{
    Name = "my app",
    Description = "Does Cool Things",
    AiPrompt = "Use queries before commands.",
    Version = "1.0.0+abc123",
    Commands = [
        new CommandCapability
        {
            Pattern = "status",
            Description = "Check application status",
            MessageType = "query",
            Parameters = [],
            Options = []
        },
        // ... more commands
    ]
};
```

---

## Builder Classes (Library Side)

The builder classes in the NuGet are lightweight — they exist for the DSL syntax but do minimal runtime work.

### NuruAppBuilder

```csharp
public class NuruAppBuilder
{
    internal ITerminal? Terminal { get; private set; }
    internal ILoggerFactory? LoggerFactory { get; private set; }
    
    public NuruAppBuilder AddConfiguration() => this;
    public NuruAppBuilder ConfigureServices(Action<IServiceCollection> configure) => this;
    public NuruAppBuilder AddBehavior(Type behaviorType) => this;
    public NuruAppBuilder UseTerminal(ITerminal terminal) { Terminal = terminal; return this; }
    public NuruAppBuilder AddHelp(Action<HelpOptions>? configure = null) => this;
    public NuruAppBuilder AddRepl(Action<ReplOptions>? configure = null) => this;
    public NuruAppBuilder WithName(string name) => this;
    public NuruAppBuilder WithDescription(string description) => this;
    public NuruAppBuilder WithAiPrompt(string prompt) => this;
    public RouteBuilder Map(string pattern) => new RouteBuilder(this, pattern);
    public GroupBuilder WithGroupPrefix(string prefix) => new GroupBuilder(this, prefix);
    
    public NuruCoreApp Build() => new NuruCoreApp 
    { 
        Terminal = Terminal,
        LoggerFactory = LoggerFactory
    };
}
```

### RouteBuilder

```csharp
public class RouteBuilder
{
    public RouteBuilder WithHandler(Delegate handler) => this;
    public RouteBuilder WithDescription(string description) => this;
    public RouteBuilder WithAlias(string alias) => this;
    public RouteBuilder AsQuery() => this;
    public RouteBuilder AsCommand() => this;
    public RouteBuilder AsIdempotentCommand() => this;
    public NuruAppBuilder Done() => _parent;
}
```

### NuruCoreApp

```csharp
public class NuruCoreApp
{
    public ITerminal? Terminal { get; init; }
    public ILoggerFactory? LoggerFactory { get; init; }
    
    public Task<int> RunAsync(string[] args)
    {
        // This should be intercepted by generator
        // If not intercepted, throw or fallback
        throw new InvalidOperationException(
            "RunAsync was not intercepted. Ensure the Nuru source generator is enabled.");
    }
}
```

---

## Generator Implementation Phases

### Phase 1: Minimal End-to-End

**Goal:** Single route working with interceptor

1. Detect `NuruApp.CreateBuilder()` chain
2. Find single `.Map("status").WithHandler(() => "healthy")` route  
3. Find `app.RunAsync(args)` call site
4. Emit interceptor with hardcoded match

**Test:** `dsl-example.cs` passes when run with `status` argument

### Phase 2: Multiple Routes

**Goal:** Route table generation

1. Parse all `.Map()` calls in builder chain
2. Handle nested `WithGroupPrefix()` groups
3. Generate pattern matching for all routes
4. Handle route parameters `{name}`

### Phase 3: Parameter Binding

**Goal:** Handler parameter injection

1. Analyze handler lambda parameters
2. Distinguish route params vs services
3. Generate service resolution code
4. Generate parameter extraction from args

### Phase 4: Service Integration

**Goal:** Full DI support

1. Parse `ConfigureServices` lambda
2. Track service registrations
3. Generate service provider setup
4. Generate scoped resolution for requests

### Phase 5: Built-in Features

**Goal:** Help, version, capabilities

1. Generate `--help` output from route metadata
2. Generate `--version` from assembly info
3. Generate `--capabilities` JSON response
4. Add `WithAiPrompt` to capabilities

### Phase 6: Behaviors/Middleware

**Goal:** Pipeline execution

1. Parse `AddBehavior()` calls with order
2. Generate pipeline wrapping around handlers
3. Handle async behaviors

### Phase 7: REPL Support

**Goal:** Interactive mode

1. Detect `AddRepl()` in builder
2. Generate REPL loop code
3. Handle REPL-specific options

---

## Attribute-Based Routes

In addition to fluent DSL, routes can be defined via attributes:

```csharp
[NuruRoute("users list")]
[AsQuery]
public class ListUsersHandler : IRequestHandler<ListUsersRequest, string>
{
    public Task<string> Handle(ListUsersRequest request) => Task.FromResult("user list");
}
```

The generator should:
1. Scan for `[NuruRoute]` attributes in the assembly
2. Merge with fluent-defined routes
3. Error on conflicts

---

## Error Handling

### Compile-Time Diagnostics

| Code    | Severity | Description                            |
| ------- | -------- | -------------------------------------- |
| NURU001 | Error    | Handler parameter not bound            |
| NURU002 | Error    | Duplicate route pattern                |
| NURU003 | Warning  | Route has no description               |
| NURU004 | Error    | Service not registered                 |
| NURU005 | Error    | RunAsync not found in builder chain    |
| NURU006 | Warning  | Message type not specified (AsQuery, etc.) |

### Runtime Errors

- Unknown command → Exit code 1, help suggestion
- Handler exception → Exit code 1, error message to terminal

---

## Testing Strategy

### Unit Tests

1. Builder API tests — verify fluent methods work
2. Generator output tests — verify correct code is emitted

### Integration Tests

Integration tests use hardcoded args to verify specific routes:

```csharp
// Test file: tests/timewarp-nuru-core-tests/routing/dsl-example.cs
using TestTerminal terminal = new();

NuruCoreApp app = NuruApp.CreateBuilder(args)
    // ... DSL configuration
    .Build();

// Test with hardcoded args (not production pattern)
int exitCode = await app.RunAsync(["status"]);

// Validate
exitCode.ShouldBe(0);
terminal.OutputContains("healthy").ShouldBeTrue();
```

**Key distinction:**
- **Production:** `return await app.RunAsync(args);` — real command-line args
- **Tests:** `await app.RunAsync(["status"]);` — hardcoded args for verification

### Test Cases

1. `dsl-example.cs` — comprehensive DSL example that runs
2. Single route tests — minimal cases
3. Parameter binding tests
4. Service injection tests

### AOT Validation

1. Publish with `PublishAot=true`
2. Verify no trimmer warnings
3. Verify no runtime reflection

---

## Open Questions

### Resolved

| Question                                   | Decision                               |
| ------------------------------------------ | -------------------------------------- |
| Intercept `Build()` or `RunAsync()`?         | `RunAsync()` — all context available     |
| `NuruAppOptions` or fluent metadata?         | Fluent (`.WithName()`, etc.)             |
| Separate `UseLogging()` method?              | No — use `ConfigureServices.AddLogging` |
| Behavior syntax                            | `typeof(T<,>)` — valid C#              |

### Open

| Question                               | Options                       |
| -------------------------------------- | ----------------------------- |
| Optional route parameters              | `{key?}` syntax? Default values? |
| Async handlers                         | `Task<T>` return detection    |
| Complex return types                   | JSON serialization?           |
| Handler method groups vs lambdas       | Both supported?               |

---

## References

- `tests/timewarp-nuru-core-tests/routing/dsl-example.cs` — DSL reference implementation
- `.agent/workspace/2024-12-25T01-00-00_v2-generator-architecture.md` — Architecture overview
- `sandbox/experiments/manual-runtime-construction.cs` — Manual invoker example
- `source/timewarp-nuru-core/capabilities/capabilities-response.cs` — Capabilities types
- `samples/attributed-routes/` — Attribute-based route examples
