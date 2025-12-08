// test-real-app.cs - Test harness for real-app.cs
// This file is included at build time via Directory.Build.props when NURU_TEST is set
// Usage: NURU_TEST=test-real-app.cs ./real-app.cs
//
// The ModuleInitializer sets up NuruTestContext.TestRunner before Main() runs.
// When real-app.cs calls RunAsync(), control is handed to our test runner.

using System.Runtime.CompilerServices;
using Shouldly;
using TimeWarp.Nuru;

public static class TestHarness
{
  [ModuleInitializer]
  public static void Initialize()
  {
    NuruTestContext.TestRunner = RunTestsAsync;
  }

  private static async Task<int> RunTestsAsync(NuruCoreApp app)
  {
    Console.WriteLine("=== Testing real-app.cs with NuruTestContext ===\n");

    int passed = 0;
    int failed = 0;

    // Test 1: Basic greeting
    Console.Write("Test 1: greet command... ");
    try
    {
      using TestTerminal terminal = new();
      TestTerminalContext.Current = terminal;

      await app.RunAsync(["greet", "World"]);

      terminal.OutputContains("Hello, World!").ShouldBeTrue();
      Console.WriteLine("PASSED".Green());
      passed++;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"FAILED: {ex.Message}".Red());
      failed++;
    }
    finally
    {
      TestTerminalContext.Current = null;
    }

    // Test 2: Deploy with dry-run option
    Console.Write("Test 2: deploy --dry-run... ");
    try
    {
      using TestTerminal terminal = new();
      TestTerminalContext.Current = terminal;

      await app.RunAsync(["deploy", "production", "--dry-run"]);

      terminal.OutputContains("[DRY RUN]").ShouldBeTrue();
      terminal.OutputContains("production").ShouldBeTrue();
      Console.WriteLine("PASSED".Green());
      passed++;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"FAILED: {ex.Message}".Red());
      failed++;
    }
    finally
    {
      TestTerminalContext.Current = null;
    }

    // Test 3: Deploy without dry-run
    Console.Write("Test 3: deploy (actual)... ");
    try
    {
      using TestTerminal terminal = new();
      TestTerminalContext.Current = terminal;

      await app.RunAsync(["deploy", "staging"]);

      terminal.OutputContains("Deploying to staging").ShouldBeTrue();
      terminal.OutputContains("DRY RUN").ShouldBeFalse();
      Console.WriteLine("PASSED".Green());
      passed++;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"FAILED: {ex.Message}".Red());
      failed++;
    }
    finally
    {
      TestTerminalContext.Current = null;
    }

    // Test 4: Version command
    Console.Write("Test 4: version... ");
    try
    {
      using TestTerminal terminal = new();
      TestTerminalContext.Current = terminal;

      await app.RunAsync(["version"]);

      terminal.OutputContains("RealApp v1.0.0").ShouldBeTrue();
      Console.WriteLine("PASSED".Green());
      passed++;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"FAILED: {ex.Message}".Red());
      failed++;
    }
    finally
    {
      TestTerminalContext.Current = null;
    }

    // Test 5: Unknown command returns error
    Console.Write("Test 5: unknown command... ");
    try
    {
      using TestTerminal terminal = new();
      TestTerminalContext.Current = terminal;

      int exitCode = await app.RunAsync(["unknown-command"]);

      exitCode.ShouldBe(1);
      terminal.ErrorContains("No matching command found").ShouldBeTrue();
      Console.WriteLine("PASSED".Green());
      passed++;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"FAILED: {ex.Message}".Red());
      failed++;
    }
    finally
    {
      TestTerminalContext.Current = null;
    }

    // Summary
    Console.WriteLine();
    Console.WriteLine($"=== Results: {passed} passed, {failed} failed ===".Bold());

    return failed > 0 ? 1 : 0;
  }
}
