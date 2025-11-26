#!/usr/bin/dotnet --

return await RunTests<RepeatedOptionsTests>(clearCache: true);

[TestTag("Routing")]
[ClearRunfileCache]
public class RepeatedOptionsTests
{
  public static async Task Should_match_basic_repeated_option_docker_run_env_A_B_C()
  {
    // Arrange
    string[]? boundE = null;
    NuruApp app = new NuruAppBuilder()
      .Map("docker run --env {e}*", (string[] e) => { boundE = e; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["docker", "run", "--env", "A", "--env", "B", "--env", "C"]);

    // Assert
    exitCode.ShouldBe(0);
    boundE.ShouldNotBeNull();
    boundE.Length.ShouldBe(3);
    boundE[0].ShouldBe("A");
    boundE[1].ShouldBe("B");
    boundE[2].ShouldBe("C");

    await Task.CompletedTask;
  }

  public static async Task Should_match_empty_repeated_option_docker_run()
  {
    // Arrange
    string[]? boundE = null;
    NuruApp app = new NuruAppBuilder()
      .Map("docker run --env {e}*", (string[] e) => { boundE = e; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["docker", "run"]);

    // Assert
    exitCode.ShouldBe(0);
    boundE.ShouldNotBeNull();
    boundE.Length.ShouldBe(0);

    await Task.CompletedTask;
  }

  public static async Task Should_match_typed_repeated_option_process_id_1_2_3()
  {
    // Arrange
    int[]? boundId = null;
    NuruApp app = new NuruAppBuilder()
      .Map("process --id {id:int}*", (int[] id) => { boundId = id; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["process", "--id", "1", "--id", "2", "--id", "3"]);

    // Assert
    exitCode.ShouldBe(0);
    boundId.ShouldNotBeNull();
    boundId.Length.ShouldBe(3);
    boundId[0].ShouldBe(1);
    boundId[1].ShouldBe(2);
    boundId[2].ShouldBe(3);

    await Task.CompletedTask;
  }

  public static async Task Should_match_repeated_option_with_alias_docker_run_env_A_e_B_env_C()
  {
    // Arrange
    string[]? boundE = null;
    NuruApp app = new NuruAppBuilder()
      .Map("docker run --env,-e {e}*", (string[] e) => { boundE = e; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["docker", "run", "--env", "A", "-e", "B", "--env", "C"]);

    // Assert
    exitCode.ShouldBe(0);
    boundE.ShouldNotBeNull();
    boundE.Length.ShouldBe(3);
    boundE[0].ShouldBe("A");
    boundE[1].ShouldBe("B");
    boundE[2].ShouldBe("C");

    await Task.CompletedTask;
  }

  public static async Task Should_match_mixed_repeated_single_deploy_env_prod_tag_v1_v2_verbose()
  {
    // Arrange
    string? boundE = null;
    string[]? boundT = null;
    bool boundVerbose = false;
    NuruApp app = new NuruAppBuilder()
      .Map("deploy --env {e} --tag {t}* --verbose", (string e, string[] t, bool verbose) => { boundE = e; boundT = t; boundVerbose = verbose; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "prod", "--tag", "v1", "--tag", "v2", "--verbose"]);

    // Assert
    exitCode.ShouldBe(0);
    boundE.ShouldBe("prod");
    boundT.ShouldNotBeNull();
    boundT.Length.ShouldBe(2);
    boundT[0].ShouldBe("v1");
    boundT[1].ShouldBe("v2");
    boundVerbose.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_single_value_repeated_option_run_flag_X()
  {
    // Arrange
    string[]? boundF = null;
    NuruApp app = new NuruAppBuilder()
      .Map("run --flag {f}*", (string[] f) => { boundF = f; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["run", "--flag", "X"]);

    // Assert
    exitCode.ShouldBe(0);
    boundF.ShouldNotBeNull();
    boundF.Length.ShouldBe(1);
    boundF[0].ShouldBe("X");

    await Task.CompletedTask;
  }
}