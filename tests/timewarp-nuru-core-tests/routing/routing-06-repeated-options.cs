#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class RepeatedOptionsTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<RepeatedOptionsTests>();

  public static async Task Should_match_basic_repeated_option_docker_run_env_A_B_C()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("docker run --env {e}*")
      .WithHandler((string[] e) => $"e:[{string.Join(",", e)}]|len:{e.Length}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["docker", "run", "--env", "A", "--env", "B", "--env", "C"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[A,B,C]").ShouldBeTrue();
    terminal.OutputContains("len:3").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_empty_repeated_option_docker_run()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("docker run --env {e}*")
      .WithHandler((string[] e) => $"e:[{string.Join(",", e)}]|len:{e.Length}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["docker", "run"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[]").ShouldBeTrue();
    terminal.OutputContains("len:0").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_typed_repeated_option_process_id_1_2_3()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("process --id {id:int}*")
      .WithHandler((int[] id) => $"id:[{string.Join(",", id)}]|len:{id.Length}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["process", "--id", "1", "--id", "2", "--id", "3"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("id:[1,2,3]").ShouldBeTrue();
    terminal.OutputContains("len:3").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_repeated_option_with_alias_docker_run_env_A_e_B_env_C()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("docker run --env,-e {e}*")
      .WithHandler((string[] e) => $"e:[{string.Join(",", e)}]|len:{e.Length}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["docker", "run", "--env", "A", "-e", "B", "--env", "C"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("e:[A,B,C]").ShouldBeTrue();
    terminal.OutputContains("len:3").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_mixed_repeated_single_deploy_env_prod_tag_v1_v2_verbose()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy --env {e} --tag {t}* --verbose")
      .WithHandler((string e, string[] t, bool verbose) => $"e:{e}|t:[{string.Join(",", t)}]|verbose:{verbose}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "prod", "--tag", "v1", "--tag", "v2", "--verbose"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("e:prod").ShouldBeTrue();
    terminal.OutputContains("t:[v1,v2]").ShouldBeTrue();
    terminal.OutputContains("verbose:True").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_single_value_repeated_option_run_flag_X()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("run --flag {f}*")
      .WithHandler((string[] f) => $"f:[{string.Join(",", f)}]|len:{f.Length}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["run", "--flag", "X"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("f:[X]").ShouldBeTrue();
    terminal.OutputContains("len:1").ShouldBeTrue();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
