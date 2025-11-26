# REPL Basic Demo

A comprehensive REPL demonstration showcasing all major route pattern types supported by TimeWarp.Nuru.

## Running the Demo

```bash
cd Samples/ReplDemo
./repl-basic-demo.cs
```

Or:

```bash
dotnet run Samples/ReplDemo/repl-basic-demo.cs
```

## Route Patterns Demonstrated

### Simple Commands (Literal Only)

```csharp
.Map("status", () => ...)
.Map("time", () => ...)
```

| Command | Description |
|---------|-------------|
| `status` | Shows system status |
| `time` | Shows current time |

### Basic Parameters

```csharp
.Map("greet {name}", (string name) => ...)
.Map("add {a:int} {b:int}", (int a, int b) => ...)
```

| Command | Description |
|---------|-------------|
| `greet Alice` | String parameter |
| `add 5 3` | Typed integer parameters |

### Enum Parameters

```csharp
// Define enum
public enum Environment { Dev, Staging, Prod }

// Register converter
.AddTypeConverter(new EnumTypeConverter<Environment>())

// Use in route
.Map("deploy {env:environment} {tag?}", (Environment env, string? tag) => ...)
```

| Command | Description |
|---------|-------------|
| `deploy dev` | Enum parameter (case-insensitive) |
| `deploy staging` | Enum parameter |
| `deploy prod v1.2` | Enum with optional tag |
| `deploy PROD` | Case-insensitive matching |

### Catch-All Parameters

```csharp
.Map("echo {*message}", (string[] message) => ...)
```

| Command | Description |
|---------|-------------|
| `echo hello world` | Captures all remaining arguments |

### Subcommands (Hierarchical Routes)

```csharp
.Map("git status", () => ...)
.Map("git commit -m {message}", (string message) => ...)
.Map("git log --count {n:int}", (int n) => ...)
```

| Command | Description |
|---------|-------------|
| `git status` | Literal subcommand |
| `git commit -m "fix bug"` | Subcommand with short option |
| `git log --count 5` | Subcommand with long option |

### Boolean Options

```csharp
.Map("build --verbose,-v", (bool verbose) => ...)
```

| Command | Description |
|---------|-------------|
| `build` | Without flag (verbose=false) |
| `build -v` | Short form flag |
| `build --verbose` | Long form flag |

### Options with Values

```csharp
.Map("search {query} --limit,-l {count:int?}", (string query, int? count) => ...)
```

| Command | Description |
|---------|-------------|
| `search foo` | Default limit (10) |
| `search foo -l 5` | Custom limit (short) |
| `search foo --limit 5` | Custom limit (long) |

### Combined Options

```csharp
.Map("backup {source} --compress,-c --output,-o {dest?}", 
    (string source, bool compress, string? dest) => ...)
```

| Command | Description |
|---------|-------------|
| `backup data` | Basic backup |
| `backup data -c` | With compression |
| `backup data -c -o backup.tar` | With compression and custom destination |
| `backup data --compress --output backup.tar` | Long form options |

## Debug Logging

Logs are written to `repl-debug.log` in the current directory. The logging is filtered to show only REPL-specific messages, excluding parsing and route registration noise.

## Route Pattern Syntax Reference

| Pattern Element | Syntax | Example |
|-----------------|--------|---------|
| Literal | plain text | `status`, `git commit` |
| Parameter | `{name}` | `{env}` |
| Typed parameter | `{name:type}` | `{count:int}` |
| Enum parameter | `{name:enumname}` | `{env:environment}` |
| Optional parameter | `{name?}` | `{tag?}` |
| Optional typed | `{name:type?}` | `{count:int?}` |
| Catch-all | `{*name}` | `{*args}` |
| Long option | `--name` | `--verbose` |
| Short option | `-x` | `-v` |
| Option with alias | `--name,-x` | `--verbose,-v` |
| Option with value | `--name {value}` | `--limit {n}` |

## Enum Type Conversion

To use enums in routes:

1. **Define the enum:**
   ```csharp
   public enum Environment { Dev, Staging, Prod }
   ```

2. **Register the converter:**
   ```csharp
   builder.AddTypeConverter(new EnumTypeConverter<Environment>());
   ```

3. **Use in route pattern:**
   ```csharp
   .Map("deploy {env:environment}", (Environment env) => ...)
   ```

The constraint name is the enum type name in lowercase (e.g., `Environment` becomes `environment`).

Enum parsing is **case-insensitive**: `dev`, `Dev`, and `DEV` all match `Environment.Dev`.

## Related

- [repl-options-showcase.cs](repl-options-showcase.cs) - Comprehensive demo of ALL ReplOptions
- [repl-options-showcase.md](repl-options-showcase.md) - Documentation for options showcase
- [Route Pattern Syntax](/documentation/developer/guides/route-pattern-syntax.md) - Full syntax reference
