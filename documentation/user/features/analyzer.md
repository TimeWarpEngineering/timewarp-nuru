# Roslyn Analyzer

TimeWarp.Nuru includes a built-in Roslyn analyzer that provides **compile-time validation** of your route patterns, catching errors before you run your application.

## Why It Matters

The analyzer prevents common mistakes that would otherwise fail at runtime:

```csharp
// ❌ This compiles without analyzer, fails at runtime
builder.Map("deploy <env>", handler);  // Wrong syntax

// ❌ This compiles without analyzer, creates ambiguous routing
builder.Map("run {file?} {*args}", handler);  // Invalid combination

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
builder.Map("deploy <env>", handler);  // Should use curly braces

// ✅ Correct
builder.Map("deploy {env}", handler);
```

### NURU_P002: Unbalanced Braces

**Problem**: Missing opening or closing brace

```csharp
// ❌ Error: Missing closing brace
builder.Map("deploy {env", handler);

// ❌ Error: Missing opening brace
builder.Map("deploy env}", handler);

// ✅ Correct
builder.Map("deploy {env}", handler);
```

### NURU_P003: Invalid Option Format

**Problem**: Incorrect option naming (multi-character with single dash)

```csharp
// ❌ Error: Multi-character option needs double-dash
builder.Map("build -verbose", handler);

// ✅ Correct: Double-dash for long options
builder.Map("build --verbose", handler);

// ✅ Correct: Single dash for single character
builder.Map("build -v", handler);

// ✅ Best: Provide both
builder.Map("build --verbose,-v", handler);
```

### NURU_P004: Invalid Type Constraint

**Problem**: Using unsupported or misspelled type names

```csharp
// ❌ Error: 'integer' is not supported
builder.Map("process {id:integer}", handler);

// ✅ Correct: Use 'int'
builder.Map("process {id:int}", handler);
```

**Supported types**: `string`, `int`, `double`, `bool`, `DateTime`, `Guid`, `long`, `decimal`, `TimeSpan`, `uri`

See [Supported Types](../reference/supported-types.md) for complete list.

### NURU_S001: Duplicate Parameter Names

**Problem**: Same parameter name used multiple times

```csharp
// ❌ Error: Parameter 'arg' appears twice
builder.Map("run {arg} {arg}", handler);

// ✅ Correct: Use unique names
builder.Map("run {source} {dest}", handler);
```

### NURU_S002: Conflicting Optional Parameters

**Problem**: Multiple consecutive optional parameters create ambiguity

```csharp
// ❌ Error: Which parameter gets the value?
builder.Map("deploy {env?} {version?}", handler);
// If user types "deploy v2.0", is it env or version?

// ✅ Correct: Only last parameter optional
builder.Map("deploy {env} {version?}", handler);

// ✅ Alternative: Use options for multiple optional values
builder.Map("deploy {env} --version? {ver?}", handler);
```

### NURU_S003: Catch-all Not at End

**Problem**: Catch-all parameter must be last

```csharp
// ❌ Error: Catch-all in middle
builder.Map("exec {*args} {script}", handler);

// ✅ Correct: Catch-all at end
builder.Map("exec {script} {*args}", handler);
```

### NURU_S004: Mixed Catch-all with Optional

**Problem**: Cannot combine optional and catch-all parameters

```csharp
// ❌ Error: Invalid combination
builder.Map("run {script?} {*args}", handler);

// ✅ Use one pattern:
builder.Map("run {script} {*args}", handler);  // Required + catch-all
// OR
builder.Map("run {script?}", handler);          // Just optional
```

### NURU_S006: Optional Before Required

**Problem**: Optional parameters must come after required ones

```csharp
// ❌ Error: Optional before required
builder.Map("copy {source?} {dest}", handler);

// ✅ Correct: Required first
builder.Map("copy {source} {dest?}", handler);
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
builder.Map("deploy <env?> <ver?>", handler);
//                      ↑       ↑
//                Multiple problems invisible until runtime
```

### With Analyzer

```csharp
// Build fails immediately with clear errors:
// NURU_P001: Invalid parameter syntax - use {env} not <env>
// NURU_S002: Conflicting optional parameters
builder.Map("deploy <env?> <ver?>", handler);
```

### Fixed Code

```csharp
// Errors fixed, builds successfully
builder.Map("deploy {env} --version? {ver?}", handler);
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
builder.Map("risky {a?} {b?}", handler);  // NOT RECOMMENDED
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
