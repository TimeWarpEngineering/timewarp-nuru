# Source Generation Design

## Overview

This document covers the **implementation details** for generating Command/Handler classes from delegate-based route registrations. It assumes the API design from `api-design.md` is finalized.

## Goals

1. **Unified execution model** — All routes become Command/Handler pairs
2. **AOT compatibility** — No reflection, no `DynamicInvoke`
3. **Remove `DelegateExecutor`** — Single execution path for all routes

## Non-Goals

- **Backward compatibility** — 3.0 is breaking
- **Runtime delegate execution** — Everything is compile-time generated

---

## Source Generator Fundamentals

### What Generators See

Source generators operate on the **syntax tree** at compile time:

```csharp
builder.Map("add {x:double} {y:double}", (double x, double y) => x + y);
```

The generator sees:
- `InvocationExpressionSyntax` — the method call
- `LiteralExpressionSyntax` — string `"add {x:double} {y:double}"`
- `LambdaExpressionSyntax` — the delegate body

With semantic analysis, it can also resolve:
- Method symbols and containing types
- Parameter types
- Return types

### What Generators Cannot See

- Runtime values
- Values computed by other code
- State accumulated across statements (without data flow analysis)

---

## Existing Infrastructure

`NuruInvokerGenerator` already:

1. Finds `Map()` and `MapDefault()` calls
2. Extracts pattern strings from literals
3. Extracts delegate signatures (parameters, types, return type)
4. Generates typed invoker methods

**We extend this** to also generate Command and Handler classes.

---

## Generation Strategy by API Method

### 1. `Map(pattern, delegate)`

**Complexity: LOW**

Single invocation, all information inline.

**Detection:**
```csharp
// Syntax predicate
node is InvocationExpressionSyntax invocation &&
invocation.Expression is MemberAccessExpressionSyntax member &&
member.Name.Identifier.Text == "Map"
```

**Extraction:**
1. First argument: pattern string literal
2. Second argument: delegate (lambda or method group)

**Generation:**
```csharp
// Input
builder.Map("add {x:double} {y:double}", (double x, double y) => {
    Console.WriteLine($"{x + y}");
});

// Generated Command
[GeneratedCode("TimeWarp.Nuru", "1.0")]
internal sealed class Add_Generated_Command : IRequest
{
    public double X { get; set; }
    public double Y { get; set; }
}

// Generated Handler
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

### 2. Default Routes (Empty Pattern)

**Complexity: LOW**

Default routes use empty string `""` as the pattern. No separate `MapDefault` method — this simplifies source generation by treating defaults as regular `Map()` calls.

```csharp
// Default with no options
builder.Map("", () => Console.WriteLine("Usage: mycli <command>"));

// Default with options
builder.Map("--verbose,-v", (bool verbose) => ShowHelp(verbose));

// Default with command
builder.Map<HelpCommand>("");
```

**Generation:** Same as regular `Map()` — pattern happens to be empty or options-only.

---

### 3. `MapMultiple(patterns[], delegate)`

**Complexity: MEDIUM**

Must parse array syntax.

**Array Syntax Variants:**
```csharp
// C# 12 collection expression
["exit", "quit", "q"]

// Implicit array
new[] { "exit", "quit", "q" }

// Explicit array
new string[] { "exit", "quit", "q" }
```

**Detection:**
```csharp
// First argument is collection/array
argument.Expression is CollectionExpressionSyntax ||
argument.Expression is ImplicitArrayCreationExpressionSyntax ||
argument.Expression is ArrayCreationExpressionSyntax
```

**Extraction:**
1. Parse array initializer to get all pattern strings
2. Use first pattern as "primary" for naming
3. Extract delegate from second argument

**Generation:**
```csharp
// Input
builder.MapMultiple(["exit", "quit", "q"], () => Environment.Exit(0));

// ONE Command (from primary pattern)
internal sealed class Exit_Generated_Command : IRequest { }

// ONE Handler
internal sealed class Exit_Generated_Handler : IRequestHandler<Exit_Generated_Command> { ... }

// Registration for all aliases (emitted in registration code)
```

---

### 4. `MapGroup(...).Map(...)`

**Complexity: HIGH**

Requires understanding group context.

#### Scenario A: Fluent Chain (Parseable)

```csharp
builder.MapGroup("docker")
    .WithGroupOptions("--debug,-D")
    .Map("run {image}", handler);
```

This is a **single expression** — the generator can walk the chain:

```
InvocationExpression: .Map("run {image}", handler)
  └── Expression: MemberAccessExpression (.Map)
       └── Expression: InvocationExpression (.WithGroupOptions("--debug"))
            └── Expression: MemberAccessExpression (.WithGroupOptions)
                 └── Expression: InvocationExpression (.MapGroup("docker"))
```

**Extraction:**
1. Walk up the expression tree
2. Collect: prefix from `MapGroup()`, options from `WithGroupOptions()`
3. Combine into full pattern: `"docker run {image} --debug,-D?"`

#### Scenario B: Variable Assignment (Requires Data Flow)

```csharp
var docker = builder.MapGroup("docker").WithGroupOptions("--debug");
docker.Map("run {image}", handler);
```

**Two separate statements.** Generator must:

1. Find assignment: `var docker = builder.MapGroup(...)`
2. Track that `docker` represents a group with prefix and options
3. When seeing `docker.Map(...)`, lookup the variable's group context

**Implementation:**

```csharp
// In syntax receiver, collect both:
// 1. Group assignments
Dictionary<string, GroupInfo> groupVariables = new();

// 2. Map calls on variables
List<(string Variable, InvocationExpressionSyntax Call)> groupMapCalls = new();

// In Execute, correlate them
```

**Data Flow Scope:**
- Track assignments within same method body
- Don't track across methods or classes (too complex, diminishing returns)

---

## Naming Strategy

### Pattern to Class Name

| Pattern | Command Name |
|---------|-------------|
| `add {x} {y}` | `AddCommand` |
| `user create {name}` | `UserCreateCommand` |
| `docker compose up` | `DockerComposeUpCommand` |
| `--help` | `HelpCommand` |
| (empty/default) | `DefaultCommand` |

**Algorithm:**
1. Extract literal segments (ignore parameters, options)
2. Remove dashes, convert to PascalCase
3. Concatenate
4. Append `Command` suffix
5. For generated handlers, append `Handler` suffix

### Collision Handling

```csharp
builder.Map("user-create", h1);  // UserCreateCommand
builder.Map("user create", h2);  // UserCreateCommand2
```

Track generated names, append numeric suffix on collision.

### Namespace

Generated code goes in:
```csharp
namespace <UserAssembly>.Generated.Commands;
```

---

## Delegate Body Transformation

### Parameter Rewriting

Replace delegate parameters with `request.PropertyName`:

```csharp
// Input lambda body
Console.WriteLine($"{x} + {y} = {x + y}")

// Output in handler
Console.WriteLine($"{request.X} + {request.Y} = {request.X + request.Y}")
```

**Implementation:**
Use `SyntaxRewriter` to find `IdentifierNameSyntax` nodes matching parameter names, replace with `MemberAccessExpressionSyntax`.

### Block vs Expression Bodies

```csharp
// Expression body
(x, y) => x + y
// Wrap in return statement

// Block body
(x, y) => { Console.WriteLine(x); return y; }
// Embed block directly
```

---

## Closure Detection

### Problem

```csharp
string prefix = "Hello";
builder.Map("greet {name}", (string name) => Console.WriteLine($"{prefix}, {name}"));
```

`prefix` is captured — generator cannot inline the body.

### Detection

Use `DataFlowAnalysis` from semantic model:

```csharp
var dataFlow = semanticModel.AnalyzeDataFlow(lambdaBody);
var captured = dataFlow.CapturedInside;  // Variables captured by lambda
```

### Strategy: Delegate Wrapper

If closure detected, generate a handler that holds the original delegate:

```csharp
internal sealed class GreetHandler : IRequestHandler<GreetCommand>
{
    private readonly Action<string> _handler;
    
    public GreetHandler(Action<string> handler) => _handler = handler;
    
    public ValueTask<Unit> Handle(GreetCommand request, CancellationToken ct)
    {
        _handler(request.Name);
        return default;
    }
}
```

**Registration must pass the delegate:**
```csharp
// Generated registration
services.AddTransient<IRequestHandler<GreetCommand>>(sp => 
    new GreetHandler((name) => Console.WriteLine($"{prefix}, {name}")));
```

**Challenge:** The registration code must capture the original lambda expression. This requires emitting code that references the lambda from the original source location.

**Alternative:** Emit a diagnostic warning/error for closures, require users to refactor.

---

## Async Handling

### Detection

```csharp
// Check return type
returnType.Name == "Task" || returnType.Name == "ValueTask" ||
returnType.OriginalDefinition.Name == "Task`1"  // Task<T>
```

### Generation

```csharp
// Input
async (string url) => { await FetchAsync(url); }

// Generated
public async ValueTask<Unit> Handle(FetchCommand request, CancellationToken ct)
{
    await FetchAsync(request.Url);
    return default;
}
```

---

## Return Values

### `void` / `Task` — No Return

```csharp
public ValueTask<Unit> Handle(...) { ...; return default; }
```

### `int` / `Task<int>` — Exit Code

```csharp
// Command
internal sealed class ValidateCommand : IRequest<int> { ... }

// Handler
public ValueTask<int> Handle(ValidateCommand request, CancellationToken ct)
{
    return new ValueTask<int>(File.Exists(request.File) ? 0 : 1);
}
```

### `T` / `Task<T>` — Generic Return

```csharp
internal sealed class ComputeCommand : IRequest<double> { ... }
```

---

## DI Registration

### Module Initializer Approach

```csharp
[GeneratedCode("TimeWarp.Nuru", "1.0")]
internal static class GeneratedCommandRegistration
{
    [ModuleInitializer]
    internal static void Register()
    {
        NuruCommandRegistry.Register<AddCommand, AddHandler>();
        NuruCommandRegistry.Register<ExitCommand, ExitHandler>();
        // ... all generated
    }
}
```

### Extension Method Approach

```csharp
public static IServiceCollection AddGeneratedCommands(this IServiceCollection services)
{
    services.AddTransient<IRequestHandler<AddCommand>, AddHandler>();
    // ...
    return services;
}
```

**Preference:** Module initializer for automatic registration, similar to existing `NuruInvokerGenerator`.

---

## Generated File Structure

```
// GeneratedCommands.g.cs

// <auto-generated/>
#nullable enable

namespace MyApp.Generated.Commands;

// Commands
internal sealed class AddCommand : IRequest { ... }
internal sealed class ExitCommand : IRequest { ... }

// Handlers  
internal sealed class AddHandler : IRequestHandler<AddCommand> { ... }
internal sealed class ExitHandler : IRequestHandler<ExitCommand> { ... }

// Registration
internal static class GeneratedCommandRegistration { ... }
```

---

## Incremental Generation

For performance, use incremental generator pattern:

```csharp
public void Initialize(IncrementalGeneratorInitializationContext context)
{
    // 1. Find candidates (syntax only, fast)
    var mapCalls = context.SyntaxProvider
        .CreateSyntaxProvider(
            predicate: IsMapInvocation,      // Fast syntax check
            transform: ExtractMapInfo)        // Semantic analysis
        .Where(info => info is not null);
    
    // 2. Collect and generate
    context.RegisterSourceOutput(mapCalls.Collect(), GenerateCommands);
}
```

**Caching:** Generator output is cached based on input syntax. Only regenerates when `Map()` calls change.

---

## Diagnostics

### Errors

| Code | Message |
|------|---------|
| NURU001 | Pattern must be a string literal |
| NURU002 | MapMultiple requires array literal |
| NURU003 | Cannot resolve group context for variable '{0}' |
| NURU004 | Closure detected — consider refactoring to avoid captured variables |

### Warnings

| Code | Message |
|------|---------|
| NURU101 | Pattern '{0}' generates same command name as '{1}' |

---

## Implementation Phases

### Phase 1: Simple Map

- `Map(pattern, delegate)` with inline lambdas
- No closures
- Sync and async
- Command + Handler generation
- Module initializer registration

### Phase 2: MapMultiple & MapDefault

- Array literal parsing
- Empty pattern handling

### Phase 3: MapGroup

- Fluent chain parsing
- Variable tracking (same method scope)
- Pattern combination

### Phase 4: Edge Cases

- Closure detection and handling
- Method group delegates
- DI parameter detection

### Phase 5: Cleanup

- Remove `DelegateExecutor`
- Remove old invoker-only generation
- Update all tests and samples
