# Document TestTerminalContext and NuruTestContext

## Description

Document the zero-configuration test isolation pattern using `TestTerminalContext` and `NuruTestContext` for testing runfiles and CLI apps.

## Requirements

- Add section to features/terminal-abstractions.md
- Explain AsyncLocal-based test isolation
- Show test harness pattern for runfiles
- Reference existing sample code

## Checklist

- [x] Add "Zero-Config Test Isolation" section to terminal-abstractions.md
- [x] Document TestTerminalContext.Current usage
- [x] Document NuruTestContext.TestRunner for runfile testing
- [x] Explain resolution order (TestTerminalContext → DI → NuruTerminal.Default)
- [x] Add example test code
- [x] Reference samples/testing/runfile-test-harness/

## Notes

Source files:
- `source/timewarp-nuru-core/io/test-terminal-context.cs`
- `source/timewarp-nuru-core/io/nuru-test-context.cs`
- `samples/testing/runfile-test-harness/overview.md`
