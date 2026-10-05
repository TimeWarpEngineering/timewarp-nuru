# Roslyn Analyzer

TimeWarp.Nuru includes a built-in Roslyn analyzer that provides **compile-time validation** of your route patterns, catching errors before you run your application.

## Why It Matters

The analyzer prevents common mistakes that would otherwise fail at runtime:

```csharp
// ❌ This compiles without analyzer, fails at runtime
builder.Map("deploy <env>").WithHandler(handler).AsCommand().Done();  // Wrong syntax

// ❌ This compiles without analyzer, creates ambiguous routing
builder.Map("run {file?} {*args}").WithHandler(handler).AsCommand().Done();  // Invalid combination

// ✅ With analyzer: Both caught at compile-time with clear error messages
```

## Installation

**No separate installation needed!** The analyzer is automatically included with TimeWarp.Nuru (version 2.1.0-beta.9+).

```xml
<PackageReference Include="TimeWarp.Nuru" Version="2.1.0-beta.9" />
```

The analyzer:
- Runs during compilation only
- Has zero runtime performance impact
- Doesn't affect application size
- Works in all IDEs (Visual Studio, VS Code, Rider, JetBrains IDEs)

## Error Categories

### Parse Errors (NURU_P###)
Syntax issues in route patterns - malformed brackets, invalid characters, unsupported types.

### Semantic Errors (NURU_S###)
Logical issues that create ambiguity or conflicts - duplicate parameters, invalid parameter ordering, incompatible combinations.

### Dependency Errors (NURU_D###)
Missing package dependencies required for specific features.

## Common Errors and Fixes

### NURU_P001: Invalid Parameter Syntax

**Problem**: Using wrong bracket syntax for parameters

```csharp
// ❌ Error: Invalid syntax
builder.Map("deploy <env>").WithHandler(handler).AsCommand().Done();  // Should use curly braces

// ✅ Correct
builder.Map("deploy {env}").WithHandler(handler).AsCommand().Done();
```

### NURU_P002: Unbalanced Braces

**Problem**: Missing opening or closing brace

```csharp
// ❌ Error: Missing closing brace
builder.Map("deploy {env").WithHandler(handler).AsCommand().Done();

// ❌ Error: Missing opening brace
builder.Map("deploy env}").WithHandler(handler).AsCommand().Done();

// ✅ Correct
builder.Map("deploy {env}").WithHandler(handler).AsCommand().Done();
```

### NURU_P003: Invalid Option Format

**Problem**: Malformed option syntax

```csharp
// ✅ Correct: Double-dash for long options
builder.Map("build --verbose").WithHandler(handler).AsCommand().Done();

// ✅ Correct: Single dash, single character
builder.Map("build -v").WithHandler(handler).AsCommand().Done();

// ✅ Correct: Single dash, multi-character (dotnet/msbuild style)
builder.Map("build -bl").WithHandler(handler).AsCommand().Done();

// ✅ Best: Provide both long and short forms
builder.Map("build --verbose,-v").WithHandler(handler).AsCommand().Done();
```

Single-dash options support multi-character names (`-bl`, `-verbosity`) to model
real-world tools like dotnet and msbuild. Matching is exact, so POSIX-style flag
grouping (`-la` meaning `-l -a`) is not supported.

### NURU_P004: Invalid Type Constraint

**Problem**: Using unsupported or misspelled type names

```csharp
// ❌ Error: 'integer' is not supported
builder.Map("process {id:integer}").WithHandler(handler).AsCommand().Done();

// ✅ Correct: Use 'int'
builder.Map("process {id:int}").WithHandler(handler).AsCommand().Done();
```

**Supported types**: `string`, `int`, `double`, `bool`, `DateTime`, `Guid`, `long`, `decimal`, `TimeSpan`, `uri`

See [Supported Types](../reference/supported-types.md) for complete list.

### NURU_S001: Duplicate Parameter Names

**Problem**: Same parameter name used multiple times

```csharp
// ❌ Error: Parameter 'arg' appears twice
builder.Map("run {arg} {arg}").WithHandler(handler).AsCommand().Done();

// ✅ Correct: Use unique names
builder.Map("run {source} {dest}").WithHandler(handler).AsCommand().Done();
```

### NURU_S002: Conflicting Optional Parameters

**Problem**: Multiple consecutive optional parameters create ambiguity

```csharp
// ❌ Error: Which parameter gets the value?
builder.Map("deploy {env?} {version?}").WithHandler(handler).AsCommand().Done();
// If user types "deploy v2.0", is it env or version?

// ✅ Correct: Only last parameter optional
builder.Map("deploy {env} {version?}").WithHandler(handler).AsCommand().Done();

// ✅ Alternative: Use options for multiple optional values
builder.Map("deploy {env} --version? {ver?}").WithHandler(handler).AsCommand().Done();
```

### NURU_S003: Catch-all Not at End

**Problem**: Catch-all parameter must be last

```csharp
// ❌ Error: Catch-all in middle
builder.Map("exec {*args} {script}").WithHandler(handler).AsCommand().Done();

// ✅ Correct: Catch-all at end
builder.Map("exec {script} {*args}").WithHandler(handler).AsCommand().Done();
```

### NURU_S004: Mixed Catch-all with Optional

**Problem**: Cannot combine optional and catch-all parameters

```csharp
// ❌ Error: Invalid combination
builder.Map("run {script?} {*args}").WithHandler(handler).AsCommand().Done();

// ✅ Use one pattern:
builder.Map("run {script} {*args}").WithHandler(handler).AsCommand().Done();  // Required + catch-all
// OR
builder.Map("run {script?}").WithHandler(handler).AsCommand().Done();          // Just optional
```

### NURU_S006: Optional Before Required

**Problem**: Optional parameters must come after required ones

```csharp
// ❌ Error: Optional before required
builder.Map("copy {source?} {dest}").WithHandler(handler).AsCommand().Done();

// ✅ Correct: Required first
builder.Map("copy {source} {dest?}").WithHandler(handler).AsCommand().Done();
```

### NURU_D001: Missing Mediator Packages

**Problem**: Using `Map<TCommand>` without required Mediator packages

```csharp
// ❌ Error: Mediator packages not installed
builder.Map<PingCommand>("ping");

// ✅ Fix: Install packages
// dotnet add package Mediator.Abstractions
// dotnet add package Mediator.SourceGenerator
```

The `Map<TCommand>` pattern uses [Mediator](https://github.com/martinothamar/Mediator) for request handling. Both packages must be directly referenced (not transitive).

## IDE Integration

The analyzer works automatically in all modern .NET IDEs:

| IDE | Support |
|-----|---------|
| Visual Studio 2022+ | ✅ Full support |
| Visual Studio Code (C# extension) | ✅ Full support |
| JetBrains Rider | ✅ Full support |
| Command-line (`dotnet build`) | ✅ Full support |

### What You See

Errors appear in multiple places:
- **Code editor**: Red squiggles under problematic code
- **Error List**: Detailed error messages with error codes
- **Build output**: Compilation errors with file/line references
- **IntelliSense**: Real-time feedback as you type

## Practical Example

### Before Analyzer

```csharp
// This code would compile, then fail at runtime
builder.Map("deploy <env?> <ver?>").WithHandler(handler).AsCommand().Done();
//                      ↑       ↑
//                Multiple problems invisible until runtime
```

### With Analyzer

```csharp
// Build fails immediately with clear errors:
// NURU_P001: Invalid parameter syntax - use {env} not <env>
// NURU_S002: Conflicting optional parameters
builder.Map("deploy <env?> <ver?>").WithHandler(handler).AsCommand().Done();
```

### Fixed Code

```csharp
// Errors fixed, builds successfully
builder.Map("deploy {env} --version? {ver?}").WithHandler(handler).AsCommand().Done();
```

## Benefits

| Without Analyzer | With Analyzer |
|------------------|---------------|
| Errors found at runtime | Errors found at compile-time |
| Cryptic runtime messages | Clear, actionable error messages |
| Trial-and-error debugging | Instant feedback in IDE |
| Production failures possible | Guaranteed valid routes |

## Suppressing Diagnostics

If you have a valid reason to suppress an error (rare), you can:

### Using #pragma

```csharp
#pragma warning disable NURU_S002
builder.Map("risky {a?} {b?}").WithHandler(handler).AsCommand().Done();  // NOT RECOMMENDED
#pragma warning restore NURU_S002
```

### Using .editorconfig

```ini
[*.cs]
dotnet_diagnostic.NURU_S002.severity = warning  # Downgrade to warning
# or
dotnet_diagnostic.NURU_S002.severity = none     # Completely suppress
```

### In project file

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);NURU_S002</NoWarn>
</PropertyGroup>
```

⚠️ **Warning**: Suppressing errors can lead to runtime failures. The analyzer exists to prevent patterns that will fail at runtime.

## Debug Diagnostics

TimeWarp.Nuru includes debug diagnostics (NURU_DEBUG*) that are hidden by default. These provide detailed information about the source generator's processing.

### Enabling Debug Diagnostics

Add to your `.editorconfig`:

```ini
[*.cs]
# Enable specific debug diagnostics
dotnet_diagnostic.NURU_DEBUG001.severity = suggestion
dotnet_diagnostic.NURU_DEBUG002.severity = suggestion

# Or enable all debug diagnostics
dotnet_diagnostic.NURU_DEBUG.severity = suggestion
```

### Available Severity Values

| Value | Description |
|-------|-------------|
| `none` | Diagnostic completely suppressed |
| `silent` | Same as `none` |
| `suggestion` | Shows as informational message |
| `warning` | Shows as compiler warning |
| `error` | Shows as compiler error |

**Note:** `info` is NOT a valid severity value (common mistake).

### Why Hidden by Default?

Debug diagnostics are verbose and primarily useful for:
- Troubleshooting source generator issues
- Understanding what routes are being generated
- Debugging custom type converter registration

For normal development, keep them hidden.

## All Error Codes

### Parse Errors (NURU_P###)

| Code | Description |
|------|-------------|
| NURU_P001 | Invalid parameter syntax (wrong brackets) |
| NURU_P002 | Unbalanced braces |
| NURU_P003 | Invalid option format |
| NURU_P004 | Invalid type constraint |
| NURU_P005 | Invalid character in pattern |
| NURU_P006 | Unexpected token |
| NURU_P007 | Null route pattern |

### Semantic Errors (NURU_S###)

| Code | Description |
|------|-------------|
| NURU_S001 | Duplicate parameter names |
| NURU_S002 | Conflicting optional parameters |
| NURU_S003 | Catch-all not at end |
| NURU_S004 | Mixed catch-all with optional |
| NURU_S005 | Option with duplicate alias |
| NURU_S006 | Optional before required |
| NURU_S007 | Invalid end-of-options separator |
| NURU_S008 | Options after end-of-options separator |

### Dependency Errors (NURU_D###)

| Code | Description |
|------|-------------|
| NURU_D001 | Missing Mediator packages for Map&lt;TCommand&gt; |

## Related Documentation

- **[Routing Patterns](routing.md)** - Complete route syntax guide
- **[Supported Types](../reference/supported-types.md)** - Available type constraints
- **[Developer Guide: Using Analyzers](../../developer/guides/using-analyzers.md)** - Implementation details
