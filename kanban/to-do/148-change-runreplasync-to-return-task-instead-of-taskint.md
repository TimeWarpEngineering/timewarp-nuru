# Change RunReplAsync to return Task instead of Task<int>

## Description

`RunReplAsync()` returns `Task<int>` which causes `ResponseDisplay.Write()` to print the exit code (`0`) when the REPL exits. This results in a spurious `0` being printed after the goodbye message:

```
> q
(0ms)
Goodbye!
0        <-- unwanted output
```

The return value (last command's exit code) isn't useful for a REPL session. Change to `Task` to prevent the value from being displayed.

## Checklist

- [ ] Change `RunReplAsync` in `nuru-app-extensions.cs` from `Task<int>` to `Task`
- [ ] Change `StartInteractiveModeAsync` in `nuru-app-extensions.cs` from `Task<int>` to `Task`
- [ ] Update invoker registration in `repl-invoker-registration.cs` from `NuruCoreAppHolder_Returns_TaskInt` to `NuruCoreAppHolder_Returns_Task`
- [ ] Update samples that use `return await app.RunReplAsync()` to `await app.RunReplAsync(); return 0;`
- [ ] Run tests to verify no regressions
- [ ] Build and manually test REPL exit no longer prints `0`

## Notes

### Root Cause

The flow is:
1. `StartInteractiveModeAsync` returns `Task<int>` with value `0` when REPL exits
2. `DelegateExecutor` extracts the return value and passes it to `ResponseDisplay.Write()`
3. `ResponseDisplay.Write()` sees an `int` (primitive) and prints it

### Files to Modify

**`source/timewarp-nuru-repl/nuru-app-extensions.cs`:**
```csharp
// Change signature
public static async Task RunReplAsync(...)
{
  // ... same body but don't return the value
  await ReplSession.RunAsync(...).ConfigureAwait(false);
}

// Change signature  
public static async Task StartInteractiveModeAsync(NuruCoreAppHolder appHolder)
{
  ArgumentNullException.ThrowIfNull(appHolder);
  await appHolder.App.RunReplAsync().ConfigureAwait(false);
}
```

**`source/timewarp-nuru-repl/repl-invoker-registration.cs`:**
```csharp
// Change from Task<int> to Task
InvokerRegistry.RegisterAsyncInvoker("NuruCoreAppHolder_Returns_Task", static async (handler, args) =>
{
  Func<NuruCoreAppHolder, Task> typedHandler = (Func<NuruCoreAppHolder, Task>)handler;
  await typedHandler((NuruCoreAppHolder)args[0]!).ConfigureAwait(false);
  return null;
});
```

**Samples to update:**
- `samples/repl-demo/repl-basic-demo.cs`
- `samples/repl-demo/repl-custom-keybindings.cs`
- `samples/repl-demo/repl-prompt-fix-demo.cs`
- `samples/repl-demo/repl-options-showcase.cs`

### Breaking Change

This is a minor breaking change for anyone using the return value of `RunReplAsync()`. However, the return value was not meaningful (just the last command's exit code) so impact should be minimal.
