#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class ComplexIntegrationTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<ComplexIntegrationTests>();

  public static async Task Should_match_docker_style_command()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("docker run -i -t --env {e}* -- {*cmd}").WithHandler((bool i, bool t, string[] e, string[] cmd) => $"i:{i}|t:{t}|e:[{string.Join(",", e)}]|cmd:[{string.Join(",", cmd)}]").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["docker", "run", "-i", "-t", "--env", "A=1", "--env", "B=2", "--", "nginx", "--port", "80"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("i:True").ShouldBeTrue();
    terminal.OutputContains("t:True").ShouldBeTrue();
    terminal.OutputContains("e:[A=1,B=2]").ShouldBeTrue();
    terminal.OutputContains("cmd:[nginx,--port,80]").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_git_commit_with_aliases()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("git commit --message,-m {msg} --amend --no-verify").WithHandler((string msg, bool amend, bool noVerify) => $"msg:{msg}|amend:{amend}|noVerify:{noVerify}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["git", "commit", "-m", "fix bug", "--amend"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("msg:fix bug").ShouldBeTrue();
    terminal.OutputContains("amend:True").ShouldBeTrue();
    terminal.OutputContains("noVerify:False").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_progressive_enhancement_build_verbose()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("build {project?} --config? {cfg?} --verbose --watch").WithHandler((string? project, string? cfg, bool verbose, bool watch) => $"project:{project ?? "NULL"}|cfg:{cfg ?? "NULL"}|verbose:{verbose}|watch:{watch}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--verbose"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("project:NULL").ShouldBeTrue();
    terminal.OutputContains("cfg:NULL").ShouldBeTrue();
    terminal.OutputContains("verbose:True").ShouldBeTrue();
    terminal.OutputContains("watch:False").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_multi_valued_with_types_process_id_1_2_tag_A_run_sh()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("process --id {id:int}* --tag {t}* {script}").WithHandler((int[] id, string[] t, string script) => $"id:[{string.Join(",", id)}]|t:[{string.Join(",", t)}]|script:{script}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["process", "--id", "1", "--id", "2", "--tag", "A", "run.sh"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("id:[1,2]").ShouldBeTrue();
    terminal.OutputContains("t:[A]").ShouldBeTrue();
    terminal.OutputContains("script:run.sh").ShouldBeTrue();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
