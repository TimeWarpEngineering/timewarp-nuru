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
- [ ] Create `TestTerminalContext` class with `AsyncLocal<TestTerminal?>` 
- [ ] Update terminal resolution in `NuruCoreApp` to check context first
- [ ] Verify parallel test safety

### Testing
- [ ] Add test demonstrating AsyncLocal isolation between parallel tests
- [ ] Add integration test calling real app via `Main()` with context

### Documentation
- [ ] Add `test-real-app` sample showing the pattern
- [ ] Add example to MCP
- [ ] Update issue #109 with implementation details

## Notes

`AsyncLocal<T>` flows with the async execution context, so each test gets its own isolated value even when running in parallel. This is the same mechanism ASP.NET Core uses for `HttpContext.Current` equivalent patterns.

Resolution order should be:
1. `TestTerminalContext.Current` (if set)
2. `ITerminal` from DI (if registered)
3. `NuruTerminal.Default` (fallback)

## References

- GitHub Issue: #109
- Related: `test-output-capture`, `test-terminal-injection`, `test-colored-output` samples
