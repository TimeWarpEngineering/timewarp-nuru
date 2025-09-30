# Using TimeWarp.Nuru Analyzers

TimeWarp.Nuru includes built-in analyzers that provide compile-time validation of your route patterns, catching common mistakes before runtime.

## Installation

**No separate installation needed!** Starting with version 2.1.0-beta.9, the analyzers are automatically included with the main TimeWarp.Nuru package.

```xml
<PackageReference Include="TimeWarp.Nuru" Version="2.1.0-beta.9" />
```

The analyzers run during compilation only and don't affect runtime performance or application size.

## What It Does

The analyzer validates your route patterns at compile time and reports errors for:

### NURU001: Invalid Parameter Syntax
Detects malformed parameter placeholders that aren't using proper curly brace syntax.

```csharp
// ❌ Error: Invalid syntax
builder.AddRoute("deploy <env>", handler);  // Should use {env}

// ✅ Correct
builder.AddRoute("deploy {env}", handler);
```

### NURU002: Unbalanced Braces
Catches missing opening or closing braces in parameters.

```csharp
// ❌ Error: Missing closing brace
builder.AddRoute("deploy {env", handler);

// ❌ Error: Missing opening brace  
builder.AddRoute("deploy env}", handler);

// ✅ Correct
builder.AddRoute("deploy {env}", handler);
```

### NURU003: Invalid Option Format
Ensures options follow proper naming conventions.

```csharp
// ❌ Error: Multi-character single-dash option
builder.AddRoute("build -verbose", handler);

// ✅ Correct: Use double-dash for long options
builder.AddRoute("build --verbose", handler);

// ✅ Correct: Single dash for single character
builder.AddRoute("build -v", handler);
```

### NURU004: Invalid Type Constraints
Validates that parameter types are supported.

```csharp
// ❌ Error: Unsupported type
builder.AddRoute("wait {ms:integer}", handler);  // Should be 'int'
builder.AddRoute("price {cost:float}", handler);  // Use 'double' instead

// ✅ Correct: Supported types
builder.AddRoute("wait {ms:int}", handler);
builder.AddRoute("price {cost:double}", handler);
```

Supported types: `string`, `int`, `long`, `double`, `decimal`, `bool`, `DateTime`, `Guid`, `TimeSpan`

### NURU005: Catch-all Parameter Position
Ensures catch-all parameters are always last.

```csharp
// ❌ Error: Catch-all not at end
builder.AddRoute("docker {*args} --verbose", handler);

// ✅ Correct: Catch-all at end
builder.AddRoute("docker --verbose {*args}", handler);
```

### NURU006: Duplicate Parameter Names
Prevents using the same parameter name multiple times.

```csharp
// ❌ Error: Duplicate parameter 'file'
builder.AddRoute("copy {file} to {file}", handler);

// ✅ Correct: Unique parameter names
builder.AddRoute("copy {source} to {destination}", handler);
```

### NURU007: Consecutive Optional Parameters
Warns about ambiguous consecutive optional parameters.

```csharp
// ❌ Warning: Ambiguous parsing
builder.AddRoute("backup {source?} {dest?}", handler);
// Which parameter gets "file.txt" in: myapp backup file.txt?

// ✅ Correct: Add literal separator
builder.AddRoute("backup {source?} to {dest?}", handler);
// Clear: myapp backup file.txt to backup.txt
```

### NURU008: Mixed Optional and Catch-all
Prevents combining optional parameters with catch-all in the same route.

```csharp
// ❌ Error: Cannot mix optional with catch-all
builder.AddRoute("run {script?} {*args}", handler);

// ✅ Correct: Use either optional OR catch-all
builder.AddRoute("run {script} {*args}", handler);  // Required + catch-all
builder.AddRoute("run {script?}", handler);         // Just optional
```

### NURU009: Duplicate Option Short Forms
Ensures option short aliases are unique within a route.

```csharp
// ❌ Error: Both use '-v'
builder.AddRoute("test --verbose,-v --validate,-v", handler);

// ✅ Correct: Unique short forms
builder.AddRoute("test --verbose,-v --validate,-a", handler);
```

## Severity Levels

- **Errors (NURU001-006, 008-009)**: Build failures - must be fixed
- **Warnings (NURU007)**: Potential issues - should be reviewed

## Suppressing Warnings

If you need to suppress a specific diagnostic:

```csharp
#pragma warning disable NURU007 // Consecutive optional parameters
builder.AddRoute("backup {source?} {dest?}", handler);
#pragma warning restore NURU007
```

Or project-wide in your `.csproj`:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);NURU007</NoWarn>
</PropertyGroup>
```

## Best Practices

1. **Always include the analyzer** during development for immediate feedback
2. **Fix all errors** before committing code
3. **Review warnings** - they often indicate design issues
4. **Use descriptive parameter names** for better error messages
5. **Test route patterns** even with analyzer validation

## Troubleshooting

### Analyzer Not Working

If the analyzer isn't reporting issues:

1. Ensure the package is properly referenced with `IncludeAssets="analyzers"`
2. Rebuild the project to trigger analyzer
3. Check that you're using `NuruAppBuilder` methods (the analyzer only validates these)

### False Positives

The analyzer validates syntax, not runtime behavior. Some valid patterns might be flagged if they're unconventional. Use `#pragma` directives to suppress if needed.

## Future Improvements

We're working on:
- Quick fixes for common issues  
- Route pattern IntelliSense
- Performance analysis for route specificity
- Additional validation rules

## See Also

- [Route Pattern Syntax](RoutePatternSyntax.md)
- [Glossary](Glossary.md) - Complete terminology reference
- [GitHub Issues](https://github.com/TimeWarpEngineering/timewarp-nuru/issues) - Report analyzer bugs