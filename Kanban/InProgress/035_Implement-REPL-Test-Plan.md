# Task 035: Implement REPL Test Plan

## Description

Implement the comprehensive REPL test plan defined in `Tests/TimeWarp.Nuru.Repl.Tests/repl-test-plan.md`. The test plan covers ~139 test cases across 16 categories for the REPL functionality.

## Parent

Task 031: Implement REPL Tab Completion (completed)

## Requirements

Implement all test files from the REPL test plan:

### Currently Implemented (5 files)
- `repl-01-session-lifecycle.cs` - Session start/stop/cleanup
- `repl-03-history-management.cs` - History operations
- `repl-03b-history-security.cs` - Sensitive data filtering
- `CommandLineParser/parser-01-basic-parsing.cs` - Basic parsing
- `CommandLineParser/parser-02-quoted-strings.cs` - Quote handling

### To Be Implemented (11 files, ~100+ test cases)
- `repl-02-command-parsing.cs` - Quote and escape handling (9 tests)
- `repl-04-history-persistence.cs` - File I/O (8 tests)
- `repl-05-console-input.cs` - Keyboard handling (10 tests)
- `repl-06-tab-completion-basic.cs` - Simple completions (8 tests)
- `repl-07-tab-completion-advanced.cs` - Complex scenarios (8 tests)
- `repl-08-syntax-highlighting.cs` - Colorization (9 tests)
- `repl-09-builtin-commands.cs` - REPL commands (8 tests)
- `repl-10-error-handling.cs` - Error recovery (8 tests)
- `repl-11-display-formatting.cs` - Output format (9 tests)
- `repl-12-configuration.cs` - Options behavior (10 tests)
- `repl-13-nuruapp-integration.cs` - App integration (8 tests)
- `repl-14-performance.cs` - Speed and resources (8 tests)
- `repl-15-edge-cases.cs` - Unusual conditions (10 tests)

## Checklist

- [ ] repl-02-command-parsing.cs
- [ ] repl-04-history-persistence.cs
- [ ] repl-05-console-input.cs
- [ ] repl-06-tab-completion-basic.cs
- [ ] repl-07-tab-completion-advanced.cs
- [ ] repl-08-syntax-highlighting.cs
- [ ] repl-09-builtin-commands.cs
- [ ] repl-10-error-handling.cs
- [ ] repl-11-display-formatting.cs
- [ ] repl-12-configuration.cs
- [ ] repl-13-nuruapp-integration.cs
- [ ] repl-14-performance.cs
- [ ] repl-15-edge-cases.cs
- [ ] Create test runner script `Tests/Scripts/run-repl-tests.cs`

## Notes

- Tests use .NET 10 single-file runfile format (not xUnit/NUnit)
- Many tests require `ITerminal` mock for console operations
- Interactive tests (arrow keys, tab completion) need mock input streams
- See `repl-test-plan.md` for detailed test case specifications
- Reference existing test files for pattern consistency
