#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

#pragma warning disable RCS1163 // Unused parameter - expected in negative test cases

// Boolean flags, mixed required and optional options, and typed option values.
// The other option-matching subsets live beside this file.

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class BooleanMixedTypedOptionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<BooleanMixedTypedOptionTests>();

  public static async Task Should_match_boolean_flag_build_verbose_true()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --verbose")
      .WithHandler((bool verbose) => $"verbose:{verbose}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--verbose"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("verbose:True").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_boolean_flag_build_false()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --verbose")
      .WithHandler((bool verbose) => $"verbose:{verbose}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("verbose:False").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_mixed_required_optional_deploy_env_prod_tag_v1_0_verbose()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env {e} --tag {t?} --verbose")
      .WithHandler((string e, string? t, bool verbose) => $"e:{e}|t:{t ?? "NULL"}|verbose:{verbose}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "prod", "--tag", "v1.0", "--verbose"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("e:prod").ShouldBeTrue();
    terminal.OutputContains("t:v1.0").ShouldBeTrue();
    terminal.OutputContains("verbose:True").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_mixed_required_optional_deploy_env_prod_defaults()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env {e} --tag? {t?} --verbose")
      .WithHandler((string e, string? t, bool verbose) => $"e:{e}|t:{t ?? "NULL"}|verbose:{verbose}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "prod"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("e:prod").ShouldBeTrue();
    terminal.OutputContains("t:NULL").ShouldBeTrue();
    terminal.OutputContains("verbose:False").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_mixed_missing_required_deploy_tag_v1_0()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env {e} --tag? {t?} --verbose").WithHandler((string e, string? t, bool verbose) => 0).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--tag", "v1.0"]);

    // Assert
    exitCode.ShouldBe(1); // Missing required --env

    await Task.CompletedTask;
  }

  public static async Task Should_match_typed_option_server_port_8080()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("server --port {num:int}")
      .WithHandler((int num) => $"num:{num}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["server", "--port", "8080"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("num:8080").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_typed_option_server_port_abc()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("server --port {num:int}")
      .WithHandler((int num) => 0)
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["server", "--port", "abc"]);

    // Assert
    exitCode.ShouldBe(1); // Type conversion failure

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
