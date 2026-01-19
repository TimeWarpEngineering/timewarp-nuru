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

  public static async Task Should_generate_delegate_handler_for_simple_literal()
  {
    // Arrange
    string pattern = "status";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern, useCommand: false);

    // Assert
    result.ShouldContain("Map");
    result.ShouldContain("\"status\"");
    result.ShouldContain("() =>");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_delegate_handler_with_string_parameter()
  {
    // Arrange
    string pattern = "greet {name}";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern, useCommand: false);

    // Assert
    result.ShouldContain("Map");
    result.ShouldContain("\"greet {name}\"");
    result.ShouldContain("(string name)");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_delegate_handler_with_optional_parameter()
  {
    // Arrange
    string pattern = "deploy {env} {tag?}";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern, useCommand: false);

    // Assert
    result.ShouldContain("Map");
    result.ShouldContain("\"deploy {env} {tag?}\"");
    result.ShouldContain("(string env, string? tag)");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_delegate_handler_with_typed_parameter()
  {
    // Arrange
    string pattern = "wait {seconds:int}";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern, useCommand: false);

    // Assert
    result.ShouldContain("Map");
    result.ShouldContain("\"wait {seconds:int}\"");
    result.ShouldContain("(int seconds)");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_delegate_handler_with_boolean_flag()
  {
    // Arrange
    string pattern = "build --verbose";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern, useCommand: false);

    // Assert
    result.ShouldContain("Map");
    result.ShouldContain("\"build --verbose\"");
    result.ShouldContain("(bool verbose)");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_delegate_handler_with_option_value()
  {
    // Arrange
    string pattern = "test {project} --filter {pattern}";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern, useCommand: false);

    // Assert
    result.ShouldContain("Map");
    result.ShouldContain("(string project");
    result.ShouldContain("string pattern)");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_delegate_handler_with_catch_all()
  {
    // Arrange
    string pattern = "docker {*args}";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern, useCommand: false);

    // Assert
    result.ShouldContain("Map");
    result.ShouldContain("\"docker {*args}\"");
    result.ShouldContain("(string[] args)");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_delegate_handler_with_option_aliases()
  {
    // Arrange
    string pattern = "backup {source} --output,-o {dest} --compress,-c";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern, useCommand: false);

    // Assert
    result.ShouldContain("Map");
    result.ShouldContain("source");
    result.ShouldContain("dest");
    result.ShouldContain("compress");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_command_handler_for_simple_pattern()
  {
    // Arrange
    string pattern = "deploy {env}";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern, useCommand: true);

    // Assert
    result.ShouldContain("public sealed class");
    result.ShouldContain("ICommand<Unit>");
    result.ShouldContain("ICommandHandler<");
    result.ShouldContain("[NuruRoute(");
    result.ShouldContain("Env");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_command_handler_with_optional_parameter()
  {
    // Arrange
    string pattern = "backup {source} {dest?}";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern, useCommand: true);

    // Assert
    result.ShouldContain("public sealed class");
    result.ShouldContain("Dest");
    result.ShouldContain("ICommand<Unit>");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_command_handler_with_complex_pattern()
  {
    // Arrange
    string pattern = "test {project} --verbose --filter {pattern}";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern, useCommand: true);

    // Assert
    result.ShouldContain("public sealed class");
    result.ShouldContain("Project");
    result.ShouldContain("bool");
    result.ShouldContain("ICommand<Unit>");
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

  public static async Task Should_default_to_delegate_mode()
  {
    // Arrange
    string pattern = "status";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern);

    // Assert
    result.ShouldContain("Map");
    result.ShouldNotContain("[NuruRoute");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Mcp
