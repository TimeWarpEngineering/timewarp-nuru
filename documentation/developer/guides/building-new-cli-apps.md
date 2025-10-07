# Building New CLI Apps - Best Practices

**Guide for developers building new command-line applications using Nuru.**

## Core Principle

The route pattern is self-contained and fully defines matching behavior. The delegate signature must match what the route provides, but does NOT influence route matching.

## Syntax Elements

### Positional Parameters
- `{param}` - Required positional parameter
- `{param?}` - Optional positional parameter

### Options (Flags with Values)
- `--flag {value}` - Required flag with required value
- `--flag {value?}` - Required flag with optional value
- `--flag? {value}` - Optional flag, but if present requires value
- `--flag? {value?}` - Optional flag with optional value

### Boolean Options
- `--flag` - Optional boolean flag (always optional, true if present)

## Greenfield Application: Minimal Route Definitions

### Example 1: Build Command with Multiple Options

**Goal**: Single route handles all variations of build command

```csharp
// ONE ROUTE handles all these cases:
.AddRoute("build {project?} --config? {cfg?} --verbose --watch",
    (string? project, string? cfg, bool verbose, bool watch) => ...)

// Matches ALL of these:
build                           // project=null, cfg=null, verbose=false, watch=false
build myapp                     // project="myapp", cfg=null, verbose=false, watch=false
build --verbose                 // project=null, cfg=null, verbose=true, watch=false
build --config                  // project=null, cfg=null, verbose=false, watch=false
build --config debug            // project=null, cfg="debug", verbose=false, watch=false
build myapp --config release --verbose --watch  // All values set
build --watch --config debug myapp              // Order doesn't matter
```

### Example 2: Deploy Command with Mixed Requirements

**Goal**: Some options required, others optional, single route

```csharp
// ONE ROUTE with mixed requirements:
.AddRoute("deploy {env} --version? {ver?} --config {cfg} --dry-run --force",
    (string env, string? ver, string cfg, bool dryRun, bool force) => ...)

// env is REQUIRED (positional)
// --config is REQUIRED (no ? on flag)
// --version is OPTIONAL (? on flag)
// --dry-run and --force are OPTIONAL (booleans always optional)

// Valid:
deploy prod --config prod.json
deploy staging --config stage.json --version v2.0
deploy dev --config dev.json --dry-run --force

// Invalid:
deploy prod                    // Missing required --config
deploy --config prod.json      // Missing required env
```

### Example 3: Database Migration Tool

**Goal**: Progressive options, all handled by one route

```csharp
// ONE ROUTE for all migration scenarios:
.AddRoute("migrate {direction?} --target? {version?} --step? {count:int?} --dry-run",
    (string? direction, string? version, int? count, bool dryRun) =>
    {
        var dir = direction ?? "up";

        if (version != null)
            MigrateTo(version, dryRun);
        else if (count != null)
            MigrateSteps(dir, count.Value, dryRun);
        else
            MigrateLatest(dir, dryRun);
    })

// All handled by ONE route:
migrate                                    // up to latest
migrate down                               // down to zero
migrate --target v3                       // to specific version
migrate up --step 2                       // up 2 migrations
migrate down --step 1 --dry-run          // simulate down 1
migrate --dry-run                         // simulate up to latest
```

## Route Pattern Independence

The route pattern MUST be self-contained:

```csharp
// Route pattern fully defines matching behavior:
"deploy --env? {environment?} --force"

// This pattern means:
// - --env is optional flag
// - if --env is present, environment value is optional
// - --force is optional boolean

// Delegate must accept what route provides:
(string? environment, bool force) => ...

// But delegate signature does NOT affect whether route matches!
```

## Comparison: Multiple Routes vs Single Route

### Old Way: Multiple Routes (Factorial Explosion)

```csharp
// Need 8 routes for 3 optional options!
.AddRoute("test", () => RunTests())
.AddRoute("test --verbose", (bool v) => RunTests(v))
.AddRoute("test --coverage", (bool c) => RunTests(coverage: c))
.AddRoute("test --watch", (bool w) => RunTests(watch: w))
.AddRoute("test --verbose --coverage", (bool v, bool c) => RunTests(v, c))
.AddRoute("test --verbose --watch", (bool v, bool w) => RunTests(v, watch: w))
.AddRoute("test --coverage --watch", (bool c, bool w) => RunTests(coverage: c, watch: w))
.AddRoute("test --verbose --coverage --watch", (bool v, bool c, bool w) => RunTests(v, c, w))
```

### New Way: Single Route

```csharp
// ONE route handles all 8 combinations:
.AddRoute("test --verbose --coverage --watch",
    (bool verbose, bool coverage, bool watch) =>
        RunTests(verbose, coverage, watch))

// Booleans are inherently optional, so this matches all combinations
```

## Flag Modifier Summary

| Pattern | Flag Required | Value Required | Example Match | Example No-Match |
|---------|--------------|----------------|---------------|------------------|
| `--flag {val}` | Yes | Yes | `--flag foo` | `--flag`, ` ` (no flag) |
| `--flag {val?}` | Yes | No | `--flag`, `--flag foo` | ` ` (no flag) |
| `--flag? {val}` | No | Yes (if flag present) | ` `, `--flag foo` | `--flag` (no value) |
| `--flag? {val?}` | No | No | ` `, `--flag`, `--flag foo` | Never |

## Why Explicit Modifiers Matter

Without explicit modifiers in the route pattern:
1. **Ambiguity**: Can't tell if option is required without looking at delegate
2. **Coupling**: Route matching depends on delegate signature
3. **Mediator Pattern Break**: Can't use command objects (no delegate to inspect)
4. **Tooling Issues**: Can't validate routes without analyzing delegates

With explicit modifiers:
1. **Clear Intent**: Route pattern tells complete story
2. **Decoupled**: Route works with any matching delegate/command
3. **Mediator Support**: `AddRoute<Command>("pattern")` works
4. **Better Tooling**: Can analyze/validate routes independently

## Greenfield Best Practices

1. **Start with Optional**: Make flags optional by default with `?`
2. **Minimize Routes**: Use one route with optional flags rather than multiple
3. **Boolean Flags**: Always optional (no modifier needed)
4. **Required Flags**: Only when truly required (like `git commit -m`)
5. **Progressive Values**: Use `{val?}` for flags that can be valueless

### Good Pattern
```csharp
// Single flexible route
.AddRoute("backup {source} --dest? {destination?} --compress --encrypt",
    (string source, string? destination, bool compress, bool encrypt) => ...)
```

### Avoid Pattern
```csharp
// Multiple rigid routes
.AddRoute("backup {source}", ...)
.AddRoute("backup {source} --dest {destination}", ...)
.AddRoute("backup {source} --compress", ...)
.AddRoute("backup {source} --dest {destination} --compress", ...)
// ... exponential explosion
```

## Command Pattern Support

With explicit route patterns, mediator/command pattern works:

```csharp
// Route pattern is self-contained
.AddRoute<BackupCommand>("backup {source} --dest? {destination?} --compress")

// Command properties match route parameters
public class BackupCommand : IRequest
{
    public string Source { get; set; }        // Required (no ? in route)
    public string? Destination { get; set; }  // Optional (? in route)
    public bool Compress { get; set; }        // Optional (boolean)
}
```

The route pattern alone determines matching - the command is just the handler.