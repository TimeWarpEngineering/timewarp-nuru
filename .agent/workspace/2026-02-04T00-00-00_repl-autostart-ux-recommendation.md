# UX Improvement Recommendation: Auto-Start REPL on Empty Arguments

**Date:** 2026-02-04  
**Status:** RECOMMEND IMPLEMENTATION  
**Priority:** Medium  
**Effort:** Low  

---

## Executive Summary

**Yes, we should add a better user experience.** The current workaround (manual args checking) is boilerplate-heavy and not discoverable. Adding `ReplOptions.AutoStartWhenEmpty` would provide a clean, declarative API that matches user expectations from other CLI frameworks.

---

## Current State Analysis

### What Users Must Do Today

```csharp
// Current workaround - 5 lines of boilerplate
string[] effectiveArgs = args.Length == 0 ? ["--interactive"] : args;
return await app.RunAsync(effectiveArgs);
```

Or the even more verbose:

```csharp
if (args.Length == 0)
{
    await app.RunReplAsync();
    return 0;
}
return await app.RunAsync(args);
```

### What's Already Built-In

The framework already supports `--interactive` / `-i` flags (emitted before user routes):

```csharp
// From interceptor-emitter.cs line 781
if (routeArgs is ["--interactive"] or ["-i"])
{
    await RunReplAsync(app).ConfigureAwait(false);
    return 0;
}
```

### The Problem

1. **Not discoverable** - Users don't know about `-i` flag until they read docs
2. **Boilerplate** - Every dual-mode app needs the same if/else
3. **Common pattern** - Many CLI tools auto-start interactive mode when no args provided
4. **User already expected it** - The original error shows users *assume* this exists

---

## Recommendation: Add `AutoStartWhenEmpty` Option

### Proposed API

```csharp
NuruApp app = NuruApp.CreateBuilder()
    .WithDescription("Ganda CLI")
    .Map("status").WithHandler(...).Done()
    .AddRepl(options =>
    {
        options.Prompt = "ganda> ";
        options.AutoStartWhenEmpty = true;  // NEW: Start REPL when no args
    })
    .Build();

// No boilerplate needed - just:
return await app.RunAsync(args);
```

### Usage Patterns

| Scenario | Without Option | With Option |
|----------|---------------|-------------|
| `ganda` (no args) | Shows "Unknown command" | Starts REPL |
| `ganda -i` | Starts REPL | Starts REPL |
| `ganda status` | Runs status command | Runs status command |
| `ganda --help` | Shows help | Shows help |

---

## Implementation Plan

### Files to Modify

| File | Change |
|------|--------|
| `source/timewarp-nuru/options/repl-options.cs` | Add `bool AutoStartWhenEmpty { get; set; } = false;` |
| `source/timewarp-nuru-analyzers/generators/models/repl-model.cs` | Add `bool AutoStartWhenEmpty` parameter |
| `source/timewarp-nuru-analyzers/generators/extractors/repl-extractor.cs` | Extract the new property |
| `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` | Emit check before user routes |

### Generated Code Change

**Current (line 781):**
```csharp
if (routeArgs is ["--interactive"] or ["-i"])
{
    await RunReplAsync(app).ConfigureAwait(false);
    return 0;
}
```

**Proposed:**
```csharp
// Check for --interactive flag OR auto-start when empty (if enabled)
if (routeArgs is ["--interactive"] or ["-i"] || 
    (app.ReplOptions?.AutoStartWhenEmpty == true && routeArgs.Length == 0))
{
    await RunReplAsync(app).ConfigureAwait(false);
    return 0;
}
```

### Why This Location?

The check must be:
1. **Before user routes** - So empty args don't match catch-all routes
2. **After config filtering** - So config args don't count as "empty"
3. **With the existing interactive check** - Logical grouping

---

## Design Considerations

### Why `false` by Default?

```csharp
public bool AutoStartWhenEmpty { get; set; } = false;
```

1. **Backward compatibility** - Existing apps continue working
2. **Explicit opt-in** - Users must consciously enable this behavior
3. **Prevents surprises** - CLI-only apps don't suddenly start REPL

### Naming Options Considered

| Name | Pros | Cons |
|------|------|------|
| `AutoStartWhenEmpty` | Clear, follows .NET naming | Slightly verbose |
| `StartReplWhenNoArguments` | Matches user's mental model | Long, inconsistent casing |
| `DefaultToInteractive` | Good semantic meaning | Could be confusing |
| `InteractiveByDefault` | Clear intent | Sounds like default behavior |

**Recommendation:** `AutoStartWhenEmpty` - clear, consistent with .NET conventions

### Edge Cases Handled

| Case | Behavior |
|------|----------|
| `args = []` | Starts REPL if enabled |
| `args = ["--Config:Key=value"]` | Config arg filtered, then empty → Starts REPL |
| `args = ["--help"]` | Help shown (built-in handled first) |
| `args = ["unknown"]` | "Unknown command" error |
| User has `Map("")` default route | User route takes precedence (if specificity higher) |

---

## Prior Art from Other Frameworks

### GitHub CLI
```bash
gh          # Shows help (doesn't auto-start interactive)
gh -i       # Interactive mode (if available)
```

### AWS CLI
```bash
aws         # Shows help
aws configure  # Interactive configuration
```

### Python Click
```python
@click.command()
@click.option('--interactive', is_flag=True)
def cli(interactive):
    if interactive or len(sys.argv) == 1:
        start_repl()
```

### Recommendation
Most modern CLI tools either:
1. Show help on empty args (safe default)
2. Provide explicit interactive flag
3. Have a config option to auto-start interactive mode

Nuru's approach with `AutoStartWhenEmpty` as opt-in strikes the right balance.

---

## Benefits

| Benefit | Description |
|---------|-------------|
| **Better UX** | Users expect this behavior from modern CLI tools |
| **Less boilerplate** | Single property vs. 5 lines of code |
| **More discoverable** | IntelliSense shows the option |
| **Consistent** | All dual-mode apps use same pattern |
| **Backward compatible** | No breaking changes |

---

## Testing Strategy

```csharp
// Test 1: Auto-start enabled, no args
[Fact]
public async Task AutoStartWhenEmpty_Enabled_NoArgs_StartsRepl()
{
    using var terminal = new TestTerminal();
    var app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("test").WithHandler(() => "executed").Done()
        .AddRepl(o => o.AutoStartWhenEmpty = true)
        .Build();

    await app.RunAsync([]);
    
    terminal.OutputContains("REPL Mode").ShouldBeTrue();
    terminal.OutputContains("executed").ShouldBeFalse();
}

// Test 2: Auto-start disabled, no args
[Fact]
public async Task AutoStartWhenEmpty_Disabled_NoArgs_ShowsUnknownCommand()
{
    using var terminal = new TestTerminal();
    var app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("test").WithHandler(() => "executed").Done()
        .AddRepl(o => o.AutoStartWhenEmpty = false) // default
        .Build();

    int exitCode = await app.RunAsync([]);
    
    exitCode.ShouldBe(1);
    terminal.OutputContains("Unknown command").ShouldBeTrue();
}

// Test 3: Auto-start enabled, has args - runs command
[Fact]
public async Task AutoStartWhenEmpty_Enabled_WithArgs_RunsCommand()
{
    using var terminal = new TestTerminal();
    var app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("test").WithHandler(() => "executed").Done()
        .AddRepl(o => o.AutoStartWhenEmpty = true)
        .Build();

    await app.RunAsync(["test"]);
    
    terminal.OutputContains("executed").ShouldBeTrue();
}
```

---

## Migration Guide (If Implemented)

### Before
```csharp
// source/timewarp-ganda/program.cs
if (args.Length == 0)
{
    args = ["--interactive"];
}
return await app.RunAsync(args);
```

### After
```csharp
// source/timewarp-ganda/program.cs
// Remove the if block entirely
return await app.RunAsync(args);

// In the AddRepl configuration:
.AddRepl(options =>
{
    options.Prompt = "ganda> ";
    options.AutoStartWhenEmpty = true;  // NEW
})
```

---

## Conclusion

**Implement this feature.** It:
- Solves a real user pain point
- Requires minimal code changes
- Maintains backward compatibility
- Follows established CLI patterns
- Makes dual-mode apps more ergonomic

The user's expectation that `StartReplWhenNoArguments` should exist was **valid**—it just needs a better name (`AutoStartWhenEmpty`) and implementation.

---

## Related Todo Items

- **#373** - Support default route starting REPL (complex, generator issues)
- **#338** - Migrate REPL demo samples to Nuru DSL API

This feature is a simpler, cleaner alternative to #373 that achieves the same user goal.

---

*Report generated by AI Assistant on 2026-02-04*
