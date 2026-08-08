#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-mcp/timewarp-nuru-mcp.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

// NOTE: GetExampleTool fetches the manifest and example files from GitHub master
// (samples/examples.json). Assertions below use example IDs/paths from that manifest;
// when samples/examples.json changes, update these assertions to match.

namespace TimeWarp.Nuru.Tests.Mcp
{

[TestTag("MCP")]
public class ExampleRetrievalTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<ExampleRetrievalTests>();

  public static async Task Should_list_all_available_examples()
  {
    // Arrange & Act
    string result = await GetExampleTool.ListExamplesAsync();

    // Assert
    result.ShouldContain("hello-world-fluent");
    result.ShouldContain("hello-world-endpoint");
    result.ShouldContain("calculator-fluent");
    result.ShouldContain("calculator-endpoint");
    result.ShouldContain("pipeline-basic-fluent");
    result.ShouldContain("logging-fluent");
    result.ShouldContain("logging-serilog-endpoint");

    await Task.CompletedTask;
  }

  public static async Task Should_retrieve_hello_world_example()
  {
    // Arrange & Act
    string result = await GetExampleTool.GetExampleAsync("hello-world-fluent");

    // Assert
    result.Length.ShouldBeGreaterThan(100);
    result.ShouldContain("fluent-hello-world-lambda.cs");
    result.ShouldContain("Fluent DSL");

    await Task.CompletedTask;
  }

  public static async Task Should_retrieve_calculator_fluent_example()
  {
    // Arrange & Act
    string result = await GetExampleTool.GetExampleAsync("calculator-fluent");

    // Assert
    result.Length.ShouldBeGreaterThan(500);
    result.ShouldContain("fluent-calculator-delegate.cs");
    result.ShouldContain("Fluent DSL");

    await Task.CompletedTask;
  }

  public static async Task Should_retrieve_hello_world_endpoint_example()
  {
    // Arrange & Act
    // NOTE: many *-endpoint manifest entries point at paths that no longer exist
    // (manifest drift, kanban 454-033); hello-world-endpoint is one that resolves.
    string result = await GetExampleTool.GetExampleAsync("hello-world-endpoint");

    // Assert
    result.Length.ShouldBeGreaterThan(100);
    result.ShouldContain("endpoint-hello-world.cs");
    result.ShouldContain("Endpoint DSL");

    await Task.CompletedTask;
  }

  public static async Task Should_retrieve_unified_pipeline_example()
  {
    // Arrange & Act
    string result = await GetExampleTool.GetExampleAsync("unified-pipeline");

    // Assert
    result.Length.ShouldBeGreaterThan(500);
    result.ShouldContain("hybrid-unified-pipeline.cs");

    await Task.CompletedTask;
  }

  public static async Task Should_retrieve_console_logging_example()
  {
    // Arrange & Act
    string result = await GetExampleTool.GetExampleAsync("logging-fluent");

    // Assert
    result.Length.ShouldBeGreaterThan(100);
    result.ShouldContain("fluent-logging-console.cs");

    await Task.CompletedTask;
  }

  public static async Task Should_retrieve_testing_output_example()
  {
    // Arrange & Act
    // NOTE: was logging-serilog-endpoint; its manifest path is dead (kanban 454-033).
    string result = await GetExampleTool.GetExampleAsync("testing-output-fluent");

    // Assert
    result.Length.ShouldBeGreaterThan(100);
    result.ShouldContain("fluent-testing-output-capture.cs");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_unknown_example()
  {
    // Arrange & Act
    string result = await GetExampleTool.GetExampleAsync("nonexistent");

    // Assert
    result.ShouldContain("not found");
    result.ShouldContain("Available examples");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_force_refresh()
  {
    // Arrange
    string firstResult = await GetExampleTool.GetExampleAsync("hello-world-fluent");

    // Act
    string refreshedResult = await GetExampleTool.GetExampleAsync("hello-world-fluent", forceRefresh: true);

    // Assert
    refreshedResult.Length.ShouldBeGreaterThan(100);
    // Content should be identical (both valid)
    refreshedResult.ShouldContain("fluent-hello-world-lambda.cs");

    await Task.CompletedTask;
  }

  public static async Task Should_use_memory_cache_on_second_call()
  {
    // Arrange - First call populates cache
    string firstResult = await GetExampleTool.GetExampleAsync("hello-world-fluent");

    // Act - Second call should use cache
    string secondResult = await GetExampleTool.GetExampleAsync("hello-world-fluent");

    // Assert - Both should be identical
    firstResult.ShouldBe(secondResult);

    await Task.CompletedTask;
  }

  public static async Task Should_check_cache_status()
  {
    // Arrange - Fetch an example first
    await GetExampleTool.GetExampleAsync("hello-world-fluent");

    // Act
    string cacheStatus = CacheManagementTool.CacheStatus();

    // Assert
    cacheStatus.ShouldContain("cache");
    cacheStatus.Length.ShouldBeGreaterThan(10);

    await Task.CompletedTask;
  }

  public static async Task Should_support_list_command_alias()
  {
    // Arrange & Act
    string result = await GetExampleTool.GetExampleAsync("list");

    // Assert - Should return same as ListExamplesAsync()
    result.ShouldContain("Available examples");
    result.ShouldContain("hello-world-fluent");
    result.ShouldContain("calculator-fluent");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Mcp
