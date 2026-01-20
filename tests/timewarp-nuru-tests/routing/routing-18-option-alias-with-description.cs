#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

/// <summary>
/// Tests for the bug reported in task 013:
/// When an option has both a short alias and a description, the long form fails to match.
/// Example: "hello {name} --upper,-u|Convert to uppercase"
/// - "hello World -u" works
/// - "hello World --upper" fails
/// </summary>
[TestTag("Routing")]
public class OptionAliasWithDescriptionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<OptionAliasWithDescriptionTests>();

  public static async Task Should_match_option_alias_with_description_short_form()
  {
    // Arrange - Pattern with alias AND description
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hello {name} --upper,-u|Convert to uppercase").WithHandler((string name, bool upper) => $"name:{name}|upper:{upper}")
      .AsQuery().Done()
      .Build();

    // Act - Use SHORT form (-u)
    int exitCode = await app.RunAsync(["hello", "World", "-u"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("name:World|upper:True").ShouldBeTrue();
  }

  public static async Task Should_match_option_alias_with_description_long_form()
  {
    // Arrange - Pattern with alias AND description
    // THIS IS THE BUG FROM TASK 013: long form should work but was failing
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hello {name} --upper,-u|Convert to uppercase").WithHandler((string name, bool upper) => $"name:{name}|upper:{upper}")
      .AsQuery().Done()
      .Build();

    // Act - Use LONG form (--upper)
    int exitCode = await app.RunAsync(["hello", "World", "--upper"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("name:World|upper:True").ShouldBeTrue();
  }

  public static async Task Should_match_option_alias_with_description_omitted()
  {
    // Arrange - Pattern with alias AND description
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hello {name} --upper,-u|Convert to uppercase").WithHandler((string name, bool upper) => $"name:{name}|upper:{upper}")
      .AsQuery().Done()
      .Build();

    // Act - Omit the option
    int exitCode = await app.RunAsync(["hello", "World"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("name:World|upper:False").ShouldBeTrue();
  }

  public static async Task Should_match_complex_pattern_with_description_deploy_dry_run_short()
  {
    // Arrange - More complex pattern from MCP tests
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy {env} --dry-run,-d|Preview mode").WithHandler((string env, bool dryRun) => $"env:{env}|dryRun:{dryRun}")
      .AsCommand().Done()
      .Build();

    // Act - Use short form
    int exitCode = await app.RunAsync(["deploy", "prod", "-d"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("env:prod|dryRun:True").ShouldBeTrue();
  }

  public static async Task Should_match_complex_pattern_with_description_deploy_dry_run_long()
  {
    // Arrange - More complex pattern from MCP tests
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy {env} --dry-run,-d|Preview mode").WithHandler((string env, bool dryRun) => $"env:{env}|dryRun:{dryRun}")
      .AsCommand().Done()
      .Build();

    // Act - Use LONG form (--dry-run) - this was the reported bug
    int exitCode = await app.RunAsync(["deploy", "prod", "--dry-run"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("env:prod|dryRun:True").ShouldBeTrue();
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
