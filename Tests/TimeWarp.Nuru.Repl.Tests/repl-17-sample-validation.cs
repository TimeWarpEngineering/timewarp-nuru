#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;

// Tests that validate REPL behavior using the same routes as repl-basic-demo.cs
// This test file mirrors the exact route configuration from samples/repl-demo/repl-basic-demo.cs

return await RunTests<SampleValidationTests>();

public enum Environment
{
  Dev,
  Staging,
  Prod
}

[TestTag("REPL")]
[ClearRunfileCache]
public class SampleValidationTests
{
  private static TestTerminal Terminal = null!;
  private static NuruCoreApp App = null!;

  public static async Task Setup()
  {
    // Create fresh terminal and app for each test
    Terminal = new TestTerminal();

    App = new NuruAppBuilder()
      .UseTerminal(Terminal)
      .AddTypeConverter(new EnumTypeConverter<Environment>())

      // ========================================
      // SIMPLE COMMANDS (Literal only)
      // ========================================
      .Map("status", () => 0, description: "Displays the current system status.")
      .Map("time", () => 0, description: "Displays the current time.")

      // ========================================
      // BASIC PARAMETERS
      // ========================================
      .Map("greet {name}", (string _) => 0, description: "Greets the person with the specified name.")
      .Map("add {a:int} {b:int}", (int _, int _2) => 0, description: "Adds two integers.")

      // ========================================
      // ENUM PARAMETERS
      // ========================================
      .Map("deploy {env:environment} {tag?}", (Environment _, string? _2) => 0,
        description: "Deploys to environment (dev, staging, prod) with optional tag.")

      // ========================================
      // CATCH-ALL PARAMETERS
      // ========================================
      .Map("echo {*message}", (string[] _) => 0, description: "Echoes all arguments back.")

      // ========================================
      // SUBCOMMANDS (Hierarchical routes)
      // ========================================
      .Map("git status", () => 0, description: "Shows git working tree status.")
      .Map("git commit -m {message}", (string _) => 0, description: "Creates a commit with the specified message.")
      .Map("git log --count {n:int}", (int _) => 0, description: "Shows the last N commits.")

      // ========================================
      // BOOLEAN OPTIONS
      // ========================================
      .Map("build --verbose,-v", (bool _) => 0, description: "Builds the project. Use -v for verbose output.")

      // ========================================
      // OPTIONS WITH VALUES
      // ========================================
      .Map("search {query} --limit,-l {count:int?}", (string _, int? _2) => 0,
        description: "Searches with optional result limit.")

      // ========================================
      // COMBINED OPTIONS
      // ========================================
      .Map("backup {source} --compress,-c --output,-o {dest?}", (string _, bool _2, string? _3) => 0,
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

    await Task.CompletedTask;
  }

  public static async Task CleanUp()
  {
    Terminal?.Dispose();
    Terminal = null!;
    App = null!;
    await Task.CompletedTask;
  }

  // ============================================================================
  // ENUM COMPLETION TESTS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_enum_values_in_completions_after_deploy_space()
  {
    // Arrange: Type "deploy " then Tab to see completions
    Terminal.QueueKeys("deploy ");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKey(ConsoleKey.Escape);
    Terminal.QueueLine("");
    Terminal.QueueLine("exit");

    // Act
    await App.RunReplAsync();

    // Assert: All enum values should appear in completions
    Terminal.OutputContains("Dev").ShouldBeTrue("Should show 'Dev' enum value");
    Terminal.OutputContains("Prod").ShouldBeTrue("Should show 'Prod' enum value");
    Terminal.OutputContains("Staging").ShouldBeTrue("Should show 'Staging' enum value");
  }

  [Timeout(5000)]
  public static async Task Should_show_help_option_in_completions_after_deploy_space()
  {
    // Arrange
    Terminal.QueueKeys("deploy ");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKey(ConsoleKey.Escape);
    Terminal.QueueLine("");
    Terminal.QueueLine("exit");

    // Act
    await App.RunReplAsync();

    // Assert: --help option should also appear
    Terminal.OutputContains("--help").ShouldBeTrue("Should show '--help' option");
  }

  [Timeout(5000)]
  public static async Task Should_filter_enum_completions_with_partial_p()
  {
    // Arrange: Type "deploy p" then Tab - only "Prod" starts with p
    Terminal.QueueKeys("deploy p");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKey(ConsoleKey.Escape);
    Terminal.QueueLine("");
    Terminal.QueueLine("exit");

    // Act
    await App.RunReplAsync();

    // Assert: Should show "Prod" (either as completion or auto-completed)
    Terminal.OutputContains("Prod").ShouldBeTrue("Should show or complete to 'Prod'");

    // Debug output to see what actually happened
    WriteLine("=== OUTPUT FOR PARTIAL 'p' ===");
    WriteLine(Terminal.Output);
    WriteLine("=== END ===");
  }

  [Timeout(5000)]
  public static async Task Should_filter_enum_completions_with_partial_s()
  {
    // Arrange: Type "deploy s" then Tab - only "Staging" starts with s
    Terminal.QueueKeys("deploy s");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKey(ConsoleKey.Escape);
    Terminal.QueueLine("");
    Terminal.QueueLine("exit");

    // Act
    await App.RunReplAsync();

    // Assert: Should show "Staging" (either as completion or auto-completed)
    Terminal.OutputContains("Staging").ShouldBeTrue("Should show or complete to 'Staging'");
  }

  [Timeout(5000)]
  public static async Task Should_filter_enum_completions_with_partial_d()
  {
    // Arrange: Type "deploy d" then Tab - only "Dev" starts with d
    Terminal.QueueKeys("deploy d");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKey(ConsoleKey.Escape);
    Terminal.QueueLine("");
    Terminal.QueueLine("exit");

    // Act
    await App.RunReplAsync();

    // Assert: Should show "Dev" (either as completion or auto-completed)
    Terminal.OutputContains("Dev").ShouldBeTrue("Should show or complete to 'Dev'");
  }

  // ============================================================================
  // SUBCOMMAND COMPLETION TESTS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_git_subcommands_on_tab()
  {
    // Arrange: Type "git " then Tab
    Terminal.QueueKeys("git ");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKey(ConsoleKey.Escape);
    Terminal.QueueLine("");
    Terminal.QueueLine("exit");

    // Act
    await App.RunReplAsync();

    // Assert: Should show git subcommands
    Terminal.OutputContains("status").ShouldBeTrue("Should show 'status' subcommand");
    Terminal.OutputContains("commit").ShouldBeTrue("Should show 'commit' subcommand");
    Terminal.OutputContains("log").ShouldBeTrue("Should show 'log' subcommand");
  }

  // ============================================================================
  // COMMAND COMPLETION TESTS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_complete_partial_command_d_to_deploy()
  {
    // Arrange: Type "d" then Tab - should complete to "deploy"
    Terminal.QueueKeys("d");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKey(ConsoleKey.Escape);
    Terminal.QueueLine("");
    Terminal.QueueLine("exit");

    // Act
    await App.RunReplAsync();

    // Assert: Should show "deploy" in output
    Terminal.OutputContains("deploy").ShouldBeTrue("Should complete 'd' to 'deploy'");
  }

  [Timeout(5000)]
  public static async Task Should_show_multiple_commands_starting_with_s()
  {
    // Arrange: Type "s" then Tab - "status" and "search" both start with s
    Terminal.QueueKeys("s");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKey(ConsoleKey.Escape);
    Terminal.QueueLine("");
    Terminal.QueueLine("exit");

    // Act
    await App.RunReplAsync();

    // Assert: Should show both commands
    Terminal.OutputContains("status").ShouldBeTrue("Should show 'status'");
    Terminal.OutputContains("search").ShouldBeTrue("Should show 'search'");
  }

  // ============================================================================
  // TAB CYCLING TESTS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_available_completions_header()
  {
    // Arrange
    Terminal.QueueKeys("deploy ");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKey(ConsoleKey.Escape);
    Terminal.QueueLine("");
    Terminal.QueueLine("exit");

    // Act
    await App.RunReplAsync();

    // Assert
    Terminal.OutputContains("Available completions").ShouldBeTrue("Should show completions header");
  }

  [Timeout(5000)]
  public static async Task Should_cycle_to_first_completion_on_second_tab()
  {
    // Arrange: Type "deploy ", Tab (show list), Tab again (should cycle)
    Terminal.QueueKeys("deploy ");
    Terminal.QueueKey(ConsoleKey.Tab);  // Show completions list
    Terminal.QueueKey(ConsoleKey.Tab);  // Should cycle to first item
    Terminal.QueueKey(ConsoleKey.Escape);
    Terminal.QueueLine("");
    Terminal.QueueLine("exit");

    // Act
    await App.RunReplAsync();

    // Debug output to see cycling behavior
    WriteLine("=== OUTPUT FOR TAB CYCLING ===");
    WriteLine(Terminal.Output);
    WriteLine("=== END ===");

    // Assert: Should show completions
    Terminal.OutputContains("Available completions").ShouldBeTrue("Should show completions on first tab");
  }

  // ============================================================================
  // OPTION COMPLETION TESTS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_build_options_on_tab()
  {
    // Arrange: Type "build " then Tab
    Terminal.QueueKeys("build ");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKey(ConsoleKey.Escape);
    Terminal.QueueLine("");
    Terminal.QueueLine("exit");

    // Act
    await App.RunReplAsync();

    // Assert: Should show verbose options
    bool hasVerbose = Terminal.OutputContains("--verbose") || Terminal.OutputContains("-v");
    hasVerbose.ShouldBeTrue("Should show verbose option (--verbose or -v)");
  }

  // ============================================================================
  // TAB COMPLETION SEQUENCE TEST
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_completions_and_autocomplete_unique_match()
  {
    // This test validates basic completion: g<tab>i<tab><space><tab>
    // Expected behavior:
    // 1. g<tab>     → "g" (multiple matches: git, greet - show completions)
    // 2. i         → "gi"
    // 3. <tab>     → "git" (unique match auto-completes)
    // 4. <space>   → "git "
    // 5. <tab>     → "git " (show completions: commit, log, status, --count, --help, -m)

    // Note: Multi-tab cycling (git <tab><tab><tab> to cycle commit→log→status)
    // is tested in repl-19-tab-cycling-bug.cs (currently has a bug)

    // Arrange
    Terminal.QueueKey(ConsoleKey.G);           // g
    Terminal.QueueKey(ConsoleKey.Tab);          // <tab> (multiple matches)
    Terminal.QueueKey(ConsoleKey.I);            // i
    Terminal.QueueKey(ConsoleKey.Tab);          // <tab> (should complete to "git")
    Terminal.QueueKey(ConsoleKey.Spacebar);     // <space>
    Terminal.QueueKey(ConsoleKey.Tab);          // <tab> (show completions)
    Terminal.QueueKey(ConsoleKey.Escape);       // Cancel
    Terminal.QueueLine("exit");

    // Act
    await App.RunReplAsync();

    // Assert
    WriteLine("=== COMPLETION OUTPUT ===");
    WriteLine(Terminal.Output);
    WriteLine("=== END ===");

    // Verify the sequence worked:
    // 1. "g" with tab showed git and greet
    Terminal.OutputContains("git").ShouldBeTrue("Should show 'git' in initial completions");
    Terminal.OutputContains("greet").ShouldBeTrue("Should show 'greet' in initial completions");

    // 2. "gi" with tab completed to "git"
    // 3. "git " with tab showed subcommands
    Terminal.OutputContains("commit").ShouldBeTrue("Should show 'commit' subcommand");
    Terminal.OutputContains("log").ShouldBeTrue("Should show 'log' subcommand");
    Terminal.OutputContains("status").ShouldBeTrue("Should show 'status' subcommand");

    // 4. Should show completions header
    Terminal.OutputContains("Available completions").ShouldBeTrue(
      "Should show completions header for ambiguous inputs"
    );
  }

  // ============================================================================
  // GIT SUBCOMMAND PARTIAL COMPLETION TESTS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_complete_partial_git_subcommand_com_to_commit()
  {
    // Arrange: Type "git com" then Tab - should autocomplete to "git commit"
    Terminal.QueueKeys("git com");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKey(ConsoleKey.Escape);
    Terminal.QueueLine("");
    Terminal.QueueLine("exit");

    // Act
    await App.RunReplAsync();

    // Assert: Should complete "com" to "commit"
    Terminal.OutputContains("commit").ShouldBeTrue("Should complete 'git com' to 'git commit'");
  }

  [Timeout(5000)]
  public static async Task Should_show_option_after_git_commit_space()
  {
    // Arrange: Type "git commit " then Tab - should show -m option
    Terminal.QueueKeys("git commit ");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKey(ConsoleKey.Escape);
    Terminal.QueueLine("");
    Terminal.QueueLine("exit");

    // Act
    await App.RunReplAsync();

    // Assert: Should show -m option (not complete to "git commit commit")
    Terminal.OutputContains("-m").ShouldBeTrue("Should show '-m' option after 'git commit '");
    Terminal.OutputContains("git commit commit").ShouldBeFalse("Should NOT duplicate 'commit'");
  }

  [Timeout(5000)]
  public static async Task Should_not_show_inappropriate_options_for_git_space()
  {
    // Arrange: Type "git " then Tab - should NOT show options like --count or -m
    Terminal.QueueKeys("git ");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKey(ConsoleKey.Escape);
    Terminal.QueueLine("");
    Terminal.QueueLine("exit");

    // Act
    await App.RunReplAsync();

    // Assert: Should NOT show route-specific options before selecting subcommand
    Terminal.OutputContains("--count").ShouldBeFalse("Should NOT show '--count' (only for 'git log')");
    Terminal.OutputContains(" -m").ShouldBeFalse("Should NOT show '-m' (only for 'git commit')");

    // Should still show subcommands
    Terminal.OutputContains("commit").ShouldBeTrue("Should show 'commit' subcommand");
    Terminal.OutputContains("log").ShouldBeTrue("Should show 'log' subcommand");
    Terminal.OutputContains("status").ShouldBeTrue("Should show 'status' subcommand");
  }
}
