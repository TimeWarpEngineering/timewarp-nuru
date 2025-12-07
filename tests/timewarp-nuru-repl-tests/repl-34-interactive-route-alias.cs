#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

// Test AddInteractiveRoute uses alias syntax for single endpoint (Issue #119)
return await RunTests<AddInteractiveRouteAliasTests>(clearCache: true);

[TestTag("REPL")]
[ClearRunfileCache]
public class AddInteractiveRouteAliasTests
{
  public static async Task Should_create_single_endpoint_for_option_aliases()
  {
    // Arrange - Issue #119: AddInteractiveRoute should use alias syntax
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);

    // Act - Add interactive route with default "--interactive,-i"
    builder.AddInteractiveRoute();

    // Assert - Should have exactly 2 endpoints: "status" + one for interactive
    // NOT 3 (which would happen if MapMultiple created separate endpoints)
    int endpointCount = builder.EndpointCollection.Endpoints.Count;
    endpointCount.ShouldBe(2, "Should have 2 endpoints (status + single interactive route)");

    // Verify the interactive endpoint exists with alias syntax
    Endpoint? interactiveEndpoint = builder.EndpointCollection.Endpoints
      .FirstOrDefault(e => e.RoutePattern.Contains("--interactive"));

    interactiveEndpoint.ShouldNotBeNull();
    interactiveEndpoint.RoutePattern.ShouldBe("--interactive,-i");

    await Task.CompletedTask;
  }

  public static async Task Should_use_alias_syntax_in_route_pattern()
  {
    // Arrange
    NuruAppBuilder builder = new();

    // Act
    builder.AddInteractiveRoute("--repl,-r");

    // Assert - Route pattern should include both forms
    Endpoint? endpoint = builder.EndpointCollection.Endpoints
      .FirstOrDefault(e => e.RoutePattern.Contains("--repl"));

    endpoint.ShouldNotBeNull();
    endpoint.RoutePattern.ShouldBe("--repl,-r");

    await Task.CompletedTask;
  }

  public static async Task Should_use_map_multiple_for_literal_commands()
  {
    // Arrange - When patterns include literals (non-option), use MapMultiple
    NuruAppBuilder builder = new();

    // Act - Mix of literal and option
    builder.AddInteractiveRoute("interactive,--interactive,-i");

    // Assert - Should create 3 separate endpoints (MapMultiple behavior)
    int endpointCount = builder.EndpointCollection.Endpoints.Count;
    endpointCount.ShouldBe(3, "Literals require MapMultiple which creates separate endpoints");

    // Verify all patterns exist
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern == "interactive");
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern == "--interactive");
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern == "-i");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_two_option_aliases()
  {
    // Arrange - Option alias syntax supports exactly 2 forms (long + short)
    NuruAppBuilder builder = new();

    // Act - Two option aliases (long + short form)
    builder.AddInteractiveRoute("--repl,-r");

    // Assert - Should create single endpoint with alias
    builder.EndpointCollection.Endpoints.Count.ShouldBe(1);

    // Access directly via index instead of FirstOrDefault to satisfy CA1826
    Endpoint endpoint = builder.EndpointCollection.Endpoints[0];
    endpoint.ShouldNotBeNull();
    endpoint.RoutePattern.ShouldBe("--repl,-r");

    await Task.CompletedTask;
  }

  public static async Task Should_fallback_to_map_multiple_for_more_than_two_options()
  {
    // Arrange - Alias syntax only works for 2 options, more requires MapMultiple
    NuruAppBuilder builder = new();

    // Act - Four option aliases (can't use alias syntax)
    builder.AddInteractiveRoute("--interactive,-i,--repl,-r");

    // Assert - Should create 4 separate endpoints (MapMultiple behavior)
    builder.EndpointCollection.Endpoints.Count.ShouldBe(4);

    // Verify all patterns exist as separate routes
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern == "--interactive");
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern == "-i");
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern == "--repl");
    builder.EndpointCollection.Endpoints.ShouldContain(e => e.RoutePattern == "-r");

    await Task.CompletedTask;
  }

  public static async Task Should_show_aliases_on_single_help_line()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);
    builder.AddInteractiveRoute();

    // Act - Get help text
    string helpText = HelpProvider.GetHelpText(builder.EndpointCollection, "testapp");

    // Assert - Help should show both forms on same line
    // The exact format depends on HelpProvider implementation, but both should appear together
    helpText.ShouldContain("-i");
    helpText.ShouldContain("--interactive");

    // Count occurrences of "Enter interactive REPL mode" - should be 1, not 2
    int descriptionCount = CountOccurrences(helpText, "Enter interactive REPL mode");
    descriptionCount.ShouldBe(1, "Description should appear once (single endpoint)");

    await Task.CompletedTask;
  }

  private static int CountOccurrences(string text, string pattern)
  {
    int count = 0;
    int index = 0;
    while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
    {
      count++;
      index += pattern.Length;
    }

    return count;
  }
}
