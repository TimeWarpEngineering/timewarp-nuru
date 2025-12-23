// sandbox/sourcegen/tests/segment-from-compiled-route-tests.cs
// Tests for SegmentDefinitionConverter.FromCompiledRoute()
// Uses public CompiledRoute types - documents known gaps.
//
// Agent: Amina
// Task: #242-step-3

namespace TimeWarp.Nuru.SourceGen.Tests;

using System.Collections.Immutable;
using TimeWarp.Nuru;

/// <summary>
/// Tests for SegmentDefinitionConverter.FromCompiledRoute().
/// These tests document the gaps when converting from the public CompiledRoute type.
/// Key gap: Option parameter type constraints are lost.
/// </summary>
public static class SegmentFromCompiledRouteTests
{
  public static int Run()
  {
    Console.WriteLine("=== SegmentDefinitionConverter.FromCompiledRoute() Tests ===");
    Console.WriteLine("NOTE: This approach loses option type constraints.");
    Console.WriteLine();

    int passed = 0;
    int failed = 0;

    // Test each pattern
    passed += TestLiteralAndTypedParameters(ref failed);
    passed += TestUntypedParameter(ref failed);
    passed += TestOptionalParameter(ref failed);
    passed += TestCatchAllParameter(ref failed);
    passed += TestBooleanFlagOption(ref failed);
    passed += TestOptionWithTypedParameter_DocumentsGap(ref failed);
    passed += TestShortAndLongFormOption(ref failed);

    Console.WriteLine();
    Console.WriteLine("==============================================");
    Console.WriteLine($"Results: {passed} passed, {failed} failed");
    Console.WriteLine("==============================================");

    return failed > 0 ? 1 : 0;
  }

  /// <summary>
  /// Test: "add {x:int} {y:int}" - Literal + typed parameters
  /// WORKS: ParameterMatcher preserves type constraint.
  /// </summary>
  private static int TestLiteralAndTypedParameters(ref int failed)
  {
    Console.WriteLine("Test: \"add {x:int} {y:int}\"");

    try
    {
      CompiledRoute route = PatternParser.Parse("add {x:int} {y:int}");
      ImmutableArray<SegmentDefinition> segments = SegmentDefinitionConverter.FromCompiledRoute(route.Segments);

      AssertEquals(3, segments.Length, "segment count");

      // Segment 0: Literal "add"
      AssertTrue(segments[0] is LiteralDefinition, "segment 0 is LiteralDefinition");
      LiteralDefinition lit = (LiteralDefinition)segments[0];
      AssertEquals("add", lit.Value, "literal value");

      // Segment 1: Parameter {x:int}
      ParameterDefinition paramX = (ParameterDefinition)segments[1];
      AssertEquals("x", paramX.Name, "param x name");
      AssertEquals("int", paramX.TypeConstraint, "param x type constraint (WORKS for parameters)");
      AssertEquals("global::System.Int32", paramX.ResolvedClrTypeName, "param x CLR type");

      // Segment 2: Parameter {y:int}
      ParameterDefinition paramY = (ParameterDefinition)segments[2];
      AssertEquals("y", paramY.Name, "param y name");
      AssertEquals("int", paramY.TypeConstraint, "param y type constraint");

      Console.WriteLine("  PASSED");
      return 1;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"  FAILED: {ex.Message}");
      failed++;
      return 0;
    }
  }

  /// <summary>
  /// Test: "greet {name}" - Untyped parameter (defaults to string)
  /// </summary>
  private static int TestUntypedParameter(ref int failed)
  {
    Console.WriteLine("Test: \"greet {name}\"");

    try
    {
      CompiledRoute route = PatternParser.Parse("greet {name}");
      ImmutableArray<SegmentDefinition> segments = SegmentDefinitionConverter.FromCompiledRoute(route.Segments);

      ParameterDefinition param = (ParameterDefinition)segments[1];
      AssertEquals("name", param.Name, "param name");
      AssertEquals(null, param.TypeConstraint, "param type constraint");
      AssertEquals("global::System.String", param.ResolvedClrTypeName, "param CLR type");

      Console.WriteLine("  PASSED");
      return 1;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"  FAILED: {ex.Message}");
      failed++;
      return 0;
    }
  }

  /// <summary>
  /// Test: "copy {source} {dest?}" - Optional parameter
  /// </summary>
  private static int TestOptionalParameter(ref int failed)
  {
    Console.WriteLine("Test: \"copy {source} {dest?}\"");

    try
    {
      CompiledRoute route = PatternParser.Parse("copy {source} {dest?}");
      ImmutableArray<SegmentDefinition> segments = SegmentDefinitionConverter.FromCompiledRoute(route.Segments);

      ParameterDefinition paramDest = (ParameterDefinition)segments[2];
      AssertEquals("dest", paramDest.Name, "dest name");
      AssertEquals(true, paramDest.IsOptional, "dest optional");

      Console.WriteLine("  PASSED");
      return 1;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"  FAILED: {ex.Message}");
      failed++;
      return 0;
    }
  }

  /// <summary>
  /// Test: "echo {*args}" - Catch-all parameter
  /// </summary>
  private static int TestCatchAllParameter(ref int failed)
  {
    Console.WriteLine("Test: \"echo {*args}\"");

    try
    {
      CompiledRoute route = PatternParser.Parse("echo {*args}");
      ImmutableArray<SegmentDefinition> segments = SegmentDefinitionConverter.FromCompiledRoute(route.Segments);

      ParameterDefinition param = (ParameterDefinition)segments[1];
      AssertEquals("args", param.Name, "param name");
      AssertEquals(true, param.IsCatchAll, "param is catch-all");

      Console.WriteLine("  PASSED");
      return 1;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"  FAILED: {ex.Message}");
      failed++;
      return 0;
    }
  }

  /// <summary>
  /// Test: "status --verbose?" - Optional boolean flag option
  /// </summary>
  private static int TestBooleanFlagOption(ref int failed)
  {
    Console.WriteLine("Test: \"status --verbose?\"");

    try
    {
      CompiledRoute route = PatternParser.Parse("status --verbose?");
      ImmutableArray<SegmentDefinition> segments = SegmentDefinitionConverter.FromCompiledRoute(route.Segments);

      OptionDefinition option = (OptionDefinition)segments[1];
      AssertEquals("--verbose", option.LongForm, "option long form (includes --)");
      AssertEquals(false, option.ExpectsValue, "option expects value (flag = false)");
      AssertEquals("global::System.Boolean", option.ResolvedClrTypeName, "option CLR type");

      Console.WriteLine("  PASSED");
      return 1;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"  FAILED: {ex.Message}");
      failed++;
      return 0;
    }
  }

  /// <summary>
  /// Test: "config --value {v:int}" - Option with typed parameter
  /// 
  /// THIS TEST DOCUMENTS THE GAP:
  /// - OptionMatcher.ParameterName = "v" (preserved)
  /// - OptionMatcher has NO Constraint property (type lost!)
  /// - We default to string, but the actual type was int
  /// </summary>
  private static int TestOptionWithTypedParameter_DocumentsGap(ref int failed)
  {
    Console.WriteLine("Test: \"config --value {v:int}\" (DOCUMENTS GAP: option type constraint lost)");

    try
    {
      CompiledRoute route = PatternParser.Parse("config --value {v:int}");
      ImmutableArray<SegmentDefinition> segments = SegmentDefinitionConverter.FromCompiledRoute(route.Segments);

      OptionDefinition option = (OptionDefinition)segments[1];
      AssertEquals("--value", option.LongForm, "option long form");
      AssertEquals(true, option.ExpectsValue, "option expects value");
      AssertEquals("v", option.ParameterName, "option parameter name (preserved)");

      // GAP: Type constraint is NOT preserved!
      Console.WriteLine();
      Console.WriteLine("    === DOCUMENTED GAP ===");
      Console.WriteLine($"    TypeConstraint: {option.TypeConstraint ?? "(null)"} -- SHOULD BE 'int'");
      Console.WriteLine($"    ResolvedClrTypeName: {option.ResolvedClrTypeName} -- SHOULD BE 'global::System.Int32'");
      Console.WriteLine("    OptionMatcher does not expose the type constraint.");
      Console.WriteLine("    This is why FromSyntax() is preferred for source generators.");
      Console.WriteLine("    === END GAP ===");
      Console.WriteLine();

      // These assertions document the gap (expected vs actual)
      AssertEquals(null, option.TypeConstraint, "option type constraint (GAP: lost, should be 'int')");
      AssertEquals("global::System.String", option.ResolvedClrTypeName, "option CLR type (GAP: defaults to string)");

      Console.WriteLine("  PASSED (gap documented)");
      return 1;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"  FAILED: {ex.Message}");
      failed++;
      return 0;
    }
  }

  /// <summary>
  /// Test: "run --force,-f" - Short and long form option (alias syntax)
  /// </summary>
  private static int TestShortAndLongFormOption(ref int failed)
  {
    Console.WriteLine("Test: \"run --force,-f\"");

    try
    {
      CompiledRoute route = PatternParser.Parse("run --force,-f");
      ImmutableArray<SegmentDefinition> segments = SegmentDefinitionConverter.FromCompiledRoute(route.Segments);

      OptionDefinition option = (OptionDefinition)segments[1];
      AssertEquals("--force", option.LongForm, "option long form (includes --)");
      AssertEquals("-f", option.ShortForm, "option short form (includes -)");

      Console.WriteLine("  PASSED");
      return 1;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"  FAILED: {ex.Message}");
      failed++;
      return 0;
    }
  }

  #region Helpers

  private static void AssertEquals<T>(T expected, T actual, string description)
  {
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
      throw new Exception($"{description}: expected '{expected}', got '{actual}'");
    }
    Console.WriteLine($"    {description}: {actual}");
  }

  private static void AssertTrue(bool condition, string description)
  {
    if (!condition)
    {
      throw new Exception($"{description}: expected true");
    }
    Console.WriteLine($"    {description}: true");
  }

  #endregion
}
