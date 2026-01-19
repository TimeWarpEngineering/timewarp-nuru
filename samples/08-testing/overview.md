# Testing Samples

This folder contains examples demonstrating how to test Nuru CLI applications using the `TestTerminal` abstraction.

## Samples

| Sample | Description |
|--------|-------------|
| [test-output-capture.cs](test-output-capture.cs) | Capture and assert on CLI output |
| [test-colored-output.cs](test-colored-output.cs) | Test handlers with colored output |
| [test-terminal-injection.cs](test-terminal-injection.cs) | ITerminal injection in route handlers |
| [runfile-test-harness/](runfile-test-harness/) | Zero-modification testing pattern for runfiles |

## Key Concepts

### TestTerminal

`TestTerminal` is a testable implementation of `ITerminal` that:
- Captures all stdout and stderr output
- Provides scripted key input for REPL testing
- Supports key sequence queuing for interactive testing

### TestTerminalContext (Ambient Context)

`TestTerminalContext` provides an `AsyncLocal<TestTerminal>` that Nuru checks at runtime:

```csharp
using TestTerminal terminal = new();
TestTerminalContext.Current = terminal;

await app.RunAsync(["greet", "World"]);

terminal.OutputContains("Hello, World!").ShouldBeTrue();
```

This enables testing without modifying the app code.

### NuruTestContext (Test Harness Pattern)

For testing **runfiles** without modifying them, use the test harness pattern:

1. Create a test file with `[ModuleInitializer]` that sets `NuruTestContext.TestRunner`
2. Use `Directory.Build.props` to conditionally include the test file
3. Set `NURU_TEST` environment variable to trigger test mode

```csharp
// test-my-app.cs
public static class TestHarness
{
  [ModuleInitializer]
  public static void Initialize()
  {
    NuruTestContext.TestRunner = async (app) =>
    {
      using TestTerminal terminal = new();
      TestTerminalContext.Current = terminal;
      
      await app.RunAsync(["greet", "Alice"]);
      
      terminal.OutputContains("Hello, Alice!").ShouldBeTrue();
      Console.WriteLine("Test passed!");
      return 0;
    };
  }
}
```

## Testing Real Apps (Zero-Modification Pattern)

See the [runfile-test-harness/](runfile-test-harness/) subfolder for the complete zero-modification testing pattern, which demonstrates testing runfiles without changing the application code.

## Running the Samples

```bash
# Run any sample directly
./samples/testing/test-output-capture.cs
./samples/testing/test-colored-output.cs
./samples/testing/test-terminal-injection.cs

# Run the runfile test harness
./samples/testing/runfile-test-harness/run-real-app-tests.cs
```

## Suppressing Closure Warnings in Tests

When writing tests, you may intentionally use closures in handlers to capture values for assertions:

```csharp
bool handlerCalled = false;
string capturedValue = "";

app.Map("test {name}")
  .WithHandler((string name) =>
  {
    handlerCalled = true;      // Closure - captures external variable
    capturedValue = name;      // Closure - captures external variable
  })
  .Done();

await app.RunAsync(["test", "Alice"]);

handlerCalled.ShouldBeTrue();
capturedValue.ShouldBe("Alice");
```

This triggers warning `NURU_H002: Handler lambda captures external variable(s)`. This is expected behavior - the source generator cannot create a Mediator handler for lambdas with closures because captured variables can't be injected at runtime.

For test code, suppress the warning:

```csharp
#pragma warning disable NURU_H002 // Test code intentionally uses closures to capture values for assertions
// ... test code with closures ...
#pragma warning restore NURU_H002
```

Or suppress at the file level by adding to the top of your test file:

```csharp
#pragma warning disable NURU_H002
```

**Note:** In production code, avoid closures in handlers. Use DI-injected parameters instead.

## See Also

- [Terminal Abstractions Documentation](../../documentation/user/features/terminal-abstractions.md)
- [Output Handling](../../documentation/user/features/output-handling.md)
