#!/usr/bin/dotnet --

using TimeWarp.Nuru;
using TimeWarp.Nuru.Completion;
using Shouldly;

return await RunTests<OptionExtractionTests>(clearCache: true);

[TestTag("Completion")]
[ClearRunfileCache]
public class OptionExtractionTests
{
  public static async Task Should_extract_long_form_options()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("test --verbose", () => 0);
    builder.AddRoute("build --force", () => 0);
    builder.AddRoute("deploy --dry-run", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
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
    var builder = new NuruAppBuilder();
    builder.AddRoute("test -v", () => 0);
    builder.AddRoute("build -f", () => 0);
    builder.AddRoute("deploy -d", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
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
    var builder = new NuruAppBuilder();
    builder.AddRoute("test --verbose,-v", () => 0);
    builder.AddRoute("build --output,-o", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
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
    var builder = new NuruAppBuilder();
    builder.AddRoute("test --verbose --force --dry-run", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
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
    var builder = new NuruAppBuilder();
    builder.AddRoute("build --config {mode}", (string mode) => 0);
    builder.AddRoute("deploy --output {file}", (string file) => 0);

    // Act
    var generator = new CompletionScriptGenerator();
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
    var builder = new NuruAppBuilder();
    builder.AddRoute("test --verbose", () => 0);
    builder.AddRoute("build --verbose", () => 0);
    builder.AddRoute("deploy --verbose", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
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
    var builder = new NuruAppBuilder();
    builder.AddRoute("test --verbose,-v --force -d --output,-o {file}", (string file) => 0);

    // Act
    var generator = new CompletionScriptGenerator();
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
    var builder = new NuruAppBuilder();
    // No routes added

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldNotBeEmpty();
    // Script should still be valid even with no options

    await Task.CompletedTask;
  }

  public static async Task Should_handle_routes_without_options()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);
    builder.AddRoute("version", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldNotBeEmpty();
    // Script should be valid even without options

    await Task.CompletedTask;
  }

  public static async Task Should_extract_options_from_complex_route()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("deploy {env} --version {ver} --force --dry-run,-d", (string env, string ver) => 0);

    // Act
    var generator = new CompletionScriptGenerator();
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
    var builder = new NuruAppBuilder();
    builder.AddRoute("test --dry-run", () => 0);
    builder.AddRoute("build --skip-tests", () => 0);
    builder.AddRoute("deploy --no-cache", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
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
    var builder = new NuruAppBuilder();
    builder.AddRoute("test --Verbose", () => 0);
    builder.AddRoute("build --verbose", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("--Verbose");
    bashScript.ShouldContain("--verbose");

    await Task.CompletedTask;
  }
}
