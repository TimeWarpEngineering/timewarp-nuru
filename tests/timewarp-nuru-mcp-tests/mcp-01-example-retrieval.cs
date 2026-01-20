#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru-mcp/timewarp-nuru-mcp.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

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
    result.ShouldContain("basic");
    result.ShouldContain("mixed");
    result.ShouldContain("delegate");
    result.ShouldContain("attributed-routes");
    result.ShouldContain("behaviors-basic");
    result.ShouldContain("behaviors-filtered");
    result.ShouldContain("console-logging");
    result.ShouldContain("serilog");

    await Task.CompletedTask;
  }

  public static async Task Should_retrieve_basic_example()
  {
    // Arrange & Act
    string result = await GetExampleTool.GetExampleAsync("basic");

    // Assert
    result.Length.ShouldBeGreaterThan(500);
    result.ShouldContain("01-calc-delegate.cs");
    result.ShouldContain("delegate routing");

    await Task.CompletedTask;
  }

  public static async Task Should_retrieve_delegate_example()
  {
    // Arrange & Act
    string result = await GetExampleTool.GetExampleAsync("delegate");

    // Assert
    result.Length.ShouldBeGreaterThan(500);
    result.ShouldContain("01-calc-delegate.cs");
    result.ShouldContain("delegate routing");

    await Task.CompletedTask;
  }

  public static async Task Should_retrieve_commands_example()
  {
    // Arrange & Act
    string result = await GetExampleTool.GetExampleAsync("commands");

    // Assert
    result.Length.ShouldBeGreaterThan(500);
    result.ShouldContain("02-calc-commands.cs");
    result.ShouldContain("command/handler pattern");

    await Task.CompletedTask;
  }

  public static async Task Should_retrieve_mixed_example()
  {
    // Arrange & Act
    string result = await GetExampleTool.GetExampleAsync("mixed");

    // Assert
    result.Length.ShouldBeGreaterThan(500);
    result.ShouldContain("03-calc-mixed.cs");
    result.ShouldContain("Mixed");

    await Task.CompletedTask;
  }

  public static async Task Should_retrieve_console_logging_example()
  {
    // Arrange & Act
    string result = await GetExampleTool.GetExampleAsync("console-logging");

    // Assert
    result.Length.ShouldBeGreaterThan(100);
    result.ShouldContain("console-logging.cs");

    await Task.CompletedTask;
  }

  public static async Task Should_retrieve_serilog_example()
  {
    // Arrange & Act
    string result = await GetExampleTool.GetExampleAsync("serilog");

    // Assert
    result.Length.ShouldBeGreaterThan(100);
    result.ShouldContain("serilog-logging.cs");

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
    string firstResult = await GetExampleTool.GetExampleAsync("basic");

    // Act
    string refreshedResult = await GetExampleTool.GetExampleAsync("basic", forceRefresh: true);

    // Assert
    refreshedResult.Length.ShouldBeGreaterThan(500);
    // Content should be identical (both valid)
    refreshedResult.ShouldContain("01-calc-delegate.cs");

    await Task.CompletedTask;
  }

  public static async Task Should_use_memory_cache_on_second_call()
  {
    // Arrange - First call populates cache
    string firstResult = await GetExampleTool.GetExampleAsync("basic");

    // Act - Second call should use cache
    string secondResult = await GetExampleTool.GetExampleAsync("basic");

    // Assert - Both should be identical
    firstResult.ShouldBe(secondResult);

    await Task.CompletedTask;
  }

  public static async Task Should_check_cache_status()
  {
    // Arrange - Fetch an example first
    await GetExampleTool.GetExampleAsync("basic");

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
    result.ShouldContain("basic");
    result.ShouldContain("delegate");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Mcp
