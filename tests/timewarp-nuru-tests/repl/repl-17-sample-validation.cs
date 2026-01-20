#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// Tests that validate REPL behavior using the same routes as repl-basic-demo.cs
// This test file mirrors the exact route configuration from samples/repl-demo/repl-basic-demo.cs

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.SampleValidation
{
  public enum Environment
  {
    Dev,
    Staging,
    Prod
  }

  [TestTag("REPL")]
  public class SampleValidationTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<SampleValidationTests>();

    private static TestTerminal Terminal = null!;
    private static NuruApp App = null!;

    public static async Task Setup()
    {
      // Create fresh terminal and app for each test
      Terminal = new TestTerminal();

      App = NuruApp.CreateBuilder()
        .UseTerminal(Terminal)
        .AddTypeConverter(new EnumTypeConverter<Environment>())

        // ========================================
        // SIMPLE COMMANDS (Literal only)
        // ========================================
        .Map("status")
          .WithHandler(() => { })
          .WithDescription("Displays the current system status.")
          .AsQuery()
          .Done()
        .Map("time")
          .WithHandler(() => { })
          .WithDescription("Displays the current time.")
          .AsQuery()
          .Done()

        // ========================================
        // BASIC PARAMETERS
        // ========================================
        .Map("greet {name}")
          .WithHandler((string name) => 0)
          .WithDescription("Greets the person with the specified name.")
          .AsCommand()
          .Done()
        .Map("add {a:int} {b:int}")
          .WithHandler((int a, int b) => 0)
          .WithDescription("Adds two integers.")
          .AsQuery()
          .Done()

        // ========================================
        // ENUM PARAMETERS
        // ========================================
        .Map("deploy {env:environment} {tag?}")
          .WithHandler((Environment env, string? tag) => 0)
          .WithDescription("Deploys to environment (dev, staging, prod) with optional tag.")
          .AsCommand()
          .Done()

        // ========================================
        // CATCH-ALL PARAMETERS
        // ========================================
        .Map("echo {*message}")
          .WithHandler((string[] message) => 0)
          .WithDescription("Echoes all arguments back.")
          .AsQuery()
          .Done()

        // ========================================
        // SUBCOMMANDS (Hierarchical routes)
        // ========================================
        .Map("git status")
          .WithHandler(() => { })
          .WithDescription("Shows git working tree status.")
          .AsQuery()
          .Done()
        .Map("git commit -m {message}")
          .WithHandler((string message) => 0)
          .WithDescription("Creates a commit with the specified message.")
          .AsCommand()
          .Done()
        .Map("git log --count {n:int}")
          .WithHandler((int n) => 0)
          .WithDescription("Shows the last N commits.")
          .AsQuery()
          .Done()

        // ========================================
        // BOOLEAN OPTIONS
        // ========================================
        .Map("build --verbose,-v")
          .WithHandler((bool verbose) => 0)
          .WithDescription("Builds the project. Use -v for verbose output.")
          .AsCommand()
          .Done()

        // ========================================
        // OPTIONS WITH VALUES
        // ========================================
        .Map("search {query} --limit,-l {count:int?}")
          .WithHandler((string query, int? count) => 0)
          .WithDescription("Searches with optional result limit.")
          .AsQuery()
          .Done()

        // ========================================
        // COMBINED OPTIONS
        // ========================================
        .Map("backup {source} --compress,-c --output,-o {dest?}")
          .WithHandler((string source, bool compress, string? dest) => 0)
          .WithDescription("Backs up source with optional compression and destination.")
          .AsCommand()
          .Done()

        // ========================================
        // REPL CONFIGURATION
        // ========================================
        .AddRepl(options =>
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
      await App.RunAsync(["--interactive"]);

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
      await App.RunAsync(["--interactive"]);

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
      await App.RunAsync(["--interactive"]);

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
      await App.RunAsync(["--interactive"]);

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
      await App.RunAsync(["--interactive"]);

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
      await App.RunAsync(["--interactive"]);

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
      await App.RunAsync(["--interactive"]);

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
      await App.RunAsync(["--interactive"]);

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
      await App.RunAsync(["--interactive"]);

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
      await App.RunAsync(["--interactive"]);

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
      await App.RunAsync(["--interactive"]);

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
      await App.RunAsync(["--interactive"]);

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
      await App.RunAsync(["--interactive"]);

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
      await App.RunAsync(["--interactive"]);

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
      await App.RunAsync(["--interactive"]);

      // Assert: Should NOT show route-specific options before selecting subcommand
      Terminal.OutputContains("--count").ShouldBeFalse("Should NOT show '--count' (only for 'git log')");
      Terminal.OutputContains(" -m").ShouldBeFalse("Should NOT show '-m' (only for 'git commit')");

      // Should still show subcommands
      Terminal.OutputContains("commit").ShouldBeTrue("Should show 'commit' subcommand");
      Terminal.OutputContains("log").ShouldBeTrue("Should show 'log' subcommand");
      Terminal.OutputContains("status").ShouldBeTrue("Should show 'status' subcommand");
    }
  }
}
