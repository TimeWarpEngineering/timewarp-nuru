namespace TimeWarp.Nuru.Tests.CompiledRouteTests;

using System.Globalization;
using System.Text;
using Shouldly;

/// <summary>
/// Helper methods for comparing CompiledRoute instances in tests.
/// </summary>
public static class CompiledRouteTestHelper
{
  /// <summary>
  /// Compares two CompiledRoute instances for equality.
  /// Returns a detailed error message if they differ, or null if they match.
  /// </summary>
  public static string? CompareRoutes(CompiledRoute expected, CompiledRoute actual, string context)
  {
    ArgumentNullException.ThrowIfNull(expected);
    ArgumentNullException.ThrowIfNull(actual);

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
  public static string? CompareSegments(RouteMatcher expected, RouteMatcher actual, string context)
  {
    ArgumentNullException.ThrowIfNull(expected);
    ArgumentNullException.ThrowIfNull(actual);

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

  /// <summary>
  /// Compares two LiteralMatcher instances for equality.
  /// </summary>
  public static string? CompareLiteralMatchers(LiteralMatcher expected, LiteralMatcher actual, string context)
  {
    ArgumentNullException.ThrowIfNull(expected);
    ArgumentNullException.ThrowIfNull(actual);

    if (expected.Value != actual.Value)
    {
      return $"[{context}] LiteralMatcher.Value mismatch: expected '{expected.Value}', got '{actual.Value}'";
    }

    return null;
  }

  /// <summary>
  /// Compares two ParameterMatcher instances for equality.
  /// </summary>
  public static string? CompareParameterMatchers(ParameterMatcher expected, ParameterMatcher actual, string context)
  {
    ArgumentNullException.ThrowIfNull(expected);
    ArgumentNullException.ThrowIfNull(actual);

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

  /// <summary>
  /// Compares two OptionMatcher instances for equality.
  /// </summary>
  public static string? CompareOptionMatchers(OptionMatcher expected, OptionMatcher actual, string context)
  {
    ArgumentNullException.ThrowIfNull(expected);
    ArgumentNullException.ThrowIfNull(actual);

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
  /// <param name="pattern">The route pattern string to parse for comparison.</param>
  /// <param name="builderRoute">The route constructed via CompiledRouteBuilder.</param>
  /// <exception cref="ShouldAssertException">Thrown if the routes do not match.</exception>
  public static void AssertRoutesMatch(string pattern, CompiledRoute builderRoute)
  {
    CompiledRoute parsedRoute = PatternParser.Parse(pattern);
    string? error = CompareRoutes(parsedRoute, builderRoute, pattern);
    if (error is not null)
    {
      throw new ShouldAssertException($"Routes do not match for pattern '{pattern}':\n{error}");
    }
  }
}
