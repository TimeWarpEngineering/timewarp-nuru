# Using TimeWarp.Nuru Analyzers

TimeWarp.Nuru includes built-in analyzers that provide compile-time validation of your route patterns, catching common mistakes before runtime.

## Installation

**No separate installation needed!** Starting with version 2.1.0-beta.9, the analyzers are automatically included with the main TimeWarp.Nuru package.

```xml
<PackageReference Include="TimeWarp.Nuru" Version="2.1.0-beta.9" />
```

The analyzers run during compilation only and don't affect runtime performance or application size.

## Error Categories

Nuru uses two categories of diagnostics:

- **Parse Errors (NURU_P###)**: Syntax issues in route patterns
- **Semantic Errors (NURU_S###)**: Validation issues that create ambiguity or conflicts

---

## Parse Errors (NURU_P###)

### NURU_P001: Invalid Parameter Syntax
Detects malformed parameter placeholders that aren't using proper curly brace syntax.

```csharp
// ❌ Error: Invalid syntax
builder.AddRoute("deploy <env>", handler);  // Should use {env}

// ✅ Correct
builder.AddRoute("deploy {env}", handler);
```

### NURU_P002: Unbalanced Braces
Catches missing opening or closing braces in parameters.

```csharp
// ❌ Error: Missing closing brace
builder.AddRoute("deploy {env", handler);

// ❌ Error: Missing opening brace
builder.AddRoute("deploy env}", handler);

// ✅ Correct
builder.AddRoute("deploy {env}", handler);
```

### NURU_P003: Invalid Option Format
Ensures options follow proper naming conventions.

```csharp
// ❌ Error: Invalid option format
builder.AddRoute("build -verbose", handler);  // Multi-character single-dash

// ✅ Correct: Use double-dash for long options
builder.AddRoute("build --verbose", handler);

// ✅ Correct: Single dash for single character
builder.AddRoute("build -v", handler);
```

### NURU_P004: Invalid Type Constraint
Validates that parameter types are supported.

```csharp
// ❌ Error: Unsupported type
builder.AddRoute("process {id:integer}", handler);  // Should be 'int'

// ✅ Supported types:
builder.AddRoute("delay {ms:int}", handler);
builder.AddRoute("scale {factor:double}", handler);
builder.AddRoute("schedule {when:DateTime}", handler);
builder.AddRoute("fetch {id:Guid}", handler);
builder.AddRoute("wait {duration:TimeSpan}", handler);
```

Supported types: `string`, `int`, `double`, `bool`, `DateTime`, `Guid`, `long`, `decimal`, `TimeSpan`

### NURU_P005: Invalid Character
Detects invalid characters in route patterns.

```csharp
// ❌ Error: Invalid character
builder.AddRoute("test @param", handler);

// ✅ Correct
builder.AddRoute("test {param}", handler);
```

### NURU_P006: Unexpected Token
The parser encountered an unexpected token in the route pattern.

```csharp
// ❌ Error: Unexpected '}'
builder.AddRoute("test }", handler);

// ✅ Correct
builder.AddRoute("test {param}", handler);
```

### NURU_P007: Null Route Pattern
Route pattern cannot be null.

```csharp
// ❌ Error: Null pattern
string? pattern = null;
builder.AddRoute(pattern!, handler);

// ✅ Correct
builder.AddRoute("valid-pattern", handler);
```

---

## Semantic Errors (NURU_S###)

### NURU_S001: Duplicate Parameter Names
Each parameter name must be unique within a route pattern.

```csharp
// ❌ Error: Duplicate parameter 'arg'
builder.AddRoute("run {arg} {arg}", handler);

// ✅ Correct: Unique names
builder.AddRoute("run {source} {dest}", handler);
```

### NURU_S002: Conflicting Optional Parameters
Having multiple consecutive optional parameters creates parsing ambiguity.

```csharp
// ❌ Error: Multiple optionals - ambiguous!
builder.AddRoute("deploy {env?} {version?}", handler);
// Input "deploy v2.0" - is it env or version?

// ✅ Correct: Single optional at end
builder.AddRoute("deploy {env} {version?}", handler);

// ✅ Alternative: Use options for multiple optional values
builder.AddRoute("deploy {env} --version? {ver?} --tag? {tag?}", handler);
```

### NURU_S003: Catch-all Not at End
Catch-all parameters must appear as the last positional parameter.

```csharp
// ❌ Error: Catch-all not at end
builder.AddRoute("exec {*args} {script}", handler);

// ✅ Correct: Catch-all at end
builder.AddRoute("exec {script} {*args}", handler);
```

### NURU_S004: Mixed Catch-all with Optional
Routes cannot contain both optional parameters and catch-all parameters.

```csharp
// ❌ Error: Cannot mix optional with catch-all
builder.AddRoute("run {script?} {*args}", handler);

// ✅ Use one or the other:
builder.AddRoute("run {script} {*args}", handler);  // Required + catch-all
builder.AddRoute("run {script?}", handler);          // Just optional
```

### NURU_S005: Option with Duplicate Alias
Options cannot have the same short form specified multiple times.

```csharp
// ❌ Error: Duplicate alias '-c'
builder.AddRoute("build --config,-c {m} --count,-c {n}", handler);

// ✅ Correct: Unique aliases
builder.AddRoute("build --config,-c {m} --count,-n {n}", handler);
```

### NURU_S006: Optional Before Required
Optional parameters must appear after all required parameters.

```csharp
// ❌ Error: Optional before required
builder.AddRoute("copy {source?} {dest}", handler);

// ✅ Correct: Required before optional
builder.AddRoute("copy {source} {dest?}", handler);
```

### NURU_S007: Invalid End-of-Options Separator
The end-of-options separator `--` must be followed by a catch-all parameter.

```csharp
// ❌ Error: No catch-all after --
builder.AddRoute("run --", handler);

// ✅ Correct: -- followed by catch-all
builder.AddRoute("run -- {*args}", handler);
```

### NURU_S008: Options After End-of-Options Separator
Options cannot appear after the end-of-options separator `--`.

```csharp
// ❌ Error: Option after --
builder.AddRoute("run -- {*args} --verbose", handler);

// ✅ Correct: Options before --
builder.AddRoute("run --verbose -- {*args}", handler);
```

---

## Severity Levels

All current errors are set to `Error` severity, which means:
- Build will fail if any errors are present
- Must be fixed before compilation succeeds

Future versions may introduce warnings for best practices.

---

## Suppressing Diagnostics

If you need to suppress a specific diagnostic (not recommended unless you have a very good reason):

### Using #pragma directives

```csharp
#pragma warning disable NURU_S002 // Conflicting optional parameters
builder.AddRoute("risky {a?} {b?}", handler);  // NOT RECOMMENDED
#pragma warning restore NURU_S002
```

### Using .editorconfig

```ini
[*.cs]
dotnet_diagnostic.NURU_S002.severity = none
```

### In project file

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);NURU_S002</NoWarn>
</PropertyGroup>
```

**Warning**: Suppressing errors can lead to runtime failures. The analyzers exist to prevent ambiguous patterns that will fail at runtime.

---

## IDE Integration

The analyzers work automatically in:
- **Visual Studio 2022+**
- **Visual Studio Code** (with C# extension)
- **JetBrains Rider**
- **Command-line** (`dotnet build`)

Errors appear in:
- Error List window
- Code editor (red squiggles)
- Build output

---

## Example: Fixing Common Errors

### Before (Multiple Errors)

```csharp
builder.AddRoute("deploy <env?> <ver?>", handler);
//                      ↑       ↑
//              NURU_P001  NURU_S002
```

### After (Fixed)

```csharp
builder.AddRoute("deploy {env} --version? {ver?}", handler);
//                      ↑                 ↑
//                  Valid parameters   Optional option
```

---

## Related Documentation

- [Syntax Rules](../design/parser/syntax-rules.md) - Complete route pattern syntax reference
- [Parameter Optionality](../design/cross-cutting/parameter-optionality.md) - Nullability-based optional design
- [Error Handling](../design/cross-cutting/error-handling.md) - Runtime error handling

---

## Debug Diagnostic

### NURU_DEBUG (Hidden)
Development diagnostic for verifying route detection. Not visible in normal builds.

This diagnostic is used during analyzer development to verify that routes are being detected correctly. It's hidden by default and doesn't affect your build.
