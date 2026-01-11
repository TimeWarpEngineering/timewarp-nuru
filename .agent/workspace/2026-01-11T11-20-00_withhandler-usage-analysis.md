# WithHandler() Usage Analysis Report

**Date:** 2026-01-11  
**Scope:** All `.WithHandler(` usages in the TimeWarp.Nuru repository  
**Total Occurrences:** ~1,247 across 162 files

---

## Executive Summary

Lambda expressions dominate handler definitions (91.5%), with roughly equal split between parameterless and parameterized forms. Method references are used sparingly (4.5%), primarily in configuration-heavy samples. Async handlers represent only 1.8% of usages.

---

## Handler Type Classification

### 1. Parameterless Lambdas: `() => ...` (40.8%)

#### 1a. Returning String Literals
**Count:** ~226 occurrences

```csharp
// samples/01-hello-world/01-hello-world-lambda.cs:14
.Map("")
  .WithHandler(() => "Hello World")
  .AsQuery()
  .Done()
```

#### 1b. Returning Integer Literals (Terminal Output, NOT Exit Codes)
**Count:** ~2 intentional occurrences (after cleanup)

**IMPORTANT:** Integer return values are written to the terminal as output, they do NOT set the exit code.
Exit codes are always 0 on success and 1 on exception. The previous ~220 occurrences of `() => 0` were
replaced with `() => { }` to avoid this confusion.

```csharp
// tests/timewarp-nuru-analyzers-tests/auto/nuru-invoker-generator-01-basic.cs:187
// This outputs "42" to terminal (tests Func<int> generator support)
.Map("").WithHandler(() => 42).AsQuery().Done()
```

#### 1c. Block Body (Side Effects)
**Count:** ~13 occurrences

```csharp
// tests/timewarp-nuru-core-tests/routing/routing-07-route-selection.cs:23
.Map("git status").WithHandler(() => { literalSelected = true; return 0; }).AsQuery().Done()

// Void with no-op
.Map("noop").WithHandler(() => { }).AsCommand().Done()
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
**Count:** ~99 occurrences

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

---

### 3. Async Lambdas (1.8%)

#### 3a. Async Parameterless
**Count:** ~4 occurrences

```csharp
// tests/timewarp-nuru-core-tests/routing/routing-22-async-task-int-return.cs:28
.Map("").WithHandler(async () =>
{
  await Task.Delay(100);
  return 42;
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

```csharp
// tests/test-apps/timewarp-nuru-testapp-delegates/program.cs:127
.Map("backup {source} {destination?}")
  .WithHandler(async (string source, string? destination) =>
  {
    string dest = destination ?? $"{source}.backup";
    Console.WriteLine($"Starting backup: {source} -> {dest}");
    await Task.Delay(500);
    Console.WriteLine($"Backup complete: {dest}");
  })
  .AsCommand()
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

```csharp
// samples/09-configuration/01-configuration-basics.cs
.Map("show")
  .WithHandler(Handlers.ShowConfig)
  .AsQuery()
  .Done()
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
// tests/timewarp-nuru-core-tests/routing/routing-23-multiple-map-same-handler.cs:40
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
// samples/_logging/console-logging.cs (modified version)
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
| Parameterless lambdas (block) | ~13 | 1.0% |
| Lambdas with params (expression) | ~522 | 41.9% |
| Lambdas with params (block) | ~99 | 7.9% |
| Async lambdas (parameterless) | ~4 | 0.3% |
| Async lambdas (with params) | ~19 | 1.5% |
| Method references (Class.Method) | ~29 | 2.3% |
| Method references (simple) | ~11 | 0.9% |
| Method references (variable) | ~16 | 1.3% |
| DI injection patterns | ~21 | 1.7% |
| Other/overlapping | ~17 | 1.4% |
| **TOTAL** | **~1,247** | **100%** |

---

## Key Observations

1. **Lambda expressions dominate** (91.5%) - The fluent API with inline lambdas is the primary usage pattern.

2. **Expression bodies preferred** - Ratio of expression:block bodies is ~10:1 for parameterized lambdas, ~40:1 for parameterless.

3. **Async is rare** (1.8%) - Most CLI operations are synchronous. Async is used for network calls, delays, and I/O operations.

4. **Method references for complex handlers** (4.5%) - Used in samples demonstrating separation of concerns and testability.

5. **DI injection for testability** (1.7%) - `ITerminal`, `IConfiguration`, `IOptions<T>`, and `ILogger<T>` are the common injection points.

6. **Common parameter types:**
   - Primitives: `string`, `int`, `double`, `bool`
   - Nullable: `string?`, `int?`
   - Arrays: `string[]` (catch-all)
   - System types: `Uri`, `FileInfo`, `DirectoryInfo`, `IPAddress`, `DateTime`, `Guid`, `TimeSpan`

---

## Recommendations for Documentation

The three hello-world samples now demonstrate the three main patterns:
1. **01-hello-world-lambda.cs** - Lambda pattern (most common)
2. **02-hello-world-method.cs** - Method reference pattern
3. **03-hello-world-attributed.cs** - Attributed route pattern (IQuery/ICommand with nested Handler class)
