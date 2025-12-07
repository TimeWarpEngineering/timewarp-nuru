#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Test HelpProvider correctly classifies single-dash and double-dash options
return await RunTests<HelpProviderOptionDetectionTests>(clearCache: true);

[TestTag("Help")]
[ClearRunfileCache]
public class HelpProviderOptionDetectionTests
{
  private static Endpoint CreateEndpoint(string pattern, string? description = null)
  {
    return new Endpoint
    {
      RoutePattern = pattern,
      CompiledRoute = PatternParser.Parse(pattern),
      Handler = () => 0,
      Description = description
    };
  }

  public static async Task Should_classify_single_dash_option_as_option()
  {
    // Arrange - Issue #118: single-dash options should appear in Options section
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("-i", "Interactive mode"));

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp");

    // Assert - "-i" should be in Options section, not Commands section
    helpText.ShouldContain("Options:");
    helpText.ShouldContain("-i");

    // Verify it's NOT in the Commands section by checking the structure
    int optionsIndex = helpText.IndexOf("Options:", StringComparison.Ordinal);
    int dashIIndex = helpText.IndexOf("-i", StringComparison.Ordinal);

    // -i should appear after "Options:" header
    dashIIndex.ShouldBeGreaterThan(optionsIndex);

    await Task.CompletedTask;
  }

  public static async Task Should_classify_double_dash_option_as_option()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("--verbose", "Verbose output"));

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp");

    // Assert
    helpText.ShouldContain("Options:");
    helpText.ShouldContain("--verbose");

    int optionsIndex = helpText.IndexOf("Options:", StringComparison.Ordinal);
    int verboseIndex = helpText.IndexOf("--verbose", StringComparison.Ordinal);
    verboseIndex.ShouldBeGreaterThan(optionsIndex);

    await Task.CompletedTask;
  }

  public static async Task Should_classify_command_without_dash_as_command()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("build", "Build the project"));

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp");

    // Assert
    helpText.ShouldContain("Commands:");
    helpText.ShouldContain("build");

    int commandsIndex = helpText.IndexOf("Commands:", StringComparison.Ordinal);
    int buildIndex = helpText.IndexOf("build", StringComparison.Ordinal);
    buildIndex.ShouldBeGreaterThan(commandsIndex);

    await Task.CompletedTask;
  }

  public static async Task Should_separate_commands_and_options_correctly()
  {
    // Arrange - Mix of commands and both types of options
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("build", "Build the project"));
    endpoints.Add(CreateEndpoint("run", "Run the app"));
    endpoints.Add(CreateEndpoint("-q", "Quiet mode"));
    endpoints.Add(CreateEndpoint("--verbose", "Verbose output"));

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "myapp");

    // Assert - both sections should exist
    helpText.ShouldContain("Commands:");
    helpText.ShouldContain("Options:");

    // Commands section should come before Options section
    int commandsIndex = helpText.IndexOf("Commands:", StringComparison.Ordinal);
    int optionsIndex = helpText.IndexOf("Options:", StringComparison.Ordinal);
    commandsIndex.ShouldBeLessThan(optionsIndex);

    // "build" and "run" should be between Commands and Options
    // Use "\n  " prefix to match the formatted output, not Usage line
    int buildIndex = helpText.IndexOf("\n  build", StringComparison.Ordinal);
    int runIndex = helpText.IndexOf("\n  run", StringComparison.Ordinal);
    buildIndex.ShouldBeGreaterThan(commandsIndex);
    buildIndex.ShouldBeLessThan(optionsIndex);
    runIndex.ShouldBeGreaterThan(commandsIndex);
    runIndex.ShouldBeLessThan(optionsIndex);

    // "-q" and "--verbose" should be after Options header
    int dashQIndex = helpText.IndexOf("\n  -q", StringComparison.Ordinal);
    int verboseIndex = helpText.IndexOf("--verbose", StringComparison.Ordinal);
    dashQIndex.ShouldBeGreaterThan(optionsIndex);
    verboseIndex.ShouldBeGreaterThan(optionsIndex);

    await Task.CompletedTask;
  }

  public static async Task Should_not_show_commands_section_when_only_options_exist()
  {
    // Arrange - only options, no commands
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("-h", "Show help"));
    endpoints.Add(CreateEndpoint("--version", "Show version"));

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp");

    // Assert
    helpText.ShouldNotContain("Commands:");
    helpText.ShouldContain("Options:");

    await Task.CompletedTask;
  }

  public static async Task Should_not_show_options_section_when_only_commands_exist()
  {
    // Arrange - only commands, no options
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("build", "Build the project"));
    endpoints.Add(CreateEndpoint("test", "Run tests"));

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp");

    // Assert
    helpText.ShouldContain("Commands:");
    helpText.ShouldNotContain("Options:");

    await Task.CompletedTask;
  }
}
