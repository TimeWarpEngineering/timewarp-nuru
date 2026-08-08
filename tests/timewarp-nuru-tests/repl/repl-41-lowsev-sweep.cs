#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

// Regression tests for the REPL/completion low-severity sweep (Task 454-030).
// Item 2 (history clear-on-load + merge-on-save), item 3 (completion template filters),
// item 5 (REPL survives an unexpected handler exception).

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.LowSevSweep
{

[TestTag("REPL")]
[TestTag("Completion")]
[TestTag("LowSevSweep")]
public class LowSevSweepTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<LowSevSweepTests>();

  private static string TempHistoryPath() =>
    Path.Combine(Path.GetTempPath(), $"nuru-454-030-{Guid.NewGuid():N}.txt");

  // internal (not private) so the source-generated interceptor can reference the method group.
  internal static string ThrowBoom() => throw new TimeoutException("kaboom");

  // ============================================================================
  // Item 2 — history: clear-on-Load, merge-on-Save
  // ============================================================================

  public static async Task Load_twice_does_not_duplicate_entries()
  {
    string path = TempHistoryPath();
    try
    {
      await File.WriteAllLinesAsync(path, ["cmd-a", "cmd-b"]);

      using TestTerminal terminal = new();
      ReplOptions options = new() { HistoryFilePath = path, PersistHistory = true };
      ReplHistory history = new(options, terminal);

      history.Load();
      history.Load(); // second Load must replace, not append

      history.Count.ShouldBe(2, "a second Load must not duplicate the loaded entries");
    }
    finally
    {
      if (File.Exists(path)) File.Delete(path);
    }
  }

  public static async Task Save_merges_a_concurrent_writers_entries()
  {
    string path = TempHistoryPath();
    try
    {
      using TestTerminal terminal = new();
      ReplOptions options = new() { HistoryFilePath = path, PersistHistory = true };
      ReplHistory history = new(options, terminal);
      history.Add("mine-1");
      history.Add("mine-2");

      // Simulate another REPL instance writing the shared file after we loaded.
      await File.WriteAllLinesAsync(path, ["other-1", "other-2"]);

      history.Save();

      string[] saved = await File.ReadAllLinesAsync(path);
      saved.ShouldContain("other-1"); // concurrent writer's entries preserved (not clobbered)
      saved.ShouldContain("other-2");
      saved.ShouldContain("mine-1"); // our entries present
      saved.ShouldContain("mine-2");
    }
    finally
    {
      if (File.Exists(path)) File.Delete(path);
    }
  }

  // ============================================================================
  // Item 3 — completion template filters / argument passing
  // ============================================================================

  public static async Task Pwsh_template_tokenizes_via_ast_not_space_split()
  {
    string script = DynamicCompletionScriptGenerator.GeneratePowerShell("myapp");

    script.ShouldContain("CommandElements"); // tokenized via the AST, not a naive split
    script.Contains("-split ' '").ShouldBeFalse("must not space-split the command line (breaks quoted args)");
    script.Contains("$words -join ' '").ShouldBeFalse("must not naively re-join args into one unquoted string");

    await Task.CompletedTask;
  }

  public static async Task Completion_filters_do_not_drop_numeric_candidates()
  {
    string pwsh = DynamicCompletionScriptGenerator.GeneratePowerShell("myapp");
    string fish = DynamicCompletionScriptGenerator.GenerateFish("myapp");

    // Only the ':' directive line is stripped; a standalone number is a valid candidate.
    pwsh.Contains("^\\d+$").ShouldBeFalse("pwsh must not skip standalone-number completion candidates");
    fish.Contains("^0$").ShouldBeFalse("fish must not drop the '0' completion candidate");
    fish.ShouldContain("string match -v -r '^:'");

    await Task.CompletedTask;
  }

  // ============================================================================
  // Item 5 — REPL survives an unexpected command exception
  // ============================================================================

  public static async Task Repl_survives_unexpected_handler_exception()
  {
    using TestTerminal terminal = new();
    terminal.QueueLine("boom");   // handler throws TimeoutException (not Argument/InvalidOperation)
    terminal.QueueLine("ok");     // must still run — REPL not torn down
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("boom")
        .WithHandler(ThrowBoom)
        .AsCommand()
        .Done()
      .Map("ok")
        .WithHandler(() => "OK-RAN")
        .AsCommand()
        .Done()
      .AddRepl(options =>
      {
        options.EnableColors = false;
        options.ContinueOnError = true;
        options.PersistHistory = false;
      })
      .Build();

    // Pre-fix, the uncaught TimeoutException propagated out of RunAsync and "ok" never ran.
    await app.RunAsync(["--interactive"]);

    terminal.OutputContains("OK-RAN")
      .ShouldBeTrue("REPL must catch an unexpected handler exception and continue to the next command");
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.LowSevSweep
