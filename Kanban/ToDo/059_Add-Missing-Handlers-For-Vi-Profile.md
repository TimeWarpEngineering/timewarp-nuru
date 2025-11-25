# Add Missing Handlers for Vi Profile

## Description

Implement missing handler methods required to fully support ViKeyBindingProfile. Currently, the Vi profile is partially implemented but lacks three handler methods that are common in Vi-style editing. This task focuses on Vi insert-mode compatible handlers.

**Goal**: Complete ViKeyBindingProfile implementation so users can use Vi-inspired key bindings in REPL.

## Parent

Related to Task 056_Add-Key-Binding-Profiles (completed - Phase 2)
Prerequisite for making ViKeyBindingProfile fully functional

## Requirements

- Implement three missing handler methods in ReplConsoleReader:
  1. `HandleKillLine()` - Delete from cursor to end (shared with Emacs)
  2. `HandleDeleteWordBackward()` - Delete word before cursor (Ctrl+W)
  3. `HandleDeleteToLineStart()` - Delete from start to cursor (Ctrl+U)
- Handlers should be `internal` for profile access
- Update ViKeyBindingProfile to use the new handlers
- All existing REPL tests must pass
- Add tests for Vi-specific functionality
- Manual testing confirms Vi profile works completely
- Code compiles without warnings

## Checklist

### Design
- [ ] Review Vi insert-mode editing behavior
- [ ] Review bash/readline Ctrl+W and Ctrl+U behavior
- [ ] Confirm word boundaries (whitespace vs punctuation)
- [ ] Plan handler method signatures

### Implementation

#### HandleKillLine (Shared with Emacs)
- [ ] Check if implemented in Task 058
- [ ] If not, implement as described in Task 058
- [ ] Make it `internal`

#### HandleDeleteWordBackward
- [ ] Add `HandleDeleteWordBackward()` method to ReplConsoleReader
  - [ ] Make it `internal`
  - [ ] Find word boundary before cursor
  - [ ] Delete from word start to cursor
  - [ ] Update `UserInput` and `CursorPosition`
  - [ ] Call `RedrawLine()`
  - [ ] Add structured logging
  - [ ] XML documentation
- [ ] Use similar word boundary logic as `HandleBackwardWord()`

#### HandleDeleteToLineStart
- [ ] Add `HandleDeleteToLineStart()` method to ReplConsoleReader
  - [ ] Make it `internal`
  - [ ] Delete from line start to cursor
  - [ ] Move cursor to position 0
  - [ ] Update `UserInput`
  - [ ] Call `RedrawLine()`
  - [ ] Add structured logging
  - [ ] XML documentation

#### Profile Update
- [ ] Update ViKeyBindingProfile.cs
  - [ ] Uncomment Ctrl+K binding (HandleKillLine)
  - [ ] Uncomment Ctrl+W binding (HandleDeleteWordBackward)
  - [ ] Uncomment Ctrl+U binding (HandleDeleteToLineStart)
- [ ] Build solution and fix compilation errors

### Testing
- [ ] Add tests to `repl-23-key-binding-profiles.cs` or create `repl-25-vi-handlers.cs`
  - [ ] Test HandleDeleteWordBackward:
    - [ ] Single word deletion
    - [ ] Multiple words
    - [ ] At word boundary
    - [ ] At line start (no-op)
  - [ ] Test HandleDeleteToLineStart:
    - [ ] Delete from middle of line
    - [ ] At line start (no-op)
    - [ ] With empty line
  - [ ] Test HandleKillLine (if not tested in Task 058)
- [ ] Run all existing REPL tests
- [ ] Manual testing with Vi profile

### Documentation
- [ ] Update ViKeyBindingProfile documentation
- [ ] Mark Vi profile as COMPLETE in Task 056 notes
- [ ] Document differences from full Vi modal editing
- [ ] Update user documentation if needed

## Notes

### Vi Insert Mode Behavior

Vi profile focuses on **insert mode** compatible bindings, not full modal editing (normal/insert/visual modes). Full modal Vi is a future enhancement.

**Insert Mode Key Bindings**:
- **Ctrl+W**: Delete word before cursor (bash/readline standard)
- **Ctrl+U**: Delete from line start to cursor (bash/readline standard)
- **Ctrl+K**: Delete from cursor to end (borrowed from Emacs, common in bash)

### Implementation Examples

#### HandleDeleteWordBackward
```csharp
/// <summary>
/// Vi/bash: DeleteWordBackward - Delete word before cursor (Ctrl+W).
/// </summary>
internal void HandleDeleteWordBackward()
{
  if (CursorPosition > 0)
  {
    int wordStart = CursorPosition;
    
    // Skip whitespace behind cursor
    while (wordStart > 0 && char.IsWhiteSpace(UserInput[wordStart - 1]))
      wordStart--;
    
    // Skip word characters to find start of word
    while (wordStart > 0 && !char.IsWhiteSpace(UserInput[wordStart - 1]))
      wordStart--;
    
    ReplLoggerMessages.DeleteWordBackwardTriggered(Logger, CursorPosition, wordStart, null);
    
    // Delete from wordStart to cursor
    UserInput = UserInput[..wordStart] + UserInput[CursorPosition..];
    CursorPosition = wordStart;
    
    ReplLoggerMessages.UserInputChanged(Logger, UserInput, CursorPosition, null);
    RedrawLine();
  }
}
```

#### HandleDeleteToLineStart
```csharp
/// <summary>
/// Vi/bash: DeleteToLineStart - Delete from line start to cursor (Ctrl+U).
/// </summary>
internal void HandleDeleteToLineStart()
{
  if (CursorPosition > 0)
  {
    ReplLoggerMessages.DeleteToLineStartTriggered(Logger, CursorPosition, null);
    
    // Keep text after cursor, delete everything before
    UserInput = UserInput[CursorPosition..];
    CursorPosition = 0;
    
    ReplLoggerMessages.UserInputChanged(Logger, UserInput, CursorPosition, null);
    RedrawLine();
  }
}
```

### ViKeyBindingProfile Bindings to Uncomment

```csharp
// In ViKeyBindingProfile.cs - currently commented out:

// Line ~90: Kill to end of line
// [(ConsoleKey.K, ConsoleModifiers.Control)] = () => reader.HandleKillLine(),

// Line ~93: Delete word backward  
// [(ConsoleKey.W, ConsoleModifiers.Control)] = () => reader.HandleDeleteWordBackward(),

// Line ~96: Delete to line start
// [(ConsoleKey.U, ConsoleModifiers.Control)] = () => reader.HandleDeleteToLineStart(),
```

### Code Locations

**File to Modify**: `Source/TimeWarp.Nuru.Repl/Input/ReplConsoleReader.cs`
- Add three new handler methods (around line 400, with other edit handlers)

**File to Update**: `Source/TimeWarp.Nuru.Repl/ViKeyBindingProfile.cs`
- Uncomment three key bindings (lines ~90, ~93, ~96)

**Test File**: Add tests to `Tests/TimeWarp.Nuru.Repl.Tests/repl-23-key-binding-profiles.cs` or create `repl-25-vi-handlers.cs`

### Estimated Effort

| Phase | Time |
|-------|------|
| Implementation (3 handlers) | 1.5 hours |
| Testing | 1 hour |
| Documentation | 30 min |
| **TOTAL** | **3 hours** |

### Risk Assessment

**RISK LEVEL: LOW**

**Why?**
- Similar to existing delete handlers
- String manipulation is straightforward
- Word boundary logic already exists in HandleBackwardWord
- No complex state management
- Easy to test

### Success Criteria

- [ ] All 3 handlers implemented and `internal`
- [ ] HandleKillLine() works (may be from Task 058)
- [ ] HandleDeleteWordBackward() deletes word correctly
- [ ] HandleDeleteToLineStart() clears to start correctly
- [ ] ViKeyBindingProfile bindings uncommented
- [ ] All existing tests pass
- [ ] New tests for Vi handlers pass
- [ ] Manual testing with Vi profile works
- [ ] Code compiles without warnings

### Word Boundary Logic

Use same word boundary definition as existing `HandleBackwardWord()`:
- Whitespace characters separate words
- Stop at first whitespace when moving backward
- Consistent with PSReadLine behavior

### Related Tasks

- **Task 058**: Add missing handlers for Emacs profile (1 handler)
- **Task 060**: Add missing handlers for VSCode profile (2 handlers)
- **Future**: Implement full Vi modal editing (normal/insert/visual modes)

### Future Enhancements

**Full Vi Modal Editing** (separate task, significant effort):
- Normal mode: h/j/k/l navigation, dd/yy/p commands
- Insert mode: i/a/o to enter, Escape to exit
- Visual mode: v to select, y/d to yank/delete
- Command mode: :w, :q, etc.
- Mode indicator in prompt

This task focuses on **insert-mode compatible** bindings only, which are useful without full modal implementation.
