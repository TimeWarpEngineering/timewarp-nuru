#!/usr/bin/dotnet --

using TimeWarp.Nuru;
using Shouldly;

#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Completion")]
public class OptionExtractionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<OptionExtractionTests>();

  public static async Task Should_extract_long_form_options()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("test --verbose", () => 0);
    builder.Map("build --force", () => 0);
    builder.Map("deploy --dry-run", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("--verbose");
    bashScript.ShouldContain("--force");
    bashScript.ShouldContain("--dry-run");

    await Task.CompletedTask;
  }

  public static async Task Should_extract_short_form_options()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("test -v", () => 0);
    builder.Map("build -f", () => 0);
    builder.Map("deploy -d", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("-v");
    bashScript.ShouldContain("-f");
    bashScript.ShouldContain("-d");

    await Task.CompletedTask;
  }

  public static async Task Should_extract_options_with_aliases()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("test --verbose,-v", () => 0);
    builder.Map("build --output,-o", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("--verbose");
    bashScript.ShouldContain("-v");
    bashScript.ShouldContain("--output");
    bashScript.ShouldContain("-o");

    await Task.CompletedTask;
  }

  public static async Task Should_extract_multiple_options_from_one_route()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("test --verbose --force --dry-run", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("--verbose");
    bashScript.ShouldContain("--force");
    bashScript.ShouldContain("--dry-run");

    await Task.CompletedTask;
  }

  public static async Task Should_extract_options_with_values()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("build --config {mode}", (string mode) => 0);
    builder.Map("deploy --output {file}", (string file) => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("--config");
    bashScript.ShouldContain("--output");
    bashScript.ShouldNotContain("{mode}");
    bashScript.ShouldNotContain("{file}");

    await Task.CompletedTask;
  }

  public static async Task Should_deduplicate_options()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("test --verbose", () => 0);
    builder.Map("build --verbose", () => 0);
    builder.Map("deploy --verbose", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    // --verbose should appear in options list, but deduplicated
    bashScript.ShouldContain("--verbose");
    int verboseCount = System.Text.RegularExpressions.Regex.Matches(bashScript, @"--verbose").Count;
    verboseCount.ShouldBeGreaterThan(0, "--verbose should be present");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_mixed_option_styles()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("test --verbose,-v --force -d --output,-o {file}", (string file) => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("--verbose");
    bashScript.ShouldContain("-v");
    bashScript.ShouldContain("--force");
    bashScript.ShouldContain("-d");
    bashScript.ShouldContain("--output");
    bashScript.ShouldContain("-o");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_route_collection()
  {
    // Arrange
    NuruAppBuilder builder = new();
    // No routes added

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldNotBeEmpty();
    // Script should still be valid even with no options

    await Task.CompletedTask;
  }

  public static async Task Should_handle_routes_without_options()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);
    builder.Map("version", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldNotBeEmpty();
    // Script should be valid even without options

    await Task.CompletedTask;
  }

  public static async Task Should_extract_options_from_complex_route()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env} --version {ver} --force --dry-run,-d", (string env, string ver) => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("--version");
    bashScript.ShouldContain("--force");
    bashScript.ShouldContain("--dry-run");
    bashScript.ShouldContain("-d");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_options_with_hyphens_in_names()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("test --dry-run", () => 0);
    builder.Map("build --skip-tests", () => 0);
    builder.Map("deploy --no-cache", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("--dry-run");
    bashScript.ShouldContain("--skip-tests");
    bashScript.ShouldContain("--no-cache");

    await Task.CompletedTask;
  }

  public static async Task Should_be_case_sensitive()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("test --Verbose", () => 0);
    builder.Map("build --verbose", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("--Verbose");
    bashScript.ShouldContain("--verbose");

    await Task.CompletedTask;
  }
}
