# Improve History Load/Save Error Messages

## Description

Enhance error messages in `ReplHistory.Load()` and `ReplHistory.Save()` to provide actionable information when file operations fail, including the file path and potential solutions.

## Parent

Code review finding from `.agent/workspace/replsession-code-review-2025-11-25-v2.md` - Issue #4

## Requirements

- Include history file path in error messages
- Provide actionable guidance for permission errors
- Distinguish between different error types (I/O vs permissions)
- Maintain non-intrusive warning behavior (don't crash REPL)

## Checklist

### Implementation
- [x] Update error messages in `Load()` method
- [x] Update error messages in `Save()` method
- [x] Include file path in all error messages
- [x] Add actionable guidance for UnauthorizedAccessException
- [x] Verify Functionality

### Testing
- [ ] Test with non-existent history directory
- [ ] Test with read-only history file
- [ ] Test with permission-denied scenario
- [ ] Verify warnings are clear and helpful

## Notes

The current error messages are generic and don't help users understand where the history file is located or how to fix permission issues.

**File to modify:**
- `Source/TimeWarp.Nuru.Repl/Repl/ReplHistory.cs` (lines 100-127, 132-154)

**Current approach (Load):**
```csharp
catch (IOException ex)
{
    Terminal.WriteLine($"Warning: Could not load history: {ex.Message}");
}
catch (UnauthorizedAccessException ex)
{
    Terminal.WriteLine($"Warning: Could not load history: {ex.Message}");
}
```

**Improved approach:**
```csharp
catch (IOException ex)
{
    Terminal.WriteLine($"Warning: Could not load history from {historyPath}");
    Terminal.WriteLine($"  Reason: {ex.Message}");
}
catch (UnauthorizedAccessException)
{
    Terminal.WriteLine($"Warning: Permission denied loading history from {historyPath}");
    Terminal.WriteLine($"  Set ReplOptions.HistoryFilePath to a writable location or disable with PersistHistory=false");
}
```

**Similar improvements needed for Save() method:**
```csharp
catch (IOException ex)
{
    Terminal.WriteLine($"Warning: Could not save history to {historyPath}");
    Terminal.WriteLine($"  Reason: {ex.Message}");
}
catch (UnauthorizedAccessException)
{
    Terminal.WriteLine($"Warning: Permission denied saving history to {historyPath}");
    Terminal.WriteLine($"  Set ReplOptions.HistoryFilePath to a writable location or disable with PersistHistory=false");
}
```

**Considerations:**
- Keep messages concise but informative
- Don't spam console with too many lines
- Balance between helpful and overwhelming

**Estimated effort:** ~15 minutes
