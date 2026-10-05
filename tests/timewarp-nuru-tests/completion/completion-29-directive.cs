#!/usr/bin/env -S dotnet --

// Completion Directive Tests
// File/Directory candidates must enable shell path completion, candidate directive flags
// (NoSpace, KeepOrder) must reach the directive line, and spaced suggestions must stay one
// candidate in every dynamic shell template (482-003 / R-4).

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Completion.Directive
{

[TestTag("Completion")]
public class CompletionDirectiveTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CompletionDirectiveTests>();

  // ==========================================================================
  // ResolveDirective
  // ==========================================================================

  public static async Task Non_path_candidates_resolve_to_NoFileComp()
  {
    CompletionDirective directive = DynamicCompletionHandler.ResolveDirective(
    [
      new CompletionCandidate("deploy", null, CompletionType.Command),
      new CompletionCandidate("--force", null, CompletionType.Option)
    ]);

    directive.ShouldBe(CompletionDirective.NoFileComp);

    await Task.CompletedTask;
  }

  public static async Task No_candidates_resolve_to_NoFileComp()
  {
    DynamicCompletionHandler.ResolveDirective([]).ShouldBe(CompletionDirective.NoFileComp);

    await Task.CompletedTask;
  }

  public static async Task File_candidate_leaves_file_completion_enabled()
  {
    CompletionDirective directive = DynamicCompletionHandler.ResolveDirective(
    [
      new CompletionCandidate("", null, CompletionType.File),
      new CompletionCandidate("deploy", null, CompletionType.Command)
    ]);

    directive.HasFlag(CompletionDirective.NoFileComp).ShouldBeFalse();
    directive.HasFlag(CompletionDirective.FilterDirs).ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Directory_candidate_enables_directory_only_completion()
  {
    CompletionDirective directive = DynamicCompletionHandler.ResolveDirective(
    [
      new CompletionCandidate("", null, CompletionType.Directory)
    ]);

    directive.ShouldBe(CompletionDirective.FilterDirs);

    await Task.CompletedTask;
  }

  public static async Task File_candidate_may_still_request_NoFileComp()
  {
    CompletionDirective directive = DynamicCompletionHandler.ResolveDirective(
    [
      new CompletionCandidate("notes.txt", null, CompletionType.File, Directive: CompletionDirective.NoFileComp)
    ]);

    directive.HasFlag(CompletionDirective.NoFileComp).ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task NoSpace_and_KeepOrder_are_copied_onto_directive()
  {
    CompletionDirective directive = DynamicCompletionHandler.ResolveDirective(
    [
      new CompletionCandidate("key=", null, CompletionType.Parameter, Directive: CompletionDirective.NoSpace),
      new CompletionCandidate("first", null, CompletionType.Parameter, Directive: CompletionDirective.KeepOrder)
    ]);

    directive.ShouldBe(CompletionDirective.NoFileComp | CompletionDirective.NoSpace | CompletionDirective.KeepOrder);

    await Task.CompletedTask;
  }

  // ==========================================================================
  // __complete callback
  // ==========================================================================

  public static async Task Complete_with_path_source_emits_file_directive_and_spaced_value()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("open {path}").WithHandler((string path) => path.Length).AsCommand().Done()
      .EnableCompletion(configure: registry =>
      {
        registry.RegisterForParameter("path", new PathSource());
      })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["__complete", "2", "app", "open"]);

    // Assert
    exitCode.ShouldBe(0);
    string[] lines = [.. terminal.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r'))];
    lines.ShouldContain("my notes.txt");
    lines.ShouldContain($":{(int)(CompletionDirective.NoSpace | CompletionDirective.KeepOrder)}");
    lines.ShouldNotContain(":4");
    lines.ShouldNotContain(""); // Empty File marker is not printed as a candidate
  }

  // ==========================================================================
  // Script templates
  // ==========================================================================

  public static async Task Zsh_template_keeps_spaced_suggestion_as_one_word()
  {
    string script = DynamicCompletionScriptGenerator.GenerateZsh("myapp");

    script.ShouldContain("completions=(\"${(@f)output}\")");
    script.Contains("completions=(${(f)output})").ShouldBeFalse(
      "zsh must split output on newlines only, inside quotes"
    );

    await Task.CompletedTask;
  }

  public static async Task Zsh_template_honors_directive_flags()
  {
    string script = DynamicCompletionScriptGenerator.GenerateZsh("myapp");

    script.ShouldContain("_files -/");
    script.ShouldContain("_files &&");
    script.ShouldContain("(( directive & 8 )) && compadd_opts+=(-S '')");
    script.ShouldContain("(( directive & 64 )) && describe_flags+=(-V)");

    await Task.CompletedTask;
  }

  public static async Task Bash_template_honors_directive_flags()
  {
    string script = DynamicCompletionScriptGenerator.GenerateBash("myapp");

    script.ShouldContain("_filedir -d");
    script.ShouldContain("compopt -o nospace");
    script.ShouldContain("compopt -o nosort");

    await Task.CompletedTask;
  }

  public static async Task Fish_template_falls_back_to_path_completion()
  {
    string script = DynamicCompletionScriptGenerator.GenerateFish("myapp");

    script.ShouldContain("__fish_complete_path");
    script.ShouldContain("__fish_complete_directories");

    await Task.CompletedTask;
  }
}

sealed class PathSource : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    yield return new CompletionCandidate("", null, CompletionType.File, Directive: CompletionDirective.NoSpace);
    yield return new CompletionCandidate("my notes.txt", null, CompletionType.File, Directive: CompletionDirective.KeepOrder);
  }
}

} // namespace TimeWarp.Nuru.Tests.Completion.Directive
