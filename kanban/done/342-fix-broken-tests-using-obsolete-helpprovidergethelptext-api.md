# Fix broken tests using obsolete HelpProvider.GetHelpText API

## Description

Several test files referenced `HelpProvider.GetHelpText()` which was deleted in #293-006 as part
of the V2 source generator migration. Help is now generated at compile time by `HelpEmitter`.

## Resolution

After investigation, discovered that `HelpProvider.GetHelpText()` was **intentionally deleted**
because the V2 source generator now emits help text at compile time via `HelpEmitter`. Tests
that tested runtime help generation are no longer relevant.

## Actions Taken

### Deleted Files (5 files)
Tests that directly called `HelpProvider.GetHelpText()` with manually-constructed `EndpointCollection`:
- `help-provider-01-option-detection.cs` - Tests options vs commands section classification
- `help-provider-02-filtering.cs` - Tests HelpOptions filtering
- `help-provider-04-default-route.cs` - Tests "(default)" marker display
- `help-provider-05-color-output.cs` - Tests ANSI color codes in help
- `routing-23-multiple-map-same-handler.cs` - Used both closures AND deleted help API

### Migrated Files (1 file)
- `help-provider-03-session-context.cs` - Kept tests for `SessionContext` class (still exists),
  removed tests that tried to inject `SessionContext` into handlers (not supported by generator)

## Analysis

The runtime `HelpProvider.GetHelpText()` was the unit-testable API for help formatting. Now that
help is compile-time generated:
1. There's no runtime API to test directly
2. Help formatting is verified by the generator's own tests
3. Integration tests can verify help output via TestTerminal if needed

## Checklist

- [x] Investigate current help generation API → Deleted, uses HelpEmitter at compile time
- [x] Decide: delete obsolete tests or migrate → Deleted (no runtime API to test)
- [x] Verify remaining tests compile and pass
