# TimeWarp.Nuru Invalid Example Investigation Report

**Date:** 2026-02-04  
**Investigator:** AI Assistant  
**Status:** COMPLETED - No Invalid Examples Found  

---

## Executive Summary

The investigation found **no invalid examples** in the TimeWarp.Nuru codebase or MCP server. The property `StartReplWhenNoArguments` does not exist in the `ReplOptions` class, and the user was attempting to use a non-existent feature. The correct pattern for dual-mode CLI/REPL applications is to use the built-in `-i`/`--interactive` flags with `app.RunAsync(args)`.

---

## Scope

This investigation examined:
1. The `ReplOptions` class implementation
2. All MCP server examples
3. All REPL-related samples in `/samples/13-repl/`
4. Related todo items and feature requests
5. Test files for default route handling

---

## Methodology

1. **Source Code Analysis**: Read the actual `ReplOptions` class implementation
2. **MCP Example Retrieval**: Fetched the `repl-options` example from the MCP server
3. **Sample Review**: Examined all REPL-related samples
4. **Grep Search**: Searched for `StartReplWhenNoArguments` across the entire codebase
5. **Todo Review**: Checked for related feature requests

---

## Findings

### Finding 1: `StartReplWhenNoArguments` Does Not Exist

**Location:** `source/timewarp-nuru/options/repl-options.cs`

The actual `ReplOptions` class contains these properties:

| Property | Type | Default |
|----------|------|---------|
| `Prompt` | `string` | `"> "` |
| `ContinuationPrompt` | `string?` | `">> "` |
| `WelcomeMessage` | `string?` | `"TimeWarp.Nuru REPL Mode..."` |
| `GoodbyeMessage` | `string?` | `"Goodbye!"` |
| `PersistHistory` | `bool` | `true` |
| `HistoryFilePath` | `string?` | `null` |
| `MaxHistorySize` | `int` | `1000` |
| `ContinueOnError` | `bool` | `true` |
| `ShowExitCode` | `bool` | `false` |
| `EnableColors` | `bool` | `true` |
| `PromptColor` | `string` | `"\x1b[32m"` (green) |
| `ShowTiming` | `bool` | `true` |
| `EnableArrowHistory` | `bool` | `true` |
| `HistoryIgnorePatterns` | `IList<string>?` | Default patterns |
| `KeyBindingProfileName` | `string` | `"Default"` |
| `KeyBindingProfile` | `object?` | `null` |

**`StartReplWhenNoArguments` is NOT in this list.**

### Finding 2: MCP Examples Are Valid

The MCP server example for `repl-options` (retrieved via `TimeWarp_Nuru_Mcp_get_example`) shows only valid properties:

```csharp
.AddRepl(options =>
{
  options.Prompt = "showcase> ";
  options.PromptColor = "\x1b[36m"; // Cyan
  options.WelcomeMessage = "...";
  options.GoodbyeMessage = "...";
  options.PersistHistory = true;
  options.HistoryFilePath = "./repl-showcase-history.txt";
  options.MaxHistorySize = 50;
  options.ContinueOnError = false;
  options.ShowExitCode = true;
  options.ShowTiming = true;
  options.EnableColors = true;
  options.EnableArrowHistory = true;
  options.KeyBindingProfileName = "Default";
})
```

All properties shown are valid and exist in the actual implementation.

### Finding 3: Correct Dual-Mode Pattern

**File:** `samples/13-repl/01-repl-cli-dual-mode.cs`

The correct pattern for CLI + REPL dual mode:

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .WithDescription("Demo app supporting both CLI and interactive REPL modes")
  .Map("greet {name}").WithHandler(...).AsCommand().Done()
  .Map("status").WithHandler(...).AsQuery().Done()
  .AddRepl(options =>
  {
    options.Prompt = "demo> ";
    options.WelcomeMessage = "...";
  })
  .Build();

// Use RunAsync, NOT RunReplAsync
return await app.RunAsync(args);
```

**Usage:**
- CLI mode: `app.exe greet Alice` (executes single command)
- REPL mode: `app.exe -i` or `app.exe --interactive` (enters interactive mode)

### Finding 4: Related Feature Request Exists

**File:** `kanban/to-do/373-support-default-route-starting-repl-via-nurucoreapp-injection.md`

There is an open todo item to support a default route that starts REPL:

```csharp
// DESIRED (not yet implemented):
.Map("")
  .WithHandler(async (NuruCoreApp app) => await app.RunReplAsync())
  .WithDescription("Start interactive REPL mode")
  .AsCommand()
  .Done()
```

This feature is **not yet implemented** and has known issues with the source generator.

### Finding 5: Current Workarounds Documented

The todo item documents these workarounds:

**Workaround 1: If/else at program level**
```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .AddRepl(...)
  .Map("command1").WithHandler(...).Done()
  .Build();

if (args.Length == 0)
{
  await app.RunReplAsync();
  return 0;
}
return await app.RunAsync(args);
```

**Workaround 2: Convert empty args to `-i` flag**
```csharp
string[] effectiveArgs = args.Length == 0 ? ["-i"] : args;
await app.RunAsync(effectiveArgs);
```

---

## Root Cause Analysis

The user likely:
1. **Assumed** the property exists based on patterns from other CLI frameworks
2. **Hallucinated** the property name when trying to implement dual-mode behavior
3. **Misunderstood** the difference between `RunAsync()` and `RunReplAsync()`

The error message they encountered:
```
'ReplOptions' does not contain a definition for 'StartReplWhenNoArguments'
```

This is the C# compiler correctly reporting that the property doesn't exist.

---

## Recommendations

### For the User (Immediate)

1. **Use the `-i`/`--interactive` flags** instead of trying to auto-start REPL:
   ```csharp
   return await app.RunAsync(args);
   ```
   Then run: `myapp -i` to enter REPL mode

2. **Or use the workaround** to auto-start REPL when no args provided:
   ```csharp
   string[] effectiveArgs = args.Length == 0 ? ["-i"] : args;
   return await app.RunAsync(effectiveArgs);
   ```

### For the Framework (Future)

1. **Consider implementing** the feature request in todo item #373
2. **Add documentation** clarifying the dual-mode pattern
3. **Consider adding** a convenience method like:
   ```csharp
   .AddRepl(options => {
     options.AutoStartWhenNoArguments = true; // Future feature
   })
   ```

---

## Conclusion

**No invalid examples exist** in the TimeWarp.Nuru codebase or MCP server. The `StartReplWhenNoArguments` property was never implemented and the user was attempting to use a non-existent feature. The correct pattern for dual-mode CLI/REPL is well-documented in `samples/13-repl/01-repl-cli-dual-mode.cs`.

---

## References

| File | Purpose |
|------|---------|
| `source/timewarp-nuru/options/repl-options.cs` | Actual ReplOptions implementation |
| `samples/13-repl/01-repl-cli-dual-mode.cs` | Correct dual-mode pattern |
| `samples/13-repl/03-repl-options.cs` | All valid REPL options |
| `kanban/to-do/373-support-default-route-starting-repl-via-nurucoreapp-injection.md` | Feature request for this functionality |
| `tests/timewarp-nuru-tests/help/help-02-default-route-help.cs` | Tests for default route handling |

---

*Report generated by AI Assistant on 2026-02-04*
