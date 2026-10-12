#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:package TimeWarp.Amuru

using TimeWarp.Amuru;

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Completion.BareBash
{

/// <summary>
/// The generated bash script must complete under plain bash, without the bash-completion
/// package (_init_completion / _filedir) sourced — kanban task 492.
/// </summary>
[TestTag("Completion")]
public class BareBashTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<BareBashTests>();

  public static async Task Should_complete_deploy_under_bare_bash_with_no_file_comp()
  {
    (int exitCode, string stdout, string stderr) = await RunBareBash(directive: 4);

    stderr.ShouldNotContain("command not found");
    exitCode.ShouldBe(0, stderr);
    stdout.Split('\n').ShouldContain("deploy");
    stdout.Split('\n').ShouldNotContain("status");
  }

  public static async Task Should_fall_back_to_compgen_for_file_candidates_under_bare_bash()
  {
    (int exitCode, string stdout, string stderr) = await RunBareBash(directive: 0);

    stderr.ShouldNotContain("command not found");
    exitCode.ShouldBe(0, stderr);
    stdout.Split('\n').ShouldContain("deploy");
    stdout.Split('\n').ShouldContain("deploy-notes.txt");
  }

  // Sources the generated script in `bash --norc --noprofile`, with a stub `app` standing in
  // for the __complete callback, then invokes the completion function for `app dep`.
  private static async Task<(int ExitCode, string Stdout, string Stderr)> RunBareBash(int directive)
  {
    string dir = Path.Combine(Path.GetTempPath(), $"nuru-492-bare-bash-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    try
    {
      string scriptPath = Path.Combine(dir, "app-completion.bash");
      await File.WriteAllTextAsync(scriptPath, DynamicCompletionScriptGenerator.GenerateBash("app"));
      await File.WriteAllTextAsync(Path.Combine(dir, "deploy-notes.txt"), "");

      string driver = $$"""
        cd '{{dir}}' || exit 3
        app() { printf 'deploy\tDeploy it\nstatus\n:{{directive}}\n'; }
        source ./app-completion.bash
        COMP_WORDS=(app dep)
        COMP_CWORD=1
        _app_completions
        printf '%s\n' "${COMPREPLY[@]}"
        """;

      CommandOutput output = await Shell.Builder("bash")
        .WithArguments("--norc", "--noprofile", "-c", driver)
        .WithNoValidation()
        .CaptureAsync();
      return (output.ExitCode, output.Stdout, output.Stderr);
    }
    finally
    {
      Directory.Delete(dir, recursive: true);
    }
  }
}

} // namespace TimeWarp.Nuru.Tests.Completion.BareBash
