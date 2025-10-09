#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru.Mcp/TimeWarp.Nuru.Mcp.csproj

return await RunTests<ErrorDocumentationTests>(clearCache: true);

[TestTag("MCP")]
[ClearRunfileCache]
public class ErrorDocumentationTests
{
  [Input("overview")]
  [Input("architecture")]
  [Input("philosophy")]
  public static async Task Should_get_error_handling_info(string area)
  {
    // Arrange & Act
    string result = await ErrorHandlingTool.GetErrorHandlingInfoAsync(area);

    // Assert
    result.ShouldNotBeNullOrEmpty();
    result.ShouldNotContain("Unknown area");
    result.Length.ShouldBeGreaterThan(50);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_unknown_area()
  {
    // Arrange & Act
    string result = await ErrorHandlingTool.GetErrorHandlingInfoAsync("invalid-area");

    // Assert
    result.ShouldContain("Unknown area");

    await Task.CompletedTask;
  }

  [Input("parsing")]
  [Input("binding")]
  [Input("conversion")]
  [Input("execution")]
  [Input("matching")]
  [Input("all")]
  public static async Task Should_get_error_scenarios(string scenario)
  {
    // Arrange & Act
    string result = await ErrorHandlingTool.GetErrorScenariosAsync(scenario);

    // Assert
    result.ShouldNotBeNullOrEmpty();
    result.ShouldNotContain("Unknown scenario");
    result.Length.ShouldBeGreaterThan(50);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_unknown_scenario()
  {
    // Arrange & Act
    string result = await ErrorHandlingTool.GetErrorScenariosAsync("invalid-scenario");

    // Assert
    (result.Contains("Unknown scenario") || result.Contains("Available scenarios")).ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_get_best_practices()
  {
    // Arrange & Act
    string result = await ErrorHandlingTool.GetErrorHandlingBestPracticesAsync();

    // Assert
    result.ShouldNotBeNullOrEmpty();
    result.ShouldNotContain("Error retrieving");
    result.Length.ShouldBeGreaterThan(50);

    await Task.CompletedTask;
  }

  public static async Task Should_cache_documentation()
  {
    // Arrange - First call to prime cache
    string firstCall = await ErrorHandlingTool.GetErrorHandlingInfoAsync("overview");

    // Act - Second call should use cache
    string secondCall = await ErrorHandlingTool.GetErrorHandlingInfoAsync("overview");

    // Assert - Both should return identical content
    firstCall.ShouldBe(secondCall);

    await Task.CompletedTask;
  }

  public static async Task Should_force_refresh_documentation()
  {
    // Arrange - First call
    string firstCall = await ErrorHandlingTool.GetErrorHandlingInfoAsync("overview");

    // Act - Force refresh
    string refreshed = await ErrorHandlingTool.GetErrorHandlingInfoAsync("overview", forceRefresh: true);

    // Assert - Content should still be valid
    refreshed.ShouldNotBeNullOrEmpty();
    refreshed.Length.ShouldBeGreaterThan(50);

    await Task.CompletedTask;
  }

  public static async Task Should_get_parsing_error_scenarios()
  {
    // Arrange & Act
    string result = await ErrorHandlingTool.GetErrorScenariosAsync("parsing");

    // Assert
    result.ShouldContain("pars");
    result.Length.ShouldBeGreaterThan(100);

    await Task.CompletedTask;
  }

  public static async Task Should_get_binding_error_scenarios()
  {
    // Arrange & Act
    string result = await ErrorHandlingTool.GetErrorScenariosAsync("binding");

    // Assert
    result.ShouldContain("bind");
    result.Length.ShouldBeGreaterThan(100);

    await Task.CompletedTask;
  }

  public static async Task Should_get_conversion_error_scenarios()
  {
    // Arrange & Act
    string result = await ErrorHandlingTool.GetErrorScenariosAsync("conversion");

    // Assert
    result.ShouldContain("conver");
    result.Length.ShouldBeGreaterThan(100);

    await Task.CompletedTask;
  }

  public static async Task Should_get_all_error_scenarios()
  {
    // Arrange & Act
    string result = await ErrorHandlingTool.GetErrorScenariosAsync("all");

    // Assert
    result.ShouldNotBeNullOrEmpty();
    result.Length.ShouldBeGreaterThan(200);

    await Task.CompletedTask;
  }
}
