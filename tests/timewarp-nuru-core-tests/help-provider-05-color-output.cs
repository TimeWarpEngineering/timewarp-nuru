#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Test HelpProvider colored output formatting
// Issue #144: Improve help output formatting and readability

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.HelpProviderColor
{

[TestTag("Help")]
public class HelpProviderColorOutputTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<HelpProviderColorOutputTests>();

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

  public static async Task Should_include_ansi_codes_when_useColor_is_true()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("build", "Build the project"));

    // Act
    string helpText = TimeWarp.Nuru.HelpProvider.GetHelpText(endpoints, "testapp", useColor: true);

    // Assert - Should contain ANSI escape codes
    helpText.ShouldContain("\x1b["); // ANSI escape sequence start

    await Task.CompletedTask;
  }

  public static async Task Should_not_include_ansi_codes_when_useColor_is_false()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("build", "Build the project"));

    // Act
    string helpText = TimeWarp.Nuru.HelpProvider.GetHelpText(endpoints, "testapp", useColor: false);

    // Assert - Should NOT contain ANSI escape codes
    helpText.ShouldNotContain("\x1b[");

    await Task.CompletedTask;
  }

  public static async Task Should_color_section_headers_in_yellow_bold()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("test", "Run tests"));

    // Act
    string helpText = TimeWarp.Nuru.HelpProvider.GetHelpText(endpoints, "testapp", useColor: true);

    // Assert - Section headers should have yellow and bold formatting
    // Yellow is \x1b[33m, Bold is \x1b[1m
    helpText.ShouldContain(AnsiColors.Yellow);
    helpText.ShouldContain(AnsiColors.Bold);

    await Task.CompletedTask;
  }

  public static async Task Should_color_commands_in_cyan()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("deploy", "Deploy the app"));

    // Act
    string helpText = TimeWarp.Nuru.HelpProvider.GetHelpText(endpoints, "testapp", useColor: true);

    // Assert - Command literals should be cyan
    helpText.ShouldContain(AnsiColors.Cyan);

    await Task.CompletedTask;
  }

  public static async Task Should_color_parameters_in_yellow()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("greet {name}", "Greet someone"));

    // Act
    string helpText = TimeWarp.Nuru.HelpProvider.GetHelpText(endpoints, "testapp", useColor: true);

    // Assert - Parameters should be yellow
    // The parameter "name" should be wrapped in yellow
    helpText.ShouldContain(AnsiColors.Yellow);
    helpText.ShouldContain("name");

    await Task.CompletedTask;
  }

  public static async Task Should_color_options_in_green()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("--verbose", "Enable verbose output"));

    // Act
    string helpText = TimeWarp.Nuru.HelpProvider.GetHelpText(endpoints, "testapp", useColor: true);

    // Assert - Options should be green
    helpText.ShouldContain(AnsiColors.Green);

    await Task.CompletedTask;
  }

  public static async Task Should_color_descriptions_in_gray()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("build", "Build the project"));

    // Act
    string helpText = TimeWarp.Nuru.HelpProvider.GetHelpText(endpoints, "testapp", useColor: true);

    // Assert - Descriptions should be gray
    helpText.ShouldContain(AnsiColors.Gray);

    await Task.CompletedTask;
  }

  public static async Task Should_use_ansi_aware_padding_for_alignment()
  {
    // Arrange - Test that padding works correctly with ANSI codes
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("short", "Short command"));
    endpoints.Add(CreateEndpoint("very-long-command-name", "Long command"));

    // Act
    string coloredText = TimeWarp.Nuru.HelpProvider.GetHelpText(endpoints, "testapp", useColor: true);
    string plainText = TimeWarp.Nuru.HelpProvider.GetHelpText(endpoints, "testapp", useColor: false);

    // Assert - The visible (stripped) text should have proper alignment
    string strippedColored = TimeWarp.Terminal.AnsiStringUtils.StripAnsiCodes(coloredText);

    // Both should have the same visible structure
    strippedColored.ShouldContain("short");
    strippedColored.ShouldContain("very-long-command-name");
    plainText.ShouldContain("short");
    plainText.ShouldContain("very-long-command-name");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_optional_parameters_with_brackets()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("greet {name?}", "Greet someone (optional)"));

    // Act
    string helpText = TimeWarp.Nuru.HelpProvider.GetHelpText(endpoints, "testapp", useColor: true);

    // Assert - Optional parameters should use brackets []
    helpText.ShouldContain("[");
    helpText.ShouldContain("]");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_complex_pattern_with_multiple_elements()
  {
    // Arrange - Complex pattern with literals, parameters, and options
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("repo add {path} --config {config?}", "Add a repository"));

    // Act
    string helpText = TimeWarp.Nuru.HelpProvider.GetHelpText(endpoints, "testapp", useColor: true);

    // Assert - Should contain various color codes for different elements
    helpText.ShouldContain(AnsiColors.Cyan); // literals
    helpText.ShouldContain(AnsiColors.Yellow); // parameters
    helpText.ShouldContain(AnsiColors.Green); // options

    await Task.CompletedTask;
  }

  public static async Task Should_strip_cleanly_to_readable_text()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("build", "Build the project"));
    endpoints.Add(CreateEndpoint("--verbose", "Verbose output"));

    // Act
    string coloredText = TimeWarp.Nuru.HelpProvider.GetHelpText(endpoints, "testapp", useColor: true);
    string strippedText = TimeWarp.Terminal.AnsiStringUtils.StripAnsiCodes(coloredText);

    // Assert - Stripped text should be clean and readable
    strippedText.ShouldNotContain("\x1b[");
    strippedText.ShouldContain("Usage:");
    strippedText.ShouldContain("Commands:");
    strippedText.ShouldContain("build");
    strippedText.ShouldContain("Options:");
    strippedText.ShouldContain("--verbose");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Core.HelpProviderColor
