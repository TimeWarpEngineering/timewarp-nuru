#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class CatchAllTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CatchAllTests>();

  public static async Task Should_match_basic_catch_all_run_one_two_three()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("run {*args}").WithHandler((string[] args) => $"args:[{string.Join(",", args)}]").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["run", "one", "two", "three"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("args:[one,two,three]").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_empty_catch_all_passthrough()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("passthrough {*args}").WithHandler((string[] args) => $"args:[{string.Join(",", args)}]|len:{args.Length}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["passthrough"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("args:[]|len:0").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_catch_all_after_literals_docker_run_nginx_port_8080()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("docker run {*cmd}").WithHandler((string[] cmd) => $"cmd:[{string.Join(",", cmd)}]").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["docker", "run", "nginx", "--port", "8080"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("cmd:[nginx,--port,8080]").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_catch_all_after_parameters_execute_test_sh_verbose_output_log_txt()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("execute {script} {*args}").WithHandler((string script, string[] args) => $"script:{script}|args:[{string.Join(",", args)}]").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["execute", "test.sh", "--verbose", "--output", "log.txt"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("script:test.sh").ShouldBeTrue();
    terminal.OutputContains("args:[--verbose,--output,log.txt]").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_catch_all_preserves_options_npm_install_save_dev_typescript()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("npm {*args}").WithHandler((string[] args) => $"args:[{string.Join(",", args)}]").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["npm", "install", "--save-dev", "typescript"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("args:[install,--save-dev,typescript]").ShouldBeTrue();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
