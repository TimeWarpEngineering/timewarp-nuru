// sandbox/sourcegen/tests/mediator-route-extractor-tests.cs
// Tests for MediatorRouteExtractor (Source 4)
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
/// Tests for extracting route definitions from Map&lt;T&gt;("pattern") chains.
/// </summary>
public static class MediatorRouteExtractorTests
{
  public static int Run()
  {
    Console.WriteLine("=== MediatorRouteExtractor Tests (Source 4) ===");
    Console.WriteLine();

    int passed = 0;
    int failed = 0;

    passed += TestIsGenericMapCall(ref failed);
    passed += TestExtractPattern(ref failed);
    passed += TestExtractTypeArgument(ref failed);
    passed += TestExtractDescription(ref failed);
    passed += TestExtractMessageType(ref failed);
    passed += TestExtractAliases(ref failed);
    passed += TestCompleteMediatorRoute(ref failed);

    Console.WriteLine();
    Console.WriteLine("==============================================");
    Console.WriteLine($"Results: {passed} passed, {failed} failed");
    Console.WriteLine("==============================================");

    return failed > 0 ? 1 : 0;
  }

  /// <summary>
  /// Test detecting generic Map&lt;T&gt; calls vs non-generic Map calls
  /// </summary>
  private static int TestIsGenericMapCall(ref int failed)
  {
    Console.WriteLine("Test: Detect generic Map<T>() vs non-generic Map()");

    try
    {
      string genericCode = @"builder.Map<AddCommand>(""add {x:int} {y:int}"").Done();";
      string nonGenericCode = @"builder.Map(""add {x:int} {y:int}"").WithHandler((int x, int y) => x + y).Done();";

      InvocationExpressionSyntax? genericMap = FindMapInvocation(genericCode);
      InvocationExpressionSyntax? nonGenericMap = FindMapInvocation(nonGenericCode);

      if (genericMap is null || nonGenericMap is null)
      {
        throw new Exception("Could not find Map invocations");
      }

      AssertTrue(MediatorRouteExtractor.IsGenericMapCall(genericMap), "generic Map<T> is detected");
      AssertTrue(!MediatorRouteExtractor.IsGenericMapCall(nonGenericMap), "non-generic Map is not detected");

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
  /// Test extracting pattern from Map&lt;T&gt;("pattern")
  /// </summary>
  private static int TestExtractPattern(ref int failed)
  {
    Console.WriteLine("Test: Extract pattern from Map<T>(\"...\")");

    try
    {
      string code = @"builder.Map<AddCommand>(""add {x:int} {y:int}"").Done();";

      InvocationExpressionSyntax? mapInvocation = FindMapInvocation(code);
      MediatorRouteExtractor.MediatorRouteResult result =
        MediatorRouteExtractor.ExtractFromMediatorMapChain(mapInvocation!);

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
  /// Test extracting type argument from Map&lt;AddCommand&gt;
  /// </summary>
  private static int TestExtractTypeArgument(ref int failed)
  {
    Console.WriteLine("Test: Extract type argument from Map<AddCommand>");

    try
    {
      string code = @"builder.Map<MyApp.Commands.AddCommand>(""add {x:int} {y:int}"").Done();";

      InvocationExpressionSyntax? mapInvocation = FindMapInvocation(code);
      MediatorRouteExtractor.MediatorRouteResult result =
        MediatorRouteExtractor.ExtractFromMediatorMapChain(mapInvocation!);

      AssertEquals("global::MyApp.Commands.AddCommand", result.RequestTypeName, "request type");

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
        builder.Map<AddCommand>(""add {x:int} {y:int}"")
          .WithDescription(""Add two integers"")
          .Done();
      ";

      InvocationExpressionSyntax? mapInvocation = FindMapInvocation(code);
      MediatorRouteExtractor.MediatorRouteResult result =
        MediatorRouteExtractor.ExtractFromMediatorMapChain(mapInvocation!);

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
  /// Test extracting message type from .AsQuery(), .AsCommand()
  /// </summary>
  private static int TestExtractMessageType(ref int failed)
  {
    Console.WriteLine("Test: Extract message type from .AsQuery()");

    try
    {
      string code = @"
        builder.Map<StatusQuery>(""status"")
          .AsQuery()
          .Done();
      ";

      InvocationExpressionSyntax? mapInvocation = FindMapInvocation(code);
      MediatorRouteExtractor.MediatorRouteResult result =
        MediatorRouteExtractor.ExtractFromMediatorMapChain(mapInvocation!);

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
  /// Test extracting aliases from .WithAlias("...")
  /// </summary>
  private static int TestExtractAliases(ref int failed)
  {
    Console.WriteLine("Test: Extract aliases from .WithAlias(\"...\")");

    try
    {
      string code = @"
        builder.Map<AddCommand>(""add {x:int} {y:int}"")
          .WithAlias(""sum"")
          .WithAlias(""plus"")
          .Done();
      ";

      InvocationExpressionSyntax? mapInvocation = FindMapInvocation(code);
      MediatorRouteExtractor.MediatorRouteResult result =
        MediatorRouteExtractor.ExtractFromMediatorMapChain(mapInvocation!);

      AssertEquals(2, result.Aliases.Length, "alias count");
      AssertEquals("sum", result.Aliases[0], "alias 0");
      AssertEquals("plus", result.Aliases[1], "alias 1");

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
  /// Test complete mediator route extraction and conversion to RouteDefinition
  /// </summary>
  private static int TestCompleteMediatorRoute(ref int failed)
  {
    Console.WriteLine("Test: Complete mediator route → RouteDefinition");

    try
    {
      // Source 4: Map<T>("pattern").WithDescription("...").AsQuery()
      string mapCode = @"
        builder.Map<MyApp.Commands.AddCommand>(""add {x:int} {y:int}"")
          .WithDescription(""Add two integers"")
          .AsQuery()
          .Done();
      ";

      // Simulated class definition for AddCommand (would come from semantic model in real generator)
      string classCode = @"
        namespace MyApp.Commands
        {
          public class AddCommand : IRequest<int>
          {
            public int X { get; set; }
            public int Y { get; set; }
          }
        }
      ";

      // 1. Extract from Map<T> chain
      InvocationExpressionSyntax? mapInvocation = FindMapInvocation(mapCode);
      MediatorRouteExtractor.MediatorRouteResult mediatorResult =
        MediatorRouteExtractor.ExtractFromMediatorMapChain(mapInvocation!);

      AssertEquals("add {x:int} {y:int}", mediatorResult.Pattern, "pattern");
      AssertEquals("Add two integers", mediatorResult.Description, "description");
      AssertEquals("Query", mediatorResult.MessageType, "message type");
      AssertEquals("global::MyApp.Commands.AddCommand", mediatorResult.RequestTypeName, "request type");

      // 2. Parse pattern to get segments
      Parser parser = new();
      ParseResult<TimeWarp.Nuru.Syntax> parseResult = parser.Parse(mediatorResult.Pattern!);
      if (!parseResult.Success)
      {
        throw new Exception("Pattern parse failed");
      }

      ImmutableArray<SegmentDefinition> segments =
        SegmentDefinitionConverter.FromSyntax(parseResult.Value!.Segments);

      AssertEquals(3, segments.Length, "segment count");

      // 3. Extract properties from class (simulating what semantic model would provide)
      ClassDeclarationSyntax? classDecl = FindClassDeclaration(classCode);
      AttributedRouteExtractor.AttributedRouteResult classResult =
        AttributedRouteExtractor.ExtractFromClass(classDecl!);

      AssertEquals(2, classResult.Properties.Length, "property count");

      // 4. Build bindings from properties
      ImmutableArray<ParameterBinding> bindings =
        AttributedRouteExtractor.BuildBindingsFromProperties(classResult.Properties, segments);

      AssertEquals(2, bindings.Length, "binding count");

      // 5. Build handler definition (mediator style)
      HandlerDefinition handler = new HandlerDefinitionBuilder()
        .AsMediator(mediatorResult.RequestTypeName!)
        .ReturnsTaskOf("global::System.Int32", "int")
        .Build();

      AssertEquals(HandlerKind.Mediator, handler.HandlerKind, "handler kind");
      AssertEquals(true, handler.IsAsync, "is async");

      // 6. Assemble RouteDefinition
      RouteDefinition route = new RouteDefinitionBuilder()
        .WithPattern(mediatorResult.Pattern!)
        .WithSegments(segments)
        .WithHandler(handler)
        .WithMessageType(mediatorResult.MessageType!)
        .WithDescription(mediatorResult.Description)
        .Build();

      // 7. Verify complete model
      AssertEquals("add {x:int} {y:int}", route.OriginalPattern, "route pattern");
      AssertEquals("Query", route.MessageType, "route message type");
      AssertEquals("Add two integers", route.Description, "route description");
      AssertEquals(3, route.Segments.Length, "route segment count");
      AssertEquals(HandlerKind.Mediator, route.Handler.HandlerKind, "route handler kind");

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
  /// Parses code and finds the first Map invocation.
  /// </summary>
  private static InvocationExpressionSyntax? FindMapInvocation(string code)
  {
    SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
    CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

    foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
    {
      string? methodName = invocation.Expression switch
      {
        MemberAccessExpressionSyntax memberAccess => memberAccess.Name switch
        {
          GenericNameSyntax generic => generic.Identifier.Text,
          IdentifierNameSyntax identifier => identifier.Identifier.Text,
          _ => null
        },
        IdentifierNameSyntax identifier => identifier.Identifier.Text,
        GenericNameSyntax generic => generic.Identifier.Text,
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
  /// Parses code and finds the first class declaration.
  /// </summary>
  private static ClassDeclarationSyntax? FindClassDeclaration(string code)
  {
    SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
    CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

    return root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
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
