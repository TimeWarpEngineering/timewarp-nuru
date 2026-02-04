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
