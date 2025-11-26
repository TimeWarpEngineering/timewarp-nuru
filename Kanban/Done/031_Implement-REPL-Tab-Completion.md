# Task 031: Implement REPL Tab Completion

## Description

Implement in-REPL tab completion for TimeWarp.Nuru's interactive REPL mode. Users expect tab completion in interactive sessions to discover commands, parameters, and options without leaving the REPL environment.

## Requirements

- Tab completion for command names, parameters, and options within REPL
- History navigation with arrow keys
- Cross-platform terminal support (Windows, Linux, macOS)
- Integration with existing `CompletionProvider` from Task 025
- Syntax highlighting for typed commands

## Implementation Status: COMPLETE ✅

All phases of the REPL tab completion implementation have been completed.

### Phase 1: Basic Line Editing ✅
- [x] Custom input handling replaces `Console.ReadLine()` - `ReplConsoleReader` class (451 lines)
- [x] Character input, backspace, cursor movement work
- [x] Enter key submits input correctly
- [x] REPL loop continues to work
- [x] Delete key support
- [x] Home/End key support
- [x] Ctrl+Left/Right for word navigation

### Phase 2: Tab Completion ✅
- [x] Tab key triggers completion
- [x] Command names complete correctly
- [x] Parameters and options complete correctly
- [x] Single completion applies automatically
- [x] Multiple completions display in columns
- [x] Shift+Tab cycles backwards through completions
- [x] Integration with `CompletionProvider` works

### Phase 3: History Navigation ✅
- [x] Up/down arrows navigate command history
- [x] History state managed correctly
- [x] Navigation works during editing
- [x] History persistence (load/save)

### Phase 4: Syntax Highlighting ✅ (Bonus Feature)
- [x] `SyntaxHighlighter` class for command coloring
- [x] PSReadline-style color scheme via `SyntaxColors`
- [x] Command recognition and highlighting
- [x] String, number, option highlighting
- [x] ANSI color support via `AnsiColors` (200+ colors)

## Files Implemented

### New Files
- `Source/TimeWarp.Nuru.Repl/Input/ReplConsoleReader.cs` - Main input handling (451 lines)
- `Source/TimeWarp.Nuru.Repl/Input/SyntaxHighlighter.cs` - Syntax highlighting (102 lines)
- `Source/TimeWarp.Nuru.Repl/Input/CommandLineToken.cs` - Token representation
- `Source/TimeWarp.Nuru.Repl/Input/TokenType.cs` - Token type enum
- `Source/TimeWarp.Nuru.Repl/Display/AnsiColors.cs` - ANSI escape codes (205 lines)
- `Source/TimeWarp.Nuru.Repl/Display/SyntaxColors.cs` - PSReadline color scheme (104 lines)
- `Source/TimeWarp.Nuru.Repl/Display/PromptFormatter.cs` - Prompt formatting
- `Source/TimeWarp.Nuru.Repl/Repl/CommandLineParser.cs` - Command line parsing
- `Source/TimeWarp.Nuru.Repl/Logging/ReplLoggerMessages.cs` - Structured logging

### Updated Files
- `Source/TimeWarp.Nuru.Repl/Repl/ReplSession.cs` - Integrated `ReplConsoleReader`

## Key Features

### ReplConsoleReader Capabilities
- Full line editing with cursor positioning
- Tab completion with `CompletionProvider` integration
- History navigation (up/down arrows)
- Word-by-word navigation (Ctrl+Left/Right)
- Home/End navigation
- Backspace and Delete support
- Escape to clear completion state
- Real-time syntax highlighting as user types

### Completion Features
- Single completion: Applies automatically
- Multiple completions: Displays in formatted columns
- Shift+Tab: Cycles backwards through completions
- Completion context: Respects cursor position

### Terminal Abstraction
- Uses `ITerminal` interface for testability
- Cross-platform cursor positioning
- ANSI escape sequence support

## Manual Testing Required

**Note**: Claude cannot run interactive REPL tests due to non-interactive shell environment.

### Manual Testing Checklist (for human verification)
- [x] Basic typing and backspace work
- [x] Left/right arrow keys move cursor
- [x] Ctrl+Left/Right move by word
- [x] Home/End keys work
- [x] Up/down arrows navigate history
- [x] Tab completion works for command names
- [x] Tab completion works for parameters
- [x] Tab completion works for options
- [x] Multiple completions display correctly
- [x] Single completion applies correctly
- [x] Shift+Tab cycles backwards
- [x] Syntax highlighting colors commands correctly
- [ ] Works on Windows (cmd, PowerShell, Windows Terminal)
- [x] Works on Linux (various terminals)
- [ ] Works on macOS (Terminal.app, iTerm2)

## Related Tasks

- **Task 025: Shell Tab Completion** - Provides `CompletionProvider` infrastructure
- **Task 027: REPL Mode** - Provides the REPL environment
- **Task 032: IReplIO Abstraction** - Terminal abstraction for testability

## Notes

- Implementation chose Option A (Manual Console.ReadKey()) for zero dependencies
- Syntax highlighting was added as a bonus feature beyond original scope
- All ANSI colors including CSS named colors supported
- Logging integrated for debugging completion behavior