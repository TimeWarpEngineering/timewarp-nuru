# Improve Exception Handling in ShowAvailableCommands

## Description

Enhance exception handling in `ReplCommands.ShowAvailableCommands()` to provide better diagnostic information when completion provider fails, while avoiding masking unexpected exceptions.

## Parent

Code review finding from `.agent/workspace/replsession-code-review-2025-11-25-v2.md` - Issue #3

## Requirements

- Replace generic exception catches with more informative handling
- Include exception type name in error messages
- Consider logging for debugging purposes
- Maintain graceful degradation (help command should never crash)

## Checklist

### Implementation
- [x] Update catch blocks in `ShowAvailableCommands()`
- [x] Include exception type in error message
- [x] Consider adding optional logging
- [x] Verify Functionality

### Testing
- [x] Test with mock completion provider that throws
- [x] Verify error message includes useful information
- [x] Ensure help command doesn't crash on exceptions

## Notes

The current implementation catches specific exceptions but provides the same generic message for both. This makes debugging difficult when completion provider fails unexpectedly.

**File to modify:**
- `Source/TimeWarp.Nuru.Repl/Repl/ReplCommands.cs` (lines 95-102)

**Current approach:**
```csharp
catch (InvalidOperationException)
{
    Terminal.WriteLine("  (Completions unavailable - check configuration)");
}
catch (ArgumentException)
{
    Terminal.WriteLine("  (Completions unavailable - check configuration)");
}
```

**Improved approach (Option 1 - Simple):**
```csharp
catch (Exception ex)
{
    Terminal.WriteLine($"  (Completions unavailable: {ex.GetType().Name})");
}
```

**Improved approach (Option 2 - With logging):**
```csharp
catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
{
    Terminal.WriteLine($"  (Completions unavailable: {ex.Message})");
    // Optional: Log full exception for debugging
}
catch (Exception ex)
{
    Terminal.WriteLine($"  (Completions failed: {ex.GetType().Name})");
    // Log unexpected exceptions
}
```

Since this is a help command (non-critical), broad exception handling is acceptable, but we should provide useful diagnostic information.

**Estimated effort:** ~10 minutes
