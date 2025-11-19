# Task 031: Implement REPL Tab Completion

## Problem

TimeWarp.Nuru has a working REPL mode (Task 027) but lacks in-REPL tab completion. Users expect tab completion in interactive sessions to discover commands, parameters, and options without leaving the REPL environment.

## Current State

- ✅ REPL mode works (Task 027) - interactive command execution, history, help
- ✅ Shell completion works (Task 025) - tab completion in bash/zsh/PowerShell
- ❌ **In-REPL tab completion** - missing feature

## Technical Challenges

### Challenge 1: Library Selection
- **ReadLine NuGet package**: Abandoned 7+ years ago, not maintained
- **Spectre.Console**: Rich UI but no native tab completion support
- **Manual Console.ReadKey()**: Requires 200-300 lines of fragile cross-platform code
- **System.CommandLine**: Has console APIs but may be heavy dependency

### Challenge 2: Cross-Platform Terminal Behavior
- Windows vs Linux vs macOS terminal differences
- Terminal capability detection
- Key code handling (arrow keys, tab, backspace, etc.)
- Cursor positioning and screen manipulation

### Challenge 3: Integration with Existing Completion Infrastructure
- Reuse `CompletionProvider` from Task 025
- Adapt shell completion context to REPL context
- Handle partial input and cursor positioning
- Display completion candidates appropriately

## Solution Approach

### Option A: Manual Console.ReadKey() Implementation
**Pros:**
- Zero external dependencies
- Full control over behavior
- Lightweight

**Cons:**
- Complex implementation (200-300 lines)
- Cross-platform compatibility challenges
- High maintenance burden
- Must handle: cursor positioning, line editing, history navigation, multi-line display

### Option B: System.CommandLine Console APIs
**Pros:**
- Modern, actively maintained
- Built-in tab completion support
- Cross-platform
- Microsoft-backed

**Cons:**
- Additional dependency
- May have API conflicts with Nuru
- Learning curve for integration

### Option C: Custom Lightweight Library
**Pros:**
- Tailored to Nuru's needs
- Can be minimal and focused
- Reuse existing completion infrastructure

**Cons:**
- Development effort
- Testing across platforms
- Maintenance overhead

## Recommended Approach: Option A (Manual Implementation)

Start with manual `Console.ReadKey()` implementation because:
1. **Zero dependencies** - Keeps REPL lightweight
2. **Learning opportunity** - Understanding terminal behavior
3. **Incremental** - Can start basic and enhance over time
4. **Control** - Full customization for Nuru's specific needs

## Implementation Plan

### Phase 1: Basic Line Editing
**Goal**: Replace `Console.ReadLine()` with custom input handling

**Files to Create:**
1. **Source/TimeWarp.Nuru.Repl/Console/ReplConsoleReader.cs** (~200 lines)
   ```csharp
   public class ReplConsoleReader
   {
       private string _currentLine = string.Empty;
       private int _cursorPosition = 0;
       private readonly List<string> _history;
       private int _historyIndex = -1;

       public string ReadLine(string prompt)
       {
           Console.Write(prompt);
           
           while (true)
           {
               var key = Console.ReadKey(true);
               
               switch (key.Key)
               {
                   case ConsoleKey.Tab:
                       HandleTabCompletion();
                       break;
                   case ConsoleKey.Enter:
                       return HandleEnter();
                   case ConsoleKey.Backspace:
                       HandleBackspace();
                       break;
                   case ConsoleKey.LeftArrow:
                       HandleLeftArrow();
                       break;
                   case ConsoleKey.RightArrow:
                       HandleRightArrow();
                       break;
                   case ConsoleKey.UpArrow:
                       HandleUpArrow();
                       break;
                   case ConsoleKey.DownArrow:
                       HandleDownArrow();
                       break;
                   default:
                       if (!char.IsControl(key.KeyChar))
                           HandleCharacter(key.KeyChar);
                       break;
               }
           }
       }
   }
   ```

**Implementation Steps:**
1. Create basic character input handling
2. Implement backspace and cursor movement
3. Add line editing (insert/overwrite mode)
4. Handle Enter key to submit input
5. Integrate with existing REPL loop

### Phase 2: Tab Completion Integration
**Goal**: Integrate with `CompletionProvider` from Task 025

**Files to Update:**
1. **Source/TimeWarp.Nuru.Repl/Console/ReplConsoleReader.cs**
   ```csharp
   private void HandleTabCompletion()
   {
       // Parse current input into arguments
       var args = ParseCommandLine(_currentLine);
       
       // Build completion context
       var context = new CompletionContext(
           Args: args,
           CursorPosition: _cursorPosition,
           Endpoints: _endpoints
       );
       
       // Get completion candidates
       var candidates = _completionProvider.GetCompletions(context);
       
       if (candidates.Count == 0)
           return;
           
       if (candidates.Count == 1)
       {
           // Single completion - apply it
           ApplyCompletion(candidates.First());
       }
       else
       {
           // Multiple completions - show them
           ShowCompletions(candidates);
       }
   }
   ```

**Implementation Steps:**
1. Integrate `CompletionProvider` from Task 025
2. Parse current input and cursor position
3. Handle single vs multiple completion scenarios
4. Implement completion application logic
5. Add completion display (list below prompt or inline)

### Phase 3: History Navigation
**Goal**: Add up/down arrow history navigation

**Files to Update:**
1. **Source/TimeWarp.Nuru.Repl/Console/ReplConsoleReader.cs**
   ```csharp
   private void HandleUpArrow()
   {
       if (_historyIndex < _history.Count - 1)
       {
           _historyIndex++;
           ReplaceLine(_history[_history.Count - 1 - _historyIndex]);
       }
   }
   
   private void HandleDownArrow()
   {
       if (_historyIndex > 0)
       {
           _historyIndex--;
           ReplaceLine(_history[_history.Count - 1 - _historyIndex]);
       }
       else if (_historyIndex == 0)
       {
           _historyIndex = -1;
           ReplaceLine(string.Empty);
       }
   }
   ```

**Implementation Steps:**
1. Load history from existing REPL history system
2. Implement up/down arrow navigation
3. Handle history state correctly
4. Maintain cursor position during navigation

### Phase 4: Polish and Cross-Platform Compatibility
**Goal**: Refine behavior and ensure cross-platform support

**Implementation Steps:**
1. **Windows Terminal Support**: Handle Windows-specific key codes
2. **Unix Terminal Support**: Handle Linux/macOS terminal behavior
3. **ANSI Escape Sequences**: Use for cursor positioning and colors
4. **Error Handling**: Graceful fallback for unsupported terminals
5. **Performance**: Optimize for responsive typing

## Files to Modify

### New Files
- `Source/TimeWarp.Nuru.Repl/Console/ReplConsoleReader.cs` - Main input handling
- `Source/TimeWarp.Nuru.Repl/Console/CompletionDisplay.cs` - Completion candidate display
- `Source/TimeWarp.Nuru.Repl/Console/CursorManager.cs` - Cross-platform cursor operations

### Updated Files
- `Source/TimeWarp.Nuru.Repl/Repl/ReplMode.cs` - Replace `Console.ReadLine()` with `ReplConsoleReader`
- `Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj` - No new dependencies needed

## Test Scenarios

### Automated Tests
1. **repl-console-reader-01-basic-input.cs** - Test character input, backspace, enter
2. **repl-console-reader-02-cursor-movement.cs** - Test arrow key navigation
3. **repl-console-reader-03-completion.cs** - Test tab completion logic
4. **repl-console-reader-04-history.cs** - Test history navigation

### Manual Testing Checklist
- [ ] Basic typing and backspace work
- [ ] Left/right arrow keys move cursor
- [ ] Up/down arrows navigate history
- [ ] Tab completion works for command names
- [ ] Tab completion works for parameters
- [ ] Tab completion works for options
- [ ] Multiple completions display correctly
- [ ] Single completion applies correctly
- [ ] Works on Windows (cmd, PowerShell, Windows Terminal)
- [ ] Works on Linux (various terminals)
- [ ] Works on macOS (Terminal.app, iTerm2)

## Success Criteria

### Phase 1: Basic Line Editing
- [ ] Custom input handling replaces `Console.ReadLine()`
- [ ] Character input, backspace, cursor movement work
- [ ] Enter key submits input correctly
- [ ] REPL loop continues to work

### Phase 2: Tab Completion
- [ ] Tab key triggers completion
- [ ] Command names complete correctly
- [ ] Parameters and options complete correctly
- [ ] Single vs multiple completion scenarios handled
- [ ] Integration with `CompletionProvider` works

### Phase 3: History Navigation
- [ ] Up/down arrows navigate command history
- [ ] History state managed correctly
- [ ] Navigation works during editing

### Phase 4: Polish
- [ ] Cross-platform compatibility verified
- [ ] Responsive typing experience
- [ ] Error handling for edge cases
- [ ] All automated tests pass

## Timeline Estimate

### Phase 1: Basic Line Editing
- Design and planning: 2 hours
- Implement `ReplConsoleReader`: 4 hours
- Basic input handling: 2 hours
- Cursor movement: 2 hours
- Testing and debugging: 2 hours
- **Subtotal: 12 hours**

### Phase 2: Tab Completion Integration
- Study `CompletionProvider` API: 1 hour
- Implement tab completion logic: 3 hours
- Completion display logic: 2 hours
- Integration testing: 2 hours
- **Subtotal: 8 hours**

### Phase 3: History Navigation
- Implement history navigation: 2 hours
- State management: 1 hour
- Testing: 1 hour
- **Subtotal: 4 hours**

### Phase 4: Polish and Cross-Platform
- Cross-platform testing: 3 hours
- ANSI escape sequence handling: 2 hours
- Performance optimization: 2 hours
- Edge case handling: 2 hours
- **Subtotal: 9 hours**

**Total Estimate: 33 hours** (~1 week of focused work)

## Priority Justification

**Priority: Medium**

### Why Not High?
- REPL is functional without tab completion
- Shell completion (Task 025) provides alternative
- Not blocking other features

### Why Not Low?
- **User Expectation**: Modern REPLs have tab completion
- **Discoverability**: Helps users learn commands
- **Competitive**: Other CLI frameworks have this feature
- **Completes REPL Experience**: Makes REPL fully-featured

## Related Tasks

- **Task 025: Shell Tab Completion** - Provides `CompletionProvider` infrastructure
- **Task 027: REPL Mode** - Provides the REPL environment to enhance
- **Task 028: Type-Aware Parameter Completion** - Superseded by dynamic completion

## Future Enhancements

### Out of Scope for Initial Implementation
1. **Syntax Highlighting** - Color commands, parameters, options as user types
2. **Inline Help** - Show parameter hints while typing
3. **Fuzzy Completion** - Fuzzy matching for completion candidates
4. **Multi-line Input** - Commands spanning multiple lines
5. **Custom Key Bindings** - User-configurable keyboard shortcuts

### Future Considerations
1. **Integration with System.CommandLine** - If manual approach proves too complex
2. **Performance Optimization** - For large command sets
3. **Accessibility** - Screen reader support
4. **Internationalization** - Right-to-left language support

## Notes

### Technical Considerations
- **ANSI Escape Sequences**: Will use for cursor positioning and colors
- **Terminal Detection**: May need to detect terminal capabilities
- **Fallback Behavior**: Graceful degradation if advanced features not supported
- **Memory Management**: Efficient handling of history and completion data

### Testing Strategy
- **Unit Tests**: For individual components (parsing, completion logic)
- **Integration Tests**: For REPL + completion interaction
- **Manual Testing**: Essential for cross-platform verification
- **User Testing**: Get feedback on typing experience

### Documentation Updates
- Update REPL documentation to mention tab completion
- Add examples of tab completion usage
- Document any configuration options
- Update troubleshooting guide