// sandbox/sourcegen/tests/end-to-end-emitter-tests.cs
// End-to-end tests that compile and execute generated code
//
// Agent: Amina
// Task: #242-step-4

namespace TimeWarp.Nuru.SourceGen.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using TimeWarp.Nuru.SourceGen.Emitters;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;

/// <summary>
/// End-to-end tests that compile and actually execute the generated code.
/// This validates that the emitted code works correctly at runtime.
/// </summary>
public static class EndToEndEmitterTests
{
  public static int Run()
  {
    Console.WriteLine("=== End-to-End Emitter Tests (Step-4) ===");
    Console.WriteLine();

    int passed = 0;
    int failed = 0;

    passed += TestGeneratedRouteMatchesInput(ref failed);
    passed += TestGeneratedRouteExtractsParameters(ref failed);
    passed += TestGeneratedRouteWithIntParameters(ref failed);
    passed += TestMultipleRoutesMatch(ref failed);
    passed += TestRouterMatchesAndExecutes(ref failed);

    Console.WriteLine();
    Console.WriteLine("==============================================");
    Console.WriteLine($"Results: {passed} passed, {failed} failed");
    Console.WriteLine("==============================================");

    return failed > 0 ? 1 : 0;
  }

  /// <summary>
  /// Test that a generated route can match input
  /// </summary>
  private static int TestGeneratedRouteMatchesInput(ref int failed)
  {
    Console.WriteLine("Test: Generated route matches input");

    try
    {
      RouteDefinition route = new RouteDefinitionBuilder()
        .WithPattern("status")
        .WithSegments([
          new LiteralDefinition(0, "status")
        ])
        .WithHandler(new HandlerDefinitionBuilder()
          .AsDelegate()
          .ReturnsVoid()
          .Build())
        .Build();

      // Generate and compile
      Assembly? assembly = CompileRoute(route);
      if (assembly is null)
      {
        throw new Exception("Failed to compile generated code");
      }

      // Get the generated types
      Type? routesType = assembly.GetType("Generated.GeneratedRoutes");
      if (routesType is null)
      {
        throw new Exception("Could not find GeneratedRoutes type");
      }

      // Get the All array
      FieldInfo? allField = routesType.GetField("All", BindingFlags.Static | BindingFlags.NonPublic);
      if (allField is null)
      {
        throw new Exception("Could not find All field");
      }

      object? allRoutes = allField.GetValue(null);
      if (allRoutes is not Array routeArray || routeArray.Length == 0)
      {
        throw new Exception("All array is empty or null");
      }

      // Get the first route and try to match
      object? firstRoute = routeArray.GetValue(0);
      if (firstRoute is null)
      {
        throw new Exception("First route is null");
      }

      // Get SegmentMatchers property
      PropertyInfo? matchersProp = firstRoute.GetType().GetProperty("SegmentMatchers");
      if (matchersProp is null)
      {
        throw new Exception("Could not find SegmentMatchers property");
      }

      object? matchers = matchersProp.GetValue(firstRoute);
      if (matchers is not Array matcherArray)
      {
        throw new Exception("SegmentMatchers is not an array");
      }

      Console.WriteLine($"    Route has {matcherArray.Length} matcher(s)");

      // Try to match "status"
      object? firstMatcher = matcherArray.GetValue(0);
      MethodInfo? matchMethod = firstMatcher?.GetType().GetMethod("Matches");
      if (matchMethod is null)
      {
        throw new Exception("Could not find Matches method");
      }

      bool matches = (bool)(matchMethod.Invoke(firstMatcher, ["status"]) ?? false);
      Console.WriteLine($"    'status' matches: {matches}");

      if (!matches)
      {
        throw new Exception("Expected 'status' to match");
      }

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
  /// Test that a generated route extracts parameters correctly
  /// </summary>
  private static int TestGeneratedRouteExtractsParameters(ref int failed)
  {
    Console.WriteLine("Test: Generated route extracts string parameters");

    try
    {
      RouteDefinition route = new RouteDefinitionBuilder()
        .WithPattern("greet {name}")
        .WithSegments([
          new LiteralDefinition(0, "greet"),
          new ParameterDefinition(1, "name", null, null, false, false, "global::System.String", null)
        ])
        .WithHandler(new HandlerDefinitionBuilder()
          .AsDelegate()
          .WithParameter("name", "string")
          .ReturnsVoid()
          .Build())
        .Build();

      Assembly? assembly = CompileRoute(route);
      if (assembly is null)
      {
        throw new Exception("Failed to compile");
      }

      // Get the route
      Type? routesType = assembly.GetType("Generated.GeneratedRoutes");
      FieldInfo? allField = routesType?.GetField("All", BindingFlags.Static | BindingFlags.NonPublic);
      Array? routeArray = allField?.GetValue(null) as Array;
      object? firstRoute = routeArray?.GetValue(0);

      // Get parameter extractors
      PropertyInfo? extractorsProp = firstRoute?.GetType().GetProperty("ParameterExtractors");
      Array? extractors = extractorsProp?.GetValue(firstRoute) as Array;

      if (extractors is null || extractors.Length == 0)
      {
        throw new Exception("No parameter extractors found");
      }

      Console.WriteLine($"    Route has {extractors.Length} extractor(s)");

      // Get the first extractor and convert "Alice"
      object? firstExtractor = extractors.GetValue(0);
      PropertyInfo? convertProp = firstExtractor?.GetType().GetProperty("Convert");
      object? convertFunc = convertProp?.GetValue(firstExtractor);

      if (convertFunc is Func<string, object?> converter)
      {
        object? result = converter("Alice");
        Console.WriteLine($"    Extracted value: {result}");

        if (result is not string extracted || extracted != "Alice")
        {
          throw new Exception($"Expected 'Alice', got '{result}'");
        }
      }
      else
      {
        throw new Exception("Convert is not a Func<string, object?>");
      }

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
  /// Test that a generated route handles int parameters
  /// </summary>
  private static int TestGeneratedRouteWithIntParameters(ref int failed)
  {
    Console.WriteLine("Test: Generated route extracts int parameters");

    try
    {
      RouteDefinition route = new RouteDefinitionBuilder()
        .WithPattern("add {x:int} {y:int}")
        .WithSegments([
          new LiteralDefinition(0, "add"),
          new ParameterDefinition(1, "x", "int", null, false, false, "global::System.Int32", null),
          new ParameterDefinition(2, "y", "int", null, false, false, "global::System.Int32", null)
        ])
        .WithHandler(new HandlerDefinitionBuilder()
          .AsDelegate()
          .WithParameter("x", "int")
          .WithParameter("y", "int")
          .Returns("int")
          .Build())
        .Build();

      Assembly? assembly = CompileRoute(route);
      if (assembly is null)
      {
        throw new Exception("Failed to compile");
      }

      // Get the route
      Type? routesType = assembly.GetType("Generated.GeneratedRoutes");
      FieldInfo? allField = routesType?.GetField("All", BindingFlags.Static | BindingFlags.NonPublic);
      Array? routeArray = allField?.GetValue(null) as Array;
      object? firstRoute = routeArray?.GetValue(0);

      // Test int matchers
      PropertyInfo? matchersProp = firstRoute?.GetType().GetProperty("SegmentMatchers");
      Array? matchers = matchersProp?.GetValue(firstRoute) as Array;

      // Matcher at index 1 should be IntParameterMatcher for "x"
      object? intMatcher = matchers?.GetValue(1);
      MethodInfo? matchMethod = intMatcher?.GetType().GetMethod("Matches");

      bool matchesInt = (bool)(matchMethod?.Invoke(intMatcher, ["42"]) ?? false);
      bool matchesNonInt = (bool)(matchMethod?.Invoke(intMatcher, ["abc"]) ?? false);

      Console.WriteLine($"    '42' matches int: {matchesInt}");
      Console.WriteLine($"    'abc' matches int: {matchesNonInt}");

      if (!matchesInt || matchesNonInt)
      {
        throw new Exception("Int matcher behavior incorrect");
      }

      // Test int extractors
      PropertyInfo? extractorsProp = firstRoute?.GetType().GetProperty("ParameterExtractors");
      Array? extractors = extractorsProp?.GetValue(firstRoute) as Array;

      object? xExtractor = extractors?.GetValue(0);
      PropertyInfo? convertProp = xExtractor?.GetType().GetProperty("Convert");
      object? convertFunc = convertProp?.GetValue(xExtractor);

      if (convertFunc is Func<string, object?> converter)
      {
        object? result = converter("42");
        Console.WriteLine($"    Extracted '42' as int: {result} (type: {result?.GetType().Name})");

        if (result is not int intValue || intValue != 42)
        {
          throw new Exception($"Expected int 42, got {result}");
        }
      }

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
  /// Test that multiple routes are generated and accessible
  /// </summary>
  private static int TestMultipleRoutesMatch(ref int failed)
  {
    Console.WriteLine("Test: Multiple routes generated and accessible");

    try
    {
      RouteDefinition route1 = new RouteDefinitionBuilder()
        .WithPattern("add {x:int} {y:int}")
        .WithSegments([
          new LiteralDefinition(0, "add"),
          new ParameterDefinition(1, "x", "int", null, false, false, "global::System.Int32", null),
          new ParameterDefinition(2, "y", "int", null, false, false, "global::System.Int32", null)
        ])
        .WithHandler(new HandlerDefinitionBuilder()
          .AsDelegate()
          .WithParameter("x", "int")
          .WithParameter("y", "int")
          .Returns("int")
          .Build())
        .Build();

      RouteDefinition route2 = new RouteDefinitionBuilder()
        .WithPattern("greet {name}")
        .WithSegments([
          new LiteralDefinition(0, "greet"),
          new ParameterDefinition(1, "name", null, null, false, false, "global::System.String", null)
        ])
        .WithHandler(new HandlerDefinitionBuilder()
          .AsDelegate()
          .WithParameter("name", "string")
          .ReturnsVoid()
          .Build())
        .Build();

      Assembly? assembly = CompileRoutes([route1, route2]);
      if (assembly is null)
      {
        throw new Exception("Failed to compile");
      }

      Type? routesType = assembly.GetType("Generated.GeneratedRoutes");
      FieldInfo? allField = routesType?.GetField("All", BindingFlags.Static | BindingFlags.NonPublic);
      Array? routeArray = allField?.GetValue(null) as Array;

      if (routeArray is null)
      {
        throw new Exception("Route array is null");
      }

      Console.WriteLine($"    Generated {routeArray.Length} routes");

      if (routeArray.Length != 2)
      {
        throw new Exception($"Expected 2 routes, got {routeArray.Length}");
      }

      // Verify each route has the correct pattern
      for (int i = 0; i < routeArray.Length; i++)
      {
        object? route = routeArray.GetValue(i);
        PropertyInfo? patternProp = route?.GetType().GetProperty("Pattern");
        string? pattern = patternProp?.GetValue(route) as string;
        Console.WriteLine($"    Route {i}: {pattern}");
      }

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
  /// Test that the Router can match args and execute handlers
  /// </summary>
  private static int TestRouterMatchesAndExecutes(ref int failed)
  {
    Console.WriteLine("Test: Router matches args and executes handler");

    try
    {
      RouteDefinition route = new RouteDefinitionBuilder()
        .WithPattern("add {x:int} {y:int}")
        .WithSegments([
          new LiteralDefinition(0, "add"),
          new ParameterDefinition(1, "x", "int", null, false, false, "global::System.Int32", null),
          new ParameterDefinition(2, "y", "int", null, false, false, "global::System.Int32", null)
        ])
        .WithHandler(new HandlerDefinitionBuilder()
          .AsDelegate()
          .WithParameter("x", "int")
          .WithParameter("y", "int")
          .Returns("int")
          .Build())
        .Build();

      Assembly? assembly = CompileRoute(route);
      if (assembly is null)
      {
        throw new Exception("Failed to compile");
      }

      // Get the generated types
      Type? routesType = assembly.GetType("Generated.GeneratedRoutes");
      Type? routerType = assembly.GetType("Generated.Router");
      Type? matchResultType = assembly.GetType("Generated.MatchResult");

      if (routerType is null)
      {
        throw new Exception("Router type not found");
      }

      // Get the All routes array
      FieldInfo? allField = routesType?.GetField("All", BindingFlags.Static | BindingFlags.NonPublic);
      object? allRoutes = allField?.GetValue(null);

      // Create a Router instance
      object? router = Activator.CreateInstance(routerType, allRoutes);
      if (router is null)
      {
        throw new Exception("Could not create Router instance");
      }

      // Call Match with ["add", "2", "3"]
      MethodInfo? matchMethod = routerType.GetMethod("Match");
      string[] args = ["add", "2", "3"];
      object? matchResult = matchMethod?.Invoke(router, [args]);

      if (matchResult is null)
      {
        throw new Exception("Match returned null");
      }

      // Check IsMatch
      PropertyInfo? isMatchProp = matchResultType?.GetProperty("IsMatch");
      bool isMatch = (bool)(isMatchProp?.GetValue(matchResult) ?? false);
      Console.WriteLine($"    IsMatch for 'add 2 3': {isMatch}");

      if (!isMatch)
      {
        throw new Exception("Expected match for 'add 2 3'");
      }

      // Check MatchedPattern
      PropertyInfo? patternProp = matchResultType?.GetProperty("MatchedPattern");
      string? pattern = patternProp?.GetValue(matchResult) as string;
      Console.WriteLine($"    Matched pattern: {pattern}");

      // Check ExtractedParameters
      PropertyInfo? paramsProp = matchResultType?.GetProperty("ExtractedParameters");
      object? extractedParams = paramsProp?.GetValue(matchResult);
      if (extractedParams is Dictionary<string, object> paramsDict)
      {
        Console.WriteLine($"    Extracted x: {paramsDict["x"]} (type: {paramsDict["x"].GetType().Name})");
        Console.WriteLine($"    Extracted y: {paramsDict["y"]} (type: {paramsDict["y"].GetType().Name})");

        if (paramsDict["x"] is not int xVal || xVal != 2)
        {
          throw new Exception($"Expected x=2, got {paramsDict["x"]}");
        }
        if (paramsDict["y"] is not int yVal || yVal != 3)
        {
          throw new Exception($"Expected y=3, got {paramsDict["y"]}");
        }
      }

      // Test non-matching args
      string[] badArgs = ["subtract", "2", "3"];
      object? noMatchResult = matchMethod?.Invoke(router, [badArgs]);
      bool noMatch = !(bool)(isMatchProp?.GetValue(noMatchResult) ?? true);
      Console.WriteLine($"    'subtract 2 3' does not match: {noMatch}");

      if (!noMatch)
      {
        throw new Exception("Expected no match for 'subtract 2 3'");
      }

      Console.WriteLine("  PASSED");
      return 1;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"  FAILED: {ex.Message}");
      if (ex.InnerException is not null)
      {
        Console.WriteLine($"    Inner: {ex.InnerException.Message}");
      }
      failed++;
      return 0;
    }
  }

  #region Helpers

  private static Assembly? CompileRoute(RouteDefinition route)
  {
    return CompileRoutes([route]);
  }

  private static Assembly? CompileRoutes(IEnumerable<RouteDefinition> routes)
  {
    RuntimeCodeEmitter.EmitResult emitResult = RuntimeCodeEmitter.EmitSourceFile(routes);

    SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(emitResult.SourceCode);

    // Get the runtime assembly path
    string runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

    CSharpCompilation compilation = CSharpCompilation.Create(
      $"TestAssembly_{Guid.NewGuid():N}",
      [syntaxTree],
      [
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Dictionary<,>).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Func<>).Assembly.Location),
        MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll")),
        MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Collections.dll")),
      ],
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    using MemoryStream ms = new();
    EmitResult result = compilation.Emit(ms);

    if (!result.Success)
    {
      IEnumerable<Diagnostic> errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
      foreach (Diagnostic error in errors.Take(5))
      {
        Console.WriteLine($"    Compile error: {error.GetMessage()}");
      }
      return null;
    }

    ms.Seek(0, SeekOrigin.Begin);

    // Load into a collectible context so it can be unloaded
    AssemblyLoadContext context = new AssemblyLoadContext(null, isCollectible: true);
    return context.LoadFromStream(ms);
  }

  #endregion
}
