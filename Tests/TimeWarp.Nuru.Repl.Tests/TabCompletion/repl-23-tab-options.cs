#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;
using TimeWarp.Nuru.Repl.Tests.TabCompletion;

// ============================================================================
// Option Completion Tests
// ============================================================================
// Tests tab completion behavior for command options:
// - Boolean options: build --verbose, build -v
// - Options with values: search foo --limit 10
// - Combined options: backup data -c -o dest
// - Short vs long options: -v vs --verbose
// - Option aliases completion
// - Partial option matching
//
// **BUGS DISCOVERED** (11/23 tests fail):
// 1. Tab after "command " does NOT show available options
//    - "build " + Tab should show --verbose, -v
//    - "search foo " + Tab should show --limit, -l
//    - "backup data " + Tab should show --compress, --output, -c, -o
//
// 2. Partial option completion doesn't work for some prefixes
//    - "search foo --l" + Tab should complete to --limit (FAILS)
//    - "backup data --c" + Tab should complete to --compress (FAILS)
//    - "backup data --o" + Tab should complete to --output (FAILS)
//
// 3. Case insensitive option matching incomplete
//    - "search foo --L" + Tab should match --limit (FAILS)
//
// These failing tests document real bugs that need fixing.
// Passing tests (12/23) show what currently works.
// ============================================================================

return await RunTests<OptionCompletionTests>();

[TestTag("REPL")]
[TestTag("TabCompletion")]
[ClearRunfileCache]
public class OptionCompletionTests
{
  private static TestTerminal Terminal = null!;
  private static NuruCoreApp App = null!;

  public static async Task Setup()
  {
    // Create fresh terminal and app for each test using helper factory
    Terminal = new TestTerminal();
    App = TestAppFactory.CreateReplDemoApp(Terminal);
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
  // BOOLEAN OPTIONS - build --verbose,-v
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_verbose_options_after_build_space()
  {
    // BUG: This test FAILS - option list not shown after command
    // Arrange: Type "build " then Tab
    Terminal.QueueKeys("build ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show verbose option (either -v or --verbose or both)
    bool hasVerbose = Terminal.OutputContains("--verbose") || Terminal.OutputContains("-v");
    hasVerbose.ShouldBeTrue("Should show verbose option (--verbose or -v)");
  }

  [Timeout(5000)]
  public static async Task Should_complete_build_dash_v()
  {
    // Arrange: Type "build -v" then Tab
    Terminal.QueueKeys("build -v");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should accept -v option
    Terminal.OutputContains("-v").ShouldBeTrue("Should accept '-v' short option");
  }

  [Timeout(5000)]
  public static async Task Should_complete_build_dash_dash_verbose()
  {
    // Arrange: Type "build --verbose" then Tab
    Terminal.QueueKeys("build --verbose");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should accept --verbose option
    Terminal.OutputContains("--verbose").ShouldBeTrue("Should accept '--verbose' long option");
  }

  [Timeout(5000)]
  public static async Task Should_complete_build_dash_dash_v_to_verbose()
  {
    // Arrange: Type "build --v" then Tab - should complete to --verbose
    Terminal.QueueKeys("build --v");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should complete to --verbose
    Terminal.OutputContains("--verbose").ShouldBeTrue("Should complete '--v' to '--verbose'");
  }

  [Timeout(5000)]
  public static async Task Should_complete_build_dash_dash_ver_to_verbose()
  {
    // Arrange: Type "build --ver" then Tab - should complete to --verbose
    Terminal.QueueKeys("build --ver");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should complete to --verbose
    Terminal.OutputContains("--verbose").ShouldBeTrue("Should complete '--ver' to '--verbose'");
  }

  // ============================================================================
  // OPTIONS WITH VALUES - search {query} --limit,-l {count:int?}
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_limit_option_after_search_query()
  {
    // BUG: This test FAILS - option list not shown after required parameter
    // Arrange: Type "search foo " then Tab
    Terminal.QueueKeys("search foo ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show limit option
    bool hasLimit = Terminal.OutputContains("--limit") || Terminal.OutputContains("-l");
    hasLimit.ShouldBeTrue("Should show limit option (--limit or -l)");
  }

  [Timeout(5000)]
  public static async Task Should_complete_search_foo_dash_l()
  {
    // Arrange: Type "search foo -l" then Tab
    Terminal.QueueKeys("search foo -l");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should accept -l option
    Terminal.OutputContains("-l").ShouldBeTrue("Should accept '-l' short option");
  }

  [Timeout(5000)]
  public static async Task Should_complete_search_foo_dash_dash_limit()
  {
    // Arrange: Type "search foo --limit" then Tab
    Terminal.QueueKeys("search foo --limit");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should accept --limit option
    Terminal.OutputContains("--limit").ShouldBeTrue("Should accept '--limit' long option");
  }

  [Timeout(5000)]
  public static async Task Should_complete_search_foo_dash_dash_l_to_limit()
  {
    // BUG: This test FAILS - partial option completion broken
    // Arrange: Type "search foo --l" then Tab - should complete to --limit
    Terminal.QueueKeys("search foo --l");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should complete to --limit
    Terminal.OutputContains("--limit").ShouldBeTrue("Should complete '--l' to '--limit'");
  }

  [Timeout(5000)]
  public static async Task Should_complete_search_foo_dash_dash_lim_to_limit()
  {
    // BUG: This test FAILS - partial option completion broken
    // Arrange: Type "search foo --lim" then Tab - should complete to --limit
    Terminal.QueueKeys("search foo --lim");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should complete to --limit
    Terminal.OutputContains("--limit").ShouldBeTrue("Should complete '--lim' to '--limit'");
  }

  // ============================================================================
  // COMBINED OPTIONS - backup {source} --compress,-c --output,-o {dest?}
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_compress_and_output_options_after_backup_source()
  {
    // BUG: This test FAILS - option list not shown after required parameter
    // Arrange: Type "backup data " then Tab
    Terminal.QueueKeys("backup data ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show both compress and output options
    bool hasOptions = Terminal.OutputContains("--compress") || Terminal.OutputContains("-c") ||
                      Terminal.OutputContains("--output") || Terminal.OutputContains("-o");
    hasOptions.ShouldBeTrue("Should show compress and output options");
  }

  [Timeout(5000)]
  public static async Task Should_complete_backup_data_dash_c()
  {
    // Arrange: Type "backup data -c" then Tab
    Terminal.QueueKeys("backup data -c");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should accept -c option
    Terminal.OutputContains("-c").ShouldBeTrue("Should accept '-c' short option");
  }

  [Timeout(5000)]
  public static async Task Should_complete_backup_data_dash_dash_compress()
  {
    // Arrange: Type "backup data --compress" then Tab
    Terminal.QueueKeys("backup data --compress");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should accept --compress option
    Terminal.OutputContains("--compress").ShouldBeTrue("Should accept '--compress' long option");
  }

  [Timeout(5000)]
  public static async Task Should_show_output_option_after_backup_data_compress()
  {
    // BUG: This test FAILS - remaining options not shown after using one option
    // Arrange: Type "backup data -c " then Tab - should still show output option
    Terminal.QueueKeys("backup data -c ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show output option
    bool hasOutput = Terminal.OutputContains("--output") || Terminal.OutputContains("-o");
    hasOutput.ShouldBeTrue("Should show output option after compress");
  }

  [Timeout(5000)]
  public static async Task Should_complete_backup_data_dash_c_dash_o()
  {
    // Arrange: Type "backup data -c -o" then Tab
    Terminal.QueueKeys("backup data -c -o");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should accept -o option
    Terminal.OutputContains("-o").ShouldBeTrue("Should accept '-o' short option");
  }

  // ============================================================================
  // PARTIAL OPTION MATCHING
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_complete_backup_data_dash_dash_c_to_compress()
  {
    // BUG: This test FAILS - partial option completion broken
    // Arrange: Type "backup data --c" then Tab - should complete to --compress
    Terminal.QueueKeys("backup data --c");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should complete to --compress
    Terminal.OutputContains("--compress").ShouldBeTrue("Should complete '--c' to '--compress'");
  }

  [Timeout(5000)]
  public static async Task Should_complete_backup_data_dash_dash_o_to_output()
  {
    // BUG: This test FAILS - partial option completion broken
    // Arrange: Type "backup data --o" then Tab - should complete to --output
    Terminal.QueueKeys("backup data --o");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should complete to --output
    Terminal.OutputContains("--output").ShouldBeTrue("Should complete '--o' to '--output'");
  }

  [Timeout(5000)]
  public static async Task Should_complete_backup_data_dash_dash_com_to_compress()
  {
    // BUG: This test FAILS - partial option completion broken
    // Arrange: Type "backup data --com" then Tab - should complete to --compress
    Terminal.QueueKeys("backup data --com");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should complete to --compress
    Terminal.OutputContains("--compress").ShouldBeTrue("Should complete '--com' to '--compress'");
  }

  [Timeout(5000)]
  public static async Task Should_complete_backup_data_dash_dash_out_to_output()
  {
    // BUG: This test FAILS - partial option completion broken
    // Arrange: Type "backup data --out" then Tab - should complete to --output
    Terminal.QueueKeys("backup data --out");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should complete to --output
    Terminal.OutputContains("--output").ShouldBeTrue("Should complete '--out' to '--output'");
  }

  // ============================================================================
  // CASE SENSITIVITY FOR OPTIONS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_complete_build_dash_dash_V_to_verbose_case_insensitive()
  {
    // This test PASSES - case insensitive matching works for some options
    // Arrange: Type "build --V" then Tab - case insensitive
    Terminal.QueueKeys("build --V");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should match case-insensitively
    Terminal.OutputContains("--verbose").ShouldBeTrue("Should match '--verbose' case-insensitively");
  }

  [Timeout(5000)]
  public static async Task Should_complete_search_foo_dash_dash_L_to_limit_case_insensitive()
  {
    // BUG: This test FAILS - case insensitive matching inconsistent
    // Arrange: Type "search foo --L" then Tab - case insensitive
    Terminal.QueueKeys("search foo --L");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should match case-insensitively
    Terminal.OutputContains("--limit").ShouldBeTrue("Should match '--limit' case-insensitively");
  }

  // ============================================================================
  // NO MATCHES FOR INVALID OPTIONS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_no_completions_for_build_dash_dash_z()
  {
    // Arrange: Type "build --z" then Tab - no options start with "z"
    Terminal.QueueKeys("build --z");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should not show verbose option
    Terminal.OutputContains("Available completions").ShouldBeFalse(
      "Should not show completions for invalid option prefix"
    );
  }

  [Timeout(5000)]
  public static async Task Should_show_no_completions_for_search_foo_dash_dash_x()
  {
    // Arrange: Type "search foo --x" then Tab - no options start with "x"
    Terminal.QueueKeys("search foo --x");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should not show limit option
    Terminal.OutputContains("Available completions").ShouldBeFalse(
      "Should not show completions for invalid option prefix"
    );
  }
}
