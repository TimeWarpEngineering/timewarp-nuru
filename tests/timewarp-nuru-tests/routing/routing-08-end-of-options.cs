#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class EndOfOptionsTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<EndOfOptionsTests>();

  public static async Task Should_match_end_of_options_with_catch_all_run_not_a_flag_file_txt()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("run -- {*args}").WithHandler((string[] args) => $"args:[{string.Join(",", args)}]|len:{args.Length}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["run", "--", "--not-a-flag", "file.txt"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("args:[--not-a-flag,file.txt]").ShouldBeTrue();
    terminal.OutputContains("len:2").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_parameter_before_end_of_options_execute_run_sh_verbose_file_txt()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("execute {script} -- {*args}").WithHandler((string script, string[] args) => $"script:{script}|args:[{string.Join(",", args)}]").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["execute", "run.sh", "--", "--verbose", "file.txt"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("script:run.sh").ShouldBeTrue();
    terminal.OutputContains("args:[--verbose,file.txt]").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_empty_args_after_separator_run()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("run -- {*args}").WithHandler((string[] args) => $"args:[{string.Join(",", args)}]|len:{args.Length}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["run", "--"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("args:[]").ShouldBeTrue();
    terminal.OutputContains("len:0").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_options_before_separator_docker_run_detach_nginx_port_80()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("docker run --detach -- {*cmd}").WithHandler((bool detach, string[] cmd) => $"detach:{detach}|cmd:[{string.Join(",", cmd)}]").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["docker", "run", "--detach", "--", "nginx", "--port", "80"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("detach:True").ShouldBeTrue();
    terminal.OutputContains("cmd:[nginx,--port,80]").ShouldBeTrue();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
