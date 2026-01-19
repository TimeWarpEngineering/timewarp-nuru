#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class BasicMatchingTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<BasicMatchingTests>();

  public static async Task Should_match_exact_literal_status_delegate()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => "healthy").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["status"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("healthy").ShouldBeTrue();
  }

  public static async Task Should_not_match_different_literal_version_delegate()
  {
    // Arrange
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .Map("status").WithHandler(() => { }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["version"]);

    // Assert
    exitCode.ShouldBe(1); // No match returns 1

    await Task.CompletedTask;
  }

  public static async Task Should_match_multi_literal_git_status_delegate()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("git status").WithHandler(() => "healthy").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["git", "status"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("healthy").ShouldBeTrue();
  }

  public static async Task Should_not_match_different_multi_literal_git_commit_delegate()
  {
    // Arrange
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .Map("git status").WithHandler(() => { }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["git", "commit"]);

    // Assert
    exitCode.ShouldBe(1);

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_case_insensitive_status_delegate()
  {
    // Arrange
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .Map("status").WithHandler(() => { }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["STATUS"]);

    // Assert
    exitCode.ShouldBe(1);

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_too_few_arguments_git_status_delegate()
  {
    // Arrange
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .Map("git status").WithHandler(() => { }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["git"]);

    // Assert
    exitCode.ShouldBe(1);

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_extra_arguments_git_status_verbose_delegate()
  {
    // Arrange
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .Map("git status").WithHandler(() => { }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["git", "status", "--verbose"]);

    // Assert
    exitCode.ShouldBe(1);

    await Task.CompletedTask;
  }

  public static async Task Should_match_empty_pattern_with_empty_input_delegate()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("").WithHandler(() => "healthy").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync([]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("healthy").ShouldBeTrue();
  }

  public static async Task Should_not_match_empty_pattern_with_anything_delegate()
  {
    // Arrange
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .Map("").WithHandler(() => { }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["anything"]);

    // Assert
    exitCode.ShouldBe(1);

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
