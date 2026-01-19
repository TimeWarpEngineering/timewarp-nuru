# Clean up samples with meaningless return 0 statements

## Description

Several sample files have handlers that `return 0` for no semantic reason. These appear to be leftover from testing the "handler returns int" feature. Handlers should only return values when the return value is meaningful (e.g., exit codes, counts, results).

**Important:** In Nuru, handler return values are OUTPUT (written to terminal), NOT exit codes. To signal failure, throw an exception.

## Checklist

- [x] Review `samples/10-type-converters/02-custom-type-converters.cs` - handlers like `send-email`, `set-theme`, `release`, `notify`, `deploy` all return 0 meaninglessly
- [x] Check other samples in `samples/` for similar patterns
- [x] Remove `return 0` where it has no semantic meaning (convert to void handlers)
- [x] Keep `return 0/1` only where it represents a meaningful exit code or result
- [x] Fix `repl-options-showcase.cs` which incorrectly assumed return values set exit codes
- [x] Update documentation in `_shell-completion-example/overview.md`

## Notes

Example of bad code (fixed):
```csharp
.WithHandler((EmailAddress to, string subject) =>
{
  Console.WriteLine($"Sending Email to {to}");
  return 0;  // meaningless - not used for anything
})
```

Fixed version:
```csharp
.WithHandler((EmailAddress to, string subject) =>
{
  Console.WriteLine($"Sending Email to {to}");
})
```

### Files fixed:
- `samples/10-type-converters/02-custom-type-converters.cs` - removed 5 meaningless `return 0`
- `samples/_repl-demo/repl-prompt-fix-demo.cs` - simplified 2 handlers to expression bodies
- `samples/_repl-demo/repl-options-showcase.cs` - fixed `success`, `fail`, `exitcode` commands to correctly demonstrate exit codes (throw exception for failure, clarify return values are output)
- `samples/_shell-completion-example/overview.md` - removed meaningless `return 0` from doc example

### Files kept as-is (legitimate exit codes):
- `samples/10-type-converters/02-custom-type-converters.cs` line 168 - main program exit
- `samples/10-type-converters/01-builtin-types.cs` line 265 - main program exit
- `samples/04-syntax-examples/syntax-examples.cs` line 195 - main program exit
- `samples/08-testing/runfile-test-harness/run-real-app-tests.cs` - test runner exit codes
- `samples/08-testing/overview.md` - TestRunner delegate returns exit code (legitimate)
