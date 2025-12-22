namespace TimeWarp.Nuru.Tests.TabCompletion;

using TimeWarp.Nuru;

// ============================================================================
// Tab Completion Test Helpers
// ============================================================================
// Provides utilities for testing REPL tab completion functionality:
// - CompletionAssertions: Extension methods for verifying completion behavior
// - KeySequenceHelpers: Extension methods for simulating user input sequences
// - TestAppFactory: Factory for creating test apps with repl-basic-demo.cs routes
//
// Usage Example:
//   NuruCoreApp app = TestAppFactory.CreateReplDemoApp(terminal);
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
/// Factory for creating test NuruCoreApp instances configured with repl-basic-demo.cs routes.
/// </summary>
public static class TestAppFactory
{
  /// <summary>
  /// Creates a NuruCoreApp configured exactly like samples/repl-demo/repl-basic-demo.cs.
  /// </summary>
  /// <param name="terminal">The test terminal to use for this app instance.</param>
  /// <returns>A fully configured NuruCoreApp ready for REPL testing.</returns>
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
  // CA2000: NuruAppBuilder is IDisposable but only disposes ConfigurationManager,
  // which is not created when using new NuruAppBuilder() (only with NuruApp.CreateBuilder())
#pragma warning disable CA2000
  public static NuruCoreApp CreateReplDemoApp(TestTerminal terminal)
  {
    return new NuruAppBuilder()
#pragma warning restore CA2000
      .UseTerminal(terminal)
      .AddTypeConverter(new EnumTypeConverter<Environment>())

      // ========================================
      // SIMPLE COMMANDS (Literal only)
      // ========================================
      .Map("status")
        .WithHandler(() => 0)
        .WithDescription("Displays the current system status.")
        .AsQuery()
        .Done()
      .Map("time")
        .WithHandler(() => 0)
        .WithDescription("Displays the current time.")
        .AsQuery()
        .Done()

      // ========================================
      // BASIC PARAMETERS
      // ========================================
      .Map("greet {name}")
        .WithHandler((string _) => 0)
        .WithDescription("Greets the person with the specified name.")
        .AsCommand()
        .Done()
      .Map("add {a:int} {b:int}")
        .WithHandler((int _, int _2) => 0)
        .WithDescription("Adds two integers.")
        .AsCommand()
        .Done()

      // ========================================
      // ENUM PARAMETERS
      // ========================================
      .Map("deploy {env:environment} {tag?}")
        .WithHandler((Environment _, string? _2) => 0)
        .WithDescription("Deploys to environment (dev, staging, prod) with optional tag.")
        .AsCommand()
        .Done()

      // ========================================
      // CATCH-ALL PARAMETERS
      // ========================================
      .Map("echo {*message}")
        .WithHandler((string[] _) => 0)
        .WithDescription("Echoes all arguments back.")
        .AsCommand()
        .Done()

      // ========================================
      // SUBCOMMANDS (Hierarchical routes)
      // ========================================
      .Map("git status")
        .WithHandler(() => 0)
        .WithDescription("Shows git working tree status.")
        .AsQuery()
        .Done()
      .Map("git commit -m {message}")
        .WithHandler((string _) => 0)
        .WithDescription("Creates a commit with the specified message.")
        .AsCommand()
        .Done()
      .Map("git log --count {n:int}")
        .WithHandler((int _) => 0)
        .WithDescription("Shows the last N commits.")
        .AsQuery()
        .Done()

      // ========================================
      // BOOLEAN OPTIONS
      // ========================================
      .Map("build --verbose,-v")
        .WithHandler((bool _) => 0)
        .WithDescription("Builds the project. Use -v for verbose output.")
        .AsCommand()
        .Done()

      // ========================================
      // OPTIONS WITH VALUES
      // ========================================
      .Map("search {query} --limit,-l {count:int?}")
        .WithHandler((string _, int? _2) => 0)
        .WithDescription("Searches with optional result limit.")
        .AsQuery()
        .Done()

      // ========================================
      // COMBINED OPTIONS
      // ========================================
      .Map("backup {source} --compress,-c --output,-o {dest?}")
        .WithHandler((string _, bool _2, string? _3) => 0)
        .WithDescription("Backs up source with optional compression and destination.")
        .AsCommand()
        .Done()

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
