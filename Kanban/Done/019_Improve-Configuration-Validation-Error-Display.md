# Improve Configuration Validation Error Display

**Status:** InProgress
**Priority:** High
**Category:** User Experience, Error Handling
**Created:** 2025-10-25
**Related:** Issue #71 (ValidateOnStart support)

## Problem

When `ValidateOnStart()` throws an `OptionsValidationException`, users see the full exception stack trace instead of clean, actionable error messages.

### Current Behavior

```
Unhandled exception. Microsoft.Extensions.Options.OptionsValidationException: CCC One username is required.
Set it in appsettings.json or EXPORTFLOW__CCCONEUSERNAME environment variable.; CCC One password is required.
Set it in appsettings.json or EXPORTFLOW__CCCONEPASSWORD environment variable.; QuickBooks Realm ID is required.
Set it in appsettings.json or EXPORTFLOW__QUICKBOOKSREALMID environment variable.
   at Microsoft.Extensions.Options.OptionsFactory`1.Create(String name)
   at Microsoft.Extensions.Options.OptionsMonitor`1.<>c.<Get>b__10_0(String name, IOptionsFactory`1 factory)
   at Microsoft.Extensions.Options.OptionsCache`1.<>c__DisplayClass3_1`1.<GetOrAdd>b__2()
   at System.Lazy`1.ViaFactory(LazyThreadSafetyMode mode)
   [... 20+ more lines of stack trace ...]
```

**Problems:**
- User messages are buried in technical stack traces
- Validation errors are concatenated with semicolons (hard to read)
- Looks like an application crash, not a configuration issue
- Not user-friendly for CLI applications

## Goal

Display clean, actionable validation errors without stack traces.

### Desired Behavior

```
❌ Configuration validation failed:

  • CCC One username is required.
    Set it in appsettings.json or EXPORTFLOW__CCCONEUSERNAME environment variable.

  • CCC One password is required.
    Set it in appsettings.json or EXPORTFLOW__CCCONEPASSWORD environment variable.

  • QuickBooks Realm ID is required.
    Set it in appsettings.json or EXPORTFLOW__QUICKBOOKSREALMID environment variable.

Run with --help for more information.
```

## Proposed Solution

### Option 1: Catch and Format in NuruAppBuilder.Build()

**Location:** `Source/TimeWarp.Nuru/NuruAppBuilder.cs` lines 320-323

```csharp
try
{
  IStartupValidator? startupValidator = serviceProvider.GetService<IStartupValidator>();
  startupValidator?.Validate();
}
catch (OptionsValidationException ex)
{
  // Display clean validation errors
  await NuruConsole.WriteErrorLineAsync("❌ Configuration validation failed:");
  await NuruConsole.WriteErrorLineAsync("");

  // Parse and display each failure
  foreach (string failure in ex.Failures)
  {
    await NuruConsole.WriteErrorLineAsync($"  • {failure}");
    await NuruConsole.WriteErrorLineAsync("");
  }

  throw new InvalidOperationException(
    "Application startup failed due to invalid configuration. See errors above.",
    ex
  );
}
```

**Pros:**
- Users see clean error messages
- Original exception preserved for logging/debugging
- No breaking changes

**Cons:**
- Sync method calling async (need to handle carefully)
- Still throws an exception (but with friendlier output first)

### Option 2: Catch in NuruApp.RunAsync()

**Location:** `Source/TimeWarp.Nuru/NuruApp.cs` line 54+

Wrap the entire `RunAsync` method to catch configuration validation errors:

```csharp
public async Task<int> RunAsync(string[] args)
{
  try
  {
    // ... existing code ...
  }
  catch (OptionsValidationException ex)
  {
    await NuruConsole.WriteErrorLineAsync("❌ Configuration validation failed:");
    await NuruConsole.WriteErrorLineAsync("");

    foreach (string failure in ex.Failures)
    {
      await NuruConsole.WriteErrorLineAsync($"  • {failure}");
      await NuruConsole.WriteErrorLineAsync("");
    }

    return 1; // Exit with error code
  }
  // ... existing catches ...
}
```

**Pros:**
- Cleaner async handling
- Returns error code instead of throwing
- More CLI-friendly

**Cons:**
- Validation error won't be caught if it happens during Build()
- Only works if Build() doesn't throw

### Recommended Approach: Combination

1. **Build()** - Display errors synchronously, then throw wrapped exception
2. **RunAsync()** - Catch wrapped exception, return exit code 1

This gives:
- Clean error display at Build() time
- Graceful exit code from RunAsync()
- Original exception preserved for debugging

## Implementation Tasks

- [ ] Update `NuruAppBuilder.Build()` to catch `OptionsValidationException`
- [ ] Format validation failures as bulleted list
- [ ] Create helper method for formatting validation errors
- [ ] Test with multiple validation failures
- [ ] Test with single validation failure
- [ ] Update sample to demonstrate improved error display
- [ ] Add integration test for error formatting
- [ ] Document error handling behavior

## Testing Scenarios

1. **Multiple validation errors** - Should display as clean bulleted list
2. **Single validation error** - Should display clearly
3. **Nested validation failures** - Should handle gracefully
4. **No validation errors** - Should behave normally (no change)

## Success Criteria

✅ Configuration validation errors displayed without stack traces
✅ Each validation failure on its own line with bullet point
✅ Clear indication that it's a configuration issue, not a bug
✅ Exit code 1 returned (not unhandled exception)
✅ Original exception still available for debugging/logging

## Notes

- This only affects apps using `.ValidateOnStart()` - no impact on apps without it
- Maintains backward compatibility (still throws exception, just displays better first)
- Aligns with CLI best practices (clean user messages, technical details on demand)
