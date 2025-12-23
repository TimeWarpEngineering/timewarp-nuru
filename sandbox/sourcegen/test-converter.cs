// sandbox/sourcegen/test-converter.cs
// Test: Verify CompiledRouteToRouteDefinition produces the expected RouteDefinition
//
// Agent: Amina
// Task: #242-step-3
//
// This test:
// 1. Parses "add {x:int} {y:int}" using the public PatternParser API
// 2. Converts to RouteDefinition using our converter
// 3. Compares key properties to the expected values from step-1
//
// Run with: dotnet run --project sandbox/sourcegen/test-converter.csproj

namespace TimeWarp.Nuru.SourceGen.Tests;

using System.Collections.Immutable;

public static class TestConverter
{
  public const string Pattern = "add {x:int} {y:int}";

  public static int Run()
  {
    Console.WriteLine("=== Test: CompiledRouteToRouteDefinition ===");
    Console.WriteLine();

    // Step 1: Parse the pattern using public API
    Console.WriteLine($"Parsing pattern: \"{Pattern}\"");
    CompiledRoute compiledRoute = PatternParser.Parse(Pattern);
    Console.WriteLine($"  Segments: {compiledRoute.Segments.Count}");
    Console.WriteLine($"  MessageType: {compiledRoute.MessageType}");
    Console.WriteLine($"  Specificity: {compiledRoute.Specificity}");
    Console.WriteLine();

    // Verify the CompiledRoute has what we expect
    Console.WriteLine("Verifying CompiledRoute structure...");
    AssertEquals(3, compiledRoute.Segments.Count, "segment count");
    AssertTrue(compiledRoute.Segments[0] is LiteralMatcher, "segment 0 is literal");
    AssertTrue(compiledRoute.Segments[1] is ParameterMatcher, "segment 1 is parameter");
    AssertTrue(compiledRoute.Segments[2] is ParameterMatcher, "segment 2 is parameter");

    LiteralMatcher literal = (LiteralMatcher)compiledRoute.Segments[0];
    AssertEquals("add", literal.Value, "literal value");

    ParameterMatcher paramX = (ParameterMatcher)compiledRoute.Segments[1];
    AssertEquals("x", paramX.Name, "param x name");
    AssertEquals("int", paramX.Constraint, "param x constraint");
    AssertEquals(false, paramX.IsOptional, "param x optional");
    AssertEquals(false, paramX.IsCatchAll, "param x catch-all");

    ParameterMatcher paramY = (ParameterMatcher)compiledRoute.Segments[2];
    AssertEquals("y", paramY.Name, "param y name");
    AssertEquals("int", paramY.Constraint, "param y constraint");
    AssertEquals(false, paramY.IsOptional, "param y optional");
    AssertEquals(false, paramY.IsCatchAll, "param y catch-all");

    Console.WriteLine("  All CompiledRoute assertions passed!");
    Console.WriteLine();

    // Step 2: Create a minimal handler definition for the test
    Console.WriteLine("Creating handler definition...");
    HandlerDefinition handler = HandlerDefinition.ForDelegate
    (
      parameters:
      [
        ParameterBinding.FromParameter
        (
          parameterName: "x",
          typeName: "global::System.Int32",
          segmentName: "x",
          isOptional: false,
          defaultValue: null,
          requiresConversion: true
        ),
        ParameterBinding.FromParameter
        (
          parameterName: "y",
          typeName: "global::System.Int32",
          segmentName: "y",
          isOptional: false,
          defaultValue: null,
          requiresConversion: true
        )
      ],
      returnType: HandlerReturnType.Of("global::System.Int32", "int"),
      isAsync: false,
      requiresCancellationToken: false
    );
    Console.WriteLine("  Handler definition created.");
    Console.WriteLine();

    // Step 3: Convert to RouteDefinition
    Console.WriteLine("Converting to RouteDefinition...");
    RouteDefinition routeDefinition = CompiledRouteToRouteDefinition.Convert
    (
      compiledRoute,
      Pattern,
      handler,
      description: "Add two integers"
    );
    Console.WriteLine($"  OriginalPattern: \"{routeDefinition.OriginalPattern}\"");
    Console.WriteLine($"  MessageType: {routeDefinition.MessageType}");
    Console.WriteLine($"  Description: {routeDefinition.Description}");
    Console.WriteLine($"  Segments: {routeDefinition.Segments.Length}");
    Console.WriteLine();

    // Step 4: Verify RouteDefinition matches expected values
    Console.WriteLine("Verifying RouteDefinition structure...");
    AssertEquals(Pattern, routeDefinition.OriginalPattern, "original pattern");
    AssertEquals("Command", routeDefinition.MessageType, "message type"); // Default is Command
    AssertEquals("Add two integers", routeDefinition.Description, "description");
    AssertEquals(3, routeDefinition.Segments.Length, "segment count");

    // Check segment 0: Literal "add"
    AssertTrue(routeDefinition.Segments[0] is LiteralDefinition, "segment 0 is LiteralDefinition");
    LiteralDefinition litDef = (LiteralDefinition)routeDefinition.Segments[0];
    AssertEquals(0, litDef.Position, "literal position");
    AssertEquals("add", litDef.Value, "literal value");

    // Check segment 1: Parameter {x:int}
    AssertTrue(routeDefinition.Segments[1] is ParameterDefinition, "segment 1 is ParameterDefinition");
    ParameterDefinition paramDefX = (ParameterDefinition)routeDefinition.Segments[1];
    AssertEquals(1, paramDefX.Position, "param x position");
    AssertEquals("x", paramDefX.Name, "param x name");
    AssertEquals("int", paramDefX.TypeConstraint, "param x type constraint");
    AssertEquals("global::System.Int32", paramDefX.ResolvedClrTypeName, "param x CLR type");
    AssertEquals(false, paramDefX.IsOptional, "param x optional");
    AssertEquals(false, paramDefX.IsCatchAll, "param x catch-all");

    // Check segment 2: Parameter {y:int}
    AssertTrue(routeDefinition.Segments[2] is ParameterDefinition, "segment 2 is ParameterDefinition");
    ParameterDefinition paramDefY = (ParameterDefinition)routeDefinition.Segments[2];
    AssertEquals(2, paramDefY.Position, "param y position");
    AssertEquals("y", paramDefY.Name, "param y name");
    AssertEquals("int", paramDefY.TypeConstraint, "param y type constraint");
    AssertEquals("global::System.Int32", paramDefY.ResolvedClrTypeName, "param y CLR type");
    AssertEquals(false, paramDefY.IsOptional, "param y optional");
    AssertEquals(false, paramDefY.IsCatchAll, "param y catch-all");

    Console.WriteLine("  All RouteDefinition assertions passed!");
    Console.WriteLine();

    // Step 5: Test convenience method FromPattern
    Console.WriteLine("Testing convenience method FromPattern...");
    RouteDefinition fromPattern = CompiledRouteToRouteDefinition.FromPattern(Pattern, handler, "Test description");
    AssertEquals(Pattern, fromPattern.OriginalPattern, "FromPattern - pattern");
    AssertEquals(3, fromPattern.Segments.Length, "FromPattern - segments");
    Console.WriteLine("  FromPattern works!");
    Console.WriteLine();

    Console.WriteLine("==============================================");
    Console.WriteLine("=== ALL TESTS PASSED! ===");
    Console.WriteLine("==============================================");

    return 0;
  }

  private static void AssertEquals<T>(T expected, T actual, string description)
  {
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
      Console.WriteLine($"  FAILED: {description}");
      Console.WriteLine($"    Expected: {expected}");
      Console.WriteLine($"    Actual:   {actual}");
      throw new Exception($"Assertion failed: {description}");
    }
    Console.WriteLine($"  ✓ {description}");
  }

  private static void AssertTrue(bool condition, string description)
  {
    if (!condition)
    {
      Console.WriteLine($"  FAILED: {description}");
      throw new Exception($"Assertion failed: {description}");
    }
    Console.WriteLine($"  ✓ {description}");
  }
}
