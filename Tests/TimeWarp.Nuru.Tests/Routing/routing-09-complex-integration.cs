#!/usr/bin/dotnet --

return await RunTests<ComplexIntegrationTests>(clearCache: true);

[TestTag("Routing")]
public class ComplexIntegrationTests
{
  public static async Task Should_match_docker_style_command()
  {
    // Arrange
    NuruAppBuilder builder = new();
    bool boundI = false;
    bool boundT = false;
    string[]? boundE = null;
    string[]? boundCmd = null;
    builder.AddRoute("docker run -i -t --env {e}* -- {*cmd}", (bool i, bool t, string[] e, string[] cmd) => { boundI = i; boundT = t; boundE = e; boundCmd = cmd; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["docker", "run", "-i", "-t", "--env", "A=1", "--env", "B=2", "--", "nginx", "--port", "80"]);

    // Assert
    exitCode.ShouldBe(0);
    boundI.ShouldBeTrue();
    boundT.ShouldBeTrue();
    boundE.ShouldNotBeNull();
    boundE.Length.ShouldBe(2);
    boundE[0].ShouldBe("A=1");
    boundE[1].ShouldBe("B=2");
    boundCmd.ShouldNotBeNull();
    boundCmd.Length.ShouldBe(3);
    boundCmd[0].ShouldBe("nginx");
    boundCmd[1].ShouldBe("--port");
    boundCmd[2].ShouldBe("80");

    await Task.CompletedTask;
  }

  public static async Task Should_match_git_commit_with_aliases()
  {
    // Arrange
    NuruAppBuilder builder = new();
    string? boundMsg = null;
    bool boundAmend = false;
    bool boundNoVerify = false;
    builder.AddRoute("git commit --message,-m {msg} --amend --no-verify", (string msg, bool amend, bool noVerify) => { boundMsg = msg; boundAmend = amend; boundNoVerify = noVerify; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["git", "commit", "-m", "fix bug", "--amend"]);

    // Assert
    exitCode.ShouldBe(0);
    boundMsg.ShouldBe("fix bug");
    boundAmend.ShouldBeTrue();
    boundNoVerify.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_match_progressive_enhancement_build_verbose()
  {
    // Arrange
    NuruAppBuilder builder = new();
    string? boundProject = null;
    string? boundCfg = null;
    bool boundVerbose = false;
    bool boundWatch = false;
    builder.AddRoute("build {project?} --config {cfg?} --verbose --watch", (string? project, string? cfg, bool verbose, bool watch) => { boundProject = project; boundCfg = cfg; boundVerbose = verbose; boundWatch = watch; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--verbose"]);

    // Assert
    exitCode.ShouldBe(0);
    boundProject.ShouldBeNull();
    boundCfg.ShouldBeNull();
    boundVerbose.ShouldBeTrue();
    boundWatch.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_match_multi_valued_with_types_process_id_1_2_tag_A_run_sh()
  {
    // Arrange
    NuruAppBuilder builder = new();
    int[]? boundId = null;
    string[]? boundT = null;
    string? boundScript = null;
    builder.AddRoute("process --id {id:int}* --tag {t}* {script}", (int[] id, string[] t, string script) => { boundId = id; boundT = t; boundScript = script; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["process", "--id", "1", "--id", "2", "--tag", "A", "run.sh"]);

    // Assert
    exitCode.ShouldBe(0);
    boundId.ShouldNotBeNull();
    boundId.Length.ShouldBe(2);
    boundId[0].ShouldBe(1);
    boundId[1].ShouldBe(2);
    boundT.ShouldNotBeNull();
    boundT.Length.ShouldBe(1);
    boundT[0].ShouldBe("A");
    boundScript.ShouldBe("run.sh");

    await Task.CompletedTask;
  }
}