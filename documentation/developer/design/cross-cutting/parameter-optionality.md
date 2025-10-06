# Optional Options Design - Nuru Implementation

> **Note**: This is the **authoritative source** for option modifier syntax and semantics. The [Syntax Rules](../parser/syntax-rules.md) document provides a quick reference but defers to this document for complete details.
>
> **See Also**: [Route Pattern Anatomy](../parser/route-pattern-anatomy.md) - Comprehensive terminology and visual breakdown of all pattern syntax elements

## Overview

This document defines how Nuru handles required vs optional options based on parameter nullability, supporting gradual refactoring scenarios where custom handlers progressively replace shell passthrough.

## Core Design Principle

Options are **required** or **optional** based on their parameter nullability:
- **Non-nullable parameter** (`{mode}`) = **Required option** (must provide flag + value)
- **Nullable parameter** (`{mode?}`) = **Optional option** (entire flag + value is optional)
- **Boolean options** remain inherently optional (presence = true, absence = false)

## Requirements Coverage Table

| Behavior | Nuru Syntax | Valid Invocations | Invalid Invocations | Handler Signature | Binding Logic | Refactoring Use Case |
|----------|-------------|-------------------|---------------------|-------------------|---------------|----------------------|
| **Required flag + Required value** | `process --xyz {level}` | `process --xyz debug`<br>`process --xyz info` | `process --xyz` (missing value)<br>`process` (missing flag) | `(string level) => ...` | `level = "debug"` | **Base intercept pattern**: Validate known values, log, then shell passthrough for legacy behavior |
| **Required flag + Optional value** | `process --xyz {level?}` | `process --xyz verbose`<br>`process --xyz` | `process` (missing flag) | `(string? level) => ...` | `level = "verbose" ?? null` | **Mode detection**: Flag must exist but value optional - perfect for `--verbose` with optional level |
| **Optional flag + Optional value** | `process --xyz? {level?}` | `process`<br>`process --xyz`<br>`process --xyz debug` | None (all valid) | `(string? level) => ...` | `level = null` or value | **Progressive override**: Start logging usage, gradually add custom handling |
| **Optional flag + Required value** | `process --xyz? {level}` | `process`<br>`process --xyz debug` | `process --xyz` (flag present but missing value) | `(string? level) => ...` | `level = null` or `"debug"` | **Conditional enhancement**: Only intercept when flag provided WITH value |
| **Pure Boolean flag (optional)** | `process --dry-run` | `process`<br>`process --dry-run` | None | `(bool dryRun) => ...` | `dryRun = true/false` | **Safe mode toggle**: Intercept for simulation, passthrough for real execution |

## Key Implementation Points

### Current Nuru Behavior (What Needs Changing)

```csharp
// CURRENT: All options in pattern are REQUIRED
.AddRoute("deploy --env {env} --force", (string env, bool force) => ...)
// Must provide BOTH --env and --force for route to match
```

### Proposed Nuru Behavior (With Our Change)

```csharp
// NEW: Nullability determines optionality
.AddRoute("deploy --env {env} --force", (string env, bool force) => ...)
// --env is REQUIRED (non-nullable string)
// --force is OPTIONAL (booleans always optional)

.AddRoute("deploy --env {env?} --config {cfg?}", (string? env, string? cfg) => ...)
// Both --env and --config are OPTIONAL (nullable parameters)
```

## Gradual Refactoring Pattern

The design explicitly supports incremental migration from shell commands to native implementations:

### Phase 1: Catch-all Passthrough
```csharp
// Current shell behavior preserved
.AddRoute("{cmd} {*args}", (string cmd, string[] args) =>
    Shell.Run(cmd, args))
```

### Phase 2: Intercept for Logging/Monitoring
```csharp
// Add observability without changing behavior
.AddRoute("deploy --env {env}", (string env, NuruContext ctx) =>
{
    Log($"Deploy to {env} with args: {ctx.OriginalArgs}");
    Shell.Run("deploy", ctx.OriginalArgs); // Passthrough
})
```

### Phase 3: Add Validation/Enhancement
```csharp
// Validate inputs, add safety checks
.AddRoute("deploy --env {env} --dry-run", (string env, bool dryRun) =>
{
    if (!ValidateEnv(env)) return Error();
    if (dryRun) return Simulate(env);
    Shell.Run("deploy", "--env", env);
})
```

### Phase 4: Full Native Implementation
```csharp
// Complete replacement, no shell dependency
.AddRoute("deploy --env {env} --config {cfg?}", (string env, string? cfg) =>
{
    var config = cfg ?? "default.json";
    return DeployService.Execute(env, config); // No shell
})
```

## Syntax Decisions

### Why No `!` Modifier?

- The **absence** of `?` already means "required" in C#/.NET conventions
- Keeps syntax minimal and familiar to C# developers
- `{param}` = required, `{param?}` = optional (matches C# nullable syntax)

### Note on Required Options

While "required option" may sound like an oxymoron, it's a common pattern in CLI tools:
- `git commit -m` - The `-m` flag is effectively required
- `docker run -it` - Often required for interactive sessions
- `kubectl apply -f` - File flag is required for apply

By supporting both required and optional options, Nuru handles real-world CLI patterns.

## NuruContext for Advanced Scenarios

When you need access to the full parsing context beyond declared parameters:

```csharp
// Check for undeclared options
.AddRoute("git commit", (NuruContext ctx) =>
{
    // Check for flags not in pattern
    if (ctx.HasOption("--amend"))
        return GitService.Amend(ctx.Options);

    // Passthrough with modification
    var args = ctx.OriginalArgs.ToList();
    args.Add("--no-verify"); // Add safety flag
    Shell.Run("git", args);
})
```

### NuruContext API

```csharp
public class NuruContext
{
    public string[] OriginalArgs { get; }
    public IReadOnlyDictionary<string, object?> Options { get; }
    public IReadOnlyList<string> PositionalArgs { get; }
    public IReadOnlyList<string> UnmatchedArgs { get; }

    public T? GetOption<T>(string name);
    public bool HasOption(string name);
    public string? GetPositional(int index);
}
```

## Implementation Approach

### Route Matching Changes

1. **OptionMatcher** gets new `IsOptional` property based on parameter nullability
2. **RouteBasedCommandResolver** checks `IsOptional` when matching
3. **RouteCompiler** sets `IsOptional` from `ParameterSyntax.IsOptional`

### Parameter Binding

- Required options missing = route doesn't match
- Optional options missing = bind null to nullable parameters
- Non-nullable parameters for missing options = route doesn't match (prevents runtime errors)

### Analyzer Support

New analyzer rule **NURU010** will warn when:
- Optional option has non-nullable parameter (potential runtime error)
- Suggests making parameter nullable for optional options

## Benefits

1. **No Factorial Explosion**: Single route handles all optional combinations
2. **Gradual Refactoring**: Progressive enhancement from shell to native
3. **Type Safety**: Compile-time checking via nullability
4. **Industry Standard**: Aligns with Click, Cobra, and other CLI frameworks
5. **AOT Compatible**: No reflection, all compile-time resolved

## Migration Path

### For Existing Code

Routes that currently require all options will continue to work but with different matching behavior:

```csharp
// Old behavior: Both options required
.AddRoute("build --config {mode} --verbose", (string mode, bool verbose) => ...)

// New behavior:
// - --config is REQUIRED (non-nullable string)
// - --verbose is OPTIONAL (booleans always optional)

// To make --config optional, change to:
.AddRoute("build --config {mode?} --verbose", (string? mode, bool verbose) => ...)
```

### Feature Flag (Optional)

For safer migration, consider a feature flag:

```csharp
new NuruAppBuilder()
    .UseOptionalOptions()  // Opt-in to new behavior
    .AddRoute(...)
```

## Success Criteria

- [x] Required options with non-nullable parameters must be present
- [x] Optional options with nullable parameters can be omitted
- [x] Single route handles all combinations of optional options
- [x] NuruContext provides access to full parse results
- [x] Clear compile-time warnings for potential issues
- [x] Gradual refactoring from shell to native is supported