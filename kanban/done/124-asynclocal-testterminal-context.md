# AsyncLocal TestTerminal Context for Zero-Config Testing

## Description

Implement an `AsyncLocal<TestTerminal>` ambient context that allows tests to inject `TestTerminal` without requiring any changes to the consumer's CLI application code.

This enables testing real Nuru apps by simply setting the ambient context before calling `Main()`:

```csharp
public static async Task Should_display_card()
{
    using TestTerminal terminal = new();
    TestTerminalContext.Current = terminal;
    
    await Program.Main(["card"]);
    
    terminal.OutputContains("Expected output").ShouldBeTrue();
}
```

**Key benefits:**
- No factory method required in the app
- No code changes to the app being tested
- Parallel-test safe (AsyncLocal is per async flow)
- Tests the exact same code that runs in production

## Requirements

- `TestTerminalContext` class with `AsyncLocal<TestTerminal?>` backing
- `NuruCoreApp` checks `TestTerminalContext.Current` before falling back to DI or `NuruTerminal.Default`
- Works with parallel test execution
- Add example showing usage pattern

## Checklist

### Implementation
- [x] Create `TestTerminalContext` class with `AsyncLocal<TestTerminal?>` 
- [x] Update terminal resolution in `NuruCoreApp` to check context first
- [x] Verify parallel test safety

### Testing
- [x] Add test demonstrating AsyncLocal isolation between parallel tests
- [x] Add integration test calling real app via `Main()` with context

### Documentation
- [x] Add `test-real-app` sample showing the pattern
- [ ] Add example to MCP
- [ ] Update issue #109 with implementation details

## Implementation Summary

### Files Created
- `source/timewarp-nuru-core/io/test-terminal-context.cs` - AsyncLocal context class

### Files Modified
- `source/timewarp-nuru-core/nuru-core-app-builder.cs` - Use `TestTerminalContext.Resolve()` in non-DI Build path
- `source/timewarp-nuru-core/nuru-core-app.cs` - Use `TestTerminalContext.Resolve()` in DI constructor and `BindParameters`
- `source/timewarp-nuru-core/execution/delegate-executor.cs` - Use resolved terminal for `ITerminal` parameters
- `source/timewarp-nuru-core/execution/mediator-executor.cs` - Use `TestTerminalContext.Resolve()` in constructor

### Tests Added
- `tests/timewarp-nuru-core-tests/test-terminal-context-01-basic.cs` - 7 tests covering context behavior

### Samples Added
- `samples/testing/runfile-test-harness/test-real-app.cs` - Demonstrates zero-config testing pattern

## Notes

`AsyncLocal<T>` flows with the async execution context, so each test gets its own isolated value even when running in parallel. This is the same mechanism ASP.NET Core uses for `HttpContext.Current` equivalent patterns.

Resolution order:
1. `TestTerminalContext.Current` (if set)
2. `ITerminal` from DI (if registered)
3. `NuruTerminal.Default` (fallback)

The implementation also ensures that when handler delegates inject `ITerminal` as a parameter, they receive the context terminal (if set) rather than the DI-registered terminal.

## References

- GitHub Issue: #109
- Related: `test-output-capture`, `test-terminal-injection`, `test-colored-output` samples
