#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Test HelpProvider default route display
// Issue #141: Fix blank entry and comma before default route aliases
return await RunTests<HelpProviderDefaultRouteTests>(clearCache: true);

[TestTag("Help")]
[ClearRunfileCache]
public class HelpProviderDefaultRouteTests
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

  public static async Task Should_display_default_route_as_default_marker()
  {
    // Arrange - Issue #141: Default route (empty pattern) should show as "(default)"
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("", "Show main view"));

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", useColor: false);

    // Assert - Should show "(default)" instead of blank
    helpText.ShouldContain("(default)");
    helpText.ShouldContain("Show main view");

    await Task.CompletedTask;
  }

  public static async Task Should_not_display_leading_comma_when_default_and_alias_share_description()
  {
    // Arrange - Issue #141: When default route and another route share description,
    // should NOT show ", list" but rather "list (default)"
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("", "Display the kanban board"));
    endpoints.Add(CreateEndpoint("list", "Display the kanban board"));

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", useColor: false);

    // Assert - Should NOT have leading comma
    helpText.ShouldNotContain(", list");
    // Should show "list (default)" or just "list" with default indicator
    helpText.ShouldContain("list (default)");
    helpText.ShouldContain("Display the kanban board");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_multiple_aliases_with_default()
  {
    // Arrange - Multiple aliases sharing description with default
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("", "Show kanban board"));
    endpoints.Add(CreateEndpoint("list", "Show kanban board"));
    endpoints.Add(CreateEndpoint("ls", "Show kanban board"));

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", useColor: false);

    // Assert - Should show aliases cleanly without LEADING comma (the bug was ", list")
    // Normal comma separation between aliases is expected: "list (default), ls"
    helpText.ShouldNotContain(", list"); // No leading comma before list
    // First alias (alphabetically) should have (default) marker
    helpText.ShouldContain("list (default)");
    helpText.ShouldContain("ls");
    // The comma between aliases is correct
    helpText.ShouldContain("list (default), ls");

    await Task.CompletedTask;
  }

  public static async Task Should_display_only_default_marker_when_default_route_is_standalone()
  {
    // Arrange - Default route with unique description
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("", "Default action"));
    endpoints.Add(CreateEndpoint("other", "Other action"));

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", useColor: false);

    // Assert
    helpText.ShouldContain("(default)");
    helpText.ShouldContain("Default action");
    helpText.ShouldContain("other");
    helpText.ShouldContain("Other action");

    await Task.CompletedTask;
  }

  public static async Task Should_not_affect_normal_aliases_without_default()
  {
    // Arrange - Normal aliases without default route
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("exit", "Exit the app"));
    endpoints.Add(CreateEndpoint("quit", "Exit the app"));

    HelpOptions options = new() { ShowReplCommandsInCli = true };

    // Act
    string helpText = HelpProvider.GetHelpText(endpoints, "testapp", null, options, HelpContext.Cli, useColor: false);

    // Assert - Normal comma-separated aliases should work
    helpText.ShouldContain("exit, quit");
    helpText.ShouldNotContain("(default)");

    await Task.CompletedTask;
  }
}
