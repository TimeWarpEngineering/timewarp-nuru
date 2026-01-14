# REPL Test Plan

> **See also**: [Test Plan Overview](../test-plan-overview.md) for the three-layer testing architecture and shared philosophy.

This test plan covers the REPL (Read-Eval-Print Loop) functionality for TimeWarp.Nuru CLI applications, focusing on interactive command execution, history management, tab completion, and syntax highlighting.

## Scope

The REPL layer is responsible for:

1. **Session Management** - Starting, running, and terminating REPL sessions
2. **Input Processing** - Reading and parsing interactive user input
3. **History Management** - Storing, navigating, and persisting command history
4. **Tab Completion** - Providing context-aware command and parameter completions
5. **Syntax Highlighting** - Real-time colorization of input based on command structure
6. **Command Execution** - Routing REPL commands and application commands
7. **Error Recovery** - Graceful handling of errors in interactive mode

Tests use numbered files (repl-01, repl-02, etc.) for systematic coverage, with 15 sections covering ~120 test scenarios.

---

## Section 1: Session Lifecycle

**File**: `repl-01-session-lifecycle.cs`
**Purpose**: Verify REPL session initialization, execution, and cleanup

### Test Cases

1. **Basic Session Start**
   - Initialize: `app.RunReplAsync()`
   - Expected: Session starts, welcome message displayed
   - Verify: `ReplSession.CurrentSession` is set

2. **Session with Custom Options**
   - Initialize: `app.RunReplAsync(new ReplOptions { Prompt = ">>> " })`
   - Expected: Custom prompt displayed
   - Verify: Options applied correctly

3. **Session Exit via Command**
   - Input: `exit`
   - Expected: Session terminates cleanly
   - Verify: `ReplSession.CurrentSession` is null after exit

4. **Session Exit via Ctrl+D (EOF)**
   - Input: EOF signal
   - Expected: Session terminates with goodbye message
   - Verify: Proper cleanup executed

5. **Session Exit via Ctrl+C**
   - Input: Cancel key press
   - Expected: Graceful cancellation
   - Verify: `Running` flag set to false

6. **Multiple Sequential Sessions**
   - Run session 1, exit, run session 2
   - Expected: Each session independent
   - Verify: No state leakage between sessions

7. **Session with Error on Exit**
   - Force error during cleanup
   - Expected: Error logged, resources still released
   - Verify: No hanging resources

---

## Section 2: Command Line Parsing

**File**: `repl-02-command-parsing.cs`
**Purpose**: Verify command line string parsing with quotes and escapes

### Test Cases

1. **Simple Command Parsing**
   - Input: `status`
   - Expected: `["status"]`
   - Verify: Single argument extracted

2. **Multi-Word Command**
   - Input: `git status`
   - Expected: `["git", "status"]`
   - Verify: Space-separated arguments

3. **Quoted String Handling**
   - Input: `greet "John Doe"`
   - Expected: `["greet", "John Doe"]`
   - Verify: Quotes removed, spaces preserved

4. **Single Quote Handling**
   - Input: `deploy 'production'`
   - Expected: `["deploy", "production"]`
   - Verify: Single quotes work like double quotes

5. **Mixed Quotes**
   - Input: `echo "Hello" 'World'`
   - Expected: `["echo", "Hello", "World"]`
   - Verify: Both quote types handled

6. **Escaped Quotes**
   - Input: `echo "Hello\"World"`
   - Expected: `["echo", "Hello\"World"]`
   - Verify: Escape sequences processed

7. **Empty Quoted Strings**
   - Input: `cmd "" ''`
   - Expected: `["cmd", "", ""]`
   - Verify: Empty strings preserved

8. **Unclosed Quotes**
   - Input: `echo "unclosed`
   - Expected: Handles gracefully or reports error
   - Verify: No crash, sensible behavior

9. **Complex Mixed Pattern**
   - Input: `docker run -d --name "my container" nginx`
   - Expected: `["docker", "run", "-d", "--name", "my container", "nginx"]`
   - Verify: Options and quoted values parsed correctly

---

## Section 3: History Management

**File**: `repl-03-history-management.cs`
**Purpose**: Verify command history storage and navigation

### Test Cases

1. **Add to History**
   - Execute: Multiple commands
   - Expected: Commands added to history list
   - Verify: History.Count increases

2. **Duplicate Command Prevention**
   - Execute: Same command twice
   - Expected: Only one entry in history
   - Verify: Last duplicate not added

3. **History Navigation Up**
   - Press: Up arrow
   - Expected: Previous command loaded
   - Verify: HistoryIndex decremented

4. **History Navigation Down**
   - Navigate up, then down
   - Expected: Returns to newer commands
   - Verify: HistoryIndex incremented

5. **History Bounds Checking**
   - Navigate past oldest/newest
   - Expected: Stops at boundaries
   - Verify: No index out of bounds

6. **Max History Size**
   - Add more than MaxHistorySize commands
   - Expected: Oldest removed (FIFO)
   - Verify: History.Count <= MaxHistorySize

7. **Clear History Command**
   - Execute: `clear-history`
   - Expected: History cleared
   - Verify: History.Count == 0

8. **Show History Command**
   - Execute: `history`
   - Expected: Numbered list displayed
   - Verify: All entries shown with indices

9. **Empty History Display**
   - Execute: `history` with no prior commands
   - Expected: "No commands in history" message
   - Verify: Graceful empty state handling

---

## Section 3b: History Security Filtering

**File**: `repl-03b-history-security.cs`
**Purpose**: Verify HistoryIgnorePatterns filtering for sensitive commands

### Test Cases

1. **Default Patterns Block Common Secrets**
   - Commands: `login --password secret123`, `set apikey=ABC`, `deploy --token xyz`
   - Expected: None added to history
   - Verify: ShouldIgnoreCommand returns true for each

2. **Case-Insensitive Pattern Matching**
   - Commands: `PASSWORD=123`, `Password=456`, `password=789`
   - Expected: All blocked regardless of case
   - Verify: RegexOptions.IgnoreCase working

3. **Wildcard Pattern Matching - Asterisk**
   - Pattern: `*secret*`
   - Commands: `secret`, `mysecret`, `secretvalue`, `has_secret_in_middle`
   - Expected: All match the pattern
   - Verify: `.*` regex conversion working

4. **Wildcard Pattern Matching - Question Mark**
   - Pattern: `log?n`
   - Commands: `login`, `log1n`, `logXn`
   - Expected: All match (single char wildcard)
   - Commands: `logiin`, `lon`
   - Expected: Don't match

5. **Custom Pattern Configuration**
   - Options: `HistoryIgnorePatterns = ["deploy prod*", "*staging*"]`
   - Commands: `deploy production`, `deploy prod --force`, `test staging env`
   - Expected: All blocked by custom patterns
   - Command: `deploy dev`
   - Expected: Allowed (doesn't match patterns)

6. **Null/Empty Pattern Handling**
   - Options: `HistoryIgnorePatterns = null`
   - Expected: All commands saved to history
   - Options: `HistoryIgnorePatterns = []`
   - Expected: All commands saved to history

7. **Pattern with Special Regex Characters**
   - Pattern: `login.*` (literal dot-star)
   - Command: `login.*`
   - Expected: Exact match only (Regex.Escape handles special chars)
   - Command: `loginABC`
   - Expected: No match (dot is literal, not wildcard)

8. **ShouldIgnoreCommand Internal Method Tests**
   - Test directly via InternalsVisibleTo
   - Input: Various commands and patterns
   - Verify: Correct boolean returns
   - Performance: < 1ms per check

9. **Integration with AddToHistory**
   - Commands matching patterns
   - Expected: Not added to History list
   - Commands not matching
   - Expected: Added normally
   - Verify: History.Count reflects filtering

10. **Pattern Priority and Order**
    - Multiple patterns, some overlapping
    - Expected: First match wins (no need to check all)
    - Verify: Performance optimization working

---

## Section 4: History Persistence

**File**: `repl-04-history-persistence.cs`
**Purpose**: Verify history file loading and saving

### Test Cases

1. **Save History on Exit**
   - Options: `PersistHistory = true`
   - Execute commands, exit
   - Expected: History file created
   - Verify: File contains commands

2. **Load History on Start**
   - Pre-existing history file
   - Start session
   - Expected: History loaded
   - Verify: Previous commands available

3. **History File Location**
   - Default: `~/.nuru/history/{appname}`
   - Custom: Via HistoryFilePath option
   - Verify: Correct path used

4. **Create Missing Directory**
   - History path doesn't exist
   - Expected: Directory created
   - Verify: Path.GetDirectoryName() exists

5. **Handle File Access Errors**
   - Read-only or locked file
   - Expected: Warning logged, continues
   - Verify: No crash, session works

6. **Corrupted History File**
   - Malformed content
   - Expected: Partial load or skip
   - Verify: Session starts normally

7. **History File Trimming**
   - File has > MaxHistorySize entries
   - Expected: Only last N loaded
   - Verify: TakeLast(MaxHistorySize) applied

8. **Disable Persistence**
   - Options: `PersistHistory = false`
   - Expected: No file operations
   - Verify: No file created/loaded

---

## Section 5: Console Input Handling

**File**: `repl-05-console-input.cs`
**Purpose**: Verify advanced console input operations

### Test Cases

1. **Character Insertion**
   - Type characters
   - Expected: Added at cursor position
   - Verify: UserInput updated correctly

2. **Backspace Key**
   - Delete character before cursor
   - Expected: Character removed
   - Verify: CursorPosition adjusted

3. **Delete Key**
   - Delete character after cursor
   - Expected: Character removed
   - Verify: String rebuilt correctly

4. **Left Arrow Navigation**
   - Move cursor left
   - Expected: CursorPosition--
   - Verify: Within bounds [0, Length]

5. **Right Arrow Navigation**
   - Move cursor right
   - Expected: CursorPosition++
   - Verify: Within bounds

6. **Ctrl+Left (Word Navigation)**
   - Jump to previous word
   - Expected: Cursor at word boundary
   - Verify: Skips whitespace correctly

7. **Ctrl+Right (Word Navigation)**
   - Jump to next word
   - Expected: Cursor at next word start
   - Verify: Handles punctuation

8. **Home Key**
   - Jump to line start
   - Expected: CursorPosition = 0
   - Verify: Instant positioning

9. **End Key**
   - Jump to line end
   - Expected: CursorPosition = Length
   - Verify: Positions after last character

10. **Escape Key**
    - Clear completion state
    - Expected: Resets completion candidates
    - Verify: CompletionIndex = -1

---

## Section 6: Tab Completion - Basic

**File**: `repl-06-tab-completion-basic.cs`
**Purpose**: Verify basic tab completion functionality

### Test Cases

1. **Single Completion Match**
   - Input: `sta[TAB]` with route `status`
   - Expected: Completes to `status`
   - Verify: Word replaced entirely

2. **Multiple Completion Matches**
   - Input: `s[TAB]` with routes `status`, `start`
   - Expected: Shows both options
   - Verify: Candidates displayed

3. **Cycle Through Completions**
   - Multiple TABs with multiple matches
   - Expected: Cycles through options
   - Verify: CompletionIndex increments

4. **Reverse Cycle (Shift+Tab)**
   - Shift+Tab with multiple matches
   - Expected: Cycles backward
   - Verify: CompletionIndex decrements

5. **No Matches**
   - Input: `xyz[TAB]`
   - Expected: No change
   - Verify: Input unchanged

6. **Empty Input Completion**
   - Input: `[TAB]` at prompt
   - Expected: Shows all commands
   - Verify: Root-level completions

7. **Partial Word Replacement**
   - Input: `deplo[TAB]` → `deploy`
   - Expected: Replaces from word start
   - Verify: FindWordStart() logic

8. **Completion with Arguments**
   - Input: `deploy prod[TAB]`
   - Expected: Context-aware suggestions
   - Verify: Uses previous arguments

---

## Section 7: Tab Completion - Advanced

**File**: `repl-07-tab-completion-advanced.cs`
**Purpose**: Verify complex completion scenarios

### Test Cases

1. **Option Completion**
   - Input: `deploy --[TAB]`
   - Expected: Shows available options
   - Verify: `--version`, `--force`, etc.

2. **Short Option Completion**
   - Input: `deploy -[TAB]`
   - Expected: Shows short options
   - Verify: `-v`, `-f`, etc.

3. **Parameter Value Completion**
   - Route: `deploy {env}` with custom source
   - Input: `deploy [TAB]`
   - Expected: Environment names
   - Verify: Custom source called

4. **Enum Parameter Completion**
   - Parameter type: enum
   - Expected: All enum values
   - Verify: EnumCompletionSource used

5. **Nested Command Completion**
   - Input: `git com[TAB]`
   - Expected: `commit`, `config`
   - Verify: Multi-level routing

6. **Mixed Position Completion**
   - Input: `cmd arg1 --opt val [TAB]`
   - Expected: Position-aware suggestions
   - Verify: Correct context detection

7. **Repeated Option Values**
   - Route: `--env {e}*`
   - Multiple `--env` completions
   - Verify: Array parameter handling

8. **Catch-All Parameter**
   - Route: `docker {*args}`
   - Expected: No specific completions
   - Verify: Falls back appropriately

---

## Section 8: Syntax Highlighting

**File**: `repl-08-syntax-highlighting.cs`
**Purpose**: Verify real-time syntax colorization

### Test Cases

1. **Command Highlighting**
   - Input: Known command
   - Expected: CommandColor applied
   - Verify: ANSI codes in output

2. **Unknown Command**
   - Input: Unrecognized text
   - Expected: DefaultTokenColor
   - Verify: Different from known commands

3. **Option Highlighting**
   - Input: `--option` or `-o`
   - Expected: KeywordColor/OperatorColor
   - Verify: Options distinguished

4. **String Literal Highlighting**
   - Input: `"quoted text"`
   - Expected: StringColor applied
   - Verify: Quotes included in coloring

5. **Number Highlighting**
   - Input: Numeric values
   - Expected: NumberColor applied
   - Verify: Integers and decimals

6. **Parameter Highlighting**
   - Input: `{param}` patterns
   - Expected: ParameterColor
   - Verify: Braces detected

7. **Mixed Token Types**
   - Complex command line
   - Expected: Each token colored appropriately
   - Verify: No color bleeding

8. **Disable Colors Option**
   - Options: `EnableColors = false`
   - Expected: No ANSI codes
   - Verify: Plain text output

9. **Cache Performance**
   - Repeated command lookups
   - Expected: Uses CommandCache
   - Verify: Constant time after first check

---

## Section 9: Built-in REPL Commands

**File**: `repl-09-builtin-commands.cs`
**Purpose**: Verify REPL-specific command routing

### Test Cases

1. **Exit Command**
   - Input: `exit`
   - Expected: Sets Running = false
   - Verify: Session terminates

2. **Quit Command**
   - Input: `quit`
   - Expected: Same as exit
   - Verify: Alias works

3. **Short Quit (q)**
   - Input: `q`
   - Expected: Same as exit
   - Verify: Shortcut works

4. **Help Command**
   - Input: `help`
   - Expected: Shows REPL commands + routes
   - Verify: Comprehensive help

5. **Clear Screen**
   - Input: `clear` or `cls`
   - Expected: Console.Clear() called
   - Verify: Screen cleared

6. **History Command**
   - Input: `history`
   - Expected: Shows command history
   - Verify: Numbered list

7. **Clear History**
   - Input: `clear-history`
   - Expected: History.Clear() called
   - Verify: History empty

8. **Command Priority**
   - REPL command vs app route conflict
   - Expected: REPL commands take precedence
   - Verify: Routing order

---

## Section 10: Error Handling

**File**: `repl-10-error-handling.cs`
**Purpose**: Verify graceful error recovery

### Test Cases

1. **Command Execution Error**
   - Handler throws exception
   - Expected: Error displayed, continues
   - Verify: ContinueOnError behavior

2. **Invalid Route Match**
   - No matching route
   - Expected: Error message shown
   - Verify: Suggests help

3. **Type Conversion Error**
   - Invalid parameter type
   - Expected: Clear error message
   - Verify: Shows expected type

4. **Console Operation Error**
   - SetCursorPosition fails
   - Expected: Fallback behavior
   - Verify: No crash

5. **History File Error**
   - Can't write history
   - Expected: Warning logged
   - Verify: Session continues

6. **Completion Provider Error**
   - Exception in custom source
   - Expected: Graceful fallback
   - Verify: No completion crash

7. **Exit Code Handling**
   - Command returns non-zero
   - Expected: Shows exit code
   - Verify: Optional continuation

8. **Multiple Errors**
   - Cascade of failures
   - Expected: Each handled independently
   - Verify: Session stability

---

## Section 11: Display and Formatting

**File**: `repl-11-display-formatting.cs`
**Purpose**: Verify output formatting and display

### Test Cases

1. **Welcome Message**
   - Options: Custom welcome
   - Expected: Displayed on start
   - Verify: Before first prompt

2. **Goodbye Message**
   - Options: Custom goodbye
   - Expected: Displayed on exit
   - Verify: After last command

3. **Custom Prompt**
   - Options: `Prompt = ">>> "`
   - Expected: Used for input
   - Verify: Consistent display

4. **Colored Prompt**
   - With EnableColors
   - Expected: Green prompt
   - Verify: ANSI codes present

5. **Exit Code Display**
   - Options: `ShowExitCode = true`
   - Expected: "Exit code: 0"
   - Verify: After each command

6. **Timing Display**
   - Options: `ShowTiming = true`
   - Expected: "(XXms)" shown
   - Verify: Stopwatch accuracy

7. **Line Redrawing**
   - After edits
   - Expected: Clean redraw
   - Verify: No artifacts

8. **Window Width Handling**
   - Long lines
   - Expected: Proper wrapping/truncation
   - Verify: Cursor positioning

9. **Completion Display Format**
   - Multiple columns
   - Expected: Aligned display
   - Verify: Uses WindowWidth

---

## Section 12: Configuration Options

**File**: `repl-12-configuration.cs`
**Purpose**: Verify all ReplOptions behavior

### Test Cases

1. **Default Options**
   - No configuration
   - Expected: Sensible defaults
   - Verify: All properties set

2. **Prompt Configuration**
   - Custom prompt string
   - Expected: Applied correctly
   - Verify: Display matches

3. **History Size Limit**
   - MaxHistorySize = 10
   - Expected: Only 10 kept
   - Verify: Oldest removed

4. **Enable/Disable Features**
   - Colors, History, Arrow keys
   - Expected: Features toggle
   - Verify: Behavior changes

5. **Continue on Error**
   - true: Keep running
   - false: Exit on error
   - Verify: Both behaviors

6. **History File Path**
   - Custom path
   - Expected: Uses specified location
   - Verify: File created there

7. **Message Customization**
   - Welcome and goodbye
   - Expected: Custom text shown
   - Verify: At right times

8. **Mixed Options**
   - Multiple options combined
   - Expected: All applied
   - Verify: No conflicts

9. **HistoryIgnorePatterns Configuration**
   - Default patterns present
   - Expected: Common sensitive patterns included
   - Verify: `*password*`, `*secret*`, `*token*`, `*apikey*`, `*credential*`

10. **Custom Prompt Color**
    - Options: `PromptColor = "\x1b[36m"` (cyan)
    - Expected: Prompt uses custom color
    - Verify: PromptFormatter uses configured color

---

## Section 13: Integration with NuruApp

**File**: `repl-13-nuruapp-integration.cs`
**Purpose**: Verify REPL integration with main app

### Test Cases

1. **Extension Method Usage**
   - `builder.AddReplSupport()`
   - Expected: Routes registered
   - Verify: Both methods added

2. **Route Registration Order**
   - REPL routes vs app routes
   - Expected: Proper precedence
   - Verify: No conflicts

3. **Options Storage**
   - Via AddReplOptions
   - Expected: Stored in app
   - Verify: Retrieved correctly

4. **LoggerFactory Integration**
   - Uses app's logger
   - Expected: Logs routed correctly
   - Verify: All components log

5. **TypeConverter Registry**
   - Shared with app
   - Expected: Same converters
   - Verify: Types work in REPL

6. **Endpoint Collection Access**
   - For completion/highlighting
   - Expected: Full access
   - Verify: All routes available

7. **Fluent API Chaining**
   - Multiple builder calls
   - Expected: All chainable
   - Verify: Returns builder

8. **Direct REPL Start**
   - `app.RunReplAsync()`
   - Expected: Immediate REPL
   - Verify: No args needed

---

## Section 14: Performance and Resources

**File**: `repl-14-performance.cs`
**Purpose**: Verify performance characteristics and resource usage

### Test Cases

1. **Startup Performance**
   - Cold start time
   - Expected: < 50ms
   - Verify: Stopwatch measurement

2. **Command Execution Speed**
   - Simple command latency
   - Expected: < 10ms overhead
   - Verify: Minus handler time

3. **History Search Performance**
   - Large history (1000 items)
   - Expected: Instant navigation
   - Verify: No lag on arrows

4. **Completion Performance**
   - Many candidates (100+)
   - Expected: < 20ms
   - Verify: Fast filtering

5. **Syntax Highlighting Speed**
   - Complex input
   - Expected: Real-time (<5ms)
   - Verify: No typing lag

6. **Memory Usage**
   - Long session
   - Expected: Stable memory
   - Verify: No leaks

7. **Cache Efficiency**
   - Command recognition cache
   - Expected: High hit rate
   - Verify: Dictionary lookups

8. **Resource Cleanup**
   - After session end
   - Expected: All released
   - Verify: CurrentSession null

---

## Section 15: Edge Cases and Stress Testing

**File**: `repl-15-edge-cases.cs`
**Purpose**: Verify behavior under unusual conditions

### Test Cases

1. **Very Long Input**
   - 1000+ character line
   - Expected: Handles gracefully
   - Verify: No buffer overflow

2. **Rapid Input**
   - Fast typing/pasting
   - Expected: Keeps up
   - Verify: No character loss

3. **Unicode Input**
   - Emoji, CJK characters
   - Expected: Proper handling
   - Verify: Display and storage

4. **Console Resize**
   - During session
   - Expected: Adapts display
   - Verify: Redraws correctly

5. **Null/Empty Handling**
   - Null options, empty arrays
   - Expected: Sensible defaults
   - Verify: No NullReference

6. **Concurrent Access**
   - Multiple threads
   - Expected: Thread-safe or documented
   - Verify: No corruption

7. **Nested REPL Sessions**
   - REPL within REPL
   - Expected: Prevented or supported
   - Verify: Clear behavior

8. **Signal Handling**
   - SIGTERM, SIGHUP
   - Expected: Graceful shutdown
   - Verify: Cleanup executed

9. **Resource Exhaustion**
   - Out of memory/disk
   - Expected: Graceful degradation
   - Verify: Error messages

10. **Platform Differences**
    - Windows vs Linux/Mac
    - Expected: Consistent behavior
    - Verify: Key codes, paths

---

## Test Organization

### Naming Convention

All test files follow numbered naming for systematic coverage:
- `repl-01-session-lifecycle.cs`
- `repl-02-command-parsing.cs`
- `repl-03-history-management.cs`
- etc.

### File Structure

Each test file uses single-file .NET 10 applications:

```csharp
#!/usr/bin/env dotnet

return await RunTests();

async Task<int> RunTests()
{
    int passed = 0;
    int failed = 0;

    // Test 1
    try
    {
        // Arrange
        ReplOptions options = new() { Prompt = ">>> " };

        // Act
        TestResult result = TestMethod(options);

        // Assert
        if (result == expected)
        {
            Console.WriteLine("✅ Test 1: Description");
            passed++;
        }
        else
        {
            Console.WriteLine("❌ Test 1: Description - Failed");
            failed++;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Test 1: Description - {ex.Message}");
        failed++;
    }

    // Summary
    Console.WriteLine($"\nResults: {passed} passed, {failed} failed");
    return failed == 0 ? 0 : 1;
}
```

### Test Runner

Execute via `Tests/Scripts/run-repl-tests.cs`:
- Runs all repl tests sequentially
- Reports aggregate pass/fail counts
- Returns non-zero on any failure

---

## Coverage Goals

### Completeness
- ✅ All REPL commands tested
- ✅ All console key handlers
- ✅ All configuration options
- ✅ History operations
- ✅ Completion scenarios
- ✅ Syntax highlighting rules
- ✅ Error conditions
- ✅ Integration points

### Real-World Validation

Include tests for realistic REPL usage patterns:
- Git-style commands: `git commit -m "message"`
- Docker commands: `docker run -d nginx`
- Build tools: `dotnet build --configuration Release`
- Package managers: `npm install --save-dev`

### Platform Testing

Verify on:
- Windows (Command Prompt, PowerShell, Windows Terminal)
- Linux (bash, zsh)
- macOS (Terminal.app, iTerm2)

---

## Success Criteria

A test suite is considered complete when:

1. **Coverage**: All REPL features have corresponding tests
2. **Clarity**: Each test demonstrates one specific behavior
3. **Isolation**: Tests don't depend on external state
4. **Performance**: Test suite runs in < 30 seconds
5. **Reliability**: No flaky tests, 100% deterministic
6. **Documentation**: Clear test names and comments
7. **Portability**: Works across platforms

---

## Implementation Notes

### Challenges

1. **Console I/O Testing**
   - Console operations are hard to unit test
   - Consider abstraction layer or test harness
   - May need integration tests with actual terminal

2. **Interactive Features**
   - Arrow keys, tab completion need special handling
   - Consider mock input streams
   - Test at appropriate abstraction level

3. **Platform Variations**
   - Key codes differ across terminals
   - File paths vary by OS
   - Consider platform-specific test sections

4. **Timing and Performance**
   - Async operations and timing
   - Use deterministic time sources where possible
   - Consider benchmark separate from functional tests

### Test Data

Create shared test data:
- Sample route collections
- Pre-built REPL options
- Mock completion sources
- Test history files

### Testing Internal Methods

Some critical security logic (like `ShouldIgnoreCommand`) is marked as `internal` to enable direct testing:
- Use `InternalsVisibleTo` attribute to allow test assembly access
- Test internal methods directly when they contain important logic
- This provides better test coverage than testing only through public APIs
- Especially important for security-related features like history filtering

### Assertions

Use clear, specific assertions:
- Not just "not null" but specific values
- Include helpful messages on failure
- Verify both positive and negative cases

---

## Related Documentation

- REPL Implementation: `source/timewarp-nuru.Repl/`
- Integration Tests: `Tests/TimeWarp.Nuru.Repl.Tests/`
- Sample Applications: `samples/repl-demo/`
- Design Documents: Task 027, 031
- Code Review: `.agent/workspace/TimeWarp.Nuru.Repl-Code-Review-Report.md`

---

## Test Categories Summary

| # | Category | Test File | Purpose | Test Count |
|---|----------|-----------|---------|------------|
| 1 | Session Lifecycle | `repl-01-session-lifecycle.cs` | Start/stop/cleanup | 7 |
| 2 | Command Parsing | `repl-02-command-parsing.cs` | Quote and escape handling | 9 |
| 3 | History Management | `repl-03-history-management.cs` | History operations | 9 |
| 3b | History Security | `repl-03b-history-security.cs` | Sensitive data filtering | 10 |
| 4 | History Persistence | `repl-04-history-persistence.cs` | File I/O | 8 |
| 5 | Console Input | `repl-05-console-input.cs` | Keyboard handling | 10 |
| 6 | Tab Completion Basic | `repl-06-tab-completion-basic.cs` | Simple completions | 8 |
| 7 | Tab Completion Advanced | `repl-07-tab-completion-advanced.cs` | Complex scenarios | 8 |
| 8 | Syntax Highlighting | `repl-08-syntax-highlighting.cs` | Colorization | 9 |
| 9 | Built-in Commands | `repl-09-builtin-commands.cs` | REPL commands | 8 |
| 10 | Error Handling | `repl-10-error-handling.cs` | Error recovery | 8 |
| 11 | Display Formatting | `repl-11-display-formatting.cs` | Output format | 9 |
| 12 | Configuration | `repl-12-configuration.cs` | Options behavior | 10 |
| 13 | Integration | `repl-13-nuruapp-integration.cs` | App integration | 8 |
| 14 | Performance | `repl-14-performance.cs` | Speed and resources | 8 |
| 15 | Edge Cases | `repl-15-edge-cases.cs` | Unusual conditions | 10 |

**Total Test Categories**: 16
**Total Test Cases**: ~139
