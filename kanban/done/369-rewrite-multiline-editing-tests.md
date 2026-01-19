# Rewrite Multiline Editing Tests for Source Generator Compatibility

## Description

The 9 tests in `repl-32-multiline-editing.cs` were skipped because they used handler closures to capture state, which the source generator doesn't support (lambdas are inlined into generated code).

These tests have been rewritten to verify behavior through handler return values and terminal output.

## Tests (9) - All Passing

1. `Should_add_new_line_with_shift_enter` ✓
2. `Should_split_line_at_cursor_with_shift_enter` ✓
3. `Should_add_multiple_lines_with_shift_enter` ✓
4. `Should_execute_full_multiline_input_on_enter` ✓
5. `Should_add_multiline_input_to_history` ✓
6. `Should_display_continuation_prompt` ✓
7. `Should_use_custom_continuation_prompt` ✓
8. `Should_handle_shift_enter_on_empty_line` ✓
9. `Should_reset_multiline_mode_after_execution` ✓

## Implementation Pattern

Handlers return string values that get printed to the terminal:

```csharp
.Map("hello {*args}")
  .WithHandler((string[] args) => $"hello[{string.Join(" ", args)}]")
  .AsCommand()
  .Done()
```

Tests verify via terminal output:

```csharp
terminal.OutputContains("hello[").ShouldBeTrue();
terminal.OutputContains("world").ShouldBeTrue();
```

## Key Changes

- Removed closure-based state capture
- Handlers return values instead of modifying external state
- Used `{*args}` catch-all pattern for multiline input
- Verified continuation prompts via `terminal.OutputContains(">>")`
- History tests use `history` command output for verification

## Test Results

```
Total: 9
Passed: 9
Failed: 0
```

## File

`tests/timewarp-nuru-tests/repl/repl-32-multiline-editing.cs`

## Completed

All 9 multiline editing tests rewritten and passing.
