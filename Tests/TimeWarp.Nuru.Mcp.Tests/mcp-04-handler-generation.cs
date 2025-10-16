#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru.Mcp/TimeWarp.Nuru.Mcp.csproj

return await RunTests<HandlerGenerationTests>(clearCache: true);

[TestTag("MCP")]
[ClearRunfileCache]
public sealed class HandlerGenerationTests
{
  public static async Task Should_generate_delegate_handler_for_simple_literal()
  {
    // Arrange
    string pattern = "status";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern, useMediator: false);

    // Assert
    result.ShouldContain("AddRoute");
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
    string result = GenerateHandlerTool.GenerateHandler(pattern, useMediator: false);

    // Assert
    result.ShouldContain("AddRoute");
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
    string result = GenerateHandlerTool.GenerateHandler(pattern, useMediator: false);

    // Assert
    result.ShouldContain("AddRoute");
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
    string result = GenerateHandlerTool.GenerateHandler(pattern, useMediator: false);

    // Assert
    result.ShouldContain("AddRoute");
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
    string result = GenerateHandlerTool.GenerateHandler(pattern, useMediator: false);

    // Assert
    result.ShouldContain("AddRoute");
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
    string result = GenerateHandlerTool.GenerateHandler(pattern, useMediator: false);

    // Assert
    result.ShouldContain("AddRoute");
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
    string result = GenerateHandlerTool.GenerateHandler(pattern, useMediator: false);

    // Assert
    result.ShouldContain("AddRoute");
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
    string result = GenerateHandlerTool.GenerateHandler(pattern, useMediator: false);

    // Assert
    result.ShouldContain("AddRoute");
    result.ShouldContain("source");
    result.ShouldContain("dest");
    result.ShouldContain("compress");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_mediator_handler_for_simple_pattern()
  {
    // Arrange
    string pattern = "deploy {env}";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern, useMediator: true);

    // Assert
    result.ShouldContain("public sealed class");
    result.ShouldContain("IRequest");
    result.ShouldContain("IRequestHandler");
    result.ShouldContain("AddRoute<");
    result.ShouldContain("string env");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_mediator_handler_with_optional_parameter()
  {
    // Arrange
    string pattern = "backup {source} {dest?}";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern, useMediator: true);

    // Assert
    result.ShouldContain("public sealed class");
    result.ShouldContain("string? dest");
    result.ShouldContain("IRequest");
    result.ShouldNotContain("// Error");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_mediator_handler_with_complex_pattern()
  {
    // Arrange
    string pattern = "test {project} --verbose --filter {pattern}";

    // Act
    string result = GenerateHandlerTool.GenerateHandler(pattern, useMediator: true);

    // Assert
    result.ShouldContain("public sealed class");
    result.ShouldContain("string project");
    result.ShouldContain("bool verbose");
    result.ShouldContain("string pattern");
    result.ShouldContain("IRequest");
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
    result.ShouldContain("AddRoute");
    result.ShouldNotContain("IRequest");

    await Task.CompletedTask;
  }
}
