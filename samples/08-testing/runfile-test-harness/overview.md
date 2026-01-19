# Runfile Test Harness

This folder demonstrates the **zero-modification pattern** for testing Nuru runfiles without changing the application code. Tests use **Jaribu** for test discovery, execution, and reporting.

## Files

| File | Description |
|------|-------------|
| [real-app.cs](real-app.cs) | Sample CLI app (system under test) |
| [test-real-app.cs](test-real-app.cs) | Jaribu test harness with `[ModuleInitializer]` |
| [run-real-app-tests.cs](run-real-app-tests.cs) | Automated test runner using Amuru |
| [Directory.Build.props](Directory.Build.props) | Conditionally includes test file and Jaribu when `NURU_TEST` is set |

## How It Works

1. **`real-app.cs`** - A normal CLI app, no test-specific code
2. **`test-real-app.cs`** - Jaribu test harness with `[ModuleInitializer]` that captures the app and runs tests
3. **`Directory.Build.props`** - Conditionally includes the test file and Jaribu package when `NURU_TEST` environment variable is set

## NuruTestContext with Jaribu

For testing **runfiles** without modifying them, use the Jaribu test harness pattern:

1. Create a test harness that captures the `NuruCoreApp` instance
2. Call Jaribu's `RunTests<T>()` to execute test methods
3. Write tests using Jaribu's `Should_` naming convention

```csharp
// test-my-app.cs
using TimeWarp.Jaribu;
using static TimeWarp.Jaribu.TestRunner;

public static class TestHarness
{
  internal static NuruCoreApp? App;

  [ModuleInitializer]
  public static void Initialize()
  {
    NuruTestContext.TestRunner = async (app) =>
    {
      App = app;  // Capture the real app
      return await RunTests<MyAppTests>(clearCache: false);
    };
  }
}

[TestTag("MyApp")]
public class MyAppTests
{
  public static async Task CleanUp()
  {
    TestTerminalContext.Current = null;
    await Task.CompletedTask;
  }

  public static async Task Should_greet_with_name()
  {
    using TestTerminal terminal = new();
    TestTerminalContext.Current = terminal;
    
    await TestHarness.App!.RunAsync(["greet", "Alice"]);
    
    terminal.OutputContains("Hello, Alice!").ShouldBeTrue();
    await Task.CompletedTask;
  }
}
```

## Benefits of Jaribu

- **Attribute-based test discovery** - Tests are discovered automatically by naming convention (`Should_*`)
- **Built-in cleanup support** - `CleanUp()` method runs after each test
- **Data-driven tests** - Use `[Input]` attribute for parameterized tests
- **Test tagging** - Use `[TestTag]` for filtering and organization
- **Skip support** - Use `[Skip("reason")]` to skip tests
- **Timeout support** - Use `[Timeout(ms)]` for long-running tests

## Running Tests Manually

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

## Running Tests Automatically

Use the test runner:

```bash
./run-real-app-tests.cs
```

This runner:
1. Sets `NURU_TEST` environment variable
2. Cleans and rebuilds with test harness
3. Runs tests
4. Clears environment variable
5. Cleans and rebuilds for production
6. Verifies normal operation

## CI/CD Considerations

- **Production builds**: Don't set `NURU_TEST` - app builds normally
- **Test builds**: Set `NURU_TEST=test-file.cs` before building
- **Always clean** when changing `NURU_TEST` - runfile cache doesn't track env vars

## See Also

- [Testing Samples Overview](../overview.md)
- [Terminal Abstractions Documentation](../../../documentation/user/features/terminal-abstractions.md)
- [Output Handling](../../../documentation/user/features/output-handling.md)
