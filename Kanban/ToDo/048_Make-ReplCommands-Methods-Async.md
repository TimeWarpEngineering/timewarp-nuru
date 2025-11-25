# Make ReplCommands Methods Async

## Description

Convert ReplCommands methods to async signatures for consistency with async terminal operations used throughout the codebase.

## Parent

Code review finding from `.agent/workspace/replsession-code-review-2025-11-25-v2.md` - Issue #1

## Requirements

- Convert synchronous ReplCommands methods to async equivalents
- Update all callers to use async/await pattern
- Ensure `ConfigureAwait(false)` is used consistently in library code
- Verify all Terminal operations use async methods where available

## Checklist

### Implementation
- [ ] Convert `ShowReplHelp()` to `ShowReplHelpAsync()`
- [ ] Convert `ShowHistory()` to `ShowHistoryAsync()`
- [ ] Convert `ClearScreen()` to `ClearScreenAsync()`
- [ ] Update `ShowAvailableCommands()` to async if needed
- [ ] Update route registrations in `NuruAppExtensions.AddReplRoutes()`
- [ ] Update any tests that call these methods
- [ ] Verify Functionality

### Documentation
- [ ] Update XML comments to reflect async nature

## Notes

The current synchronous methods work but are inconsistent with async terminal operations elsewhere. Methods like `ShowReplHelp()` perform multiple `Terminal.WriteLine()` calls which could be async.

**Files to modify:**
- `Source/TimeWarp.Nuru.Repl/Repl/ReplCommands.cs`
- `Source/TimeWarp.Nuru.Repl/NuruAppExtensions.cs` (route registrations)

**Example conversion:**
```csharp
// Before
public void ShowReplHelp()
{
    Terminal.WriteLine("REPL Commands:");
}

// After  
public async Task ShowReplHelpAsync()
{
    await Terminal.WriteLineAsync("REPL Commands:").ConfigureAwait(false);
}
```

**Estimated effort:** ~15 minutes
