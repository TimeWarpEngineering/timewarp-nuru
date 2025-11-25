# Refactor Key Bindings to Action Pattern

## Description

Simplify key binding initialization in ReplConsoleReader by using `Action` instead of `Func<bool>`, eliminating 47 redundant `return true;` statements and separating exit keys into a dedicated `HashSet`. This refactoring establishes a cleaner foundation for future key binding profiles (Phase 2) and custom bindings (Phase 3).

**Goal**: Reduce code repetition and improve clarity without changing behavior.

## Parent

Related to task 054_Extract-Tab-Completion-Logic-From-ReplConsoleReader (completed)
Part of 3-phase key binding evolution:
- **Phase 1** (this task): Action + ExitSet pattern
- **Phase 2** (task 056): IKeyBindingProfile interface with Emacs/Vi/VSCode modes
- **Phase 3** (task 057): Custom key bindings via builder API

## Requirements

- Convert `Dictionary<(ConsoleKey, ConsoleModifiers), Func<bool>>` to `Dictionary<(ConsoleKey, ConsoleModifiers), Action>`
- Create `HashSet<(ConsoleKey, ConsoleModifiers)> ExitKeys` for Enter and Ctrl+D
- Update ReadLine loop to check ExitKeys after executing handler
- Remove all 47 `return true;` statements
- Use method group syntax where possible (e.g., `HandleBackwardChar` instead of `() => { HandleBackwardChar(); return true; }`)
- All existing REPL tests must pass
- No behavioral changes
- Code compiles without warnings

## Checklist

### Design
- [x] Review current key binding architecture in ReplConsoleReader
- [x] Identify all exit keys (Enter, Ctrl+D)
- [x] Plan ReadLine loop refactoring

### Implementation
- [x] Change KeyBindings field type from `Dictionary<..., Func<bool>>` to `Dictionary<..., Action>`
- [x] Add ExitKeys field: `HashSet<(ConsoleKey, ConsoleModifiers)>`
- [x] Update InitializeKeyBindings() to return Action-based dictionary
  - [x] Remove all `return true;` statements
  - [x] Use lambda only when needed: `() => HandleTabCompletion(reverse: false)`
  - [x] Use method groups where possible: `HandleBackwardChar`
- [x] Create InitializeExitKeys() method
  - [x] Add (ConsoleKey.Enter, ConsoleModifiers.None)
  - [x] Add (ConsoleKey.D, ConsoleModifiers.Control)
- [x] Update ReadLine() loop logic
  - [x] Execute handler as Action (no return value)
  - [x] Check `if (ExitKeys.Contains(keyBinding))` after handler
  - [x] Return UserInput or null based on key type
- [x] Build solution and fix any compilation errors
- [x] Verify Roslynator rules pass

### Testing
- [x] Run all REPL tests (should pass unchanged)
- [x] Verify Enter still submits command
- [x] Verify Ctrl+D still exits REPL
- [x] Verify all other keys still function correctly
- [ ] Manual testing in REPL session

### Documentation
- [x] Add code comments explaining ExitKeys pattern
- [ ] Update any relevant documentation

## Notes

### Current Architecture Issues

**Repetitive Pattern** (47 occurrences):
```csharp
[(ConsoleKey.Tab, ConsoleModifiers.None)] = () => { HandleTabCompletion(reverse: false); return true; },
[(ConsoleKey.LeftArrow, ConsoleModifiers.None)] = () => { HandleBackwardChar(); return true; },
```

**Exit Keys** (2 occurrences):
```csharp
[(ConsoleKey.Enter, ConsoleModifiers.None)] = () => { HandleEnter(); return false; },
[(ConsoleKey.D, ConsoleModifiers.Control)] = () => { Terminal.WriteLine(); return false; },
```

### After Refactoring

**Simplified Bindings**:
```csharp
private readonly Dictionary<(ConsoleKey, ConsoleModifiers), Action> KeyBindings;
private readonly HashSet<(ConsoleKey, ConsoleModifiers)> ExitKeys;

private Dictionary<(ConsoleKey, ConsoleModifiers), Action> InitializeKeyBindings()
{
  return new()
  {
    // Method group syntax where possible
    [(ConsoleKey.LeftArrow, ConsoleModifiers.None)] = HandleBackwardChar,
    [(ConsoleKey.RightArrow, ConsoleModifiers.None)] = HandleForwardChar,
    
    // Lambda only when needed for parameters
    [(ConsoleKey.Tab, ConsoleModifiers.None)] = () => HandleTabCompletion(reverse: false),
    [(ConsoleKey.Tab, ConsoleModifiers.Shift)] = () => HandleTabCompletion(reverse: true),
    
    // Exit keys handled separately
    [(ConsoleKey.Enter, ConsoleModifiers.None)] = HandleEnter,
    [(ConsoleKey.D, ConsoleModifiers.Control)] = () => Terminal.WriteLine(),
  };
}

private HashSet<(ConsoleKey, ConsoleModifiers)> InitializeExitKeys()
{
  return new()
  {
    (ConsoleKey.Enter, ConsoleModifiers.None),
    (ConsoleKey.D, ConsoleModifiers.Control)
  };
}
```

**Updated ReadLine Loop**:
```csharp
if (KeyBindings.TryGetValue(keyBinding, out Action? handler))
{
  handler();
  
  // Check if this key should exit the read loop
  if (ExitKeys.Contains(keyBinding))
  {
    return keyInfo.Key == ConsoleKey.Enter ? UserInput : null;
  }
}
```

### Benefits

✅ **~50 fewer lines** of repetitive `return true;` statements
✅ **Clearer intent** - most keys continue, only 2 exit
✅ **Method group syntax** - more concise where applicable
✅ **Foundation for Phase 2** - profiles can easily wrap this pattern
✅ **No behavioral changes** - purely internal refactoring

### Code Locations

**File to Modify**: `Source/TimeWarp.Nuru.Repl/Input/ReplConsoleReader.cs`

**Methods to Change**:
- `InitializeKeyBindings()` - lines 67-121 (return Action instead of Func<bool>)
- `ReadLine()` - lines 143-168 (update loop logic)

**New Method**:
- `InitializeExitKeys()` - create new method

### Estimated Effort

| Phase | Time |
|-------|------|
| Design review | 15 min |
| Implementation | 30-45 min |
| Test & verify | 15 min |
| **TOTAL** | **1-1.5 hours** |

**Recommendation**: Complete in single focused session.

### Risk Assessment

**RISK LEVEL: LOW**

**Why?**:
- Internal refactoring only (no API changes)
- Logic stays the same, just reorganized
- Easy to rollback via Git revert
- All existing tests will catch regressions
- No new dependencies

### Success Criteria

- [ ] KeyBindings uses Action type
- [ ] ExitKeys HashSet created
- [ ] Zero `return true;` statements in InitializeKeyBindings
- [ ] All REPL tests pass (18/18 tab completion + others)
- [ ] Code compiles without warnings
- [ ] Manual testing confirms no behavior changes

### Future Work

This task is Phase 1 of the key binding evolution:
- **Task 056** (Phase 2): Add IKeyBindingProfile with Emacs/Vi/VSCode modes
- **Task 057** (Phase 3): Add custom key bindings via builder API

The Action-based pattern established here will make Phase 2 straightforward:
```csharp
// Phase 2 will wrap this cleanly
public interface IKeyBindingProfile
{
  Dictionary<(ConsoleKey, ConsoleModifiers), Action> GetBindings();
  HashSet<(ConsoleKey, ConsoleModifiers)> GetExitKeys();
}
```

## Implementation Notes

**Completed:** 2025-11-25

**Commit:** e4aa9bbe - "Refactor REPL key bindings from Func<bool> to Action pattern"

**Changes Made:**
1. Changed `KeyBindings` from `Dictionary<..., Func<bool>>` to `Dictionary<..., Action>`
2. Added `ExitKeys` as `HashSet<(ConsoleKey, ConsoleModifiers)>`
3. Refactored `InitializeKeyBindings()`:
   - Removed all 47 `return true;` statements
   - Used method group syntax (e.g., `HandleBackwardChar` instead of `() => { HandleBackwardChar(); return true; }`)
   - Used lambdas only when parameters needed (e.g., `() => HandleTabCompletion(reverse: false)`)
4. Added `InitializeExitKeys()` static method using collection expression syntax
5. Updated `ReadLine()` loop to execute Action and check `ExitKeys.Contains()`

**Test Results:**
- Session lifecycle: 11/11 ✅
- PSReadLine keybindings: 25/25 ✅
- Escape key: 3/3 ✅
- Tab completion: 8/8 ✅
- **Total: 47/47 tests passing** ✅

**Code Quality:**
- Full solution builds with 0 warnings, 0 errors
- All Roslynator rules pass
- Reduced code from ~54 lines to ~49 lines in InitializeKeyBindings()
- Eliminated 47 redundant return statements

**Next Steps:**
- Manual testing in REPL session (user must perform - Claude cannot test interactive features)
- Consider moving task to Done folder after user confirms manual testing
- Phase 2 (Task 056): Implement IKeyBindingProfile with Emacs/Vi/VSCode modes
