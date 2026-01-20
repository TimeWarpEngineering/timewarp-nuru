#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru-mcp/timewarp-nuru-mcp.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Mcp
{

[TestTag("MCP")]
public sealed class HandlerGenerationTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<HandlerGenerationTests>();

  public static async Task Should_generate_WithHandler_for_simple_literal()
  {
    // Arrange
    string pattern = "status";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern);

    // Assert - V2 fluent DSL
    result.ShouldContain(".Map(\"status\")");
    result.ShouldContain(".WithHandler(");
    result.ShouldContain("() =>");
    result.ShouldContain(".AsCommand()");
    result.ShouldContain(".Done()");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_WithHandler_with_string_parameter()
  {
    // Arrange
    string pattern = "greet {name}";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern);

    // Assert
    result.ShouldContain(".Map(\"greet {name}\")");
    result.ShouldContain(".WithHandler(");
    result.ShouldContain("(string name)");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_WithHandler_with_optional_parameter()
  {
    // Arrange
    string pattern = "deploy {env} {tag?}";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern);

    // Assert
    result.ShouldContain(".Map(\"deploy {env} {tag?}\")");
    result.ShouldContain(".WithHandler(");
    result.ShouldContain("(string env, string? tag)");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_WithHandler_with_typed_parameter()
  {
    // Arrange
    string pattern = "wait {seconds:int}";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern);

    // Assert
    result.ShouldContain(".Map(\"wait {seconds:int}\")");
    result.ShouldContain(".WithHandler(");
    result.ShouldContain("(int seconds)");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_WithHandler_with_boolean_flag()
  {
    // Arrange
    string pattern = "build --verbose";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern);

    // Assert
    result.ShouldContain(".Map(\"build --verbose\")");
    result.ShouldContain(".WithHandler(");
    result.ShouldContain("(bool verbose)");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_WithHandler_with_option_value()
  {
    // Arrange
    string pattern = "test {project} --filter {pattern}";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern);

    // Assert
    result.ShouldContain(".Map(");
    result.ShouldContain(".WithHandler(");
    result.ShouldContain("(string project");
    result.ShouldContain("string pattern)");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_WithHandler_with_catch_all()
  {
    // Arrange
    string pattern = "docker {*args}";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern);

    // Assert
    result.ShouldContain(".Map(\"docker {*args}\")");
    result.ShouldContain(".WithHandler(");
    result.ShouldContain("(string[] args)");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_WithHandler_with_option_aliases()
  {
    // Arrange
    string pattern = "backup {source} --output,-o {dest} --compress,-c";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern);

    // Assert
    result.ShouldContain(".Map(");
    result.ShouldContain(".WithHandler(");
    result.ShouldContain("source");
    result.ShouldContain("dest");
    result.ShouldContain("compress");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_include_NuruRoute_alternative_example()
  {
    // Arrange
    string pattern = "deploy {env}";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern);

    // Assert - Should show [NuruRoute] as alternative
    result.ShouldContain("[NuruRoute(");
    result.ShouldContain("DeployEndpoint");
    result.ShouldContain("Env");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_include_AddBehavior_example()
  {
    // Arrange
    string pattern = "backup {source}";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern);

    // Assert - Should include pipeline behaviors documentation
    result.ShouldContain(".AddBehavior(");
    result.ShouldContain("INuruBehavior");
    result.ShouldContain(".Implements<");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_include_endpoint_classification()
  {
    // Arrange
    string pattern = "status";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern);

    // Assert - Should show endpoint classification options
    result.ShouldContain(".AsCommand()");
    result.ShouldContain("AsQuery()");
    result.ShouldContain("AsIdempotentCommand()");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  [Input("test {param {nested}")]
  [Input("invalid --")]
  [Input("deploy {env")]
  public static async Task Should_handle_invalid_patterns_gracefully(string pattern)
  {
    // Arrange & Act
    string result = GenerateHandlerTool.GenerateHandler(pattern);

    // Assert
    result.ShouldContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_V2_fluent_DSL_by_default()
  {
    // Arrange
    string pattern = "status";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern);

    // Assert - V2 fluent DSL is the primary pattern
    result.ShouldContain("V2 Fluent DSL Pattern with .WithHandler()");
    result.ShouldContain(".WithDescription(");
    result.ShouldContain(".Done()");
    result.ShouldContain("Source-generated at compile time");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Mcp
