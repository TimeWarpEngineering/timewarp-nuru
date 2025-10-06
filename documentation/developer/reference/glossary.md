# TimeWarp.Nuru CLI Framework - Comprehensive Glossary

This glossary provides detailed definitions and examples of all key terms used throughout the TimeWarp.Nuru route-based CLI framework.

## Table of Contents

- [📝 Route Pattern Syntax Terms](#-route-pattern-syntax-terms)
- [🔧 Parameter Types](#-parameter-types)
- [🎯 Execution Approaches](#-execution-approaches)
- [📊 Type System Terms](#-type-system-terms)
- [🛠️ Framework Architecture Terms](#-framework-architecture-terms)
- [📋 Command Organization Terms](#-command-organization-terms)
- [🎨 Advanced Features](#-advanced-features)

---

## 📝 Route Pattern Syntax Terms

### Route Pattern
**Definition**: A string template that defines how command-line arguments should be matched and parsed by the CLI framework. Route patterns use a combination of literal segments, [**parameters**](#parameters), and [**options**](#options) to specify valid command formats.

**Examples**:
```csharp
.AddRoute("git status", () => {})     // Simple literal route
.AddRoute("deploy {env} --version {tag} --dry-run", handler)  // Complex route
.AddRoute("docker run {*args}", handler)  // Catch-all route
```

### Literal Segments
**Definition**: Plain text portions of a [**route pattern**](#route-pattern) that must match exactly against command-line arguments. Literals form the fixed parts of commands that don't vary.

**Examples**:
```csharp
builder.AddRoute("git status", () => {})        // "git" and "status" are literals
builder.AddRoute("backup command", () => {})     // "backup" and "command" are literals
```

### Parameters
**Definition**: Dynamic placeholders in route patterns that capture values from command-line arguments. Parameters are denoted by curly braces `{}` and can include type constraints and descriptive information.

**Format Variations**:
- Basic: `{name}`
- Typed: `{name:int}`
- Optional: `{name?}`
- Catch-all: `{*args}`

**Examples**:
```csharp
// Basic parameters
builder.AddRoute("greet {name}", (string name) => {})  // Required string parameter
builder.AddRoute("wait {seconds:int}", (int s) => {})  // Required integer parameter

// Multiple parameters
builder.AddRoute("copy {source} {destination}", (string source, string dest) => {})

// Optional parameters
builder.AddRoute("deploy {env} {tag?}", (string env, string? tag) => {})

// Catch-all parameters
builder.AddRoute("docker {*args}", (string[] args) => {})
```

**See Also**: [**Arguments**](#arguments), [**Type Constraints**](#type-constraints)

### Options
**Definition**: Command-line flags that modify behavior. Options start with `--` (long form) or `-` (short form) and can optionally accept values. They appear after positional [**parameters**](#parameters) in route patterns.

**Format Variations**:
- Boolean options (presence/absence): `--verbose`, `-v`
- Options with values: `--config {value}`, `-c {value}`
- Short aliases: `-v,--verbose`

**Examples**:
```csharp
// Boolean options
builder.AddRoute("build --verbose", (bool verbose) => {})
        .AddRoute("test -v", (bool v) => {})

// Options with values
builder.AddRoute("deploy --version {ver}", (string ver) => {})
        .AddRoute("run --config,-c {cfg}", (string cfg) => {})

// Multiple options
builder.AddRoute("server --port,-p {port} --host {host} --debug,-d",
    (int port, string host, bool debug) => {})
```

**Related Terms**: [**Short Aliases**](#short-aliases), [**Boolean Options**](#boolean-options)

### Short Aliases
**Definition**: Single-character abbreviations for options using the `-` prefix, often combined with long-form options using commas. Short aliases provide convenient shorthand for commonly used options.

**Examples**:
```csharp
builder.AddRoute("run --verbose,-v", handler)     // -v is short alias for --verbose
builder.AddRoute("build --output,-o {path}", handler)  // -o is short alias for --output
```

### Boolean Options
**Definition**: Options that represent true/false flags. When present, they're automatically set to `true`. No value is expected after a boolean option.

**Examples**:
```csharp
// Route definition
builder.AddRoute("compile --optimize --debug,-d", (bool opt, bool debug) => {})

// Usage examples
myapp compile --optimize --debug  // Both flags set to true
myapp compile --debug             // Only debug=true, optimize=false
myapp compile --optimize          // Only optimize=true, debug=false
myapp compile                     // Both false (default)
```

### Arguments
**Definition**: The actual values provided by users when executing commands. Arguments are parsed according to the [**route pattern**](#route-pattern) and converted to the appropriate types automatically.

**Examples**:
Given route pattern `"deploy {env} --version {tag}"`:
```bash
# Command execution
myapp deploy production --version v1.2.3

# Parsed arguments
# env = "production" (argument)
# tag = "v1.2.3" (argument)

# Another example with optional parameters
myapp deploy production   # env = "production", tag = null
```

**Related Terms**: [**Parameters**](#parameters), [**Route Pattern**](#route-pattern)

---

## 🔧 Parameter Types

### Positional Parameters
**Definition**: [**Parameters**](#parameters) that appear in a specific order within the command line, before any [**options**](#options). They are identified by their position rather than a name flag.

**Examples**:
```csharp
// Route: "copy {source} {destination}"
myapp copy file1.txt backup.txt    // source="file1.txt", destination="backup.txt"

// Route: "add {x:int} {y:int}"
myapp add 5 10                     // x=5, y=10

// Route: "docker {command} {*args}"
myapp docker run ubuntu            // command="run", args=["ubuntu"]
```

### Optional Parameters
**Definition**: Positional [**parameters**](#parameters) marked with `?` that can be omitted from the command line. When omitted, they receive `null` values (or default values if specified).

**Examples**:
```csharp
builder.AddRoute("deploy {env} {tag?}", (string env, string? tag) => {
    // In handler: tag can be null if not provided
})

// Valid command usages:
myapp deploy production v1.2   // env="production", tag="v1.2"
myapp deploy production        // env="production", tag=null
```

**Constraints**:
- Cannot be consecutive optional parameters (creates ambiguity)
- Workaround: Use literals to separate them

```csharp
// ❌ Ambiguous - won't work
builder.AddRoute("backup {source?} {dest?}", handler)

// ✅ Clear separation
builder.AddRoute("backup {source?} to {dest?}", handler)
```

### Catch-all Parameters
**Definition**: [**Parameters**](#parameters) prefixed with `*` that capture all remaining arguments as an array. Must be the final parameter in a route pattern.

**Examples**:
```csharp
builder.AddRoute("docker {*args}", (string[] args) => {
    // Captures all arguments after "docker"
})

// Command: myapp docker run -it --rm ubuntu bash
// args = ["run", "-it", "--rm", "ubuntu", "bash"]
```

### Required Parameters
**Definition**: Positional [**parameters**](#parameters) that must be provided - the absence of a required parameter causes a parsing error. This is the default behavior for all parameters without `?`.

**Examples**:
```csharp
builder.AddRoute("deploy {env}", (string env) => {})  // env is required

// ✓ Valid
myapp deploy production          // env="production"

// ❌ Error: missing required parameter
myapp deploy                     // Throws exception
```

---

## 🎯 Execution Approaches

### Direct Approach
**Definition**: Command handlers implemented as simple delegate functions taking strongly-typed parameters. This approach provides maximum performance with minimal overhead, ideal for simple commands.

**Characteristics**:
- Zero allocation overhead
- Automatic parameter binding
- Synchronous execution by default
- Async support available (Task/Task<T>)

**Examples**:
```csharp
// Synchronous Direct Approach
builder.AddRoute("ping {count:int}",
    (int count) => {
        for(int i = 0; i < count; i++)
            Console.WriteLine("pong");
    });

// Asynchronous Direct Approach
builder.AddRoute("add {x:double} {y:double}",
    async (double x, double y) => {
        await Task.Delay(10); // Async work
        Console.WriteLine($"{x} + {y} = {x + y}");
    });

// Returning values
builder.AddRoute("calculate {a:double} {b:double}",
    (double a, double b) => a + b); // Returns result to stdout
```

**See Also**: [**Mediator Approach**](#mediator-approach), [**Mixed Approach**](#mixed-approach)

### Mediator Approach
**Definition**: Command handlers structured using the Mediator pattern with dedicated command classes and handler implementations. Enables complex business logic with dependency injection.

**Components**:

1. **Command Classes** - Inherit from `IRequest` or `IRequest<TResponse>`
2. **Handler Classes** - Implement `IRequestHandler<TCommand>` or `IRequestHandler<TCommand,TResponse>`
3. **Nested Handler Pattern** - Handler classes nested inside command classes for organization
4. **Dependency Injection Support** - Automatic injection of services

**Examples**:

```csharp
// Synchronous Mediator Command
public class ComputeCommand : IRequest<double>
{
    public double X { get; set; }
    public double Y { get; set; }

    public class Handler : IRequestHandler<ComputeCommand, double>
    {
        public Task<double> Handle(ComputeCommand cmd, CancellationToken ct)
            => Task.FromResult(cmd.X + cmd.Y);
    }
}

// Async Mediator Command
public class FetchDataCommand : IRequest<string>
{
    public string Url { get; set; }

    public class Handler(HttpClient http) : IRequestHandler<FetchDataCommand, string>
    {
        public async Task<string> Handle(FetchDataCommand cmd, CancellationToken ct)
        {
            var response = await http.GetAsync(cmd.Url, ct);
            return await response.Content.ReadAsStringAsync(ct);
        }
    }
}

// Registration with DI
builder.AddDependencyInjection()
       .Services.AddHttpClient()
       .AddRoute<ComputeCommand>("add {x:double} {y:double}")
       .AddRoute<FetchDataCommand>("fetch {url}");

// Alternative registration for commands without response
public class StatusCommand : IRequest
{
    public class Handler : IRequestHandler<StatusCommand>
    {
        public Task Handle(StatusCommand cmd, CancellationToken ct)
        {
            Console.WriteLine("System OK");
            return Task.CompletedTask;
        }
    }
}
```

**See Also**: [**Direct Approach**](#direct-approach), [**Mixed Approach**](#mixed-approach)

### Mixed Approach
**Definition**: Combining both Direct and Mediator approaches within the same application. Use [**Direct Approach**](#direct-approach) for high-performance simple commands and [**Mediator Approach**](#mediator-approach) for complex operations requiring dependency injection.

**Examples**:
```csharp
NuruAppBuilder builder = new();

// Simple commands: Direct Approach (fast)
builder.AddRoute("ping", () => Console.WriteLine("pong"))
       .AddRoute("status", () => Console.WriteLine("OK"));

// Enable DI for complex commands
builder.AddDependencyInjection();
builder.Services.AddScoped<ICalculator, Calculator>();

// Complex commands: Mediator Approach (structured)
builder.AddRoute<FactorialCommand>("factorial {n:int}")
       .AddRoute<DeployCommand>("deploy {env} --version {version?}");
```

**Benefits**:
- Optimal performance where needed
- Testable complex logic with [**Direct Approach**](#direct-approach)
- Single application deployment
- Clear separation between simple and complex commands

---

## 📊 Type System Terms

### Type Constraints
**Definition**: Type annotations on [**parameters**](#parameters) that specify the expected .NET type. Constraints enable automatic [**type conversion**](#type-conversion) from string arguments to strongly-typed objects.

**Supported Types**:
- `string` (default if no type specified)
- `int` (System.Int32)
- `long` (System.Int64)
- `double`, `float`, `decimal` (floating-point numbers)
- `bool` (boolean with flexible parsing: true/false, yes/no, 1/0)
- `DateTime` (datetime parsing)
- `Guid` (GUID parsing)
- `Uri` (URI validation)

**Examples**:
```csharp
builder
    .AddRoute("create {name}", (string name) => {})                    // Default string
    .AddRoute("wait {ms:int}", (int milliseconds) => {})              // Integer
    .AddRoute("price {cost:decimal}", (decimal cost) => {})           // Decimal
    .AddRoute("enabled {flag:bool}", (bool flag) => {})               // Boolean
    .AddRoute("schedule {when:DateTime}", (DateTime when) => {})      // DateTime
    .AddRoute("process {id:Guid}", (Guid id) => {})                   // GUID
    .AddRoute("download {url:Uri}", (Uri url) => {});                 // URI
```

### Type Conversion
**Definition**: The automatic process of converting string [**arguments**](#arguments) to strongly-typed .NET objects based on parameter [**type constraints**](#type-constraints). Built-in converters handle common types with error handling.

**Error Handling**: Invalid conversions throw `InvalidOperationException` with descriptive error messages.

### Type Converter Registry
**Definition**: Central registry for managing [**type converters**](#type-conversion) that handle string-to-type conversion. Supports both built-in and custom converters through the `ITypeConverterRegistry` interface.

**Custom Converters**:
```csharp
// Register custom converter
builder.AddTypeConverter(new MyCustomConverter());

// Example custom converter
public class MyCustomConverter : IRouteTypeConverter
{
    public string ConstraintName => "rgb";  // {color:rgb}
    public Type TargetType => typeof(Color);

    public bool TryConvert(string value, Type targetType, out object? result)
    {
        if (targetType != typeof(Color)) { result = null; return false; }

        // Parse "FF0000" format to Color object
        result = Color.FromHex(value);
        return true;
    }
}
```

### Catch-all Array Types
**Definition**: When using [**catch-all parameters**](#catch-all-parameters) with [**type constraints**](#type-constraints), the framework supports typed arrays for element conversion.

**Examples**:
```csharp
// Automatic string array (default)
builder.AddRoute("copy {*files}", (string[] files) => {})

// Typed arrays
builder.AddRoute("sum {*numbers:int}", (int[] numbers) => {})
        .AddRoute("average {*values:double}", (double[] values) => {})

// Usage:
myapp copy *.txt                  // files = ["file1.txt", "file2.txt"]
myapp sum 1 2 3                   // numbers = [1, 2, 3]
myapp average 1.5 2.3 4.1         // values = [1.5, 2.3, 4.1]
```

---

## 🛠️ Framework Architecture Terms

### Compiled Route
**Definition**: Runtime representation of a parsed [**route pattern**](#route-pattern) containing all matching logic and metadata. Created during route registration and cached for performance.

**Key Components**:
- **Positional Matchers**: Ordered segments (literals and parameters) that must be matched before any options
- **Option Matchers**: Required options that must be present
- **Catch-all Parameter Name**: Name of the catch-all parameter if present
- **Specificity Score**: Ranking for route matching order

### Route Matcher
**Definition**: Base interface for components that match portions of route patterns against command [**arguments**](#arguments). Abstract base class implemented by specialized matchers.

**Hierarchy**:
- `RouteMatcher` (abstract base)
  - `LiteralMatcher` - Matches exact text
  - `ParameterMatcher` - Captures parameter values
  - `OptionMatcher` - Handles option parsing

### Endpoint Collection
**Definition**: Thread-safe repository of all registered route endpoints. Provides specificity-based ordering and facilitates [**route resolution**](#command-resolver) during command processing.

**Methods**:
- `Add(RouteEndpoint)` - Register new endpoint
- `IEnumerator<RouteEndpoint>` - Iterate endpoints in specificity order

### Route Endpoint
**Definition**: Individual registered route containing the pattern, compiled form, handler, and metadata. Acts as the bridge between route definition and [**command execution**](#execution).

**Properties**:
```csharp
public class RouteEndpoint
{
    public required string RoutePattern { get; set; }           // e.g., "deploy {env} --version {tag}"
    public required CompiledRoute CompiledRoute { get; set; }   // Runtime matcher
    public Delegate? Handler { get; set; }                      // Direct approach handler
    public Type? CommandType { get; set; }                      // Mediator approach type
    public string? Description { get; set; }                    // Help text
    public int Order { get; set; }                              // Specificity-based ordering
}
```

### Command Resolver
**Definition**: Component responsible for matching command-line [**arguments**](#arguments) against registered routes and extracting parameter values. Implements the routing logic that powers the framework.

**Key Method**:
```csharp
public static ResolverResult Resolve(string[] args, EndpointCollection endpoints,
                                   ITypeConverterRegistry converters, ILogger logger)
```

### Parameter Binder
**Definition**: Component that maps extracted string values to strongly-typed method parameters, handling both Direct delegates and Mediator commands with dependency injection.

**Two Binder Types**:
- **Direct Binder**: Maps arguments to method parameters
- **DI Binder**: Includes dependency injection for service parameters

---

## 📋 Command Organization Terms

### Commands
**Definition**: Action verbs that constitute the primary functions of your CLI. Commands appear first in route patterns and define the type of operation being performed.

**Examples**:
```csharp
// Basic commands
builder.AddRoute("build", () => {})
       .AddRoute("test", () => {})
       .AddRoute("deploy", () => {})
       .AddRoute("status", () => {})
       .AddRoute("clean", () => {});
```

**Implementation Styles**:
- **Simple Commands**: Use [**Direct Approach**](#direct-approach)
- **Complex Commands**: Use [**Mediator Approach**](#mediator-approach)
- **Grouped Commands**: Use command prefixes for organization

### Subcommands
**Definition**: Hierarchical grouping of related commands. Subcommands create namespaces for related functionality, making CLIs more organized and discoverable.

**Examples**:
```csharp
// Git-style subcommands
builder.AddRoute("git status", () => {})
       .AddRoute("git commit", () => {})
       .AddRoute("git push", () => {})
       .AddRoute("git pull", () => {})

// Docker-style subcommands
builder.AddRoute("docker build", () => {})
       .AddRoute("docker run", () => {})
       .AddRoute("docker compose", () => {});

// Command prefixes for logical grouping
const string gitPrefix = "git";
builder.AddRoute($"{gitPrefix} status", handler)
       .AddRoute($"{gitPrefix} commit", handler);
```

**Benefits**:
- **Organization**: Related commands grouped together
- **Discovery**: Help system can show command groups
- **Modularity**: Commands can be developed in separate modules
- **Readability**: Intent is clearer with explicit grouping

### Help Text
**Definition**: Human-readable descriptions and usage examples generated automatically from [**route patterns**](#route-pattern). Help can be generated globally or per-command.

**Features**:
- Automatic parameter descriptions from pipe syntax
- Type information display
- Optional/required parameter indicators
- Short alias information

### Auto Help
**Definition**: Framework feature that automatically registers `--help` routes for all commands, providing consistent help experience without manual implementation.

**Examples**:
```csharp
// Enable automatic help generation
builder.AddAutoHelp();

// Automatically creates:
// --help                                    (global help)
// git --help                             (git command group help)
// git status --help                      (specific command help)
// git commit --help                      (specific command help)

// Command-specific help includes parameter descriptions
builder.AddRoute("deploy {env|Deployment environment} --version {ver|Version tag} --dry-run,-d|Preview only",
    (string env, string ver, bool dry) => {});
```

---

## 🎨 Advanced Features

### Descriptions
**Definition**: Descriptive text attached to routes or parameters using pipe `|` syntax. Descriptions appear in automatically generated [**help text**](#help-text) and improve usability.

**Examples**:
```csharp
// Parameter descriptions
builder.AddRoute("deploy {env|Target environment (dev, staging, prod)}", handler)

// Option descriptions
builder.AddRoute("build --verbose|Enable debug output", handler)

// Short alias descriptions
builder.AddRoute("test --output,-o {path|Output file path}", handler)

// Complex descriptions
builder.AddRoute("backup {source|Source directory or file} " +
                "--compress,-c|Compress archived file " +
                "--output,-o {path|Backup file location} " +
                "--exclude,-e {patterns|File patterns to exclude}",
    (string source, string path, string[] patterns, bool compress) => {});
```

### Specificity Scoring
**Definition**: Algorithm that ranks routes by how specific they are for optimal matching order. Higher specificity routes are tried first to avoid ambiguous matches.

**Scoring Factors** (in order of precedence):
1. **Literal Segments**: +15 points (most specific)
2. **Required Parameters**: +2 points
3. **Option Parameters**: +5 points
4. **Required Options**: +10 points
5. **Optional Elements**: 0 points (neutral)
6. **Catch-all Parameters**: -20 points (least specific)

**Examples**:
```csharp
// Highest specificity (31 points)
builder.AddRoute("git commit --amend", handler)

// Medium specificity (17 points)
builder.AddRoute("git commit {message}", handler)

// Lowest specificity (-3 points)
builder.AddRoute("git {*args}", handler)
```

### Dependency Injection (DI)
**Definition**: Framework feature allowing services to be injected into command handlers. Enables complex business logic with proper separation of concerns using the [**Mediator Approach**](#mediator-approach).

**Setup**:
```csharp
// Enable DI
builder.AddDependencyInjection();

// Register services
builder.Services
    .AddSingleton<ILogger, ConsoleLogger>()
    .AddHttpClient()
    .AddScoped<IValidationService, ValidationService>();

// Inject into handlers
public class DeployCommand : IRequest
{
    public string Environment { get; set; }

    public class Handler(ILogger logger, HttpClient http, IValidationService validator) 
        : IRequestHandler<DeployCommand>
    {
        public async Task Handle(DeployCommand cmd, CancellationToken ct)
        {
            logger.LogInformation("Deploying to {Environment}", cmd.Environment);
            // Use injected services...
        }
    }
}
```

---

## 📚 Cross-Reference Index

### Syntax to Semantics
- [Route Pattern](#route-pattern) → [Compiled Route](#compiled-route)
- [Parameters](#parameters) → [Arguments](#arguments)
- [Options](#options) → [Option Matchers](#route-matcher)

### Execution Models
- [Direct Approach](#direct-approach) → Simple delegates, maximum performance
- [Mediator Approach](#mediator-approach) → Complex commands with DI
- [Mixed Approach](#mixed-approach) → Best of both worlds

### Parameter Types
- [Literal Segments](#literal-segments) → Exact text matching
- [Required Parameters](#required-parameters) → Must be provided
- [Optional Parameters](#optional-parameters) → Can be omitted
- [Catch-all Parameters](#catch-all-parameters) → Remaining arguments array

### Related Concepts
- [Type Constraints](#type-constraints) → [Type Conversion](#type-conversion) → [Type Converter Registry](#type-converter-registry)
- [Command Resolver](#command-resolver) → [Parameter Binder](#parameter-binder) → Execution
- [Specificity Scoring](#specificity-scoring) → Route ordering → Resolution logic

---

## 📖 Additional Resources

- **[Route Pattern Syntax Guide](RoutePatternSyntax.md)** - Complete syntax reference
- **[C# Coding Standards](CsharpCodingStandards.md)** - Project coding conventions
- **[Architecture Documentation](ParserClassesSyntaxVsSemantics.md)** - System design details
- **[Roslynator Rules](RoslynatorRules.md)** - Code analysis rules reference

---

*This glossary provides the comprehensive reference for TimeWarp.Nuru terminology. Terms are organized by functional categories with cross-references to related concepts for easy navigation.*