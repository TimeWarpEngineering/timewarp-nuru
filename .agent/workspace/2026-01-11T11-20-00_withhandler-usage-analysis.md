# WithHandler() Usage Analysis Report

**Date:** 2026-01-11 (Updated)  
**Scope:** All `.WithHandler(` usages in the TimeWarp.Nuru repository  
**Total Occurrences:** ~1,247 across 162 files

---

## Executive Summary

Lambda expressions dominate handler definitions (91.5%), with roughly equal split between parameterless and parameterized forms. Method references are used sparingly (4.5%), primarily in configuration-heavy samples. Async handlers represent only 1.8% of usages.

### Important Clarification: Return Values vs Exit Codes

**Handler return values are OUTPUT, not exit codes.**

- When a handler returns a value (e.g., `() => 42` or `() => "Hello"`), that value is **printed to the terminal**
- `RunAsync()` always returns **0 on success** and **1 on exception**
- To signal failure, throw an exception from your handler

This was a major source of confusion - ~220 occurrences of `() => 0` in tests were misleading because they appeared to set exit codes but actually output "0" to the terminal.

---

## Cleanup Summary (2026-01-11)

| Commit | Change | Files |
|--------|--------|-------|
| `f797233e` | Replace `() => 0` with `() => { }` in completion/repl/routing tests | 32 |
| `499d5b21` | Rewrite route selection tests to use TestTerminal output verification | 1 |
| `063dbbb8` | Remove `return 0` from closure handlers in routing binding tests | 16 |
| `911d9d76` | Remove `return 0` from configuration/options tests | 5 |

**Total:** ~220+ handler patterns fixed across 54 files.

---

## Handler Type Classification

### 1. Parameterless Lambdas: `() => ...` (40.8%)

#### 1a. Returning String Literals (Output)
**Count:** ~226 occurrences

```csharp
// samples/01-hello-world/01-hello-world-lambda.cs:14
.Map("")
  .WithHandler(() => "Hello World")  // Outputs "Hello World" to terminal
  .AsQuery()
  .Done()
```

#### 1b. Returning Integer Literals (Output, NOT Exit Codes)
**Count:** ~2 intentional occurrences (after cleanup)

**IMPORTANT:** Integer return values are written to the terminal as output, they do NOT set the exit code. The previous ~220 occurrences of `() => 0` were replaced with `() => { }` to avoid this confusion.

```csharp
// tests/timewarp-nuru-analyzers-tests/auto/nuru-invoker-generator-01-basic.cs:187
// This outputs "42" to terminal (tests Func<int> generator support)
.Map("").WithHandler(() => 42).AsQuery().Done()

// tests/timewarp-nuru-core-tests/routing/routing-22-async-task-int-return.cs
// Tests async Task<int> handler support
.Map("").WithHandler(async () => {
  await Task.Delay(1);
  return 42; // Outputs "42" to terminal
}).AsCommand().Done()
```

#### 1c. Void Block Body (Side Effects or No-Op)
**Count:** ~230+ occurrences (after cleanup)

```csharp
// No-op handler (tests don't care about output)
.Map("status").WithHandler(() => { }).AsQuery().Done()

// Closure capture for test assertions
.Map("greet {name}").WithHandler((string name) => { boundName = name; }).AsQuery().Done()
```

---

### 2. Lambdas with Parameters: `(params) => ...` (49.8%)

#### 2a. Single Parameter - Expression Body
**Count:** Most common pattern

```csharp
// samples/04-syntax-examples/syntax-examples.cs
.Map("greet {name}")
  .WithHandler((string name) => Console.WriteLine($"Hello {name}"))
  .AsCommand().Done()
```

#### 2b. Multiple Parameters - Expression Body

```csharp
// samples/02-calculator/01-calc-delegate.cs:13
.Map("add {x:double} {y:double}")
  .WithHandler((double x, double y) => WriteLine($"{x} + {y} = {x + y}"))
  .AsQuery()
  .Done()
```

#### 2c. Array Parameter (Catch-all/Rest)

```csharp
// tests/test-apps/timewarp-nuru-testapp-delegates/program.cs:30
.Map("docker run {*args}")
  .WithHandler((string[] args) => WriteLine($"docker run {string.Join(" ", args)}"))
  .AsCommand().Done()
```

#### 2d. Optional Parameters

```csharp
// tests/test-apps/timewarp-nuru-testapp-delegates/program.cs:112
.Map("deploy {env} {tag?}")
  .WithHandler((string env, string? tag) =>
    Console.WriteLine($"Deploying to {env}" + (tag != null ? $" with tag {tag}" : "")))
  .AsCommand().Done()
```

#### 2e. Block Body with Logic

```csharp
// samples/02-calculator/01-calc-delegate.cs:28
.Map("divide {x:double} {y:double}")
  .WithHandler((double x, double y) =>
  {
    if (y == 0)
    {
      WriteLine("Error: Division by zero");
      return;
    }
    WriteLine($"{x} / {y} = {x / y}");
  })
  .AsQuery()
  .Done()
```

#### 2f. Block Body for Test Assertions (Closure Capture)

```csharp
// tests/timewarp-nuru-core-tests/routing/routing-02-parameter-binding.cs
string? boundName = null;
NuruCoreApp app = new NuruAppBuilder()
  .Map("greet {name}").WithHandler((string name) => { boundName = name; }).AsQuery().Done()
  .Build();

await app.RunAsync(["greet", "Alice"]);
boundName.ShouldBe("Alice");
```

---

### 3. Async Lambdas (1.8%)

#### 3a. Async Parameterless
**Count:** ~4 occurrences

```csharp
// tests/timewarp-nuru-core-tests/routing/routing-22-async-task-int-return.cs
.Map("").WithHandler(async () =>
{
  await Task.Delay(1);
  return 42; // Outputs "42" to terminal (tests Task<int> support)
})
```

#### 3b. Async with Parameters
**Count:** ~19 occurrences

```csharp
// samples/06-async-examples/async-examples.cs:42
.Map("fetch {url}")
  .WithHandler(async (string url) =>
  {
    Console.WriteLine($"Fetching data from {url}...");
    await Task.Delay(500);
    Console.WriteLine($"Data fetched from {url}");
  })
  .AsQuery()
  .Done()
```

---

### 4. Method References (4.5%)

#### 4a. Static Class.Method Pattern
**Count:** ~29 occurrences

```csharp
// samples/01-hello-world/02-hello-world-method.cs:15
.Map("")
  .WithHandler(Handlers.Greet)
  .AsQuery()
  .Done()

// Handler definition:
internal static class Handlers
{
  internal static void Greet(ITerminal terminal)
    => terminal.WriteLine("Hello World");
}
```

#### 4b. Simple Method Name (Local/Instance)
**Count:** ~11 occurrences

```csharp
// tests/timewarp-nuru-core-tests/generator/generator-12-method-reference-handlers.cs:36
.Map("greet {name}")
  .WithHandler(Greet)
  .AsCommand()
  .Done()

// Local method in same scope:
internal static void Greet(string name, ITerminal terminal)
  => terminal.WriteLine($"Hello, {name}!");
```

#### 4c. Variable Reference (Reusable Handlers)
**Count:** ~16 occurrences

```csharp
// tests/timewarp-nuru-core-tests/routing/routing-23-multiple-map-same-handler.cs
Action handler = () => Console.WriteLine("Handled!");
builder.Map("close").WithHandler(handler).Done()
builder.Map("shutdown").WithHandler(handler).Done()
builder.Map("bye").WithHandler(handler).Done()
```

---

### 5. DI Injection Patterns (1.7%)

#### 5a. ITerminal Injection (Testable Output)

```csharp
// samples/08-testing/03-terminal-injection.cs:47
.Map("status")
  .WithHandler((ITerminal t) =>
  {
    t.WriteLine("Service A: OK".Green());
    t.WriteLine("Service B: WARNING".Yellow());
  })
  .AsQuery()
  .Done()
```

#### 5b. IConfiguration Injection

```csharp
// samples/09-configuration/user-secrets-demo.cs:19
.Map("show")
  .WithHandler((IConfiguration config) =>
  {
    string? apiKey = config["ApiKey"];
    Console.WriteLine($"ApiKey: {apiKey ?? "(not set)"}");
  })
  .AsQuery()
  .Done()
```

#### 5c. IOptions<T> Injection

```csharp
// tests/timewarp-nuru-core-tests/generator/generator-13-ioptions-parameter-injection.cs
.Map("show-db")
  .WithHandler((IOptions<DatabaseOptions> opts) => $"Host: {opts.Value.Host}")
  .AsQuery()
  .Done()
```

#### 5d. Mixed DI + Route Parameters

```csharp
// Combined pattern
.Map("deploy {env}")
  .WithHandler((string env, ITerminal t) =>
  {
    t.WriteLine($"Deploying to {env}...".Cyan());
  })
  .AsCommand()
  .Done()
```

#### 5e. ILogger<T> Injection

```csharp
// samples/_logging/console-logging.cs
.Map("greet {name}")
  .WithHandler((string name, ILogger<Program> logger) =>
  {
    logger.LogInformation("Greeting user: {Name}", name);
    Console.WriteLine($"Hello, {name}!");
  })
  .AsCommand()
  .Done()
```

---

## Distribution Summary

| Handler Type | Count | % |
|-------------|-------|---|
| Parameterless lambdas (expression) | ~496 | 39.8% |
| Parameterless lambdas (block/void) | ~230+ | 18.4% |
| Lambdas with params (expression) | ~522 | 41.9% |
| Lambdas with params (block) | ~99 | 7.9% |
| Async lambdas (parameterless) | ~4 | 0.3% |
| Async lambdas (with params) | ~19 | 1.5% |
| Method references (Class.Method) | ~29 | 2.3% |
| Method references (simple) | ~11 | 0.9% |
| Method references (variable) | ~16 | 1.3% |
| DI injection patterns | ~21 | 1.7% |

---

## Key Observations

1. **Lambda expressions dominate** (91.5%) - The fluent API with inline lambdas is the primary usage pattern.

2. **Expression bodies preferred** - Ratio of expression:block bodies is ~10:1 for parameterized lambdas.

3. **Void handlers for tests** - After cleanup, test handlers that don't need output use `() => { }` or `(x) => { boundValue = x; }`.

4. **Async is rare** (1.8%) - Most CLI operations are synchronous. Async is used for network calls, delays, and I/O operations.

5. **Method references for complex handlers** (4.5%) - Used in samples demonstrating separation of concerns and testability.

6. **DI injection for testability** (1.7%) - `ITerminal`, `IConfiguration`, `IOptions<T>`, and `ILogger<T>` are the common injection points.

7. **Common parameter types:**
   - Primitives: `string`, `int`, `double`, `bool`
   - Nullable: `string?`, `int?`
   - Arrays: `string[]` (catch-all)
   - System types: `Uri`, `FileInfo`, `DirectoryInfo`, `IPAddress`, `DateTime`, `Guid`, `TimeSpan`

---

## Best Practices

### DO:
- Use `() => "result"` when you want to output a value to the terminal
- Use `() => { }` when you don't care about output (especially in tests)
- Use `(x) => { boundValue = x; }` in tests to capture values for assertions
- Use `TestTerminal` and `terminal.OutputContains()` to verify handler output in tests
- Throw exceptions to signal errors (exit code will be 1)

### DON'T:
- Use `() => 0` thinking it sets the exit code (it outputs "0" to terminal)
- Use `return 0;` in closure handlers unless testing `Func<int>` or `Task<int>` support
- Confuse handler return values with process exit codes

---

## Test Patterns

### Route Selection Tests (verify which route matched)
```csharp
using TestTerminal terminal = new();
NuruCoreApp app = new NuruAppBuilder()
  .UseTerminal(terminal)
  .Map("git status").WithHandler(() => "literal:git-status").AsQuery().Done()
  .Map("git {command}").WithHandler((string command) => $"param:{command}").AsQuery().Done()
  .Build();

await app.RunAsync(["git", "status"]);
terminal.OutputContains("literal:git-status").ShouldBeTrue();
terminal.OutputContains("param:").ShouldBeFalse();
```

### Parameter Binding Tests (verify values are parsed correctly)
```csharp
string? boundName = null;
int boundPort = 0;
NuruCoreApp app = new NuruAppBuilder()
  .Map("connect {host} {port:int}")
  .WithHandler((string host, int port) => { boundHost = host; boundPort = port; })
  .AsCommand().Done()
  .Build();

await app.RunAsync(["connect", "localhost", "8080"]);
boundHost.ShouldBe("localhost");
boundPort.ShouldBe(8080);
```

---

## Recommendations for Documentation

The three hello-world samples demonstrate the three main patterns:
1. **01-hello-world-lambda.cs** - Lambda pattern (most common)
2. **02-hello-world-method.cs** - Method reference pattern
3. **03-hello-world-attributed.cs** - Attributed route pattern (IQuery/ICommand with nested Handler class)
