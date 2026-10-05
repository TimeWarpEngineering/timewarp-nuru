# Using TimeWarp.Nuru Analyzers

TimeWarp.Nuru includes built-in analyzers that provide compile-time validation of your route patterns, catching common mistakes before runtime.

## Installation

**No separate installation needed!** The analyzers ship inside the TimeWarp.Nuru package.

```xml
<PackageReference Include="TimeWarp.Nuru" />
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
builder.Map("deploy <env>").WithHandler(handler).AsCommand().Done();  // Should use {env}

// ✅ Correct
builder.Map("deploy {env}").WithHandler(handler).AsCommand().Done();
```

### NURU_P002: Unbalanced Braces
Catches missing opening or closing braces in parameters.

```csharp
// ❌ Error: Missing closing brace
builder.Map("deploy {env").WithHandler(handler).AsCommand().Done();

// ❌ Error: Missing opening brace
builder.Map("deploy env}").WithHandler(handler).AsCommand().Done();

// ✅ Correct
builder.Map("deploy {env}").WithHandler(handler).AsCommand().Done();
```

### NURU_P003: Invalid Option Format

The diagnostic message is “options must start with '--' or '-'”. A token that already starts with `-` or `--` is an option. That includes multi-character single-dash names.

```csharp
// ✅ Correct: Double-dash for long options
builder.Map("build --verbose").WithHandler(handler).AsCommand().Done();

// ✅ Correct: Single dash, single character
builder.Map("build -v").WithHandler(handler).AsCommand().Done();

// ✅ Correct: Single dash, multi-character (dotnet/msbuild style)
builder.Map("build -verbose").WithHandler(handler).AsCommand().Done();
builder.Map("build -bl").WithHandler(handler).AsCommand().Done();

// ✅ Best: Provide both long and short forms
builder.Map("build --verbose,-v").WithHandler(handler).AsCommand().Done();
```

Matching is exact, so POSIX-style flag grouping (`-la` meaning `-l` and `-a`) is not supported. The current parser does not construct `InvalidOptionFormatError` for a dash-prefixed token.

### NURU_P004: Invalid Type Constraint
Validates that parameter types are supported.

```csharp
// ❌ Error: Unsupported type
builder.Map("process {id:integer}").WithHandler(handler).AsCommand().Done();  // Should be 'int'

// ✅ Supported types:
builder.Map("delay {ms:int}").WithHandler(handler).AsCommand().Done();
builder.Map("scale {factor:double}").WithHandler(handler).AsCommand().Done();
builder.Map("schedule {when:DateTime}").WithHandler(handler).AsCommand().Done();
builder.Map("fetch {id:Guid}").WithHandler(handler).AsCommand().Done();
builder.Map("wait {duration:TimeSpan}").WithHandler(handler).AsCommand().Done();
```

Supported types: `string`, `int`, `double`, `bool`, `DateTime`, `Guid`, `long`, `decimal`, `TimeSpan`

### NURU_P005: Invalid Character
Detects invalid characters in route patterns.

```csharp
// ❌ Error: Invalid character
builder.Map("test @param").WithHandler(handler).AsCommand().Done();

// ✅ Correct
builder.Map("test {param}").WithHandler(handler).AsCommand().Done();
```

### NURU_P006: Unexpected Token
The parser encountered an unexpected token in the route pattern.

```csharp
// ❌ Error: Unexpected '}'
builder.Map("test }").WithHandler(handler).AsCommand().Done();

// ✅ Correct
builder.Map("test {param}").WithHandler(handler).AsCommand().Done();
```

### NURU_P007: Null Route Pattern
Route pattern cannot be null.

```csharp
// ❌ Error: Null pattern
string? pattern = null;
builder.Map(pattern!).WithHandler(handler).AsCommand().Done();

// ✅ Correct
builder.Map("valid-pattern").WithHandler(handler).AsCommand().Done();
```

---

## Semantic Errors (NURU_S###)

### NURU_S001: Duplicate Parameter Names
Each parameter name must be unique within a route pattern.

```csharp
// ❌ Error: Duplicate parameter 'arg'
builder.Map("run {arg} {arg}").WithHandler(handler).AsCommand().Done();

// ✅ Correct: Unique names
builder.Map("run {source} {dest}").WithHandler(handler).AsCommand().Done();
```

### NURU_S002: Conflicting Optional Parameters
Having multiple consecutive optional parameters creates parsing ambiguity.

```csharp
// ❌ Error: Multiple optionals - ambiguous!
builder.Map("deploy {env?} {version?}").WithHandler(handler).AsCommand().Done();
// Input "deploy v2.0" - is it env or version?

// ✅ Correct: Single optional at end
builder.Map("deploy {env} {version?}").WithHandler(handler).AsCommand().Done();

// ✅ Alternative: Use options for multiple optional values
builder.Map("deploy {env} --version? {ver?} --tag? {tag?}").WithHandler(handler).AsCommand().Done();
```

### NURU_S003: Catch-all Not at End
Catch-all parameters must appear as the last positional parameter.

```csharp
// ❌ Error: Catch-all not at end
builder.Map("exec {*args} {script}").WithHandler(handler).AsCommand().Done();

// ✅ Correct: Catch-all at end
builder.Map("exec {script} {*args}").WithHandler(handler).AsCommand().Done();
```

### NURU_S004: Mixed Catch-all with Optional
Routes cannot contain both optional parameters and catch-all parameters.

```csharp
// ❌ Error: Cannot mix optional with catch-all
builder.Map("run {script?} {*args}").WithHandler(handler).AsCommand().Done();

// ✅ Use one or the other:
builder.Map("run {script} {*args}").WithHandler(handler).AsCommand().Done();  // Required + catch-all
builder.Map("run {script?}").WithHandler(handler).AsCommand().Done();          // Just optional
```

### NURU_S005: Option with Duplicate Alias
Options cannot have the same short form specified multiple times.

```csharp
// ❌ Error: Duplicate alias '-c'
builder.Map("build --config,-c {m} --count,-c {n}").WithHandler(handler).AsCommand().Done();

// ✅ Correct: Unique aliases
builder.Map("build --config,-c {m} --count,-n {n}").WithHandler(handler).AsCommand().Done();
```

### NURU_S006: Optional Before Required
Optional parameters must appear after all required parameters.

```csharp
// ❌ Error: Optional before required
builder.Map("copy {source?} {dest}").WithHandler(handler).AsCommand().Done();

// ✅ Correct: Required before optional
builder.Map("copy {source} {dest?}").WithHandler(handler).AsCommand().Done();
```

### NURU_S007: Invalid End-of-Options Separator
The end-of-options separator `--` must be followed by a catch-all parameter.

```csharp
// ❌ Error: No catch-all after --
builder.Map("run --").WithHandler(handler).AsCommand().Done();

// ✅ Correct: -- followed by catch-all
builder.Map("run -- {*args}").WithHandler(handler).AsCommand().Done();
```

### NURU_S008: Options After End-of-Options Separator
Options cannot appear after the end-of-options separator `--`.

```csharp
// ❌ Error: Option after --
builder.Map("run -- {*args} --verbose").WithHandler(handler).AsCommand().Done();

// ✅ Correct: Options before --
builder.Map("run --verbose -- {*args}").WithHandler(handler).AsCommand().Done();
```

---

## Typed endpoints

`.Map<TEndpoint>()` takes no pattern argument. The route lives on `[NuruRoute]`. There is no `NURU_D001` diagnostic.

```csharp
using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("ping", Description = "Send a ping")]
public sealed class PingCommand : ICommand<Unit>
{
}

NuruApp app = NuruApp.CreateBuilder()
  .Map<PingCommand>()
  .Build();
```

Mediator contracts (`ICommand<T>`, `Unit`, handlers) come from `TimeWarp.Mediator`, which the Nuru package references. The generated host calls `AddGeneratedMediator()`. Do not reference `Mediator.Abstractions` or `Mediator.SourceGenerator`, and do not call `AddMediator()`. See [Endpoints](../../user/features/endpoints.md) for a handler.

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
builder.Map("risky {a?} {b?}").WithHandler(handler).AsCommand().Done();  // NOT RECOMMENDED
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
builder.Map("deploy <env?> <ver?>").WithHandler(handler).AsCommand().Done();
//                      ↑       ↑
//              NURU_P001  NURU_S002
```

### After (Fixed)

```csharp
builder.Map("deploy {env} --version? {ver?}").WithHandler(handler).AsCommand().Done();
//                      ↑                 ↑
//                  Valid parameters   Optional option
```

---

## Related Documentation

- [Syntax Rules](../design/parser/syntax-rules.md) - Complete route pattern syntax reference
- [Parameter Optionality](../design/cross-cutting/parameter-optionality.md) - Nullability-based optional design
- [Error Handling](../design/cross-cutting/error-handling.md) - Runtime error handling
