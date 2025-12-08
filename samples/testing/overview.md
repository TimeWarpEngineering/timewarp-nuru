# Testing Samples

This folder contains examples demonstrating how to test Nuru CLI applications using the `TestTerminal` abstraction.

## Samples

| Sample | Description |
|--------|-------------|
| [test-output-capture.cs](test-output-capture.cs) | Capture and assert on CLI output |
| [test-colored-output.cs](test-colored-output.cs) | Test handlers with colored output |
| [test-terminal-injection.cs](test-terminal-injection.cs) | ITerminal injection in route handlers |
| [real-app.cs](real-app.cs) | Sample CLI app (system under test) |
| [test-real-app.cs](test-real-app.cs) | Test harness for real-app.cs |
| [run-real-app-tests.cs](run-real-app-tests.cs) | Automated test runner using Amuru |

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

The `real-app.cs` and `test-real-app.cs` demonstrate testing a runfile without changing it:

### How It Works

1. **`real-app.cs`** - A normal CLI app, no test-specific code
2. **`test-real-app.cs`** - Test harness with `[ModuleInitializer]`
3. **`Directory.Build.props`** - Conditionally includes test file when `NURU_TEST` is set

### Running Tests Manually

```bash
# Set the environment variable
export NURU_TEST=test-real-app.cs  # bash
$env:NURU_TEST = "test-real-app.cs"  # PowerShell

# IMPORTANT: Clean to force rebuild with test harness
dotnet clean ./real-app.cs

# Run - tests execute instead of normal app
./real-app.cs

# Clean up: remove env var and rebuild for production
unset NURU_TEST  # bash
Remove-Item Env:NURU_TEST  # PowerShell
dotnet clean ./real-app.cs
```

### Running Tests Automatically

Use the test runner script:

```bash
./run-real-app-tests.cs
```

This script:
1. Sets `NURU_TEST` environment variable
2. Cleans and rebuilds with test harness
3. Runs tests
4. Clears environment variable
5. Cleans and rebuilds for production
6. Verifies normal operation

### CI/CD Considerations

- **Production builds**: Don't set `NURU_TEST` - app builds normally
- **Test builds**: Set `NURU_TEST=test-file.cs` before building
- **Always clean** when changing `NURU_TEST` - runfile cache doesn't track env vars

## Running the Samples

```bash
# Run any sample directly
./samples/testing/test-output-capture.cs
./samples/testing/test-colored-output.cs
./samples/testing/test-terminal-injection.cs

# Run the real-app test suite
./samples/testing/run-real-app-tests.cs
```

## See Also

- [Terminal Abstractions Documentation](../../documentation/user/features/terminal-abstractions.md)
- [Output Handling](../../documentation/user/features/output-handling.md)
