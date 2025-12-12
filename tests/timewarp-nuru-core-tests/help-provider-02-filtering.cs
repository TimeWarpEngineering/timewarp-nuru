#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Test HelpProvider filtering based on HelpOptions and HelpContext
return await RunTests<HelpProviderFilteringTests>(clearCache: true);

[TestTag("Help")]
[ClearRunfileCache]
public class HelpProviderFilteringTests
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

  public static async Task Should_hide_per_command_help_routes_by_default()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("blog", "Open blog"));
    endpoints.Add(CreateEndpoint("blog --help?", "Show help for blog")); // Per-command help route

    HelpOptions options = new();

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", null, options, HelpContext.Cli, useColor: false);

    // Assert
    helpText.ShouldContain("blog");
    helpText.ShouldNotContain("blog --help");

    await Task.CompletedTask;
  }

  public static async Task Should_show_per_command_help_routes_when_enabled()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("blog", "Open blog"));
    endpoints.Add(CreateEndpoint("blog --help?", "Show help for blog"));

    HelpOptions options = new() { ShowPerCommandHelpRoutes = true };

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", null, options, HelpContext.Cli, useColor: false);

    // Assert
    helpText.ShouldContain("blog");
    // Optional --help? flag is formatted as [--help] in plain text
    helpText.ShouldContain("blog [--help]");

    await Task.CompletedTask;
  }

  public static async Task Should_hide_repl_commands_in_cli_mode_by_default()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("mycommand", "User command"));
    endpoints.Add(CreateEndpoint("exit", "Exit the REPL"));
    endpoints.Add(CreateEndpoint("quit", "Exit the REPL"));
    endpoints.Add(CreateEndpoint("q", "Exit the REPL"));
    endpoints.Add(CreateEndpoint("clear", "Clear the screen"));
    endpoints.Add(CreateEndpoint("history", "Show history"));

    HelpOptions options = new();

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", null, options, HelpContext.Cli, useColor: false);

    // Assert
    helpText.ShouldContain("mycommand");
    helpText.ShouldNotContain("exit");
    helpText.ShouldNotContain("quit");
    helpText.ShouldNotContain("\n  q ");
    helpText.ShouldNotContain("clear");
    helpText.ShouldNotContain("history");

    await Task.CompletedTask;
  }

  public static async Task Should_show_repl_commands_in_repl_mode()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("mycommand", "User command"));
    endpoints.Add(CreateEndpoint("exit", "Exit the REPL"));
    endpoints.Add(CreateEndpoint("quit", "Exit the REPL"));
    endpoints.Add(CreateEndpoint("clear", "Clear the screen"));

    HelpOptions options = new();

    // Act - Using REPL context
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", null, options, HelpContext.Repl, useColor: false);

    // Assert - REPL commands should be visible in REPL mode
    helpText.ShouldContain("mycommand");
    helpText.ShouldContain("exit");
    helpText.ShouldContain("quit");
    helpText.ShouldContain("clear");

    await Task.CompletedTask;
  }

  public static async Task Should_hide_completion_routes_by_default()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("mycommand", "User command"));
    endpoints.Add(CreateEndpoint("__complete {index:int} {*words}", "Shell completion callback"));
    endpoints.Add(CreateEndpoint("--generate-completion {shell}", "Generate completion script"));
    endpoints.Add(CreateEndpoint("--install-completion {shell?}", "Install completion script"));

    HelpOptions options = new();

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", null, options, HelpContext.Cli, useColor: false);

    // Assert
    helpText.ShouldContain("mycommand");
    helpText.ShouldNotContain("__complete");
    helpText.ShouldNotContain("--generate-completion");
    helpText.ShouldNotContain("--install-completion");

    await Task.CompletedTask;
  }

  public static async Task Should_show_completion_routes_when_enabled()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("mycommand", "User command"));
    endpoints.Add(CreateEndpoint("--generate-completion {shell}", "Generate completion script"));

    HelpOptions options = new() { ShowCompletionRoutes = true };

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", null, options, HelpContext.Cli, useColor: false);

    // Assert
    helpText.ShouldContain("mycommand");
    helpText.ShouldContain("--generate-completion");

    await Task.CompletedTask;
  }

  public static async Task Should_exclude_routes_matching_custom_patterns()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("mycommand", "User command"));
    endpoints.Add(CreateEndpoint("debug-info", "Debug info"));
    endpoints.Add(CreateEndpoint("internal-test", "Internal test"));

    HelpOptions options = new()
    {
      ExcludePatterns = ["*-debug", "debug-*", "*-internal", "internal-*"]
    };

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", null, options, HelpContext.Cli, useColor: false);

    // Assert
    helpText.ShouldContain("mycommand");
    helpText.ShouldNotContain("debug-info");
    helpText.ShouldNotContain("internal-test");

    await Task.CompletedTask;
  }

  public static async Task Should_group_aliases_by_description()
  {
    // Arrange - exit, quit, q all have the same description
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("exit", "Exit the REPL"));
    endpoints.Add(CreateEndpoint("quit", "Exit the REPL"));
    endpoints.Add(CreateEndpoint("q", "Exit the REPL"));

    HelpOptions options = new() { ShowReplCommandsInCli = true };

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", null, options, HelpContext.Cli, useColor: false);

    // Assert - All three should be grouped together on one line
    // The pattern should show: "exit, q, quit" (alphabetically sorted within group)
    helpText.ShouldContain("exit, q, quit");

    await Task.CompletedTask;
  }

  public static async Task Should_always_hide_base_help_routes()
  {
    // Arrange - Base help routes should always be hidden (they don't need to show themselves)
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("mycommand", "User command"));
    endpoints.Add(CreateEndpoint("--help", "Show help"));
    endpoints.Add(CreateEndpoint("--help?", "Show help"));
    endpoints.Add(CreateEndpoint("help", "Show help"));

    HelpOptions options = new();

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", null, options, HelpContext.Cli, useColor: false);

    // Assert
    helpText.ShouldContain("mycommand");
    // Base help routes should not appear in their own output
    int optionsIndex = helpText.IndexOf("Options:", StringComparison.Ordinal);
    if (optionsIndex >= 0)
    {
      // If there's an Options section, it should not contain --help
      string optionsSection = helpText.Substring(optionsIndex);
      optionsSection.ShouldNotContain("--help");
    }

    await Task.CompletedTask;
  }

  public static async Task Should_show_repl_commands_in_cli_when_explicitly_enabled()
  {
    // Arrange
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("exit", "Exit the REPL"));

    HelpOptions options = new() { ShowReplCommandsInCli = true };

    // Act - CLI context but with ShowReplCommandsInCli enabled
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", null, options, HelpContext.Cli, useColor: false);

    // Assert
    helpText.ShouldContain("exit");

    await Task.CompletedTask;
  }
}
