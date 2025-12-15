#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Tests for CompiledRouteBuilder
// Verifies that builder-constructed routes match PatternParser.Parse() output

using System.Globalization;
using System.Text;
using TimeWarp.Jaribu;
using TimeWarp.Nuru;
using Shouldly;
using static System.Console;
using static TimeWarp.Jaribu.TestRunner;

return await RunTests<CompiledRouteBuilderTests>();

/// <summary>
/// Tests for CompiledRouteBuilder - verifies parity with PatternParser.Parse()
/// </summary>
[TestTag("CompiledRouteBuilder")]
public sealed class CompiledRouteBuilderTests
{
  // ============================================================================
  // Helper Methods for Comparing CompiledRoute Instances
  // ============================================================================

  /// <summary>
  /// Compares two CompiledRoute instances for equality.
  /// Returns a detailed error message if they differ, or null if they match.
  /// </summary>
  private static string? CompareRoutes(CompiledRoute expected, CompiledRoute actual, string context)
  {
    StringBuilder errors = new();

    // Compare Specificity
    if (expected.Specificity != actual.Specificity)
    {
      errors.AppendLine(CultureInfo.InvariantCulture, $"[{context}] Specificity mismatch: expected {expected.Specificity}, got {actual.Specificity}");
    }

    // Compare CatchAllParameterName
    if (expected.CatchAllParameterName != actual.CatchAllParameterName)
    {
      errors.AppendLine(CultureInfo.InvariantCulture, $"[{context}] CatchAllParameterName mismatch: expected '{expected.CatchAllParameterName}', got '{actual.CatchAllParameterName}'");
    }

    // Compare Segments count
    if (expected.Segments.Count != actual.Segments.Count)
    {
      errors.AppendLine(CultureInfo.InvariantCulture, $"[{context}] Segment count mismatch: expected {expected.Segments.Count}, got {actual.Segments.Count}");
      return errors.ToString();
    }

    // Compare each segment
    for (int i = 0; i < expected.Segments.Count; i++)
    {
      string? segmentError = CompareSegments(expected.Segments[i], actual.Segments[i], $"{context}[{i}]");
      if (segmentError is not null)
      {
        errors.AppendLine(segmentError);
      }
    }

    return errors.Length > 0 ? errors.ToString() : null;
  }

  /// <summary>
  /// Compares two RouteMatcher segments for equality.
  /// </summary>
  private static string? CompareSegments(RouteMatcher expected, RouteMatcher actual, string context)
  {
    // Check type match first
    if (expected.GetType() != actual.GetType())
    {
      return $"[{context}] Segment type mismatch: expected {expected.GetType().Name}, got {actual.GetType().Name}";
    }

    return expected switch
    {
      LiteralMatcher expectedLiteral => CompareLiteralMatchers(expectedLiteral, (LiteralMatcher)actual, context),
      ParameterMatcher expectedParam => CompareParameterMatchers(expectedParam, (ParameterMatcher)actual, context),
      OptionMatcher expectedOption => CompareOptionMatchers(expectedOption, (OptionMatcher)actual, context),
      _ => $"[{context}] Unknown segment type: {expected.GetType().Name}"
    };
  }

  private static string? CompareLiteralMatchers(LiteralMatcher expected, LiteralMatcher actual, string context)
  {
    if (expected.Value != actual.Value)
    {
      return $"[{context}] LiteralMatcher.Value mismatch: expected '{expected.Value}', got '{actual.Value}'";
    }

    return null;
  }

  private static string? CompareParameterMatchers(ParameterMatcher expected, ParameterMatcher actual, string context)
  {
    StringBuilder errors = new();

    if (expected.Name != actual.Name)
    {
      errors.AppendLine(CultureInfo.InvariantCulture, $"[{context}] ParameterMatcher.Name mismatch: expected '{expected.Name}', got '{actual.Name}'");
    }

    if (expected.IsCatchAll != actual.IsCatchAll)
    {
      errors.AppendLine(CultureInfo.InvariantCulture, $"[{context}] ParameterMatcher.IsCatchAll mismatch: expected {expected.IsCatchAll}, got {actual.IsCatchAll}");
    }

    if (expected.Constraint != actual.Constraint)
    {
      errors.AppendLine(CultureInfo.InvariantCulture, $"[{context}] ParameterMatcher.Constraint mismatch: expected '{expected.Constraint}', got '{actual.Constraint}'");
    }

    if (expected.IsOptional != actual.IsOptional)
    {
      errors.AppendLine(CultureInfo.InvariantCulture, $"[{context}] ParameterMatcher.IsOptional mismatch: expected {expected.IsOptional}, got {actual.IsOptional}");
    }

    // Note: Description comparison is optional - parser may not preserve descriptions

    return errors.Length > 0 ? errors.ToString() : null;
  }

  private static string? CompareOptionMatchers(OptionMatcher expected, OptionMatcher actual, string context)
  {
    StringBuilder errors = new();

    if (expected.MatchPattern != actual.MatchPattern)
    {
      errors.AppendLine(CultureInfo.InvariantCulture, $"[{context}] OptionMatcher.MatchPattern mismatch: expected '{expected.MatchPattern}', got '{actual.MatchPattern}'");
    }

    if (expected.ExpectsValue != actual.ExpectsValue)
    {
      errors.AppendLine(CultureInfo.InvariantCulture, $"[{context}] OptionMatcher.ExpectsValue mismatch: expected {expected.ExpectsValue}, got {actual.ExpectsValue}");
    }

    if (expected.ParameterName != actual.ParameterName)
    {
      errors.AppendLine(CultureInfo.InvariantCulture, $"[{context}] OptionMatcher.ParameterName mismatch: expected '{expected.ParameterName}', got '{actual.ParameterName}'");
    }

    if (expected.AlternateForm != actual.AlternateForm)
    {
      errors.AppendLine(CultureInfo.InvariantCulture, $"[{context}] OptionMatcher.AlternateForm mismatch: expected '{expected.AlternateForm}', got '{actual.AlternateForm}'");
    }

    if (expected.IsOptional != actual.IsOptional)
    {
      errors.AppendLine(CultureInfo.InvariantCulture, $"[{context}] OptionMatcher.IsOptional mismatch: expected {expected.IsOptional}, got {actual.IsOptional}");
    }

    if (expected.IsRepeated != actual.IsRepeated)
    {
      errors.AppendLine(CultureInfo.InvariantCulture, $"[{context}] OptionMatcher.IsRepeated mismatch: expected {expected.IsRepeated}, got {actual.IsRepeated}");
    }

    if (expected.ParameterIsOptional != actual.ParameterIsOptional)
    {
      errors.AppendLine(CultureInfo.InvariantCulture, $"[{context}] OptionMatcher.ParameterIsOptional mismatch: expected {expected.ParameterIsOptional}, got {actual.ParameterIsOptional}");
    }

    // Note: Description comparison is optional

    return errors.Length > 0 ? errors.ToString() : null;
  }

  /// <summary>
  /// Asserts that a builder-constructed route matches the parsed route.
  /// </summary>
  private static void AssertRoutesMatch(string pattern, CompiledRoute builderRoute)
  {
    CompiledRoute parsedRoute = PatternParser.Parse(pattern);
    string? error = CompareRoutes(parsedRoute, builderRoute, pattern);
    if (error is not null)
    {
      throw new ShouldAssertException($"Routes do not match for pattern '{pattern}':\n{error}");
    }
  }

  // ============================================================================
  // Test Cases
  // ============================================================================

  /// <summary>
  /// Test: Simple literal route ("greet")
  /// </summary>
  public static async Task Should_build_simple_literal_route()
  {
    // Arrange
    const string pattern = "greet";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("greet")
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Literal + parameter ("greet {name}")
  /// </summary>
  public static async Task Should_build_literal_with_parameter()
  {
    // Arrange
    const string pattern = "greet {name}";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("greet")
      .WithParameter("name")
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Multiple literals ("git commit")
  /// </summary>
  public static async Task Should_build_multiple_literals()
  {
    // Arrange
    const string pattern = "git commit";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("git")
      .WithLiteral("commit")
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Optional parameter ("greet {name?}")
  /// </summary>
  public static async Task Should_build_optional_parameter()
  {
    // Arrange
    const string pattern = "greet {name?}";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("greet")
      .WithOptionalParameter("name")
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Typed parameter ("add {x:int} {y:int}")
  /// </summary>
  public static async Task Should_build_typed_parameters()
  {
    // Arrange
    const string pattern = "add {x:int} {y:int}";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("add")
      .WithParameter("x", type: "int")
      .WithParameter("y", type: "int")
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Boolean flag option ("deploy --force")
  /// </summary>
  public static async Task Should_build_boolean_flag_option()
  {
    // Arrange
    const string pattern = "deploy --force";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("deploy")
      .WithOption("force")
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Option with short form ("deploy --force,-f")
  /// </summary>
  public static async Task Should_build_option_with_short_form()
  {
    // Arrange
    const string pattern = "deploy --force,-f";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("deploy")
      .WithOption("force", shortForm: "f")
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Option with value ("deploy --config {file}")
  /// </summary>
  public static async Task Should_build_option_with_value()
  {
    // Arrange
    const string pattern = "deploy --config {file}";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("deploy")
      .WithOption("config", parameterName: "file", expectsValue: true)
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Catch-all parameter ("exec {*args}")
  /// </summary>
  public static async Task Should_build_catch_all_parameter()
  {
    // Arrange
    const string pattern = "exec {*args}";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("exec")
      .WithCatchAll("args")
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Complex route ("deploy {env} --force,-f --config,-c {file?}")
  /// </summary>
  public static async Task Should_build_complex_route()
  {
    // Arrange
    const string pattern = "deploy {env} --force,-f --config,-c {file?}";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("deploy")
      .WithParameter("env")
      .WithOption("force", shortForm: "f")
      .WithOption("config", shortForm: "c", parameterName: "file", expectsValue: true, parameterIsOptional: true)
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Only catch-all disallows multiple
  /// </summary>
  public static async Task Should_throw_on_multiple_catch_all()
  {
    // Arrange & Act & Assert
    Should.Throw<InvalidOperationException>(() =>
    {
      new CompiledRouteBuilder()
        .WithCatchAll("args1")
        .WithCatchAll("args2")
        .Build();
    });

    WriteLine("PASS: Multiple catch-all throws InvalidOperationException");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Typed option value ("server --port {port:int}")
  /// </summary>
  public static async Task Should_build_typed_option_value()
  {
    // Arrange
    const string pattern = "server --port {port:int}";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("server")
      .WithOption("port", parameterName: "port", expectsValue: true, parameterType: "int")
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test: Optional flag modifier ("deploy --verbose?")
  /// </summary>
  public static async Task Should_build_optional_flag()
  {
    // Arrange
    const string pattern = "deploy --verbose?";
    CompiledRoute builderRoute = new CompiledRouteBuilder()
      .WithLiteral("deploy")
      .WithOption("verbose", isOptionalFlag: true)
      .Build();

    // Act & Assert
    AssertRoutesMatch(pattern, builderRoute);
    WriteLine($"PASS: {pattern}");

    await Task.CompletedTask;
  }
}
