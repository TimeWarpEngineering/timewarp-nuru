# Add Missing Handlers for VSCode Profile

## Description

Implement missing handler methods required to fully support VSCodeKeyBindingProfile. Currently, the VSCode profile is partially implemented but lacks two handler methods that are standard in VSCode and modern IDEs.

**Goal**: Complete VSCodeKeyBindingProfile implementation so users can use VSCode-style key bindings in REPL.

## Parent

Related to Task 056_Add-Key-Binding-Profiles (completed - Phase 2)
Prerequisite for making VSCodeKeyBindingProfile fully functional

## Requirements

- Implement two missing handler methods in ReplConsoleReader:
  1. `HandleKillLine()` - Delete from cursor to end (shared with Emacs/Vi)
  2. `HandleDeleteWordBackward()` - Delete word before cursor (shared with Vi)
- Handlers should be `internal` for profile access
- Update VSCodeKeyBindingProfile to use the new handlers
- All existing REPL tests must pass
- Add tests for VSCode-specific keybindings
- Manual testing confirms VSCode profile works completely
- Code compiles without warnings

## Checklist

### Design
- [ ] Review VSCode editor keybindings
- [ ] Confirm Ctrl+K and Ctrl+Backspace behavior
- [ ] Verify compatibility with Windows/Mac/Linux

### Implementation

#### HandleKillLine (Shared with Emacs/Vi)
- [ ] Check if implemented in Task 058
- [ ] If not, implement as described in Task 058
- [ ] Verify it's `internal`

#### HandleDeleteWordBackward (Shared with Vi)
- [ ] Check if implemented in Task 059
- [ ] If not, implement as described in Task 059
- [ ] Verify it's `internal`

#### Profile Update
- [ ] Update VSCodeKeyBindingProfile.cs
  - [ ] Uncomment Ctrl+K binding (HandleKillLine)
  - [ ] Uncomment Ctrl+Backspace binding (HandleDeleteWordBackward)
- [ ] Build solution and fix compilation errors

### Testing
- [ ] Add tests to `repl-23-key-binding-profiles.cs` or create `repl-26-vscode-handlers.cs`
  - [ ] Test Ctrl+K deletes to end
  - [ ] Test Ctrl+Backspace deletes word backward
  - [ ] Test interaction with Ctrl+Arrow word movement
  - [ ] Verify no conflicts with other bindings
- [ ] Run all existing REPL tests
- [ ] Manual testing with VSCode profile
  - [ ] Ctrl+K works like VSCode
  - [ ] Ctrl+Backspace works like VSCode
  - [ ] Ctrl+Left/Right word movement still works

### Documentation
- [ ] Update VSCodeKeyBindingProfile documentation
- [ ] Mark VSCode profile as COMPLETE in Task 056 notes
- [ ] Update user documentation if needed
- [ ] Note platform-specific keybinding differences (if any)

## Notes

### VSCode Key Bindings

VSCode uses modern IDE conventions:
- **Ctrl+K**: Delete from cursor to end of line (Kill Line)
- **Ctrl+Backspace**: Delete word before cursor (Delete Word Left)
- **Ctrl+Delete**: Delete word after cursor (not implemented yet - future)

**Note**: On macOS, these typically use Cmd instead of Ctrl, but REPL runs in terminal where Cmd is not available, so Ctrl is used.

### Handler Reuse

Both required handlers are shared with other profiles:
1. **HandleKillLine()**: Shared with Emacs (Task 058) and Vi (Task 059)
2. **HandleDeleteWordBackward()**: Shared with Vi (Task 059)

If Tasks 058 and 059 are complete, this task is mostly just uncommenting bindings in VSCodeKeyBindingProfile.

### VSCodeKeyBindingProfile Bindings to Uncomment

```csharp
// In VSCodeKeyBindingProfile.cs - currently commented out:

// Line ~78: Kill to end of line
// [(ConsoleKey.K, ConsoleModifiers.Control)] = () => reader.HandleKillLine(),

// Line ~81: Delete word backward
// [(ConsoleKey.Backspace, ConsoleModifiers.Control)] = () => reader.HandleDeleteWordBackward(),
```

### Code Locations

**File to Check**: `Source/TimeWarp.Nuru.Repl/Input/ReplConsoleReader.cs`
- Verify `HandleKillLine()` exists (from Task 058)
- Verify `HandleDeleteWordBackward()` exists (from Task 059)

**File to Update**: `Source/TimeWarp.Nuru.Repl/VSCodeKeyBindingProfile.cs`
- Uncomment two key bindings (lines ~78, ~81)

**Test File**: Add tests to `Tests/TimeWarp.Nuru.Repl.Tests/repl-23-key-binding-profiles.cs` or create `repl-26-vscode-handlers.cs`

### Task Scope

**Size**: Small task
- Both required handlers should exist from Tasks 058 and 059
- Primarily uncommenting existing bindings in profile
- Simple verification and testing
- Should fit easily in one AI context window

**Note**: This task depends on completion of Tasks 058 and 059.

### Risk Assessment

**RISK LEVEL: VERY LOW**

**Why?**
- Handlers should already exist from Tasks 058 and 059
- Just uncommenting existing bindings
- VSCode is well-documented and consistent
- No platform-specific issues (runs in terminal)

### Success Criteria

- [ ] HandleKillLine() exists and is `internal`
- [ ] HandleDeleteWordBackward() exists and is `internal`
- [ ] VSCodeKeyBindingProfile bindings uncommented
- [ ] All existing tests pass
- [ ] New tests for VSCode profile pass
- [ ] Manual testing with VSCode profile works
- [ ] Ctrl+K deletes to end
- [ ] Ctrl+Backspace deletes word backward
- [ ] Code compiles without warnings

### VSCode Keybinding Comparison

| Action | VSCode (Windows/Linux) | VSCode (macOS) | REPL |
|--------|----------------------|----------------|------|
| Delete to end | Ctrl+K | Cmd+K | Ctrl+K |
| Delete word left | Ctrl+Backspace | Option+Backspace | Ctrl+Backspace |
| Delete word right | Ctrl+Delete | Option+Delete | (future) |
| Move word left | Ctrl+Left | Option+Left | Ctrl+Left |
| Move word right | Ctrl+Right | Option+Right | Ctrl+Right |

**Note**: Terminal limitations mean Cmd/Option not available, so Ctrl is used for all modifiers in REPL.

### Related Tasks

- **Task 058**: Add missing handlers for Emacs profile (HandleKillLine)
- **Task 059**: Add missing handlers for Vi profile (HandleDeleteWordBackward + others)
- **Future**: Add Ctrl+Delete (delete word forward) handler

### Platform Considerations

**Windows/Linux**:
- Ctrl+Backspace works directly
- Ctrl+K works directly

**macOS**:
- Terminal typically uses Ctrl (not Cmd)
- Should work same as Windows/Linux in terminal context

**Testing**: Test on all three platforms if possible, but terminal behavior should be consistent.

### Future Enhancements

**Additional VSCode Keybindings** (separate task):
- Ctrl+Delete: Delete word forward
- Ctrl+Shift+K: Delete entire line
- Ctrl+/: Toggle line comment (context-specific)
- Alt+Up/Down: Move line up/down (multi-line support needed)

This task focuses on the two most essential VSCode editing shortcuts that match existing handler implementations.
