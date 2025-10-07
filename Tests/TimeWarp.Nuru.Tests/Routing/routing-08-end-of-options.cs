#!/usr/bin/dotnet --

return await RunTests<EndOfOptionsTests>(clearCache: true);

[TestTag("Routing")]
public class EndOfOptionsTests
{
  public static async Task Should_match_end_of_options_with_catch_all_run_not_a_flag_file_txt()
  {
    // Arrange
    NuruAppBuilder builder = new();
    string[]? boundArgs = null;
    builder.AddRoute("run -- {*args}", (string[] args) => { boundArgs = args; return 0; });

    NuruApp app = builder.Build();

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
    NuruAppBuilder builder = new();
    string? boundScript = null;
    string[]? boundArgs = null;
    builder.AddRoute("execute {script} -- {*args}", (string script, string[] args) => { boundScript = script; boundArgs = args; return 0; });

    NuruApp app = builder.Build();

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
    NuruAppBuilder builder = new();
    string[]? boundArgs = null;
    builder.AddRoute("run -- {*args}", (string[] args) => { boundArgs = args; return 0; });

    NuruApp app = builder.Build();

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
    NuruAppBuilder builder = new();
    builder.UseDebugLogging();
    bool boundDetach = false;
    string[]? boundCmd = null;
    builder.AddRoute("docker run --detach -- {*cmd}", (bool detach, string[] cmd) => { boundDetach = detach; boundCmd = cmd; return 0; });

    NuruApp app = builder.Build();

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