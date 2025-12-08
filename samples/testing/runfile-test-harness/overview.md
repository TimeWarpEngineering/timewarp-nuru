# Runfile Test Harness

This folder demonstrates the **zero-modification pattern** for testing Nuru runfiles without changing the application code.

## Files

| File | Description |
|------|-------------|
| [real-app.cs](real-app.cs) | Sample CLI app (system under test) |
| [test-real-app.cs](test-real-app.cs) | Test harness with `[ModuleInitializer]` |
| [run-real-app-tests.cs](run-real-app-tests.cs) | Automated test runner using Amuru |
| [Directory.Build.props](Directory.Build.props) | Conditionally includes test file when `NURU_TEST` is set |

## How It Works

1. **`real-app.cs`** - A normal CLI app, no test-specific code
2. **`test-real-app.cs`** - Test harness with `[ModuleInitializer]` that sets `NuruTestContext.TestRunner`
3. **`Directory.Build.props`** - Conditionally includes the test file when `NURU_TEST` environment variable is set

## NuruTestContext (Test Harness Pattern)

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
