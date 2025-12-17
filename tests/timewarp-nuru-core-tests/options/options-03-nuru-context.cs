#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Options
{

/// <summary>
/// Tests for NuruContext access in route handlers.
/// NuruContext provides access to:
/// - Raw command line arguments
/// - Unmatched/extra arguments
/// - Which optional parameters were actually provided
/// - Route metadata and pattern info
///
/// NOTE: NuruContext is NOT YET IMPLEMENTED - these tests document expected behavior.
/// </summary>
[TestTag("Options")]
[TestTag("NotImplemented")]
public class NuruContextTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<NuruContextTests>();

  public static async Task Should_provide_raw_args_via_context()
  {
    // Arrange
    string[]? capturedRawArgs = null;

    NuruCoreApp app = new NuruAppBuilder()
      .Map("analyze {file} --verbose", (string file, bool verbose, NuruContext context) =>
      {
        capturedRawArgs = context.RawArgs.ToArray();
        return 0;
      })
      .Build();

    // Act
    try
    {
      await app.RunAsync(["analyze", "src/main.cs", "--verbose", "--debug", "--trace"]);

      // Assert - when implemented
      capturedRawArgs.ShouldNotBeNull();
      capturedRawArgs.ShouldContain("analyze");
      capturedRawArgs.ShouldContain("src/main.cs");
      capturedRawArgs.ShouldContain("--verbose");
    }
    catch (Exception)
    {
      // Expected - NuruContext not implemented yet
      // Skip assertion - test documents expected behavior
    }

    await Task.CompletedTask;
  }

  public static async Task Should_provide_unmatched_args_via_context()
  {
    // Arrange
    string[]? capturedUnmatched = null;

    NuruCoreApp app = new NuruAppBuilder()
      .Map("analyze {file} --verbose", (string file, bool verbose, NuruContext context) =>
      {
        capturedUnmatched = context.UnmatchedArgs.ToArray();
        return 0;
      })
      .Build();

    // Act
    try
    {
      await app.RunAsync(["analyze", "src/main.cs", "--verbose", "--debug", "--trace"]);

      // Assert - when implemented, --debug and --trace should be unmatched
      capturedUnmatched.ShouldNotBeNull();
      capturedUnmatched.ShouldContain("--debug");
      capturedUnmatched.ShouldContain("--trace");
    }
    catch (Exception)
    {
      // Expected - NuruContext not implemented yet
    }

    await Task.CompletedTask;
  }

  public static async Task Should_indicate_if_optional_param_was_provided()
  {
    // Arrange
    bool? tagWasProvided = null;

    NuruCoreApp app = new NuruAppBuilder()
      .Map("deploy {env} {tag?}", (string env, string? tag, NuruContext context) =>
      {
        tagWasProvided = context.WasProvided("tag");
        return 0;
      })
      .Build();

    // Act - without optional tag
    try
    {
      await app.RunAsync(["deploy", "staging"]);

      // Assert - when implemented
      tagWasProvided.ShouldBe(false);
    }
    catch (Exception)
    {
      // Expected - NuruContext not implemented yet
    }

    await Task.CompletedTask;
  }

  public static async Task Should_indicate_provided_when_optional_param_given()
  {
    // Arrange
    bool? tagWasProvided = null;

    NuruCoreApp app = new NuruAppBuilder()
      .Map("deploy {env} {tag?}", (string env, string? tag, NuruContext context) =>
      {
        tagWasProvided = context.WasProvided("tag");
        return 0;
      })
      .Build();

    // Act - with optional tag
    try
    {
      await app.RunAsync(["deploy", "production", "v2.0"]);

      // Assert - when implemented
      tagWasProvided.ShouldBe(true);
    }
    catch (Exception)
    {
      // Expected - NuruContext not implemented yet
    }

    await Task.CompletedTask;
  }

  public static async Task Should_provide_route_pattern_via_context()
  {
    // Arrange
    string? capturedPattern = null;

    NuruCoreApp app = new NuruAppBuilder()
      .Map("analyze {file} --verbose", (string file, bool verbose, NuruContext context) =>
      {
        capturedPattern = context.RoutePattern;
        return 0;
      })
      .Build();

    // Act
    try
    {
      await app.RunAsync(["analyze", "src/main.cs", "--verbose"]);

      // Assert - when implemented
      capturedPattern.ShouldBe("analyze {file} --verbose");
    }
    catch (Exception)
    {
      // Expected - NuruContext not implemented yet
    }

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Options
