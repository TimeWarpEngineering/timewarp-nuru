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
    bool upperUsed = false;
    string? capturedName = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("hello {name} --upper,-u|Convert to uppercase", (string name, bool upper) =>
      {
        capturedName = name;
        upperUsed = upper;
        return 0;
      })
      .Build();

    // Act - Use SHORT form (-u)
    int exitCode = await app.RunAsync(["hello", "World", "-u"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedName.ShouldBe("World");
    upperUsed.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_option_alias_with_description_long_form()
  {
    // Arrange - Pattern with alias AND description
    // THIS IS THE BUG FROM TASK 013: long form should work but was failing
    bool upperUsed = false;
    string? capturedName = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("hello {name} --upper,-u|Convert to uppercase", (string name, bool upper) =>
      {
        capturedName = name;
        upperUsed = upper;
        return 0;
      })
      .Build();

    // Act - Use LONG form (--upper)
    int exitCode = await app.RunAsync(["hello", "World", "--upper"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedName.ShouldBe("World");
    upperUsed.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_option_alias_with_description_omitted()
  {
    // Arrange - Pattern with alias AND description
    bool upperUsed = true;
    string? capturedName = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("hello {name} --upper,-u|Convert to uppercase", (string name, bool upper) =>
      {
        capturedName = name;
        upperUsed = upper;
        return 0;
      })
      .Build();

    // Act - Omit the option
    int exitCode = await app.RunAsync(["hello", "World"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedName.ShouldBe("World");
    upperUsed.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_match_complex_pattern_with_description_deploy_dry_run_short()
  {
    // Arrange - More complex pattern from MCP tests
    // Note: Parameter name must match the kebab-case option name converted to camelCase
    bool dryRun = false;
    string? capturedEnv = null;
#pragma warning disable RCS1163, IDE0060
    NuruCoreApp app = new NuruAppBuilder()
      .Map("deploy {env} --dry-run,-d|Preview mode", (string env, bool dryrun) =>
      {
        capturedEnv = env;
        dryRun = dryrun;
        return 0;
      })
      .Build();
#pragma warning restore RCS1163, IDE0060

    // Act - Use short form
    int exitCode = await app.RunAsync(["deploy", "prod", "-d"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedEnv.ShouldBe("prod");
    dryRun.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_complex_pattern_with_description_deploy_dry_run_long()
  {
    // Arrange - More complex pattern from MCP tests
    bool dryRun = false;
    string? capturedEnv = null;
#pragma warning disable RCS1163, IDE0060
    NuruCoreApp app = new NuruAppBuilder()
      .Map("deploy {env} --dry-run,-d|Preview mode", (string env, bool dryrun) =>
      {
        capturedEnv = env;
        dryRun = dryrun;
        return 0;
      })
      .Build();
#pragma warning restore RCS1163, IDE0060

    // Act - Use LONG form (--dry-run) - this was the reported bug
    int exitCode = await app.RunAsync(["deploy", "prod", "--dry-run"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedEnv.ShouldBe("prod");
    dryRun.ShouldBeTrue();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
