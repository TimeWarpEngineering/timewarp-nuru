# Rewrite Multiline Editing Tests for Source Generator Compatibility

## Description

The 9 tests in `repl-32-multiline-editing.cs` are skipped because they used handler closures to capture state, which the source generator doesn't support (lambdas are inlined into generated code).

These tests need to be rewritten to verify behavior through terminal output instead of captured state variables.

## Skipped Tests (9)

1. `Should_add_new_line_with_shift_enter`
2. `Should_split_line_at_cursor_with_shift_enter`
3. `Should_add_multiple_lines_with_shift_enter`
4. `Should_execute_full_multiline_input_on_enter`
5. `Should_add_multiline_input_to_history`
6. `Should_display_continuation_prompt`
7. `Should_use_custom_continuation_prompt`
8. `Should_handle_shift_enter_on_empty_line`
9. `Should_reset_multiline_mode_after_execution`

## Original Test Pattern (Not Supported)

```csharp
string? executedCommand = null;

NuruCoreApp app = NuruApp.CreateBuilder([])
  .Map("echo {*args}")
    .WithHandler((string[] args) => {
      executedCommand = string.Join(" ", args);  // Closure captures external variable
      return 0;
    })
    .Done()
  .Build();

// ... run REPL ...
executedCommand.ShouldBe("expected value");  // Verify via captured state
```

## New Test Pattern (Source Generator Compatible)

```csharp
using TestTerminal terminal = new();
terminal.QueueKey(ConsoleKey.Enter, shift: true);  // Shift+Enter
terminal.QueueKeys("line 2");
terminal.QueueKey(ConsoleKey.Enter);
terminal.QueueLine("exit");

NuruCoreApp app = NuruApp.CreateBuilder([])
  .UseTerminal(terminal)
  .Map("{*args}")
    .WithHandler((string[] args) => {
      terminal.WriteLine($"INPUT: {string.Join(" ", args)}");
      return 0;
    })
    .AsCommand()
    .Done()
  .AddRepl(options => options.EnableColors = false)
  .Build();

await app.RunAsync(["--interactive"]);

// Verify via terminal output instead of captured state
terminal.OutputContains("INPUT:").ShouldBeTrue();
terminal.OutputContains("hello").ShouldBeTrue();
terminal.OutputContains("world").ShouldBeTrue();
```

## Features to Test

1. **Shift+Enter behavior** - Adds new line instead of executing
2. **Line splitting** - Shift+Enter at cursor splits current line
3. **Multiple lines** - Can add 3+ lines
4. **Execution** - Enter executes all lines as single command
5. **History** - Multiline commands saved to history as single entry
6. **Continuation prompt** - Shows `>>` or custom prompt on continuation lines
7. **Edge cases** - Empty lines, mode reset after execution

## Implementation Plan

### Test-by-Test Implementation

| Test | Command | Handler Pattern | Verification |
|------|---------|-----------------|--------------|
| `Should_add_new_line_with_shift_enter` | `{*args}` | `terminal.WriteLine($"INPUT: {string.Join(" ", args)}")` | `OutputContains("hello")` && `OutputContains("world")` |
| `Should_split_line_at_cursor_with_shift_enter` | `{*args}` | Same, use `\|` separator in output | `OutputContains("hello")` && `OutputContains("world")` |
| `Should_add_multiple_lines_with_shift_enter` | `{*args}` | Same | All 3 lines in output |
| `Should_execute_full_multiline_input_on_enter` | `deploy --env {env}` | `terminal.WriteLine($"DEPLOY: {env}")` | `OutputContains("production")` |
| `Should_add_multiline_input_to_history` | `{*args}` | Output marker | `history` shows multiline entry |
| `Should_display_continuation_prompt` | `{*args}` | Options: `ContinuationPrompt = ">> "` | `OutputContains(">>")` |
| `Should_use_custom_continuation_prompt` | `{*args}` | Options: `ContinuationPrompt = "... "` | `OutputContains("...")` |
| `Should_handle_shift_enter_on_empty_line` | `{*args}` | Same | Content captured |
| `Should_reset_multiline_mode_after_execution` | `single` + `{*args}` | Separate handlers | Both execute |

### Reference Test Patterns (from timewarp-nuru-repl-tests-reference-only)

The original tests used these command patterns:

```csharp
// Test 1-3, 6-7, 9: Catch-all pattern
.Map("{*args}")
  .WithHandler((string[] args) => {
    terminal.WriteLine($"INPUT: {string.Join(" ", args)}");
    return 0;
  })
  .AsCommand()
  .Done()

// Test 4: Typed parameter
.Map("deploy --env {env}")
  .WithHandler((string env) => {
    terminal.WriteLine($"DEPLOY: {env}");
    return 0;
  })
  .AsCommand()
  .Done()

// Test 9: Mode reset with separate patterns
.Map("single")
  .WithHandler(() => {
    terminal.WriteLine("SINGLE");
    return 0;
  })
  .AsQuery()
  .Done()
.Map("{*args}")
  .WithHandler((string[] _) => {
    terminal.WriteLine("MULTI");
    return 0;
  })
  .AsCommand()
  .Done()
```

### Known Flaky Tests

The reference file marks 2 tests as flaky:
- `Should_add_multiline_input_to_history` - Uses `UpArrow` to recall from history
- `Should_reset_multiline_mode_after_execution` - Timing-sensitive state reset

These may need `[Skip]` or investigation if they fail.

## File

`tests/timewarp-nuru-tests/repl/repl-32-multiline-editing.cs`

## Priority

Low - Multiline editing works; only tests are missing
