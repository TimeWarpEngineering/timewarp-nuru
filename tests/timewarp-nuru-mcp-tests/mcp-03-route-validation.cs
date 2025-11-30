#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru-mcp/timewarp-nuru-mcp.csproj

return await RunTests<RouteValidationTests>(clearCache: true);

[TestTag("MCP")]
[ClearRunfileCache]
public class RouteValidationTests
{
  [Input("status")]
  [Input("git commit")]
  [Input("deploy {env}")]
  [Input("deploy {env} {tag?}")]
  [Input("delay {ms:int}")]
  [Input("docker {*args}")]
  [Input("build --verbose")]
  [Input("build --config {mode}")]
  public static async Task Should_validate_correct_route_patterns(string pattern)
  {
    // Arrange & Act
    string result = ValidateRouteTool.ValidateRoute(pattern);

    // Assert
    result.ShouldContain(pattern);
    result.ShouldNotContain("Error");
    result.ShouldNotContain("Invalid");
    result.Length.ShouldBeGreaterThan(50);

    await Task.CompletedTask;
  }

  public static async Task Should_validate_complex_pattern_with_descriptions()
  {
    // Arrange
    string pattern = "deploy {env|Environment} --dry-run,-d|Preview";

    // Act
    string result = ValidateRouteTool.ValidateRoute(pattern);

    // Assert
    result.ShouldContain(pattern);
    result.ShouldNotContain("Error");

    await Task.CompletedTask;
  }

  [Input("deploy {env")]
  [Input("prompt <input>")]
  public static async Task Should_detect_invalid_route_patterns(string pattern)
  {
    // Arrange & Act
    string result = ValidateRouteTool.ValidateRoute(pattern);

    // Assert
    result.ShouldContain(pattern);
    // Should indicate validation failure
    (result.Contains("Error") || result.Contains("Invalid") || result.Contains("NURU_")).ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_provide_detailed_feedback_for_valid_pattern()
  {
    // Arrange
    string pattern = "deploy {env} --tag {t}";

    // Act
    string result = ValidateRouteTool.ValidateRoute(pattern);

    // Assert
    result.ShouldContain("deploy");
    result.ShouldContain("{env}");
    result.ShouldContain("--tag");
    result.ShouldContain("{t}");

    await Task.CompletedTask;
  }

  public static async Task Should_show_parameter_types()
  {
    // Arrange
    string pattern = "wait {seconds:int} {message?}";

    // Act
    string result = ValidateRouteTool.ValidateRoute(pattern);

    // Assert
    result.ShouldContain("int");
    result.ShouldContain("?"); // Optional parameters shown with ? symbol

    await Task.CompletedTask;
  }

  public static async Task Should_show_catch_all_parameter()
  {
    // Arrange
    string pattern = "run {*args}";

    // Act
    string result = ValidateRouteTool.ValidateRoute(pattern);

    // Assert
    result.ShouldContain("args");
    result.ShouldContain("*");

    await Task.CompletedTask;
  }

  public static async Task Should_show_option_flags()
  {
    // Arrange
    string pattern = "build --verbose --watch";

    // Act
    string result = ValidateRouteTool.ValidateRoute(pattern);

    // Assert
    result.ShouldContain("--verbose");
    result.ShouldContain("--watch");

    await Task.CompletedTask;
  }

  public static async Task Should_show_option_with_value()
  {
    // Arrange
    string pattern = "server --port {num:int}";

    // Act
    string result = ValidateRouteTool.ValidateRoute(pattern);

    // Assert
    result.ShouldContain("--port");
    result.ShouldContain("num");
    result.ShouldContain("int");

    await Task.CompletedTask;
  }

  public static async Task Should_show_option_aliases()
  {
    // Arrange
    string pattern = "build --verbose,-v";

    // Act
    string result = ValidateRouteTool.ValidateRoute(pattern);

    // Assert
    result.ShouldContain("--verbose");
    result.ShouldContain("-v");

    await Task.CompletedTask;
  }

  public static async Task Should_detect_unclosed_brace()
  {
    // Arrange
    string pattern = "deploy {env";

    // Act
    string result = ValidateRouteTool.ValidateRoute(pattern);

    // Assert
    result.ShouldContain("deploy {env");
    (result.Contains("Error") || result.Contains("unclosed") || result.Contains("NURU_")).ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_detect_invalid_angle_brackets()
  {
    // Arrange
    string pattern = "prompt <input>";

    // Act
    string result = ValidateRouteTool.ValidateRoute(pattern);

    // Assert
    result.ShouldContain("prompt <input>");
    (result.Contains("Error") || result.Contains("Invalid") || result.Contains("NURU_")).ShouldBeTrue();

    await Task.CompletedTask;
  }
}
