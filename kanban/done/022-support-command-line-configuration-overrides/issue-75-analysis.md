# Issue #75 Analysis: Command-Line Configuration Override Support

**Issue:** [#75 - How to add a route to override an appsetting on the command line?](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/75)
**Author:** Stuart Grassie (@sgrassie)
**Status:** OPEN
**Date:** 2025-11-06

---

## Problem Statement

The user wants to replicate ASP.NET Core's command-line configuration override behavior in TimeWarp.Nuru:

```bash
# ASP.NET Core style
dotnet run --FooOptions:Url=https://override.example.com
```

With appsettings.json:
```json
{
  "FooOptions": {
    "Url": "https://default.example.com",
    "MaxItems": 10
  }
}
```

And ASP.NET setup:
```csharp
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddCommandLine(args);   // command line overrides JSON
```

**Question:** How to add a Nuru route that maps to this pattern?

---

## Current State Analysis

### ✅ Good News: This Already Works!

TimeWarp.Nuru **already supports** configuration overrides via command line using the exact same mechanism as ASP.NET Core.

**Evidence from codebase:**

`Source/TimeWarp.Nuru/NuruAppBuilder.cs` (lines 106-152):
```csharp
public NuruAppBuilder AddConfiguration(string[]? args = null)
{
    // ... environment and base path setup ...

    IConfigurationBuilder configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
      .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
      // ... more JSON sources ...
      .AddEnvironmentVariables();

    if (args?.Length > 0)
    {
      configuration.AddCommandLine(args);  // ← Same as ASP.NET!
    }

    IConfigurationRoot configurationRoot = configuration.Build();
    ServiceCollection.AddSingleton<IConfiguration>(configurationRoot);

    return this;
}
```

### Supported Configuration Override Formats

Because Nuru uses `Microsoft.Extensions.Configuration.CommandLine`, it supports:

```bash
# Hierarchical keys with colon separator
myapp --FooOptions:Url=https://example.com --FooOptions:Timeout=30

# Space separator
myapp --FooOptions:Url https://example.com

# Forward slash prefix (Windows style)
myapp /FooOptions:Url=https://example.com
```

---

## The Confusion: Two Separate Argument Systems

### System 1: Nuru's Routing System (Pattern Matching)

**Purpose:** Match command patterns and extract route-specific parameters

**Example:**
```csharp
.AddRoute("deploy {env} --dry-run --force", (string env, bool dryRun, bool force) =>
{
    Console.WriteLine($"Deploying to {env}");
})
```

**Usage:**
```bash
myapp deploy production --dry-run --force
```

**How it works:**
- `EndpointResolver` matches the pattern against args
- Extracts: `env="production"`, `dryRun=true`, `force=true`
- Binds to delegate parameters
- Does NOT affect IConfiguration

### System 2: Configuration System (Settings Management)

**Purpose:** Provide app-wide configuration from multiple sources

**Example:**
```csharp
.AddConfiguration(args)
.ConfigureServices((services, config) =>
{
    services.Configure<FooOptions>(config.GetSection("FooOptions"));
})
.AddRoute("run", (IOptions<FooOptions> options) =>
{
    var foo = options.Value;
    Console.WriteLine($"URL: {foo.Url}");
})
```

**Usage:**
```bash
myapp run --FooOptions:Url=https://override.example.com
```

**How it works:**
- `AddCommandLine(args)` processes ALL args passed to `AddConfiguration()`
- Overrides configuration values from JSON/environment
- Available via `IConfiguration` or `IOptions<T>`
- Does NOT participate in route matching

### ⚠️ The Challenge: Args Are Shared

Currently, the same `string[] args` is used by BOTH systems:
1. Passed to `AddConfiguration(args)` → becomes configuration overrides
2. Used by `EndpointResolver.Resolve(args)` → matches route patterns

**This creates potential conflicts:**

```bash
# What if you have a route option AND want to override config?
myapp deploy staging --force --Database:Host=staging-db.com
```

- `--force` might be a route option in the pattern
- `--Database:Host=staging-db.com` might be a config override
- Both are in the same args array

---

## Solution Approaches

### Option A: Use Configuration System Only (Recommended for Issue #75)

**For the specific scenario in issue #75**, the user doesn't need routing at all!

**Full working example:**

```csharp
#!/usr/bin/dotnet --
#:package TimeWarp.Nuru@1.0.0-beta.6
#:property UserSecretsId=12345678-1234-1234-1234-123456789abc

using Microsoft.Extensions.Options;
using TimeWarp.Nuru;

public class FooOptions
{
    public string Url { get; set; } = string.Empty;
    public int MaxItems { get; set; }
}

return await NuruApp.CreateBuilder(args)
    .EnableDependencyInjection()
    .AddConfiguration(args)  // ← Enables config system with command-line support
    .ConfigureServices((services, config) =>
    {
        // Bind configuration section to strongly-typed options
        services.Configure<FooOptions>(config.GetSection("FooOptions"));
    })
    .AddRoute("run", (IOptions<FooOptions> options) =>
    {
        var foo = options.Value;
        Console.WriteLine($"URL: {foo.Url}");
        Console.WriteLine($"MaxItems: {foo.MaxItems}");
        return Task.CompletedTask;
    })
    .Build()
    .RunAsync();
```

**appsettings.json:**
```json
{
  "FooOptions": {
    "Url": "https://default.example.com",
    "MaxItems": 10
  }
}
```

**Usage:**
```bash
# Use defaults from appsettings.json
./myapp.cs run

# Override via command line
./myapp.cs run --FooOptions:Url=https://override.example.com

# Override multiple values
./myapp.cs run --FooOptions:Url=https://override.example.com --FooOptions:MaxItems=20
```

**Why this works:**
1. `AddConfiguration(args)` passes args to `AddCommandLine(args)`
2. CommandLine provider has higher precedence than JSON files
3. Configuration system automatically merges all sources
4. `IOptions<FooOptions>` gets the merged, overridden values
5. Route pattern is simple (`"run"`) - no options that could conflict

### Option B: Separate Route Options from Config Overrides

**For complex scenarios with both routing options AND config overrides:**

```csharp
.AddRoute("deploy {env} --dry-run --verbose", async (
    string env,
    bool dryRun,
    bool verbose,
    IConfiguration config) =>  // ← Still get config access
{
    // Route options from pattern
    Console.WriteLine($"Deploying to {env}");
    Console.WriteLine($"Dry run: {dryRun}");
    Console.WriteLine($"Verbose: {verbose}");

    // Config values (potentially overridden via --Key:Value)
    var dbHost = config["Database:Host"];
    Console.WriteLine($"Database: {dbHost}");

    return Task.CompletedTask;
})
```

**Usage:**
```bash
# Route options + config override
./myapp.cs deploy staging --dry-run --Database:Host=staging-db.com
```

**How it works:**
- `--dry-run` matches route pattern → extracted as `dryRun=true`
- `--Database:Host=staging-db.com` doesn't match pattern → passed through to config system
- Both are available to the handler

**⚠️ Current Limitation:**
The route resolver currently treats ALL `--key=value` args as potential route options. Args that don't match the pattern are currently ignored, not passed to the configuration system separately.

### Option C: Explicit Configuration-Only Routes

**Pattern:** Create routes specifically for configuration management:

```csharp
.AddRoute("config set {key} {value}", (string key, string value, IConfiguration config) =>
{
    // Update configuration programmatically
    // (Would require IConfigurationRoot and writable source)
    Console.WriteLine($"Setting {key}={value}");
    return Task.CompletedTask;
})
.AddRoute("config get {key}", (string key, IConfiguration config) =>
{
    var value = config[key];
    Console.WriteLine($"{key}={value}");
    return Task.CompletedTask;
})
```

**Usage:**
```bash
./myapp.cs config set FooOptions:Url https://example.com
./myapp.cs config get FooOptions:Url
```

---

## Existing Examples in Codebase

### 1. Configuration Basics Sample

**Location:** `samples/configuration/configuration-basics.cs`

Demonstrates:
- Both `ConfigureServices` overloads (with and without config)
- Strongly-typed options with `IOptions<T>`
- Conditional service registration based on configuration
- AOT-compatible configuration binding

### 2. Configuration Validation Sample

**Location:** `samples/configuration/configuration-validation.cs`

Demonstrates:
- DataAnnotations validation
- Custom validation logic
- FluentValidation integration
- `ValidateOnStart()` for fail-fast behavior

### 3. UserSecretsDemo Sample

**Location:** `samples/configuration/UserSecretsDemo/`

Full csproj-based project demonstrating:
- User secrets integration
- IConfiguration injection into route handlers
- Environment-based configuration

### 4. User Secrets Property Sample

**Location:** `samples/configuration/user-secrets-property.cs`

Demonstrates:
- UserSecretsId via MSBuild property directive:
  ```csharp
  #:property UserSecretsId=your-guid-here
  ```

---

## Recommended Answer for Issue #75

**Direct answer to "How do I add a route which maps to this in Nuru?"**

You don't need to map it in the route! The configuration override happens automatically when you use `AddConfiguration(args)`.

**Complete example:**

```csharp
#!/usr/bin/dotnet --
#:package TimeWarp.Nuru@1.0.0-beta.6

using Microsoft.Extensions.Options;
using TimeWarp.Nuru;

public class FooOptions
{
    public string Url { get; set; } = string.Empty;
    public int MaxItems { get; set; }
}

return await NuruApp.CreateBuilder(args)
    .EnableDependencyInjection()
    .AddConfiguration(args)  // ← This is the key!
    .ConfigureServices((services, config) =>
    {
        services.Configure<FooOptions>(config.GetSection("FooOptions"));
    })
    .AddRoute("start", (IOptions<FooOptions> options) =>
    {
        Console.WriteLine($"Starting with URL: {options.Value.Url}");
        Console.WriteLine($"Max items: {options.Value.MaxItems}");
        return Task.CompletedTask;
    })
    .Build()
    .RunAsync();
```

**Run it:**
```bash
# Use appsettings.json defaults
./app.cs start

# Override URL
./app.cs start --FooOptions:Url=https://override.example.com

# Override multiple settings
./app.cs start --FooOptions:Url=https://override.example.com --FooOptions:MaxItems=50
```

**Key points:**
1. `AddConfiguration(args)` enables the Microsoft.Extensions.Configuration system
2. It automatically adds the CommandLine provider with the args
3. CommandLine overrides have higher precedence than JSON files
4. Access configuration via `IConfiguration` or `IOptions<T>` injection
5. The route pattern (`"start"`) doesn't need to mention the config keys

---

## Architectural Considerations

### Current Design Philosophy

Nuru treats routing and configuration as **separate concerns**:

1. **Routing** - command structure and handler selection
   - Defined via route patterns
   - Extracts positional parameters and named options
   - Binds to delegate parameters

2. **Configuration** - application settings management
   - Defined via JSON files, environment, and command line
   - Hierarchical key-value structure
   - Accessed via `IConfiguration` or `IOptions<T>`

### Potential Enhancement: Argument Segregation

If explicit separation is desired, the API could support:

```csharp
.AddConfiguration(args, consumeOnlyUnmatchedArgs: true)
```

This would:
1. Let routing extract pattern-defined options first
2. Pass remaining args (like `--Key:Value`) to configuration system
3. Provide clearer separation between route options and config overrides

**Trade-offs:**
- ✅ Pro: Clearer mental model - route options vs config overrides
- ✅ Pro: No conflicts between route options and config keys
- ❌ Con: More complex API surface
- ❌ Con: Two-pass argument processing (performance impact)
- ❌ Con: May break existing code that relies on current behavior

---

## Conclusion

**For Issue #75:** No code changes needed! The functionality already exists via `AddConfiguration(args)`.

**Documentation Gap:** The issue reveals that this capability isn't well-documented. Consider:
1. Adding a sample showing config override via command line
2. Updating README with configuration override examples
3. Adding documentation about the relationship between routing args and config args

**Potential Future Enhancement:** Consider explicit argument segregation if user confusion continues, but current design is flexible and follows Microsoft.Extensions.Configuration conventions.

---

## References

- NuruAppBuilder.cs: Lines 106-152 (AddConfiguration method)
- samples/configuration/configuration-basics.cs
- samples/configuration/configuration-validation.cs
- samples/configuration/UserSecretsDemo/
- Microsoft.Extensions.Configuration.CommandLine documentation
