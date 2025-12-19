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
    string[]? boundArgs = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("run -- {*args}").WithHandler((string[] args) => { boundArgs = args; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["run", "--", "--not-a-flag", "file.txt"]);

    // Assert
    exitCode.ShouldBe(0);
    boundArgs.ShouldNotBeNull();
    boundArgs.Length.ShouldBe(2);
    boundArgs[0].ShouldBe("--not-a-flag");
    boundArgs[1].ShouldBe("file.txt");

    await Task.CompletedTask;
  }

  public static async Task Should_match_parameter_before_end_of_options_execute_run_sh_verbose_file_txt()
  {
    // Arrange
    string? boundScript = null;
    string[]? boundArgs = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("execute {script} -- {*args}").WithHandler((string script, string[] args) => { boundScript = script; boundArgs = args; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["execute", "run.sh", "--", "--verbose", "file.txt"]);

    // Assert
    exitCode.ShouldBe(0);
    boundScript.ShouldBe("run.sh");
    boundArgs.ShouldNotBeNull();
    boundArgs.Length.ShouldBe(2);
    boundArgs[0].ShouldBe("--verbose");
    boundArgs[1].ShouldBe("file.txt");

    await Task.CompletedTask;
  }

  public static async Task Should_match_empty_args_after_separator_run()
  {
    // Arrange
    string[]? boundArgs = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("run -- {*args}").WithHandler((string[] args) => { boundArgs = args; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["run", "--"]);

    // Assert
    exitCode.ShouldBe(0);
    boundArgs.ShouldNotBeNull();
    boundArgs.Length.ShouldBe(0);

    await Task.CompletedTask;
  }

  public static async Task Should_parse_options_before_separator_docker_run_detach_nginx_port_80()
  {
    // Arrange
    bool boundDetach = false;
    string[]? boundCmd = null;
    NuruCoreApp app = new NuruAppBuilder()
      .UseDebugLogging()
      .Map("docker run --detach -- {*cmd}").WithHandler((bool detach, string[] cmd) => { boundDetach = detach; boundCmd = cmd; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["docker", "run", "--detach", "--", "nginx", "--port", "80"]);

    // Assert
    exitCode.ShouldBe(0);
    boundDetach.ShouldBeTrue();
    boundCmd.ShouldNotBeNull();
    boundCmd.Length.ShouldBe(3);
    boundCmd[0].ShouldBe("nginx");
    boundCmd[1].ShouldBe("--port");
    boundCmd[2].ShouldBe("80");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
