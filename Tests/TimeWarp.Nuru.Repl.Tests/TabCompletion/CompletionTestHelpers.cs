namespace TimeWarp.Nuru.Repl.Tests.TabCompletion;

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;

// ============================================================================
// Tab Completion Test Helpers
// ============================================================================
// Provides utilities for testing REPL tab completion functionality:
// - CompletionAssertions: Extension methods for verifying completion behavior
// - KeySequenceHelpers: Extension methods for simulating user input sequences
// - TestAppFactory: Factory for creating test apps with repl-basic-demo.cs routes
//
// Usage Example:
//   var app = TestAppFactory.CreateReplDemoApp(terminal);
//   terminal.TypeAndTab("g");
//   terminal.CleanupAndExit();
//   await app.RunReplAsync();
//   terminal.ShouldShowCompletions("git", "greet");
// ============================================================================

/// <summary>
/// Extension methods for asserting tab completion behavior in TestTerminal output.
/// </summary>
public static class CompletionAssertions
{
  /// <summary>
  /// Asserts that all expected completions appear in the terminal output.
  /// </summary>
  /// <param name="terminal">The test terminal to check.</param>
  /// <param name="expected">The completion strings that should appear in output.</param>
  /// <exception cref="AssertionException">When any expected completion is not found.</exception>
  public static void ShouldShowCompletions(this TestTerminal terminal, params string[] expected)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    ArgumentNullException.ThrowIfNull(expected);

    foreach (string completion in expected)
    {
      terminal.OutputContains(completion).ShouldBeTrue(
        $"Should show completion '{completion}' in output"
      );
    }
  }

  /// <summary>
  /// Asserts that none of the unexpected completions appear in the terminal output.
  /// </summary>
  /// <param name="terminal">The test terminal to check.</param>
  /// <param name="unexpected">The completion strings that should NOT appear in output.</param>
  /// <exception cref="AssertionException">When any unexpected completion is found.</exception>
  public static void ShouldNotShowCompletions(this TestTerminal terminal, params string[] unexpected)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    ArgumentNullException.ThrowIfNull(unexpected);

    foreach (string completion in unexpected)
    {
      terminal.OutputContains(completion).ShouldBeFalse(
        $"Should NOT show completion '{completion}' in output"
      );
    }
  }

  /// <summary>
  /// Asserts that a unique match was auto-completed (text appears but no completion list shown).
  /// </summary>
  /// <param name="terminal">The test terminal to check.</param>
  /// <param name="expected">The text that should have been auto-completed.</param>
  /// <exception cref="AssertionException">When expected text is missing or completion list is shown.</exception>
  /// <remarks>
  /// Auto-completion occurs when there is exactly one match for the input.
  /// The completion list header "Available completions" should NOT appear in this case.
  /// </remarks>
  public static void ShouldAutoComplete(this TestTerminal terminal, string expected)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    ArgumentNullException.ThrowIfNull(expected);

    terminal.OutputContains(expected).ShouldBeTrue(
      $"Should auto-complete to '{expected}'"
    );
    terminal.OutputContains("Available completions").ShouldBeFalse(
      "Should NOT show completion list header for unique match auto-completion"
    );
  }

  /// <summary>
  /// Asserts that the completion list header is displayed in the terminal output.
  /// </summary>
  /// <param name="terminal">The test terminal to check.</param>
  /// <exception cref="AssertionException">When completion list header is not found.</exception>
  /// <remarks>
  /// The completion list is shown when there are multiple possible completions.
  /// </remarks>
  public static void ShouldShowCompletionList(this TestTerminal terminal)
  {
    ArgumentNullException.ThrowIfNull(terminal);

    terminal.OutputContains("Available completions").ShouldBeTrue(
      "Should show 'Available completions' header when displaying completion list"
    );
  }
}

/// <summary>
/// Extension methods for simulating user input key sequences in TestTerminal.
/// </summary>
public static class KeySequenceHelpers
{
  /// <summary>
  /// Types a string of text followed by a Tab key press.
  /// </summary>
  /// <param name="terminal">The test terminal to queue input on.</param>
  /// <param name="text">The text to type before pressing Tab.</param>
  /// <remarks>
  /// This is a common pattern for testing completion: type partial input, then Tab to complete.
  /// </remarks>
  public static void TypeAndTab(this TestTerminal terminal, string text)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    ArgumentNullException.ThrowIfNull(text);

    terminal.QueueKeys(text);
    terminal.QueueKey(ConsoleKey.Tab);
  }

  /// <summary>
  /// Presses the Tab key multiple times in sequence.
  /// </summary>
  /// <param name="terminal">The test terminal to queue input on.</param>
  /// <param name="count">The number of times to press Tab.</param>
  /// <remarks>
  /// Useful for testing completion cycling behavior (Tab, Tab, Tab... cycles through matches).
  /// </remarks>
  public static void TabMultipleTimes(this TestTerminal terminal, int count)
  {
    ArgumentNullException.ThrowIfNull(terminal);

    for (int i = 0; i < count; i++)
    {
      terminal.QueueKey(ConsoleKey.Tab);
    }
  }

  /// <summary>
  /// Simulates cleanup sequence to exit REPL gracefully: Escape, Enter, "exit" command.
  /// </summary>
  /// <param name="terminal">The test terminal to queue input on.</param>
  /// <remarks>
  /// Standard cleanup pattern for REPL tests:
  /// 1. Escape - Cancel any pending completion or clear current input
  /// 2. Enter - Submit empty line (no-op)
  /// 3. "exit" - Exit REPL mode
  /// </remarks>
  public static void CleanupAndExit(this TestTerminal terminal)
  {
    ArgumentNullException.ThrowIfNull(terminal);

    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("");
    terminal.QueueLine("exit");
  }
}

/// <summary>
/// Factory for creating test NuruApp instances configured with repl-basic-demo.cs routes.
/// </summary>
public static class TestAppFactory
{
  /// <summary>
  /// Creates a NuruApp configured exactly like Samples/ReplDemo/repl-basic-demo.cs.
  /// </summary>
  /// <param name="terminal">The test terminal to use for this app instance.</param>
  /// <returns>A fully configured NuruApp ready for REPL testing.</returns>
  /// <remarks>
  /// This factory creates an app with all 12 routes from repl-basic-demo.cs:
  ///
  /// SIMPLE COMMANDS:
  /// - status
  /// - time
  ///
  /// BASIC PARAMETERS:
  /// - greet {name}
  /// - add {a:int} {b:int}
  ///
  /// ENUM PARAMETERS:
  /// - deploy {env:environment} {tag?}
  ///
  /// CATCH-ALL PARAMETERS:
  /// - echo {*message}
  ///
  /// SUBCOMMANDS:
  /// - git status
  /// - git commit -m {message}
  /// - git log --count {n:int}
  ///
  /// OPTIONS:
  /// - build --verbose,-v
  /// - search {query} --limit,-l {count:int?}
  /// - backup {source} --compress,-c --output,-o {dest?}
  ///
  /// All handlers return 0 (success) for testing purposes.
  /// REPL is configured with minimal options: "demo> " prompt and arrow history enabled.
  /// </remarks>
  public static NuruApp CreateReplDemoApp(TestTerminal terminal)
  {
    return new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddTypeConverter(new EnumTypeConverter<Environment>())

      // ========================================
      // SIMPLE COMMANDS (Literal only)
      // ========================================
      .AddRoute("status", () => 0, description: "Displays the current system status.")
      .AddRoute("time", () => 0, description: "Displays the current time.")

      // ========================================
      // BASIC PARAMETERS
      // ========================================
      .AddRoute("greet {name}", (string _) => 0, description: "Greets the person with the specified name.")
      .AddRoute("add {a:int} {b:int}", (int _, int _2) => 0, description: "Adds two integers.")

      // ========================================
      // ENUM PARAMETERS
      // ========================================
      .AddRoute("deploy {env:environment} {tag?}", (Environment _, string? _2) => 0,
        description: "Deploys to environment (dev, staging, prod) with optional tag.")

      // ========================================
      // CATCH-ALL PARAMETERS
      // ========================================
      .AddRoute("echo {*message}", (string[] _) => 0, description: "Echoes all arguments back.")

      // ========================================
      // SUBCOMMANDS (Hierarchical routes)
      // ========================================
      .AddRoute("git status", () => 0, description: "Shows git working tree status.")
      .AddRoute("git commit -m {message}", (string _) => 0, description: "Creates a commit with the specified message.")
      .AddRoute("git log --count {n:int}", (int _) => 0, description: "Shows the last N commits.")

      // ========================================
      // BOOLEAN OPTIONS
      // ========================================
      .AddRoute("build --verbose,-v", (bool _) => 0, description: "Builds the project. Use -v for verbose output.")

      // ========================================
      // OPTIONS WITH VALUES
      // ========================================
      .AddRoute("search {query} --limit,-l {count:int?}", (string _, int? _2) => 0,
        description: "Searches with optional result limit.")

      // ========================================
      // COMBINED OPTIONS
      // ========================================
      .AddRoute("backup {source} --compress,-c --output,-o {dest?}", (string _, bool _2, string? _3) => 0,
        description: "Backs up source with optional compression and destination.")

      // ========================================
      // REPL CONFIGURATION
      // ========================================
      .AddReplSupport(options =>
      {
        options.Prompt = "demo> ";
        options.EnableArrowHistory = true;
      })
      .Build();
  }
}

/// <summary>
/// Environment enum for deploy command - demonstrates enum type conversion.
/// </summary>
public enum Environment
{
  Dev,
  Staging,
  Prod
}
