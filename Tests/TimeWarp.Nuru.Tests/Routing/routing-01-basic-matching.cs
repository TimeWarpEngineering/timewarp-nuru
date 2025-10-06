#!/usr/bin/dotnet --

return await RunTests<BasicMatchingTests>(clearCache: true);

[TestTag("Routing")]
public class BasicMatchingTests
{
  public static async Task Should_match_exact_literal_status_delegate()
  {
    // Arrange
    NuruAppBuilder builder = new();
    bool matched = false;
    builder.AddRoute("status", () => { matched = true; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["status"]);

    // Assert
    exitCode.ShouldBe(0);
    matched.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_different_literal_version_delegate()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.AddRoute("status", () => 0);

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["version"]);

    // Assert
    exitCode.ShouldBe(1); // No match returns 1

    await Task.CompletedTask;
  }

  public static async Task Should_match_multi_literal_git_status_delegate()
  {
    // Arrange
    NuruAppBuilder builder = new();
    bool matched = false;
    builder.AddRoute("git status", () => { matched = true; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["git", "status"]);

    // Assert
    exitCode.ShouldBe(0);
    matched.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_different_multi_literal_git_commit_delegate()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.AddRoute("git status", () => 0);

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["git", "commit"]);

    // Assert
    exitCode.ShouldBe(1);

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_case_insensitive_status_delegate()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.AddRoute("status", () => 0);

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["STATUS"]);

    // Assert
    exitCode.ShouldBe(1);

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_too_few_arguments_git_status_delegate()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.AddRoute("git status", () => 0);

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["git"]);

    // Assert
    exitCode.ShouldBe(1);

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_extra_arguments_git_status_verbose_delegate()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.AddRoute("git status", () => 0);

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["git", "status", "--verbose"]);

    // Assert
    exitCode.ShouldBe(1);

    await Task.CompletedTask;
  }

  public static async Task Should_match_empty_pattern_with_empty_input_delegate()
  {
    // Arrange
    NuruAppBuilder builder = new();
    bool matched = false;
    builder.AddDefaultRoute(() => { matched = true; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync([]);

    // Assert
    exitCode.ShouldBe(0);
    matched.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_empty_pattern_with_anything_delegate()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.AddDefaultRoute(() => 0);

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["anything"]);

    // Assert
    exitCode.ShouldBe(1);

    await Task.CompletedTask;
  }

  // Mediator consistency will be verified in Section 11
}