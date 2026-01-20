#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

  [TestTag("Routing")]
  public class OptionalParametersTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<OptionalParametersTests>();

    public static async Task Should_match_required_string_deploy_prod()
    {
      // Arrange
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("deploy {env}").WithHandler((string env) => $"env:{env}").AsCommand().Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["deploy", "prod"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("env:prod").ShouldBeTrue();

      await Task.CompletedTask;
    }

    public static async Task Should_not_match_missing_required_string_deploy()
    {
      // Arrange
      using TestTerminal terminal = new();
#pragma warning disable RCS1163 // Unused parameter
      NuruApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("deploy {env}").WithHandler((string env) => { }).AsCommand().Done()
        .Build();
#pragma warning restore RCS1163 // Unused parameter

      // Act
      int exitCode = await app.RunAsync(["deploy"]);

      // Assert
      exitCode.ShouldBe(1); // Missing required parameter

      await Task.CompletedTask;
    }

    public static async Task Should_match_optional_string_deploy_prod()
    {
      // Arrange
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("deploy {env?}").WithHandler((string? env) => $"env:{env}").AsCommand().Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["deploy", "prod"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("env:prod").ShouldBeTrue();

      await Task.CompletedTask;
    }

    public static async Task Should_match_optional_string_deploy_null()
    {
      // Arrange
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("deploy {env?}").WithHandler((string? env) => $"env:{env ?? "NULL"}").AsCommand().Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["deploy"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("env:NULL").ShouldBeTrue();

      await Task.CompletedTask;
    }

    public static async Task Should_match_optional_integer_list_10()
    {
      // Arrange
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("list {count:int?}").WithHandler((int? count) => $"count:{count}").AsQuery().Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["list", "10"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("count:10").ShouldBeTrue();

      await Task.CompletedTask;
    }

    public static async Task Should_match_optional_integer_list_null()
    {
      // Arrange
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("list {count:int?}").WithHandler((int? count) => $"count:{count?.ToString(CultureInfo.InvariantCulture) ?? "NULL"}").AsQuery().Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["list"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("count:NULL").ShouldBeTrue();

      await Task.CompletedTask;
    }

    public static async Task Should_match_mixed_required_optional_deploy_prod_v1_0()
    {
      // Arrange
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("deploy {env} {tag?}").WithHandler((string env, string? tag) => $"env:{env},tag:{tag}").AsCommand().Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["deploy", "prod", "v1.0"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("env:prod,tag:v1.0").ShouldBeTrue();

      await Task.CompletedTask;
    }

    public static async Task Should_match_mixed_required_optional_deploy_prod_null()
    {
      // Arrange
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("deploy {env} {tag?}").WithHandler((string env, string? tag) => $"env:{env},tag:{tag ?? "NULL"}").AsCommand().Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["deploy", "prod"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("env:prod,tag:NULL").ShouldBeTrue();

      await Task.CompletedTask;
    }

    public static async Task Should_not_match_mixed_missing_required_deploy()
    {
      // Arrange
      using TestTerminal terminal = new();
#pragma warning disable RCS1163 // Unused parameter
      NuruApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("deploy {env} {tag?}").WithHandler((string env, string? tag) => { }).AsCommand().Done()
        .Build();
#pragma warning restore RCS1163 // Unused parameter

      // Act
      int exitCode = await app.RunAsync(["deploy"]);

      // Assert
      exitCode.ShouldBe(1); // Missing required env

      await Task.CompletedTask;
    }
  }

} // namespace TimeWarp.Nuru.Tests.Routing
