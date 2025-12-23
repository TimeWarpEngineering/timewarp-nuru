// sandbox/sourcegen/tests/segment-from-syntax-tests.cs
// Tests for SegmentDefinitionConverter.FromSyntax()
// Uses internal Syntax types - full fidelity conversion.
//
// Agent: Amina
// Task: #242-step-3

namespace TimeWarp.Nuru.SourceGen.Tests;

using System.Collections.Immutable;
using TimeWarp.Nuru;

/// <summary>
/// Tests for SegmentDefinitionConverter.FromSyntax().
/// These tests verify that all segment information is preserved when converting
/// from the internal Syntax types.
/// </summary>
public static class SegmentFromSyntaxTests
{
  public static int Run()
  {
    Console.WriteLine("=== SegmentDefinitionConverter.FromSyntax() Tests ===");
    Console.WriteLine();

    int passed = 0;
    int failed = 0;

    // Test each pattern
    passed += TestLiteralAndTypedParameters(ref failed);
    passed += TestUntypedParameter(ref failed);
    passed += TestOptionalParameter(ref failed);
    passed += TestCatchAllParameter(ref failed);
    passed += TestBooleanFlagOption(ref failed);
    passed += TestOptionWithTypedParameter(ref failed);
    passed += TestShortAndLongFormOption(ref failed);
    passed += TestOptionalOptionWithOptionalTypedParameter(ref failed);

    Console.WriteLine();
    Console.WriteLine("==============================================");
    Console.WriteLine($"Results: {passed} passed, {failed} failed");
    Console.WriteLine("==============================================");

    return failed > 0 ? 1 : 0;
  }

  /// <summary>
  /// Test: "add {x:int} {y:int}" - Literal + typed parameters
  /// </summary>
  private static int TestLiteralAndTypedParameters(ref int failed)
  {
    Console.WriteLine("Test: \"add {x:int} {y:int}\"");

    try
    {
      Syntax syntax = Parse("add {x:int} {y:int}");
      ImmutableArray<SegmentDefinition> segments = SegmentDefinitionConverter.FromSyntax(syntax.Segments);

      AssertEquals(3, segments.Length, "segment count");

      // Segment 0: Literal "add"
      AssertTrue(segments[0] is LiteralDefinition, "segment 0 is LiteralDefinition");
      LiteralDefinition lit = (LiteralDefinition)segments[0];
      AssertEquals(0, lit.Position, "literal position");
      AssertEquals("add", lit.Value, "literal value");

      // Segment 1: Parameter {x:int}
      AssertTrue(segments[1] is ParameterDefinition, "segment 1 is ParameterDefinition");
      ParameterDefinition paramX = (ParameterDefinition)segments[1];
      AssertEquals(1, paramX.Position, "param x position");
      AssertEquals("x", paramX.Name, "param x name");
      AssertEquals("int", paramX.TypeConstraint, "param x type constraint");
      AssertEquals("global::System.Int32", paramX.ResolvedClrTypeName, "param x CLR type");
      AssertEquals(false, paramX.IsOptional, "param x optional");
      AssertEquals(false, paramX.IsCatchAll, "param x catch-all");

      // Segment 2: Parameter {y:int}
      AssertTrue(segments[2] is ParameterDefinition, "segment 2 is ParameterDefinition");
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
      Syntax syntax = Parse("greet {name}");
      ImmutableArray<SegmentDefinition> segments = SegmentDefinitionConverter.FromSyntax(syntax.Segments);

      AssertEquals(2, segments.Length, "segment count");

      ParameterDefinition param = (ParameterDefinition)segments[1];
      AssertEquals("name", param.Name, "param name");
      AssertEquals(null, param.TypeConstraint, "param type constraint (null = untyped)");
      AssertEquals("global::System.String", param.ResolvedClrTypeName, "param CLR type (defaults to string)");

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
      Syntax syntax = Parse("copy {source} {dest?}");
      ImmutableArray<SegmentDefinition> segments = SegmentDefinitionConverter.FromSyntax(syntax.Segments);

      AssertEquals(3, segments.Length, "segment count");

      ParameterDefinition paramSource = (ParameterDefinition)segments[1];
      AssertEquals("source", paramSource.Name, "source name");
      AssertEquals(false, paramSource.IsOptional, "source optional");

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
      Syntax syntax = Parse("echo {*args}");
      ImmutableArray<SegmentDefinition> segments = SegmentDefinitionConverter.FromSyntax(syntax.Segments);

      AssertEquals(2, segments.Length, "segment count");

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
      Syntax syntax = Parse("status --verbose?");
      ImmutableArray<SegmentDefinition> segments = SegmentDefinitionConverter.FromSyntax(syntax.Segments);

      AssertEquals(2, segments.Length, "segment count");

      AssertTrue(segments[1] is OptionDefinition, "segment 1 is OptionDefinition");
      OptionDefinition option = (OptionDefinition)segments[1];
      AssertEquals("verbose", option.LongForm, "option long form");
      AssertEquals(false, option.ExpectsValue, "option expects value (flag = false)");
      AssertEquals(true, option.IsOptional, "option is optional");
      AssertEquals("global::System.Boolean", option.ResolvedClrTypeName, "option CLR type (flag = bool)");

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
  /// THIS IS THE KEY TEST: Syntax preserves type constraint, CompiledRoute loses it.
  /// </summary>
  private static int TestOptionWithTypedParameter(ref int failed)
  {
    Console.WriteLine("Test: \"config --value {v:int}\" (KEY TEST: option type constraint)");

    try
    {
      Syntax syntax = Parse("config --value {v:int}");
      ImmutableArray<SegmentDefinition> segments = SegmentDefinitionConverter.FromSyntax(syntax.Segments);

      AssertEquals(2, segments.Length, "segment count");

      OptionDefinition option = (OptionDefinition)segments[1];
      AssertEquals("value", option.LongForm, "option long form");
      AssertEquals(true, option.ExpectsValue, "option expects value");
      AssertEquals("v", option.ParameterName, "option parameter name");

      // KEY ASSERTION: Type constraint is preserved!
      AssertEquals("int", option.TypeConstraint, "option type constraint (MUST be preserved)");
      AssertEquals("global::System.Int32", option.ResolvedClrTypeName, "option CLR type");

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
  /// Test: "run --force,-f" - Short and long form option (alias syntax)
  /// </summary>
  private static int TestShortAndLongFormOption(ref int failed)
  {
    Console.WriteLine("Test: \"run --force,-f\"");

    try
    {
      Syntax syntax = Parse("run --force,-f");
      ImmutableArray<SegmentDefinition> segments = SegmentDefinitionConverter.FromSyntax(syntax.Segments);

      AssertEquals(2, segments.Length, "segment count");

      OptionDefinition option = (OptionDefinition)segments[1];
      AssertEquals("force", option.LongForm, "option long form");
      AssertEquals("f", option.ShortForm, "option short form");

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
  /// Test: "log --level {l:int?}" - Option with optional typed parameter
  /// Note: The ? inside the braces {l:int?} makes the parameter optional
  /// </summary>
  private static int TestOptionalOptionWithOptionalTypedParameter(ref int failed)
  {
    Console.WriteLine("Test: \"log --level {l:int?}\"");

    try
    {
      Syntax syntax = Parse("log --level {l:int?}");
      ImmutableArray<SegmentDefinition> segments = SegmentDefinitionConverter.FromSyntax(syntax.Segments);

      AssertEquals(2, segments.Length, "segment count");

      OptionDefinition option = (OptionDefinition)segments[1];
      AssertEquals("level", option.LongForm, "option long form");
      AssertEquals(true, option.ExpectsValue, "option expects value");
      AssertEquals(true, option.ParameterIsOptional, "option parameter is optional");
      AssertEquals("int?", option.TypeConstraint, "option type constraint (includes ? for optional)");

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

  /// <summary>
  /// Parse a pattern and return the Syntax AST.
  /// Uses internal parser API.
  /// </summary>
  private static Syntax Parse(string pattern)
  {
    Parser parser = new();
    ParseResult<Syntax> result = parser.Parse(pattern);

    if (!result.Success)
    {
      string errors = string.Join(", ", result.ParseErrors.Select(e => e.ToString()));
      throw new Exception($"Parse failed: {errors}");
    }

    return result.Value!;
  }

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
