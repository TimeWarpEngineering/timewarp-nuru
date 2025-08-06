# Logging System

TimeWarp.Nuru includes a comprehensive logging system for debugging and monitoring framework behavior. The logging system provides granular control over which components produce output and at what verbosity level.

## Quick Start

By default, logging is disabled. To enable basic debug logging for all components:

### Bash
```bash
export NURU_LOG_LEVEL=Debug
./your-app command args
```

Or for a single command:

```bash
NURU_LOG_LEVEL=Debug ./your-app command args
```

### PowerShell (pwsh)
```powershell
$env:NURU_LOG_LEVEL = "Debug"
./your-app command args
```

Or for a single command:

```powershell
$env:NURU_LOG_LEVEL = "Debug"; ./your-app command args
```

## Log Levels

The framework supports six log levels, from most to least verbose:

| Level | Value | Description |
|-------|-------|-------------|
| `Trace` | 0 | Most detailed information, shows internal operations |
| `Debug` | 1 | Detailed debugging information |
| `Info` | 2 | General informational messages |
| `Warning` | 3 | Warning messages for potential issues |
| `Error` | 4 | Error messages for failures |
| `None` | 5 | No logging output |

## Components

Logging can be configured per component to reduce noise and focus on specific areas:

| Component | Description |
|-----------|-------------|
| `Registration` | Route registration during app startup |
| `Lexer` | Tokenization of route patterns |
| `Parser` | Parsing route patterns into AST |
| `Matcher` | Route matching and resolution at runtime |
| `Binder` | Parameter binding to delegate methods |
| `TypeConverter` | Type conversion operations |
| `HelpGen` | Help text generation |

## Configuration Methods

### 1. Global Level (All Components)

Set a default level for all components:

#### Bash
```bash
export NURU_LOG_LEVEL=Debug
```

#### PowerShell
```powershell
$env:NURU_LOG_LEVEL = "Debug"
```

### 2. Per-Component Level

Configure individual components using `NURU_LOG_<COMPONENT>`:

#### Bash
```bash
export NURU_LOG_REGISTRATION=Info
export NURU_LOG_PARSER=Debug
export NURU_LOG_MATCHER=Trace
export NURU_LOG_BINDER=None
```

#### PowerShell
```powershell
$env:NURU_LOG_REGISTRATION = "Info"
$env:NURU_LOG_PARSER = "Debug"
$env:NURU_LOG_MATCHER = "Trace"
$env:NURU_LOG_BINDER = "None"
```

### 3. Combined Configuration

Use `NURU_LOG` to configure multiple components in one line:

#### Bash
```bash
export NURU_LOG="Registration:info,Parser:debug,Matcher:trace"
```

#### PowerShell
```powershell
$env:NURU_LOG = "Registration:info,Parser:debug,Matcher:trace"
```

Format: `Component1:level1,Component2:level2,...`

### 4. Configuration Priority

Settings are applied in this order (later overrides earlier):
1. `NURU_LOG_LEVEL` (global default)
2. `NURU_LOG` (combined configuration)
3. `NURU_LOG_<COMPONENT>` (individual component)

## Examples

### Debug Route Registration Only

See how routes are being registered:

#### Bash
```bash
NURU_LOG_REGISTRATION=Debug ./your-app --help
```

#### PowerShell
```powershell
$env:NURU_LOG_REGISTRATION = "Debug"; ./your-app --help
```

Output:
```
[DEBUG][Registration] Registering route: 'add {x:double} {y:double}'
[DEBUG][Registration] Registering route: 'subtract {x:double} {y:double}'
```

### Trace Route Matching

See detailed matching logic:

#### Bash
```bash
NURU_LOG_MATCHER=Trace ./your-app add 5 3
```

#### PowerShell
```powershell
$env:NURU_LOG_MATCHER = "Trace"; ./your-app add 5 3
```

Output:
```
[TRACE][Matcher] [1/2] Checking route: 'add {x:double} {y:double}'
[TRACE][Matcher] Matching 3 positional segments against 3 arguments
[TRACE][Matcher] Attempting to match 'add' against add
[TRACE][Matcher]   Literal 'add' matched
[TRACE][Matcher] Attempting to match '5' against {x:double}
[TRACE][Matcher]   Extracted parameter 'x' = '5'
```

### Parser Debugging

See how patterns are parsed:

#### Bash
```bash
NURU_LOG_PARSER=Debug ./your-app
```

#### PowerShell
```powershell
$env:NURU_LOG_PARSER = "Debug"; ./your-app
```

Output:
```
[DEBUG][Parser] Parsing pattern: 'deploy {env} --dry-run'
[DEBUG][Parser]     → 3 segments: Literal: 'deploy', Parameter: name='env', Option: longName='dry-run'
```

### Combined Debugging

Debug registration and matching, with trace-level parser output:

#### Bash
```bash
NURU_LOG="Registration:debug,Parser:trace,Matcher:debug" ./your-app test
```

#### PowerShell
```powershell
$env:NURU_LOG = "Registration:debug,Parser:trace,Matcher:debug"; ./your-app test
```

### Production Monitoring

Only show warnings and errors:

#### Bash
```bash
NURU_LOG_LEVEL=Warning ./your-app
```

#### PowerShell
```powershell
$env:NURU_LOG_LEVEL = "Warning"; ./your-app
```

## Programmatic Configuration

You can also configure logging in your application code:

```csharp
#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;

// Configure logging before building the app
Environment.SetEnvironmentVariable("NURU_LOG_LEVEL", "Debug");
Environment.SetEnvironmentVariable("NURU_LOG_MATCHER", "Trace");

var app = new NuruAppBuilder()
    .AddRoute("test", () => Console.WriteLine("Test"))
    .Build();

return await app.RunAsync(args);
```

## Output Format

Log messages include level and component information:

- **Trace/Debug**: `[LEVEL][Component] message`
- **Info and above**: `[LEVEL] message`

Examples:
```
[INFO] Starting route registration
[DEBUG][Parser] Parsing pattern: 'test {value}'
[TRACE][Matcher] Attempting to match 'test' against test
[WARN] No matching route found
[ERROR] Failed to convert parameter 'count' to type 'int'
```

## Backwards Compatibility

The legacy `NURU_DEBUG=true` environment variable is still supported and enables Debug level for all components when no other configuration is present.

## Performance Considerations

- Logging has minimal overhead when disabled (level checks are performed before message formatting)
- Trace level can produce significant output and should only be used for debugging
- In production, use `Warning` or `Error` levels to minimize performance impact

## Troubleshooting Common Scenarios

### "Why isn't my route matching?"

Enable matcher trace logging to see the matching process:

#### Bash
```bash
NURU_LOG_MATCHER=Trace ./your-app your-command
```

#### PowerShell
```powershell
$env:NURU_LOG_MATCHER = "Trace"; ./your-app your-command
```

### "How are my routes being parsed?"

Enable parser debug logging:

#### Bash
```bash
NURU_LOG_PARSER=Debug ./your-app
```

#### PowerShell
```powershell
$env:NURU_LOG_PARSER = "Debug"; ./your-app
```

### "What routes are registered?"

Enable registration info logging:

#### Bash
```bash
NURU_LOG_REGISTRATION=Info ./your-app
```

#### PowerShell
```powershell
$env:NURU_LOG_REGISTRATION = "Info"; ./your-app
```

### "I want to see everything!"

Enable trace for all components:

#### Bash
```bash
NURU_LOG_LEVEL=Trace ./your-app
```

#### PowerShell
```powershell
$env:NURU_LOG_LEVEL = "Trace"; ./your-app
```

## Tips

1. Start with `Debug` level for general troubleshooting
2. Use `Trace` only when you need to see internal operations
3. Combine component-specific logging to focus on problem areas
4. Disable logging in production unless monitoring specific issues
5. Use the combined format (`NURU_LOG`) for temporary debugging sessions
6. Use individual component variables for persistent configuration