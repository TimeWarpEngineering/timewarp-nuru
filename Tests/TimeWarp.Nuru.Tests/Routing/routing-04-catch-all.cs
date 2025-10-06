#!/usr/bin/dotnet --

return await RunTests<CatchAllTests>(clearCache: true);

[TestTag("Routing")]
public class CatchAllTests
{
  public static async Task Should_match_basic_catch_all_run_one_two_three()
  {
    // Arrange
    NuruAppBuilder builder = new();
    string[]? boundArgs = null;
    builder.AddRoute("run {*args}", (string[] args) => { boundArgs = args; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["run", "one", "two", "three"]);

    // Assert
    exitCode.ShouldBe(0);
    boundArgs.ShouldNotBeNull();
    boundArgs.Length.ShouldBe(3);
    boundArgs[0].ShouldBe("one");
    boundArgs[1].ShouldBe("two");
    boundArgs[2].ShouldBe("three");

    await Task.CompletedTask;
  }

  public static async Task Should_match_empty_catch_all_passthrough()
  {
    // Arrange
    NuruAppBuilder builder = new();
    string[]? boundArgs = null;
    builder.AddRoute("passthrough {*args}", (string[] args) => { boundArgs = args; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["passthrough"]);

    // Assert
    exitCode.ShouldBe(0);
    boundArgs.ShouldNotBeNull();
    boundArgs.Length.ShouldBe(0);

    await Task.CompletedTask;
  }

  public static async Task Should_match_catch_all_after_literals_docker_run_nginx_port_8080()
  {
    // Arrange
    NuruAppBuilder builder = new();
    string[]? boundCmd = null;
    builder.AddRoute("docker run {*cmd}", (string[] cmd) => { boundCmd = cmd; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["docker", "run", "nginx", "--port", "8080"]);

    // Assert
    exitCode.ShouldBe(0);
    boundCmd.ShouldNotBeNull();
    boundCmd.Length.ShouldBe(3);
    boundCmd[0].ShouldBe("nginx");
    boundCmd[1].ShouldBe("--port");
    boundCmd[2].ShouldBe("8080");

    await Task.CompletedTask;
  }

  public static async Task Should_match_catch_all_after_parameters_execute_test_sh_verbose_output_log_txt()
  {
    // Arrange
    NuruAppBuilder builder = new();
    string? boundScript = null;
    string[]? boundArgs = null;
    builder.AddRoute("execute {script} {*args}", (string script, string[] args) => { boundScript = script; boundArgs = args; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["execute", "test.sh", "--verbose", "--output", "log.txt"]);

    // Assert
    exitCode.ShouldBe(0);
    boundScript.ShouldBe("test.sh");
    boundArgs.ShouldNotBeNull();
    boundArgs.Length.ShouldBe(3);
    boundArgs[0].ShouldBe("--verbose");
    boundArgs[1].ShouldBe("--output");
    boundArgs[2].ShouldBe("log.txt");

    await Task.CompletedTask;
  }

  public static async Task Should_match_catch_all_preserves_options_npm_install_save_dev_typescript()
  {
    // Arrange
    NuruAppBuilder builder = new();
    string[]? boundArgs = null;
    builder.AddRoute("npm {*args}", (string[] args) => { boundArgs = args; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["npm", "install", "--save-dev", "typescript"]);

    // Assert
    exitCode.ShouldBe(0);
    boundArgs.ShouldNotBeNull();
    boundArgs.Length.ShouldBe(3);
    boundArgs[0].ShouldBe("install");
    boundArgs[1].ShouldBe("--save-dev");
    boundArgs[2].ShouldBe("typescript");

    await Task.CompletedTask;
  }
}