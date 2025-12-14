# Code Generation Design Document

## Overview

This document explores the design for generating `IRequest` Command classes and `IRequestHandler<T>` Handler classes from delegate-based route registrations at compile time.

## Goals

1. **Unified execution model** — All routes (delegate and command-based) flow through Command/Handler pattern
2. **AOT compatibility** — No reflection for delegate invocation
3. **Testability** — Generated commands can be unit tested
4. **Backward compatible** — Existing code continues to work

## Source Generator Context

Source generators run at **compile time** and see only the **syntax tree** (the code as written). They do NOT execute code, so they cannot observe runtime behavior.

### What the Generator Can See

```csharp
// Syntax tree nodes - generator sees these
builder.Map("add {x:double} {y:double}", (double x, double y) => x + y);
```

- `InvocationExpressionSyntax` — the `Map(...)` call
- `LiteralExpressionSyntax` — the pattern string `"add {x:double} {y:double}"`
- `LambdaExpressionSyntax` — the delegate `(double x, double y) => x + y`

### What the Generator Cannot See

- Runtime values
- Results of method calls
- State accumulated across multiple statements

---

## Route Registration Methods

### 1. `Map(pattern, delegate)` — Simple Case

```csharp
builder.Map("add {x:double} {y:double}", (double x, double y) => {
    Console.WriteLine($"{x + y}");
});
```

**Generator Complexity: LOW**

- Pattern is a string literal — directly extractable
- Delegate signature available via semantic model
- Existing `NuruInvokerGenerator` already handles this case

**Generated Output:**

```csharp
[GeneratedCode("TimeWarp.Nuru", "1.0")]
internal sealed class Add_Generated_Command : IRequest
{
    public double X { get; set; }
    public double Y { get; set; }
}

[GeneratedCode("TimeWarp.Nuru", "1.0")]  
internal sealed class Add_Generated_Handler : IRequestHandler<Add_Generated_Command>
{
    public ValueTask<Unit> Handle(Add_Generated_Command request, CancellationToken ct)
    {
        Console.WriteLine($"{request.X + request.Y}");
        return default;
    }
}
```

---

### 2. `MapMultiple(patterns[], delegate)` — Array of Patterns

```csharp
builder.MapMultiple(["exit", "quit", "q"], () => Environment.Exit(0), "Exit the application");
```

**Generator Complexity: MEDIUM**

- Must recognize `MapMultiple` method
- Must parse array initializer syntax `["exit", "quit", "q"]`
- All patterns share ONE handler — generate one Command/Handler pair
- Register the command for all pattern aliases

**Syntax Parsing:**

```csharp
// CollectionExpressionSyntax (C# 12 collection literals)
["exit", "quit", "q"]

// Or ImplicitArrayCreationExpressionSyntax  
new[] { "exit", "quit", "q" }

// Or ArrayCreationExpressionSyntax
new string[] { "exit", "quit", "q" }
```

**Generated Output:**

```csharp
// ONE command (named from first/primary pattern)
[GeneratedCode("TimeWarp.Nuru", "1.0")]
internal sealed class Exit_Generated_Command : IRequest { }

[GeneratedCode("TimeWarp.Nuru", "1.0")]
internal sealed class Exit_Generated_Handler : IRequestHandler<Exit_Generated_Command>
{
    public ValueTask<Unit> Handle(Exit_Generated_Command request, CancellationToken ct)
    {
        Environment.Exit(0);
        return default;
    }
}

// Registration (generated or transformed):
// builder.Map<Exit_Generated_Command>("exit", ...);
// builder.Map<Exit_Generated_Command>("quit", ...);
// builder.Map<Exit_Generated_Command>("q", ...);
```

**Open Questions:**

- Should generator transform the `MapMultiple` call, or emit separate registration code?
- How to handle patterns with different parameters but same handler?

---

### 3. `MapGroup(...).Map(...)` — Grouped Routes

```csharp
var docker = builder.MapGroup("docker")
    .WithGroupOptions("--debug,-D --log-level {level?}");

docker.Map("run {image}", (string image, bool debug, string? logLevel) => ...);
docker.Map("build {path}", (string path, bool debug, string? logLevel) => ...);
```

**Generator Complexity: HIGH**

The generator sees TWO separate statements:

```
Statement 1: var docker = builder.MapGroup("docker").WithGroupOptions("--debug,-D");
Statement 2: docker.Map("run {image}", handler);
```

To understand `docker.Map(...)`, the generator must:

1. Recognize `docker` is a variable
2. Find where `docker` was assigned
3. Extract the group prefix (`"docker"`) and options (`"--debug,-D"`)
4. Combine: `"docker run {image} --debug,-D? --log-level {level?}"`

**This is data flow analysis** — tracking values across statements.

#### Approach A: Full Data Flow Analysis

Track variable assignments and method chains:

```csharp
// Generator maintains state:
// variableGroups["docker"] = { prefix: "docker", options: "--debug,-D --log-level {level?}" }

// When seeing docker.Map(...):
// 1. Lookup "docker" in variableGroups
// 2. Expand pattern with group context
```

**Pros:** Handles any code structure
**Cons:** Complex, error-prone, may miss edge cases

#### Approach B: Require Inline Fluent Chains

Restrict to patterns the generator can parse as single expressions:

```csharp
// SUPPORTED - single expression
builder.MapGroup("docker")
    .WithGroupOptions("--debug,-D")
    .Map("run {image}", (string image, bool debug) => ...);

// NOT SUPPORTED - separate statements
var docker = builder.MapGroup("docker");
docker.Map("run {image}", handler);  // Generator can't resolve this
```

**Pros:** Simple parsing, no data flow needed
**Cons:** Restricts how users write code

#### Approach C: Skip Codegen for Groups

Groups use existing `DelegateExecutor` path. Only simple `Map()` calls get codegen.

**Pros:** Ship faster, simpler generator
**Cons:** Inconsistent — some routes are commands, some aren't

#### Approach D: Attribute-Based Alternative

Offer an attribute-based model for complex cases:

```csharp
[CommandGroup("docker", Options = "--debug,-D")]
public static class DockerCommands
{
    [Command("run {image}")]
    public static void Run(string image, bool debug) { }
}
```

**Pros:** Explicit, no data flow analysis needed
**Cons:** Different mental model from fluent API

---

## Naming Strategy

Generated classes need unique, readable names derived from patterns.

### Proposed Convention

| Pattern | Generated Command Name |
|---------|----------------------|
| `add {x} {y}` | `Add_Generated_Command` |
| `user create {name}` | `UserCreate_Generated_Command` |
| `docker compose up` | `DockerComposeUp_Generated_Command` |
| `git log --oneline` | `GitLogOneline_Generated_Command` |

**Rules:**

1. Extract literal segments from pattern
2. PascalCase each segment
3. Concatenate with no separator
4. Append `_Generated_Command` suffix
5. For collisions, append numeric suffix (`_2`, `_3`, etc.)

### Collision Detection

If two patterns produce the same name:

```csharp
builder.Map("user-create", handler1);  // UserCreate_Generated_Command
builder.Map("user create", handler2);  // UserCreate_Generated_Command_2
```

---

## Delegate Body Handling

The generator must emit the delegate's body inside the handler.

### Simple Expressions

```csharp
// Input
(double x, double y) => Console.WriteLine($"{x + y}")

// Output in handler
public ValueTask<Unit> Handle(Command request, CancellationToken ct)
{
    Console.WriteLine($"{request.X + request.Y}");
    return default;
}
```

**Transformation:** Replace parameter references (`x`, `y`) with property access (`request.X`, `request.Y`).

### Block Bodies

```csharp
// Input
(string name) => {
    if (string.IsNullOrEmpty(name))
        throw new ArgumentException("Name required");
    Console.WriteLine($"Hello, {name}!");
}

// Output - embed entire block
public ValueTask<Unit> Handle(Command request, CancellationToken ct)
{
    if (string.IsNullOrEmpty(request.Name))
        throw new ArgumentException("Name required");
    Console.WriteLine($"Hello, {request.Name}!");
    return default;
}
```

### Closures / Captured Variables

**Problem:**

```csharp
string prefix = "Hello";
builder.Map("greet {name}", (string name) => Console.WriteLine($"{prefix}, {name}"));
```

The delegate captures `prefix`. The generator cannot:
- Know the value of `prefix` at compile time
- Emit code that references a local variable from different scope

**Options:**

1. **Reject closures** — Emit diagnostic error if delegate captures variables
2. **Generate delegate wrapper** — Handler holds reference to original delegate, calls it
3. **Require explicit capture** — Force users to pass captured values as parameters

**Recommended: Option 2 for closures**

```csharp
// If closure detected, generate:
internal sealed class Greet_Generated_Handler : IRequestHandler<Greet_Generated_Command>
{
    private readonly Action<string> _originalDelegate;
    
    public Greet_Generated_Handler(Action<string> originalDelegate)
    {
        _originalDelegate = originalDelegate;
    }
    
    public ValueTask<Unit> Handle(Greet_Generated_Command request, CancellationToken ct)
    {
        _originalDelegate(request.Name);
        return default;
    }
}
```

---

## Async Delegates

### `Func<..., Task>`

```csharp
builder.Map("fetch {url}", async (string url) => {
    var result = await httpClient.GetStringAsync(url);
    Console.WriteLine(result);
});
```

**Generated Handler:**

```csharp
public async ValueTask<Unit> Handle(FetchCommand request, CancellationToken ct)
{
    var result = await httpClient.GetStringAsync(request.Url);
    Console.WriteLine(result);
    return default;
}
```

### `Func<..., Task<T>>`

```csharp
builder.Map("count {path}", async (string path) => {
    return await File.ReadAllLinesAsync(path).Length;
});
```

**Question:** Should this generate `IRequest<int>` instead of `IRequest`?

---

## Return Values

### Exit Codes (`Func<..., int>`)

```csharp
builder.Map("validate {file}", (string file) => {
    return File.Exists(file) ? 0 : 1;
});
```

**Generated:**

```csharp
internal sealed class Validate_Generated_Command : IRequest<int>
{
    public string File { get; set; }
}

internal sealed class Validate_Generated_Handler : IRequestHandler<Validate_Generated_Command, int>
{
    public ValueTask<int> Handle(Validate_Generated_Command request, CancellationToken ct)
    {
        return new ValueTask<int>(File.Exists(request.File) ? 0 : 1);
    }
}
```

---

## DI Registration

The generator must emit code to register all handlers with DI.

### Option A: Extension Method

```csharp
// Generated
public static class GeneratedCommandRegistration
{
    public static IServiceCollection AddNuruGeneratedCommands(this IServiceCollection services)
    {
        services.AddTransient<IRequestHandler<Add_Generated_Command>, Add_Generated_Handler>();
        services.AddTransient<IRequestHandler<Exit_Generated_Command>, Exit_Generated_Handler>();
        // ... all generated handlers
        return services;
    }
}

// User calls:
builder.ConfigureServices(services => services.AddNuruGeneratedCommands());
```

### Option B: Module Initializer (Automatic)

```csharp
// Generated - runs automatically at assembly load
[ModuleInitializer]
internal static void RegisterGeneratedCommands()
{
    // Register with some global registry
}
```

**Note:** Current `NuruInvokerGenerator` uses module initializer pattern.

---

## Phased Implementation

### Phase 1: Simple `Map()` Only

- Handle `builder.Map("pattern", delegate)`
- Generate Command + Handler
- Skip `MapMultiple`, `MapGroup`
- No closure support (emit warning)

### Phase 2: `MapMultiple` Support

- Parse array initializers
- Generate single Command/Handler for aliases
- Handle different array syntax variants

### Phase 3: Closures

- Detect captured variables
- Generate delegate-wrapping handlers

### Phase 4: `MapGroup` (If Needed)

- Evaluate whether data flow analysis is worth it
- Consider alternative approaches (attributes, fluent-only)

---

## Open Questions

1. **Opt-in or automatic?** Should codegen be automatic for all `Map()` calls, or require an attribute/flag?

2. **Coexistence with DelegateExecutor?** During transition, both paths exist. How to choose which runs?

3. **Debugging experience?** Generated code should have good debug symbols, source mapping.

4. **Naming collisions across assemblies?** If two assemblies generate same command name, what happens?

5. **Incremental generation?** Source generators should be incremental for performance. How to cache efficiently?
