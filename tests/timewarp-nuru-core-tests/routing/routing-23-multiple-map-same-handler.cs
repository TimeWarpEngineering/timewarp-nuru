#!/usr/bin/dotnet --

// Task 205: Verify calling Map multiple times with same handler works properly
// This validates the migration path from MapMultiple to multiple Map calls

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class MultipleMapSameHandlerTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<MultipleMapSameHandlerTests>();

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

  /// <summary>
  /// Test that multiple patterns with same handler all route correctly
  /// </summary>
  public static async Task Should_route_multiple_patterns_to_same_handler()
  {
    // Arrange
    int executionCount = 0;
    Func<int> handler = () => { executionCount++; };

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .Map("close").WithHandler(handler).WithDescription("Close the application").Done()
      .Map("shutdown").WithHandler(handler).WithDescription("Close the application").Done()
      .Map("bye").WithHandler(handler).WithDescription("Close the application").Done()
      .Build();

    // Act & Assert - each pattern should invoke the handler
    executionCount = 0;
    int exitCode1 = await app.RunAsync(["close"]);
    exitCode1.ShouldBe(0);
    executionCount.ShouldBe(1);

    executionCount = 0;
    int exitCode2 = await app.RunAsync(["shutdown"]);
    exitCode2.ShouldBe(0);
    executionCount.ShouldBe(1);

    executionCount = 0;
    int exitCode3 = await app.RunAsync(["bye"]);
    exitCode3.ShouldBe(0);
    executionCount.ShouldBe(1);
  }

  /// <summary>
  /// Test that multiple patterns with same description are grouped in help output
  /// </summary>
  public static async Task Should_group_patterns_with_same_description_in_help()
  {
    // Arrange - use non-REPL command names to avoid filtering
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("close", "Close the application"));
    endpoints.Add(CreateEndpoint("shutdown", "Close the application"));
    endpoints.Add(CreateEndpoint("bye", "Close the application"));
    endpoints.Add(CreateEndpoint("status", "Show status"));

    // Act
    string helpText = TimeWarp.Nuru.HelpProvider.GetHelpText(endpoints, "testapp", useColor: false);

    // Assert - patterns with same description should be grouped together
    helpText.ShouldContain("close");
    helpText.ShouldContain("shutdown");
    helpText.ShouldContain("bye");
    helpText.ShouldContain("Close the application");
    helpText.ShouldContain("status");
    helpText.ShouldContain("Show status");

    // The grouped patterns should appear together (comma-separated)
    // Check that we don't have duplicate "Close the application" lines
    int closeDescriptionCount = System.Text.RegularExpressions.Regex.Matches(helpText, "Close the application").Count;
    closeDescriptionCount.ShouldBe(1, "Patterns with same description should be grouped, not duplicated");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that execution invokes the correct shared handler with parameters
  /// </summary>
  public static async Task Should_execute_shared_handler_with_parameters()
  {
    // Arrange
    string? capturedName = null;
    Func<string, int> handler = (name) => { capturedName = name; };

    NuruCoreApp app = NuruApp.CreateSlimBuilder([])
      .Map("greet {name}").WithHandler(handler).WithDescription("Greet someone").Done()
      .Map("hello {name}").WithHandler(handler).WithDescription("Greet someone").Done()
      .Map("hi {name}").WithHandler(handler).WithDescription("Greet someone").Done()
      .Build();

    // Act & Assert - each pattern should pass the parameter correctly
    capturedName = null;
    await app.RunAsync(["greet", "Alice"]);
    capturedName.ShouldBe("Alice");

    capturedName = null;
    await app.RunAsync(["hello", "Bob"]);
    capturedName.ShouldBe("Bob");

    capturedName = null;
    await app.RunAsync(["hi", "Charlie"]);
    capturedName.ShouldBe("Charlie");
  }

  /// <summary>
  /// Test that async handlers work with multiple Map calls
  /// </summary>
  public static async Task Should_support_async_handlers_with_multiple_patterns()
  {
    // Arrange
    int executionCount = 0;
    Func<Task<int>> handler = async () =>
    {
      await Task.Delay(1);
      executionCount++;
      return 42; // Outputs "42" to terminal (tests Task<int> handler support)
    };

    NuruCoreApp app = NuruApp.CreateSlimBuilder([])
      .Map("save").WithHandler(handler).WithDescription("Save data").Done()
      .Map("write").WithHandler(handler).WithDescription("Save data").Done()
      .Build();

    // Act & Assert
    executionCount = 0;
    int exitCode1 = await app.RunAsync(["save"]);
    exitCode1.ShouldBe(0);
    executionCount.ShouldBe(1);

    executionCount = 0;
    int exitCode2 = await app.RunAsync(["write"]);
    exitCode2.ShouldBe(0);
    executionCount.ShouldBe(1);
  }

  /// <summary>
  /// Test that different descriptions create separate groups in help
  /// </summary>
  public static async Task Should_not_group_patterns_with_different_descriptions()
  {
    // Arrange - use non-REPL command names to avoid filtering
    EndpointCollection endpoints = [];
    endpoints.Add(CreateEndpoint("close", "Close the application"));
    endpoints.Add(CreateEndpoint("shutdown", "Shutdown immediately"));

    // Act
    string helpText = TimeWarp.Nuru.HelpProvider.GetHelpText(endpoints, "testapp", useColor: false);

    // Assert - different descriptions should NOT be grouped
    helpText.ShouldContain("Close the application");
    helpText.ShouldContain("Shutdown immediately");

    // Both descriptions should appear separately
    int closeCount = System.Text.RegularExpressions.Regex.Matches(helpText, "Close the application").Count;
    int shutdownCount = System.Text.RegularExpressions.Regex.Matches(helpText, "Shutdown immediately").Count;
    closeCount.ShouldBe(1);
    shutdownCount.ShouldBe(1);

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
