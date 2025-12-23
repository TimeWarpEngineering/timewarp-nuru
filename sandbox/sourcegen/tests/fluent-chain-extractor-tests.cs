// sandbox/sourcegen/tests/fluent-chain-extractor-tests.cs
// Tests for FluentChainExtractor and DelegateAnalyzer
//
// Agent: Amina
// Task: #242-step-3

namespace TimeWarp.Nuru.SourceGen.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TimeWarp.Nuru.SourceGen.Extractors;
using System.Collections.Immutable;

/// <summary>
/// Tests for extracting route information from fluent Map() chains.
/// </summary>
public static class FluentChainExtractorTests
{
  public static int Run()
  {
    Console.WriteLine("=== FluentChainExtractor Tests ===");
    Console.WriteLine();

    int passed = 0;
    int failed = 0;

    passed += TestExtractPattern(ref failed);
    passed += TestExtractDescription(ref failed);
    passed += TestExtractMessageType(ref failed);
    passed += TestExtractHandlerLambda(ref failed);
    passed += TestExtractDelegateParametersSyntaxOnly(ref failed);
    passed += TestCompleteChain(ref failed);

    Console.WriteLine();
    Console.WriteLine("==============================================");
    Console.WriteLine($"Results: {passed} passed, {failed} failed");
    Console.WriteLine("==============================================");

    return failed > 0 ? 1 : 0;
  }

  /// <summary>
  /// Test extracting pattern from Map("add {x:int} {y:int}")
  /// </summary>
  private static int TestExtractPattern(ref int failed)
  {
    Console.WriteLine("Test: Extract pattern from Map(\"...\")");

    try
    {
      string code = @"
        builder.Map(""add {x:int} {y:int}"")
          .WithHandler((int x, int y) => x + y)
          .Done();
      ";

      InvocationExpressionSyntax? mapInvocation = FindMapInvocation(code);
      if (mapInvocation is null)
      {
        throw new Exception("Could not find Map invocation");
      }

      FluentChainExtractor.FluentChainResult result = FluentChainExtractor.ExtractFromMapChain(mapInvocation);

      AssertEquals("add {x:int} {y:int}", result.Pattern, "pattern");

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
  /// Test extracting description from .WithDescription("...")
  /// </summary>
  private static int TestExtractDescription(ref int failed)
  {
    Console.WriteLine("Test: Extract description from .WithDescription(\"...\")");

    try
    {
      string code = @"
        builder.Map(""add {x:int} {y:int}"")
          .WithHandler((int x, int y) => x + y)
          .WithDescription(""Add two integers"")
          .Done();
      ";

      InvocationExpressionSyntax? mapInvocation = FindMapInvocation(code);
      FluentChainExtractor.FluentChainResult result = FluentChainExtractor.ExtractFromMapChain(mapInvocation!);

      AssertEquals("Add two integers", result.Description, "description");

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
  /// Test extracting message type from .AsQuery(), .AsCommand(), etc.
  /// </summary>
  private static int TestExtractMessageType(ref int failed)
  {
    Console.WriteLine("Test: Extract message type from .AsQuery()");

    try
    {
      string code = @"
        builder.Map(""status"")
          .WithHandler(() => ""OK"")
          .AsQuery()
          .Done();
      ";

      InvocationExpressionSyntax? mapInvocation = FindMapInvocation(code);
      FluentChainExtractor.FluentChainResult result = FluentChainExtractor.ExtractFromMapChain(mapInvocation!);

      AssertEquals("Query", result.MessageType, "message type");

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
  /// Test extracting lambda from .WithHandler(lambda)
  /// </summary>
  private static int TestExtractHandlerLambda(ref int failed)
  {
    Console.WriteLine("Test: Extract lambda from .WithHandler(...)");

    try
    {
      string code = @"
        builder.Map(""add {x:int} {y:int}"")
          .WithHandler((int x, int y) => x + y)
          .Done();
      ";

      InvocationExpressionSyntax? mapInvocation = FindMapInvocation(code);
      FluentChainExtractor.FluentChainResult result = FluentChainExtractor.ExtractFromMapChain(mapInvocation!);

      AssertNotNull(result.HandlerLambda, "handler lambda");
      AssertTrue(result.HandlerLambda is ParenthesizedLambdaExpressionSyntax, "lambda is parenthesized");

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
  /// Test extracting delegate parameters using syntax-only analysis
  /// </summary>
  private static int TestExtractDelegateParametersSyntaxOnly(ref int failed)
  {
    Console.WriteLine("Test: Extract delegate parameters (syntax only)");

    try
    {
      string code = @"
        builder.Map(""add {x:int} {y:int}"")
          .WithHandler((int x, int y) => x + y)
          .Done();
      ";

      InvocationExpressionSyntax? mapInvocation = FindMapInvocation(code);
      FluentChainExtractor.FluentChainResult chainResult = FluentChainExtractor.ExtractFromMapChain(mapInvocation!);

      if (chainResult.HandlerLambda is null)
      {
        throw new Exception("Handler lambda not found");
      }

      DelegateAnalyzer.DelegateAnalysisResult delegateResult =
        DelegateAnalyzer.AnalyzeLambdaSyntaxOnly(chainResult.HandlerLambda);

      AssertEquals(2, delegateResult.Parameters.Length, "parameter count");

      DelegateAnalyzer.DelegateParameter paramX = delegateResult.Parameters[0];
      AssertEquals("x", paramX.Name, "param x name");
      AssertEquals("global::System.Int32", paramX.TypeName, "param x type");

      DelegateAnalyzer.DelegateParameter paramY = delegateResult.Parameters[1];
      AssertEquals("y", paramY.Name, "param y name");
      AssertEquals("global::System.Int32", paramY.TypeName, "param y type");

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
  /// Test complete chain extraction and conversion to RouteDefinition
  /// </summary>
  private static int TestCompleteChain(ref int failed)
  {
    Console.WriteLine("Test: Complete chain → RouteDefinition");

    try
    {
      string code = @"
        builder.Map(""deploy {env} --force,-f"")
          .WithHandler((string env, bool force) => Console.WriteLine($""Deploying to {env}""))
          .WithDescription(""Deploy to environment"")
          .AsCommand()
          .Done();
      ";

      // 1. Extract from chain
      InvocationExpressionSyntax? mapInvocation = FindMapInvocation(code);
      FluentChainExtractor.FluentChainResult chainResult = FluentChainExtractor.ExtractFromMapChain(mapInvocation!);

      AssertEquals("deploy {env} --force,-f", chainResult.Pattern, "pattern");
      AssertEquals("Deploy to environment", chainResult.Description, "description");
      AssertEquals("Command", chainResult.MessageType, "message type");

      // 2. Analyze delegate
      DelegateAnalyzer.DelegateAnalysisResult delegateResult =
        DelegateAnalyzer.AnalyzeLambdaSyntaxOnly(chainResult.HandlerLambda!);

      AssertEquals(2, delegateResult.Parameters.Length, "delegate param count");
      AssertEquals("env", delegateResult.Parameters[0].Name, "param 0 name");
      AssertEquals("force", delegateResult.Parameters[1].Name, "param 1 name");

      // 3. Parse pattern and convert to segments
      Parser parser = new();
      ParseResult<TimeWarp.Nuru.Syntax> parseResult = parser.Parse(chainResult.Pattern!);
      if (!parseResult.Success)
      {
        throw new Exception("Pattern parse failed");
      }

      ImmutableArray<SegmentDefinition> segments =
        SegmentDefinitionConverter.FromSyntax(parseResult.Value!.Segments);

      AssertEquals(3, segments.Length, "segment count");

      // 4. Build handler definition
      HandlerDefinitionBuilder handlerBuilder = new HandlerDefinitionBuilder().AsDelegate();

      foreach (DelegateAnalyzer.DelegateParameter param in delegateResult.Parameters)
      {
        // Determine if this is a flag based on type
        if (param.TypeName == "global::System.Boolean")
        {
          handlerBuilder.WithFlagParameter(param.Name, param.Name);
        }
        else
        {
          handlerBuilder.WithParameter(param.Name, param.TypeName);
        }
      }

      handlerBuilder.ReturnsVoid();
      HandlerDefinition handler = handlerBuilder.Build();

      // 5. Assemble RouteDefinition
      RouteDefinition route = new RouteDefinitionBuilder()
        .WithPattern(chainResult.Pattern!)
        .WithSegments(segments)
        .WithHandler(handler)
        .WithMessageType(chainResult.MessageType!)
        .WithDescription(chainResult.Description)
        .Build();

      // 6. Verify complete model
      AssertEquals("deploy {env} --force,-f", route.OriginalPattern, "route pattern");
      AssertEquals("Command", route.MessageType, "route message type");
      AssertEquals("Deploy to environment", route.Description, "route description");
      AssertEquals(3, route.Segments.Length, "route segment count");
      AssertEquals(2, route.Handler.Parameters.Length, "route handler param count");

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
  /// Parses code and finds the first Map() invocation.
  /// </summary>
  private static InvocationExpressionSyntax? FindMapInvocation(string code)
  {
    SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
    CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

    // Find all invocations and look for Map
    foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
    {
      string? methodName = invocation.Expression switch
      {
        MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
        IdentifierNameSyntax identifier => identifier.Identifier.Text,
        _ => null
      };

      if (methodName == "Map")
      {
        return invocation;
      }
    }

    return null;
  }

  private static void AssertEquals<T>(T expected, T actual, string description)
  {
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
      throw new Exception($"{description}: expected '{expected}', got '{actual}'");
    }
    Console.WriteLine($"    {description}: {actual}");
  }

  private static void AssertNotNull<T>(T? value, string description) where T : class
  {
    if (value is null)
    {
      throw new Exception($"{description}: expected non-null");
    }
    Console.WriteLine($"    {description}: (not null)");
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
