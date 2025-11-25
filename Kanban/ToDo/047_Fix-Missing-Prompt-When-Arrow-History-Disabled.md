# Fix Missing Prompt When Arrow History Disabled

## Description

Fix bug in `ReplSession.ReadCommandInput()` where the prompt is not displayed to the user when `EnableArrowHistory` is false. This results in poor user experience with no visible prompt before input.

**Current state:** When `ReplOptions.EnableArrowHistory = false`, `Terminal.ReadLine()` is called without displaying the prompt
**Desired state:** Prompt displayed before reading input, consistent with arrow history enabled behavior

## Requirements

### Functional Requirements
- [ ] Prompt displayed when `EnableArrowHistory` is false
- [ ] Prompt formatting respects `ReplOptions.EnableColors` and `ReplOptions.PromptColor`
- [ ] Prompt format identical to arrow history enabled mode
- [ ] No regression in existing REPL functionality
- [ ] User sees consistent prompt regardless of EnableArrowHistory setting

### Non-Functional Requirements
- [ ] No breaking changes to public API
- [ ] Minimal code change (2-3 lines)
- [ ] All existing tests pass
- [ ] Add test to verify prompt displayed in both modes

## Checklist

### Design
- [x] Identify the issue location (ReplSession.ReadCommandInput line 170)
- [ ] Verify PromptFormatter.Format is the correct utility to use
- [ ] Decide between Terminal.Write vs Terminal.WriteLine for prompt
- [ ] Confirm no side effects from adding prompt display

### Implementation
- [ ] Add prompt display before Terminal.ReadLine() call
- [ ] Use `Terminal.Write(PromptFormatter.Format(ReplOptions))` for consistency
- [ ] Verify prompt appears on same line as user input (use Write, not WriteLine)
- [ ] Test manually with `EnableArrowHistory = false`

### Testing
- [ ] Run existing test suite - ensure all tests pass
- [ ] Add unit test: Verify prompt displayed when EnableArrowHistory = false
- [ ] Add unit test: Verify prompt formatted with colors when EnableColors = true
- [ ] Add unit test: Verify prompt formatted without colors when EnableColors = false
- [ ] Add integration test: Full REPL session with EnableArrowHistory = false
- [ ] Manual test: Start REPL with arrow history disabled, verify prompt visible

### Documentation
- [ ] Update code review report with "Fixed H2" status
- [ ] No XML documentation changes needed (private method)

## Notes

### Analysis References
- Code review: `.agent/workspace/replsession-code-review-2025-11-25.md` (High Priority Issue H2)

### Current Code (Line 153-171)

```csharp
private string? ReadCommandInput()
{
  if (ReplOptions.EnableArrowHistory)
  {
    var consoleReader =
      new ReplConsoleReader
        (
          History.AsReadOnly,
          CompletionProvider,
          NuruApp.Endpoints,
          ReplOptions,
          LoggerFactory,
          Terminal
        );
    return consoleReader.ReadLine(ReplOptions.Prompt);  // ← Prompt handled by ReplConsoleReader
  }

  return Terminal.ReadLine();  // ← BUG: No prompt displayed!
}
```

### Issue Details

**Problem:**
- When `EnableArrowHistory = true`: `ReplConsoleReader.ReadLine()` receives prompt and displays it
- When `EnableArrowHistory = false`: `Terminal.ReadLine()` called without prompt - **user sees nothing**

**User Impact:**
- Confusing user experience - appears to be hanging
- No indication that REPL is waiting for input
- Inconsistent behavior between arrow history on/off

**Root Cause:**
- `ReplConsoleReader.ReadLine(prompt)` handles prompt display internally
- Direct `Terminal.ReadLine()` does not - caller must display prompt first

### Proposed Fix

**Solution:**
```csharp
private string? ReadCommandInput()
{
  if (ReplOptions.EnableArrowHistory)
  {
    var consoleReader =
      new ReplConsoleReader
        (
          History.AsReadOnly,
          CompletionProvider,
          NuruApp.Endpoints,
          ReplOptions,
          LoggerFactory,
          Terminal
        );
    return consoleReader.ReadLine(ReplOptions.Prompt);
  }

  // Display prompt before reading input
  Terminal.Write(PromptFormatter.Format(ReplOptions));
  return Terminal.ReadLine();
}
```

**Changes:**
- Add 1 line: `Terminal.Write(PromptFormatter.Format(ReplOptions));`
- Use `Write` (not `WriteLine`) so prompt and input appear on same line
- Use `PromptFormatter.Format(ReplOptions)` for consistency with ReplConsoleReader

### Testing Approach

**Manual Testing:**
1. Create sample REPL app
2. Set `ReplOptions.EnableArrowHistory = false`
3. Run REPL
4. Verify prompt appears before input
5. Type command and verify it executes
6. Verify prompt appears for next command

**Automated Testing:**
1. Mock Terminal
2. Set EnableArrowHistory = false
3. Call ReadCommandInput()
4. Verify Terminal.Write called with formatted prompt
5. Verify Terminal.ReadLine called after prompt

### Edge Cases

**Case 1: Empty prompt**
- PromptFormatter validates prompt is not null/empty
- Should throw ArgumentException if prompt is empty
- Existing validation is sufficient

**Case 2: Colors disabled**
- PromptFormatter.Format respects EnableColors flag
- Returns plain text when colors disabled
- No special handling needed

**Case 3: Custom prompt color**
- PromptFormatter uses ReplOptions.PromptColor
- Custom colors work automatically
- No special handling needed

### Comparison with ReplConsoleReader

**ReplConsoleReader.ReadLine(prompt) - Line 128-135:**
```csharp
public string? ReadLine(string prompt)
{
  ArgumentException.ThrowIfNullOrEmpty(prompt);

  ReplLoggerMessages.ReadLineStarted(Logger, prompt, History.Count, null);

  string formattedPrompt = PromptFormatter.Format(prompt, ReplOptions.EnableColors, ReplOptions.PromptColor);
  Terminal.Write(formattedPrompt);  // ← Displays prompt

  UserInput = string.Empty;
  CursorPosition = 0;
  // ... rest of method
```

**Observation:**
- ReplConsoleReader calls `Terminal.Write(formattedPrompt)` before reading input
- Our fix should do the same for consistency
- Use same PromptFormatter.Format for identical formatting

### Benefits

1. **User Experience**: Clear prompt indicates REPL is ready for input
2. **Consistency**: Same prompt behavior regardless of EnableArrowHistory setting
3. **Simplicity**: One-line fix, minimal code change
4. **Correctness**: Follows same pattern as ReplConsoleReader

### Risks & Mitigations

**Risk: Prompt displayed twice in some scenarios**
- **Assessment**: Unlikely - EnableArrowHistory is boolean, only one path executes
- **Mitigation**: Existing if/else structure ensures only one branch runs
- **Testing**: Verify with both settings

**Risk: Terminal.Write not flushing immediately**
- **Assessment**: Terminal abstraction should handle flushing
- **Mitigation**: If issue occurs, consider Terminal.Flush() if available
- **Testing**: Manual testing will reveal any issues

**Risk: Breaking existing tests that expect no prompt**
- **Assessment**: Tests may have been written to work around the bug
- **Mitigation**: Update tests to expect prompt (correct behavior)
- **Testing**: Run full test suite, fix any failing tests

### Performance Impact
- **Negligible**: One additional Terminal.Write call per command input
- **No regression**: Same behavior as ReplConsoleReader path

### Code Review Issues Addressed

From `.agent/workspace/replsession-code-review-2025-11-25.md`:

**HIGH Priority Issue H2: Incomplete Prompt Handling in ReadCommandInput**
- ✅ **Fixed**: Add prompt display before Terminal.ReadLine()
- ✅ **Impact**: Improved user experience when arrow history disabled
- ✅ **Severity**: HIGH (poor UX) → RESOLVED

### Alternative Solutions Considered

**Alternative 1: Always use ReplConsoleReader**
- **Approach**: Remove the if/else, always create ReplConsoleReader
- **Pros**: Single code path, consistent behavior
- **Cons**: Unnecessary overhead when arrow history not needed
- **Decision**: Rejected - EnableArrowHistory exists for a reason (performance, compatibility)

**Alternative 2: Make Terminal.ReadLine() accept prompt parameter**
- **Approach**: Modify ITerminal interface to add prompt parameter
- **Pros**: Consistent API
- **Cons**: Breaking change, affects all Terminal implementations
- **Decision**: Rejected - too invasive for simple bug fix

**Alternative 3: Create SimpleConsoleReader for non-arrow mode**
- **Approach**: New class that just handles prompt + readline
- **Pros**: Consistent architecture (both paths use reader classes)
- **Cons**: Over-engineering for 1-line fix
- **Decision**: Rejected - YAGNI (You Aren't Gonna Need It)

### File Structure After Fix
```
Source/TimeWarp.Nuru.Repl/Repl/
  └─ ReplSession.cs      (~265 lines, +1 line)
```

### Related Code

**PromptFormatter.cs** (already exists, no changes needed):
- `Format(ReplOptions)` - Takes ReplOptions, returns formatted prompt
- `Format(prompt, enableColors, promptColor)` - Lower-level formatting
- Handles color codes, validation

**ITerminal.cs** (interface, no changes needed):
- `Write(string)` - Write without newline
- `ReadLine()` - Read line of input
- `WriteLine()` - Write with newline

## Implementation Notes

### Step-by-Step Implementation

1. **Open ReplSession.cs**
2. **Navigate to ReadCommandInput method** (around line 153)
3. **Locate the else branch** with `return Terminal.ReadLine();`
4. **Add prompt display before ReadLine**:
   ```csharp
   Terminal.Write(PromptFormatter.Format(ReplOptions));
   return Terminal.ReadLine();
   ```
5. **Save file**
6. **Build solution** - verify no compilation errors
7. **Run tests** - verify all pass
8. **Manual test** - create test app with EnableArrowHistory = false
9. **Verify prompt displays correctly**
10. **Add unit test** for prompt display
11. **Commit changes**

### Testing Checklist

**Before Fix:**
- [ ] Create REPL app with EnableArrowHistory = false
- [ ] Observe: No prompt displayed (bug reproduced)

**After Fix:**
- [ ] Run same REPL app
- [ ] Observe: Prompt displayed before input
- [ ] Verify: Prompt has correct color (if enabled)
- [ ] Verify: Input works correctly
- [ ] Verify: Prompt reappears after command execution

**Automated Tests:**
- [ ] Test with EnableArrowHistory = false, EnableColors = true
- [ ] Test with EnableArrowHistory = false, EnableColors = false
- [ ] Test with EnableArrowHistory = true (should not regress)
- [ ] Test with custom prompt string
- [ ] Test with custom prompt color

[Notes will be added during implementation]
