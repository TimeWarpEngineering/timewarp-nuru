#!/usr/bin/dotnet --
#pragma warning disable RCS1163 // Unused parameter - parameters must match route pattern names for binding

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class RouteSelectionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<RouteSelectionTests>();

  public static async Task Should_select_literal_over_parameter_git_status()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("git status").WithHandler(() => "literal:git-status").AsQuery().Done()
      .Map("git {command}").WithHandler((string command) => $"param:{command}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["git", "status"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("literal:git-status").ShouldBeTrue();
    terminal.OutputContains("param:").ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_select_typed_parameter_delay_500()
  {
    // Arrange
    // NOTE: Previously this test had two overlapping routes:
    //   .Map("delay {ms:int}") and .Map("delay {duration}")
    // These would now trigger NURU_R001 (overlapping type constraints).
    // Real CLIs should use explicit subcommands instead.
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("delay {ms:int}").WithHandler((int ms) => $"typed:{ms}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["delay", "500"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("typed:500").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_use_explicit_subcommands_for_different_types()
  {
    // Arrange
    // This is the recommended pattern instead of type-based disambiguation:
    // Use explicit subcommands like "delay-ms" vs "delay-duration"
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("delay-ms {ms:int}").WithHandler((int ms) => $"typed:{ms}").AsQuery().Done()
      .Map("delay-duration {duration}").WithHandler((string duration) => $"untyped:{duration}").AsQuery().Done()
      .Build();

    // Act - Test typed route
    int exitCode1 = await app.RunAsync(["delay-ms", "500"]);
    exitCode1.ShouldBe(0);
    terminal.OutputContains("typed:500").ShouldBeTrue();

    // Act - Test untyped route
    terminal.ClearOutput();
    int exitCode2 = await app.RunAsync(["delay-duration", "5s"]);
    exitCode2.ShouldBe(0);
    terminal.OutputContains("untyped:5s").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_select_required_over_optional_deploy_prod()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy {env}").WithHandler((string env) => $"required:{env}").AsQuery().Done()
      .Map("deploy {env?}").WithHandler((string? env) => $"optional:{env}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "prod"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("required:prod").ShouldBeTrue();
    terminal.OutputContains("optional:").ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_select_no_option_over_required_option_build()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("build").WithHandler(() => "no-option:build").AsQuery().Done()
      .Map("build --config {m}").WithHandler((string m) => $"required-option:config={m}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("no-option:build").ShouldBeTrue();
    terminal.OutputContains("required-option:").ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_select_catch_all_fallback_git_push()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("git status").WithHandler(() => "literal:git-status").AsQuery().Done()
      .Map("git commit").WithHandler(() => "literal:git-commit").AsQuery().Done()
      .Map("git {*args}").WithHandler((string[] args) => $"catchall:{string.Join(",", args)}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["git", "push"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("literal:").ShouldBeFalse();
    terminal.OutputContains("catchall:push").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_select_first_registered_on_equal_specificity_greet_Alice()
  {
    // Arrange - Equal specificity but different literals, so only one matches
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("greet {name}").WithHandler((string name) => $"greet:{name}").AsQuery().Done()
      .Map("hello {person}").WithHandler((string person) => $"hello:{person}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["greet", "Alice"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("greet:Alice").ShouldBeTrue();
    terminal.OutputContains("hello:").ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_select_progressive_specificity_deploy_prod_tag_v1_0()
  {
    // Arrange
    // Tests progressive specificity: more specific routes (with --tag) win over simpler ones
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy {env} --tag {t}").WithHandler((string env, string t) => $"with-tag:{env},{t}").AsQuery().Done()
      .Map("deploy {env}").WithHandler((string env) => $"env-only:{env}").AsQuery().Done()
      .Map("deploy").WithHandler(() => "bare-deploy").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "prod", "--tag", "v1.0"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("with-tag:prod,v1.0").ShouldBeTrue();
    terminal.OutputContains("env-only:").ShouldBeFalse();
    terminal.OutputContains("bare-deploy").ShouldBeFalse();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
