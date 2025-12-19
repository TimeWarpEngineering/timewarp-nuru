# Update Remaining Test Files to New Fluent API

## Description

Task 198 updated samples to the new `Map().WithHandler().Done()` fluent API, but many test files still use the old `Map(pattern, handler)` API which was removed in Task 192.

These tests are currently skipped with `[Skip("Awaiting Task 200")]` to maintain clean test runs.

## Parent

151-implement-delegate-generation-phase-2

## Checklist

### Core Tests
- [ ] `tests/timewarp-nuru-core-tests/options/options-01-mixed-required-optional.cs`

### REPL Tests
- [ ] `tests/timewarp-nuru-repl-tests/repl-23-key-binding-profiles.cs`
- [ ] `tests/timewarp-nuru-repl-tests/repl-24-custom-key-bindings.cs`
- [ ] `tests/timewarp-nuru-repl-tests/repl-35-interactive-route-execution.cs`

### Completion Tests
- [ ] `tests/timewarp-nuru-completion-tests/static/completion-04-cursor-context.cs`

### Analyzer Tests
- [ ] `tests/timewarp-nuru-analyzers-tests/auto/delegate-signature-01-models.cs`
- [ ] `tests/timewarp-nuru-analyzers-tests/manual/should-pass-map-non-generic.cs`

### Scripts
- [ ] `tests/scripts/run-nuru-tests.cs`

## Notes

Migration pattern:
```csharp
// Old API
app.Map("deploy {env}", (string env) => Deploy(env));

// New fluent API
app.Map("deploy {env}")
   .WithHandler((string env) => Deploy(env))
   .AsCommand()
   .Done();
```
