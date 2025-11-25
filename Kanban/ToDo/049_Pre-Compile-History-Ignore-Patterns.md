# Pre-Compile History Ignore Patterns

## Description

Optimize ReplHistory performance by pre-compiling regex patterns for history ignore filters instead of compiling them on every command execution.

## Parent

Code review finding from `.agent/workspace/replsession-code-review-2025-11-25-v2.md` - Issue #2

## Requirements

- Pre-compile regex patterns in ReplHistory constructor
- Store compiled patterns as readonly field
- Maintain existing wildcard pattern behavior (`*` and `?`)
- Ensure case-insensitive matching is preserved
- No breaking changes to ReplOptions API

## Checklist

### Implementation
- [ ] Add `List<Regex> CompiledIgnorePatterns` field to ReplHistory
- [ ] Move pattern compilation logic to constructor
- [ ] Update `ShouldIgnore()` to use pre-compiled patterns
- [ ] Add regex compilation options (IgnoreCase, Compiled)
- [ ] Handle null/empty patterns gracefully
- [ ] Verify Functionality

### Testing
- [ ] Test with various wildcard patterns (`exit*`, `quit`, `help?`)
- [ ] Test with empty/null pattern lists
- [ ] Verify case-insensitive matching still works
- [ ] Benchmark performance improvement (optional)

## Notes

The current implementation compiles regex patterns on every call to `ShouldIgnore()`, which is invoked for each command entered. While the performance impact is likely negligible for human-speed REPL interaction, pre-compilation is a best practice.

**File to modify:**
- `Source/TimeWarp.Nuru.Repl/Repl/ReplHistory.cs`

**Current approach (lines 74-95):**
```csharp
public bool ShouldIgnore(string command)
{
    foreach (string pattern in Options.HistoryIgnorePatterns)
    {
        // Compiles regex every time
        string regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*", StringComparison.Ordinal)
            .Replace("\\?", ".", StringComparison.Ordinal)
            + "$";
        if (Regex.IsMatch(command, regexPattern, RegexOptions.IgnoreCase))
            return true;
    }
}
```

**Optimized approach:**
```csharp
private readonly List<Regex> CompiledIgnorePatterns = [];

internal ReplHistory(ReplOptions options, ITerminal terminal)
{
    // ... existing code ...
    
    // Pre-compile patterns once
    if (options.HistoryIgnorePatterns is not null)
    {
        foreach (string pattern in options.HistoryIgnorePatterns)
        {
            if (!string.IsNullOrEmpty(pattern))
            {
                string regexPattern = "^" + Regex.Escape(pattern)
                    .Replace("\\*", ".*", StringComparison.Ordinal)
                    .Replace("\\?", ".", StringComparison.Ordinal)
                    + "$";
                CompiledIgnorePatterns.Add(
                    new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled)
                );
            }
        }
    }
}

public bool ShouldIgnore(string command)
{
    foreach (Regex regex in CompiledIgnorePatterns)
    {
        if (regex.IsMatch(command))
            return true;
    }
    return false;
}
```

**Estimated effort:** ~30 minutes
