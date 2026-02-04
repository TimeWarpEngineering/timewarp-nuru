# Add AutoStartWhenEmpty to ReplOptions

## Description

Add a property to `ReplOptions` that starts REPL automatically when no arguments are provided:

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .WithDescription("Ganda CLI")
  .Map("status").WithHandler(() => "OK").Done()
  .AddRepl(options =>
  {
    options.Prompt = "ganda> ";
    options.AutoStartWhenEmpty = true;  // Start REPL when no args
  })
  .Build();

return await app.RunAsync(args);  // No boilerplate needed!
```

## Why

A user attempting dual-mode CLI behavior tried this code:

```csharp
.AddRepl(options =>
{
  options.StartReplWhenNoArguments = true;  // ERROR: Property doesn't exist!
})
```

This shows users expect this pattern. Other CLI frameworks support similar functionality.

## Current Behavior

Users must add boilerplate:

```csharp
string[] effectiveArgs = args.Length == 0 ? ["--interactive"] : args;
return await app.RunAsync(effectiveArgs);
```

## Implementation

### Files to Modify

| File | Change |
|------|--------|
| `source/timewarp-nuru/options/repl-options.cs` | Add `bool AutoStartWhenEmpty { get; set; } = false;` |
| `source/timewarp-nuru-analyzers/generators/models/repl-model.cs` | Add `bool AutoStartWhenEmpty` parameter |
| `source/timewarp-nuru-analyzers/generators/extractors/repl-extractor.cs` | Extract the new property |
| `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` | Emit check before user routes |

### Generated Code Change

**Location:** `interceptor-emitter.cs` line 781, in `EmitInteractiveFlag()`:

```csharp
// Current:
if (routeArgs is ["--interactive"] or ["-i"])
{
  await RunReplAsync(app).ConfigureAwait(false);
  return 0;
}

// Proposed:
if (routeArgs is ["--interactive"] or ["-i"] ||
    (routeArgs.Length == 0 && app.ReplOptions?.AutoStartWhenEmpty == true))
{
  await RunReplAsync(app).ConfigureAwait(false);
  return 0;
}
```

## Checklist

- [ ] Add `AutoStartWhenEmpty` property to `ReplOptions` class
- [ ] Add `AutoStartWhenEmpty` to `ReplModel` record
- [ ] Update `ReplExtractor` to extract the new property
- [ ] Update `InterceptorEmitter` to emit the check
- [ ] Add unit test: AutoStart enabled, no args → starts REPL
- [ ] Add unit test: AutoStart disabled, no args → shows unknown command
- [ ] Add unit test: AutoStart enabled, has args → runs command
- [ ] Update `samples/13-repl/01-repl-cli-dual-mode.cs`
- [ ] Update `source/timewarp-ganda/program.cs` to remove boilerplate

## Migration

### Before

```csharp
// source/timewarp-ganda/program.cs
string[] effectiveArgs = args.Length == 0 ? ["--interactive"] : args;
return await app.RunAsync(effectiveArgs);
```

### After

```csharp
// source/timewarp-ganda/program.cs
return await app.RunAsync(args);

// In AddRepl configuration:
.AddRepl(options =>
{
  options.Prompt = "ganda> ";
  options.AutoStartWhenEmpty = true;
})
```

## Notes

## Implementation Plan: Add AutoStartWhenEmpty to ReplOptions

### Overview

This feature adds a new `AutoStartWhenEmpty` property to `ReplOptions` that, when enabled, automatically starts the REPL when no command-line arguments are provided. This eliminates the need for users to manually check `args.Length == 0` and pass `--interactive`.

### Files to Modify

#### 1. Runtime Configuration (`source/timewarp-nuru/options/repl-options.cs`)

Add the `AutoStartWhenEmpty` property to the `ReplOptions` class:

```csharp
/// <summary>
/// Whether to automatically start REPL when no arguments are provided.
/// When true, running the app without arguments enters interactive mode.
/// Default is false.
/// </summary>
public bool AutoStartWhenEmpty { get; set; } = false;
```

#### 2. Design-Time Model (`source/timewarp-nuru-analyzers/generators/models/repl-model.cs`)

Add `AutoStartWhenEmpty` to the `ReplModel` record and update its default:

```csharp
public sealed record ReplModel(
  string Prompt,
  string ContinuationPrompt,
  ImmutableArray<string> ExitCommands,
  int HistorySize,
  bool EnableSyntaxHighlighting,
  bool EnableAutoComplete,
  bool AutoStartWhenEmpty)  // NEW PARAMETER
{
  public static readonly ReplModel Default = new(
    Prompt: "> ",
    ContinuationPrompt: "... ",
    ExitCommands: ["exit", "quit", "q"],
    HistorySize: 100,
    EnableSyntaxHighlighting: true,
    EnableAutoComplete: true,
    AutoStartWhenEmpty: false);  // UPDATED
}
```

#### 3. DSL Interpreter (`source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs`)

**Current (line 992-1003):**
```csharp
private static object? DispatchAddRepl(InvocationExpressionSyntax invocation, object? receiver)
{
  if (receiver is not IIrAppBuilder appBuilder)
  {
    throw new InvalidOperationException(
      $"AddRepl() must be called on an app builder. Location: {invocation.GetLocation().GetLineSpan()}");
  }

  // For now, just enable REPL with defaults
  // TODO: Phase 5+ - extract options from lambda if present
  return appBuilder.AddRepl();
}
```

**Proposed:** Create a `ReplOptionsExtractor` class and update `DispatchAddRepl` to extract options from the lambda expression.

#### 4. Interceptor Emitter (`source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs`)

Update `EmitInteractiveFlag` (line 775-788) to check `AutoStartWhenEmpty`.

### New Files to Create

#### 5. ReplOptions Extractor (`source/timewarp-nuru-analyzers/generators/extractors/repl-options-extractor.cs`)

Create a new extractor class to handle lambda expressions.

### Testing

#### 6. Unit Tests

Create: `tests/timewarp-nuru-tests/repl/repl-38-auto-start-when-empty.cs`

Tests to include:
- `AutoStart_enabled_no_args_starts_repl` - When `AutoStartWhenEmpty = true` and no args, REPL starts
- `AutoStart_disabled_no_args_unknown_command` - When `AutoStartWhenEmpty = false` and no args, shows "Unknown command"
- `AutoStart_enabled_with_args_runs_command` - When `AutoStartWhenEmpty = true` and args provided, runs command normally
- `Interactive_flag_still_works` - `--interactive` and `-i` flags still work regardless of `AutoStartWhenEmpty`

### Implementation Order

1. Add `AutoStartWhenEmpty` to `ReplOptions` (runtime)
2. Add `AutoStartWhenEmpty` to `ReplModel` (design-time)
3. Create `ReplOptionsExtractor`
4. Update `DispatchAddRepl` in `DslInterpreter`
5. Update `EmitInteractiveFlag` in `InterceptorEmitter`
6. Add unit tests
7. Update sample documentation
