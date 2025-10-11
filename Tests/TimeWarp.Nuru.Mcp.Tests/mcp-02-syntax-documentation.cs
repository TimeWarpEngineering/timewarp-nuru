#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru.Mcp/TimeWarp.Nuru.Mcp.csproj

return await RunTests<SyntaxDocumentationTests>(clearCache: true);

[TestTag("MCP")]
[ClearRunfileCache]
public class SyntaxDocumentationTests
{
  [Input("literals")]
  [Input("parameters")]
  [Input("types")]
  [Input("optional")]
  [Input("catchall")]
  [Input("options")]
  [Input("descriptions")]
  public static async Task Should_get_specific_syntax_element(string element)
  {
    // Arrange & Act
    string result = GetSyntaxTool.GetSyntax(element);

    // Assert
    result.ShouldNotBeNullOrEmpty();
    result.ShouldNotContain("Unknown syntax element");
    result.Length.ShouldBeGreaterThan(50);

    await Task.CompletedTask;
  }

  public static async Task Should_get_all_syntax()
  {
    // Arrange & Act
    string result = GetSyntaxTool.GetSyntax("all");

    // Assert
    result.ShouldContain("Route Pattern Syntax Reference");
    result.Length.ShouldBeGreaterThan(500);

    await Task.CompletedTask;
  }

  [Input("param")]
  [Input("opt")]
  [Input("catch")]
  public static async Task Should_support_partial_matching(string partial)
  {
    // Arrange & Act
    string result = GetSyntaxTool.GetSyntax(partial);

    // Assert
    result.ShouldNotContain("Unknown syntax element");
    result.Length.ShouldBeGreaterThan(50);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_unknown_syntax_element()
  {
    // Arrange & Act
    string result = GetSyntaxTool.GetSyntax("foobar");

    // Assert
    result.ShouldContain("Unknown syntax element");
    result.ShouldContain("Available elements");

    await Task.CompletedTask;
  }

  [Input("basic")]
  [Input("typed")]
  [Input("optional")]
  [Input("catchall")]
  [Input("options")]
  [Input("complex")]
  public static async Task Should_get_pattern_examples(string feature)
  {
    // Arrange & Act
    string result = GetSyntaxTool.GetPatternExamples(feature);

    // Assert
    result.ShouldNotBeNullOrEmpty();
    result.ShouldNotContain("Unknown feature");
    result.ShouldContain("```csharp");
    result.Length.ShouldBeGreaterThan(100);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_unknown_pattern_feature()
  {
    // Arrange & Act
    string result = GetSyntaxTool.GetPatternExamples("nonexistent");

    // Assert
    result.ShouldContain("Unknown feature");

    await Task.CompletedTask;
  }

  public static async Task Should_return_literals_syntax_details()
  {
    // Arrange & Act
    string result = GetSyntaxTool.GetSyntax("literals");

    // Assert
    result.ShouldContain("status");
    result.ShouldContain("git commit");

    await Task.CompletedTask;
  }

  public static async Task Should_return_parameters_syntax_details()
  {
    // Arrange & Act
    string result = GetSyntaxTool.GetSyntax("parameters");

    // Assert
    result.ShouldContain("{name}");

    await Task.CompletedTask;
  }

  public static async Task Should_return_types_syntax_details()
  {
    // Arrange & Act
    string result = GetSyntaxTool.GetSyntax("types");

    // Assert
    result.ShouldContain(":int");
    result.ShouldContain(":double");
    result.ShouldContain(":bool");

    await Task.CompletedTask;
  }

  public static async Task Should_return_optional_syntax_details()
  {
    // Arrange & Act
    string result = GetSyntaxTool.GetSyntax("optional");

    // Assert
    result.ShouldContain("?");
    result.ShouldContain("nullable");

    await Task.CompletedTask;
  }

  public static async Task Should_return_catchall_syntax_details()
  {
    // Arrange & Act
    string result = GetSyntaxTool.GetSyntax("catchall");

    // Assert
    result.ShouldContain("{*args}");
    result.ShouldContain("array");

    await Task.CompletedTask;
  }

  public static async Task Should_return_options_syntax_details()
  {
    // Arrange & Act
    string result = GetSyntaxTool.GetSyntax("options");

    // Assert
    result.ShouldContain("--");
    result.ShouldContain("-");

    await Task.CompletedTask;
  }

  public static async Task Should_return_descriptions_syntax_details()
  {
    // Arrange & Act
    string result = GetSyntaxTool.GetSyntax("descriptions");

    // Assert
    result.ShouldContain("|");
    result.ShouldContain("Description");

    await Task.CompletedTask;
  }
}
