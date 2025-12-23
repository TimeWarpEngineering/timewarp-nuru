// sandbox/sourcegen/tests/fluent-route-builder-extractor-tests.cs
// Tests for FluentRouteBuilderExtractor (Source 2)
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
/// Tests for extracting segment definitions from fluent route builder lambdas.
/// </summary>
public static class FluentRouteBuilderExtractorTests
{
  public static int Run()
  {
    Console.WriteLine("=== FluentRouteBuilderExtractor Tests (Source 2) ===");
    Console.WriteLine();

    int passed = 0;
    int failed = 0;

    passed += TestExtractLiteral(ref failed);
    passed += TestExtractParameter(ref failed);
    passed += TestExtractTypedParameter(ref failed);
    passed += TestExtractOptionalParameter(ref failed);
    passed += TestExtractOption(ref failed);
    passed += TestExtractOptionWithType(ref failed);
    passed += TestExtractFlag(ref failed);
    passed += TestCompleteRouteBuilder(ref failed);

    Console.WriteLine();
    Console.WriteLine("==============================================");
    Console.WriteLine($"Results: {passed} passed, {failed} failed");
    Console.WriteLine("==============================================");

    return failed > 0 ? 1 : 0;
  }

  /// <summary>
  /// Test extracting a literal segment from WithLiteral("add")
  /// </summary>
  private static int TestExtractLiteral(ref int failed)
  {
    Console.WriteLine("Test: Extract literal from WithLiteral(\"add\")");

    try
    {
      string code = @"r => r.WithLiteral(""add"").Done()";

      LambdaExpressionSyntax? lambda = FindLambda(code);
      if (lambda is null)
      {
        throw new Exception("Could not find lambda");
      }

      FluentRouteBuilderExtractor.FluentRouteBuilderResult result =
        FluentRouteBuilderExtractor.ExtractFromRouteBuilderLambda(lambda);

      AssertEquals(1, result.Segments.Length, "segment count");
      AssertTrue(result.Segments[0] is LiteralDefinition, "is LiteralDefinition");

      LiteralDefinition literal = (LiteralDefinition)result.Segments[0];
      AssertEquals("add", literal.Value, "literal value");
      AssertEquals(0, literal.Position, "position");

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
  /// Test extracting an untyped parameter from WithParameter("name")
  /// </summary>
  private static int TestExtractParameter(ref int failed)
  {
    Console.WriteLine("Test: Extract parameter from WithParameter(\"name\")");

    try
    {
      string code = @"r => r.WithLiteral(""greet"").WithParameter(""name"").Done()";

      LambdaExpressionSyntax? lambda = FindLambda(code);
      FluentRouteBuilderExtractor.FluentRouteBuilderResult result =
        FluentRouteBuilderExtractor.ExtractFromRouteBuilderLambda(lambda!);

      AssertEquals(2, result.Segments.Length, "segment count");
      AssertTrue(result.Segments[1] is ParameterDefinition, "is ParameterDefinition");

      ParameterDefinition param = (ParameterDefinition)result.Segments[1];
      AssertEquals("name", param.Name, "param name");
      AssertEquals(1, param.Position, "position");
      AssertEquals("global::System.String", param.ResolvedClrTypeName, "CLR type (default string)");
      AssertTrue(param.TypeConstraint is null, "type constraint is null");

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
  /// Test extracting a typed parameter from WithParameter("x", "int")
  /// </summary>
  private static int TestExtractTypedParameter(ref int failed)
  {
    Console.WriteLine("Test: Extract typed parameter from WithParameter(\"x\", \"int\")");

    try
    {
      string code = @"r => r.WithLiteral(""add"").WithParameter(""x"", ""int"").WithParameter(""y"", ""int"").Done()";

      LambdaExpressionSyntax? lambda = FindLambda(code);
      FluentRouteBuilderExtractor.FluentRouteBuilderResult result =
        FluentRouteBuilderExtractor.ExtractFromRouteBuilderLambda(lambda!);

      AssertEquals(3, result.Segments.Length, "segment count");

      ParameterDefinition paramX = (ParameterDefinition)result.Segments[1];
      AssertEquals("x", paramX.Name, "param x name");
      AssertEquals("int", paramX.TypeConstraint, "param x type constraint");
      AssertEquals("global::System.Int32", paramX.ResolvedClrTypeName, "param x CLR type");

      ParameterDefinition paramY = (ParameterDefinition)result.Segments[2];
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
  /// Test extracting an optional parameter using named argument
  /// </summary>
  private static int TestExtractOptionalParameter(ref int failed)
  {
    Console.WriteLine("Test: Extract optional parameter with named argument");

    try
    {
      string code = @"r => r.WithLiteral(""copy"").WithParameter(""source"").WithParameter(""dest"", optional: true).Done()";

      LambdaExpressionSyntax? lambda = FindLambda(code);
      FluentRouteBuilderExtractor.FluentRouteBuilderResult result =
        FluentRouteBuilderExtractor.ExtractFromRouteBuilderLambda(lambda!);

      AssertEquals(3, result.Segments.Length, "segment count");

      ParameterDefinition source = (ParameterDefinition)result.Segments[1];
      AssertEquals(false, source.IsOptional, "source is not optional");

      ParameterDefinition dest = (ParameterDefinition)result.Segments[2];
      AssertEquals("dest", dest.Name, "dest name");
      AssertEquals(true, dest.IsOptional, "dest is optional");

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
  /// Test extracting an option with short form from WithOption("force", "f")
  /// </summary>
  private static int TestExtractOption(ref int failed)
  {
    Console.WriteLine("Test: Extract option from WithOption(\"force\", \"f\")");

    try
    {
      string code = @"r => r.WithLiteral(""deploy"").WithOption(""force"", ""f"").Done()";

      LambdaExpressionSyntax? lambda = FindLambda(code);
      FluentRouteBuilderExtractor.FluentRouteBuilderResult result =
        FluentRouteBuilderExtractor.ExtractFromRouteBuilderLambda(lambda!);

      AssertEquals(2, result.Segments.Length, "segment count");
      AssertTrue(result.Segments[1] is OptionDefinition, "is OptionDefinition");

      OptionDefinition option = (OptionDefinition)result.Segments[1];
      AssertEquals("force", option.LongForm, "long form");
      AssertEquals("f", option.ShortForm, "short form");
      AssertEquals(false, option.ExpectsValue, "is flag (no value)");
      AssertEquals("global::System.Boolean", option.ResolvedClrTypeName, "CLR type (bool for flag)");

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
  /// Test extracting an option with type constraint from WithOption("count", "c", "int")
  /// </summary>
  private static int TestExtractOptionWithType(ref int failed)
  {
    Console.WriteLine("Test: Extract option with type from WithOption(\"count\", \"c\", \"int\")");

    try
    {
      string code = @"r => r.WithLiteral(""run"").WithOption(""count"", ""c"", ""int"").Done()";

      LambdaExpressionSyntax? lambda = FindLambda(code);
      FluentRouteBuilderExtractor.FluentRouteBuilderResult result =
        FluentRouteBuilderExtractor.ExtractFromRouteBuilderLambda(lambda!);

      AssertEquals(2, result.Segments.Length, "segment count");

      OptionDefinition option = (OptionDefinition)result.Segments[1];
      AssertEquals("count", option.LongForm, "long form");
      AssertEquals("c", option.ShortForm, "short form");
      AssertEquals(true, option.ExpectsValue, "expects value");
      AssertEquals("int", option.TypeConstraint, "type constraint");
      AssertEquals("global::System.Int32", option.ResolvedClrTypeName, "CLR type");
      AssertEquals("count", option.ParameterName, "parameter name");

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
  /// Test extracting a flag from WithFlag("verbose", "v")
  /// </summary>
  private static int TestExtractFlag(ref int failed)
  {
    Console.WriteLine("Test: Extract flag from WithFlag(\"verbose\", \"v\")");

    try
    {
      string code = @"r => r.WithLiteral(""status"").WithFlag(""verbose"", ""v"").Done()";

      LambdaExpressionSyntax? lambda = FindLambda(code);
      FluentRouteBuilderExtractor.FluentRouteBuilderResult result =
        FluentRouteBuilderExtractor.ExtractFromRouteBuilderLambda(lambda!);

      AssertEquals(2, result.Segments.Length, "segment count");
      AssertTrue(result.Segments[1] is OptionDefinition, "is OptionDefinition");

      OptionDefinition flag = (OptionDefinition)result.Segments[1];
      AssertEquals("verbose", flag.LongForm, "long form");
      AssertEquals("v", flag.ShortForm, "short form");
      AssertEquals(false, flag.ExpectsValue, "is flag");
      AssertEquals(true, flag.IsOptional, "is optional");
      AssertEquals("global::System.Boolean", flag.ResolvedClrTypeName, "CLR type");

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
  /// Test complete route builder extraction and conversion to RouteDefinition
  /// </summary>
  private static int TestCompleteRouteBuilder(ref int failed)
  {
    Console.WriteLine("Test: Complete route builder → RouteDefinition");

    try
    {
      // Source 2: Map(r => r.WithLiteral(...).WithParameter(...)).WithHandler(delegate)
      string mapCode = @"
        builder.Map(r => r
            .WithLiteral(""deploy"")
            .WithParameter(""env"")
            .WithOption(""force"", ""f"")
            .Done())
          .WithHandler((string env, bool force) => Console.WriteLine($""Deploying to {env}""))
          .WithDescription(""Deploy to environment"")
          .AsCommand()
          .Done();
      ";

      // 1. Find the Map invocation
      InvocationExpressionSyntax? mapInvocation = FindMapInvocation(mapCode);
      if (mapInvocation is null)
      {
        throw new Exception("Could not find Map invocation");
      }

      // 2. Extract the route builder lambda (first argument to Map)
      LambdaExpressionSyntax? routeBuilderLambda = ExtractRouteBuilderLambda(mapInvocation);
      if (routeBuilderLambda is null)
      {
        throw new Exception("Could not find route builder lambda");
      }

      // 3. Extract segments from route builder
      FluentRouteBuilderExtractor.FluentRouteBuilderResult builderResult =
        FluentRouteBuilderExtractor.ExtractFromRouteBuilderLambda(routeBuilderLambda);

      AssertEquals(3, builderResult.Segments.Length, "segment count");

      // 4. Extract chain metadata (description, message type, handler)
      FluentChainExtractor.FluentChainResult chainResult =
        FluentChainExtractor.ExtractFromMapChain(mapInvocation);

      AssertEquals("Deploy to environment", chainResult.Description, "description");
      AssertEquals("Command", chainResult.MessageType, "message type");
      AssertNotNull(chainResult.HandlerLambda, "handler lambda");

      // 5. Analyze delegate
      DelegateAnalyzer.DelegateAnalysisResult delegateResult =
        DelegateAnalyzer.AnalyzeLambdaSyntaxOnly(chainResult.HandlerLambda!);

      AssertEquals(2, delegateResult.Parameters.Length, "delegate param count");

      // 6. Build handler definition
      HandlerDefinitionBuilder handlerBuilder = new HandlerDefinitionBuilder().AsDelegate();
      foreach (DelegateAnalyzer.DelegateParameter param in delegateResult.Parameters)
      {
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

      // 7. Build pattern string from segments (for display)
      string reconstructedPattern = ReconstructPattern(builderResult.Segments);
      AssertEquals("deploy {env} --force,-f", reconstructedPattern, "reconstructed pattern");

      // 8. Assemble RouteDefinition
      RouteDefinition route = new RouteDefinitionBuilder()
        .WithPattern(reconstructedPattern)
        .WithSegments(builderResult.Segments)
        .WithHandler(handler)
        .WithMessageType(chainResult.MessageType!)
        .WithDescription(chainResult.Description)
        .Build();

      // 9. Verify complete model
      AssertEquals("deploy {env} --force,-f", route.OriginalPattern, "route pattern");
      AssertEquals("Command", route.MessageType, "route message type");
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
  /// Parses code and finds the first lambda expression.
  /// </summary>
  private static LambdaExpressionSyntax? FindLambda(string code)
  {
    SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
    CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

    return root.DescendantNodes().OfType<LambdaExpressionSyntax>().FirstOrDefault();
  }

  /// <summary>
  /// Parses code and finds the first Map() invocation.
  /// </summary>
  private static InvocationExpressionSyntax? FindMapInvocation(string code)
  {
    SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
    CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

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

  /// <summary>
  /// Extracts the route builder lambda from Map(r => ...).
  /// </summary>
  private static LambdaExpressionSyntax? ExtractRouteBuilderLambda(InvocationExpressionSyntax mapInvocation)
  {
    ArgumentListSyntax? args = mapInvocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
    {
      return null;
    }

    ExpressionSyntax firstArg = args.Arguments[0].Expression;
    return firstArg as LambdaExpressionSyntax;
  }

  /// <summary>
  /// Reconstructs a pattern string from segment definitions.
  /// </summary>
  private static string ReconstructPattern(ImmutableArray<SegmentDefinition> segments)
  {
    List<string> parts = [];

    foreach (SegmentDefinition segment in segments)
    {
      switch (segment)
      {
        case LiteralDefinition literal:
          parts.Add(literal.Value);
          break;

        case ParameterDefinition param:
          string typeSpec = param.TypeConstraint is not null ? $":{param.TypeConstraint}" : "";
          if (param.IsCatchAll)
            parts.Add($"{{*{param.Name}{typeSpec}}}");
          else if (param.IsOptional)
            parts.Add($"{{{param.Name}{typeSpec}?}}");
          else
            parts.Add($"{{{param.Name}{typeSpec}}}");
          break;

        case OptionDefinition option:
          string shortSpec = option.ShortForm is not null ? $",-{option.ShortForm}" : "";
          if (option.ExpectsValue)
          {
            string optTypeSpec = option.TypeConstraint is not null ? $":{option.TypeConstraint}" : "";
            parts.Add($"--{option.LongForm}{shortSpec} {{{option.ParameterName}{optTypeSpec}}}");
          }
          else
          {
            parts.Add($"--{option.LongForm}{shortSpec}");
          }
          break;
      }
    }

    return string.Join(" ", parts);
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
