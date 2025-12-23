// sandbox/sourcegen/tests/attributed-route-extractor-tests.cs
// Tests for AttributedRouteExtractor (Source 3)
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
/// Tests for extracting route definitions from attributed classes.
/// </summary>
public static class AttributedRouteExtractorTests
{
  public static int Run()
  {
    Console.WriteLine("=== AttributedRouteExtractor Tests (Source 3) ===");
    Console.WriteLine();

    int passed = 0;
    int failed = 0;

    passed += TestExtractRouteAttribute(ref failed);
    passed += TestExtractDescriptionAttribute(ref failed);
    passed += TestExtractQueryAttribute(ref failed);
    passed += TestExtractCommandAttribute(ref failed);
    passed += TestExtractResponseType(ref failed);
    passed += TestExtractProperties(ref failed);
    passed += TestExtractRequiredProperty(ref failed);
    passed += TestCompleteAttributedRoute(ref failed);

    Console.WriteLine();
    Console.WriteLine("==============================================");
    Console.WriteLine($"Results: {passed} passed, {failed} failed");
    Console.WriteLine("==============================================");

    return failed > 0 ? 1 : 0;
  }

  /// <summary>
  /// Test extracting pattern from [Route("...")] attribute
  /// </summary>
  private static int TestExtractRouteAttribute(ref int failed)
  {
    Console.WriteLine("Test: Extract pattern from [Route(\"...\")]");

    try
    {
      string code = @"
        [Route(""add {x:int} {y:int}"")]
        public class AddCommand { }
      ";

      ClassDeclarationSyntax? classDecl = FindClassDeclaration(code);
      if (classDecl is null)
      {
        throw new Exception("Could not find class declaration");
      }

      AttributedRouteExtractor.AttributedRouteResult result =
        AttributedRouteExtractor.ExtractFromClass(classDecl);

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
  /// Test extracting description from [Description("...")] attribute
  /// </summary>
  private static int TestExtractDescriptionAttribute(ref int failed)
  {
    Console.WriteLine("Test: Extract description from [Description(\"...\")]");

    try
    {
      string code = @"
        [Route(""add {x:int} {y:int}"")]
        [Description(""Add two integers together"")]
        public class AddCommand { }
      ";

      ClassDeclarationSyntax? classDecl = FindClassDeclaration(code);
      AttributedRouteExtractor.AttributedRouteResult result =
        AttributedRouteExtractor.ExtractFromClass(classDecl!);

      AssertEquals("Add two integers together", result.Description, "description");

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
  /// Test extracting [Query] attribute as message type
  /// </summary>
  private static int TestExtractQueryAttribute(ref int failed)
  {
    Console.WriteLine("Test: Extract message type from [Query]");

    try
    {
      string code = @"
        [Route(""status"")]
        [Query]
        public class StatusQuery { }
      ";

      ClassDeclarationSyntax? classDecl = FindClassDeclaration(code);
      AttributedRouteExtractor.AttributedRouteResult result =
        AttributedRouteExtractor.ExtractFromClass(classDecl!);

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
  /// Test extracting [Command] attribute as message type
  /// </summary>
  private static int TestExtractCommandAttribute(ref int failed)
  {
    Console.WriteLine("Test: Extract message type from [Command]");

    try
    {
      string code = @"
        [Route(""deploy {env}"")]
        [Command]
        public class DeployCommand { }
      ";

      ClassDeclarationSyntax? classDecl = FindClassDeclaration(code);
      AttributedRouteExtractor.AttributedRouteResult result =
        AttributedRouteExtractor.ExtractFromClass(classDecl!);

      AssertEquals("Command", result.MessageType, "message type");

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
  /// Test extracting response type from IRequest&lt;T&gt;
  /// </summary>
  private static int TestExtractResponseType(ref int failed)
  {
    Console.WriteLine("Test: Extract response type from IRequest<T>");

    try
    {
      string code = @"
        [Route(""add {x:int} {y:int}"")]
        public class AddCommand : IRequest<int> { }
      ";

      ClassDeclarationSyntax? classDecl = FindClassDeclaration(code);
      AttributedRouteExtractor.AttributedRouteResult result =
        AttributedRouteExtractor.ExtractFromClass(classDecl!);

      AssertEquals("global::System.Int32", result.ResponseTypeName, "response type");

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
  /// Test extracting public properties
  /// </summary>
  private static int TestExtractProperties(ref int failed)
  {
    Console.WriteLine("Test: Extract public properties");

    try
    {
      string code = @"
        [Route(""add {x:int} {y:int}"")]
        public class AddCommand : IRequest<int>
        {
          public int X { get; set; }
          public int Y { get; set; }
          private string Internal { get; set; }
          public static int StaticProp { get; set; }
          public int ReadOnly { get; }
        }
      ";

      ClassDeclarationSyntax? classDecl = FindClassDeclaration(code);
      AttributedRouteExtractor.AttributedRouteResult result =
        AttributedRouteExtractor.ExtractFromClass(classDecl!);

      // Should only extract X and Y (not Internal, StaticProp, or ReadOnly)
      AssertEquals(2, result.Properties.Length, "property count");
      AssertEquals("X", result.Properties[0].Name, "prop 0 name");
      AssertEquals("global::System.Int32", result.Properties[0].TypeName, "prop 0 type");
      AssertEquals("Y", result.Properties[1].Name, "prop 1 name");

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
  /// Test extracting required property
  /// </summary>
  private static int TestExtractRequiredProperty(ref int failed)
  {
    Console.WriteLine("Test: Extract required property");

    try
    {
      // Note: C# 11 'required' keyword
      string code = @"
        [Route(""greet {name}"")]
        public class GreetCommand : IRequest<string>
        {
          public required string Name { get; set; }
          public string? OptionalMessage { get; set; }
        }
      ";

      ClassDeclarationSyntax? classDecl = FindClassDeclaration(code);
      AttributedRouteExtractor.AttributedRouteResult result =
        AttributedRouteExtractor.ExtractFromClass(classDecl!);

      AssertEquals(2, result.Properties.Length, "property count");

      AttributedRouteExtractor.PropertyInfo nameProp = result.Properties[0];
      AssertEquals("Name", nameProp.Name, "name prop name");
      AssertEquals(true, nameProp.IsRequired, "name is required");

      AttributedRouteExtractor.PropertyInfo msgProp = result.Properties[1];
      AssertEquals("OptionalMessage", msgProp.Name, "optional prop name");
      AssertEquals(false, msgProp.IsRequired, "optional is not required");

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
  /// Test complete attributed route extraction and conversion to RouteDefinition
  /// </summary>
  private static int TestCompleteAttributedRoute(ref int failed)
  {
    Console.WriteLine("Test: Complete attributed route → RouteDefinition");

    try
    {
      string code = @"
        namespace MyApp.Commands
        {
          [Route(""add {x:int} {y:int}"")]
          [Description(""Add two integers"")]
          [Query]
          public class AddCommand : IRequest<int>
          {
            public int X { get; set; }
            public int Y { get; set; }
          }
        }
      ";

      // 1. Extract from class
      ClassDeclarationSyntax? classDecl = FindClassDeclaration(code);
      AttributedRouteExtractor.AttributedRouteResult attrResult =
        AttributedRouteExtractor.ExtractFromClass(classDecl!);

      AssertEquals("add {x:int} {y:int}", attrResult.Pattern, "pattern");
      AssertEquals("Add two integers", attrResult.Description, "description");
      AssertEquals("Query", attrResult.MessageType, "message type");
      AssertEquals("global::System.Int32", attrResult.ResponseTypeName, "response type");
      AssertEquals("global::MyApp.Commands.AddCommand", attrResult.RequestTypeName, "request type");
      AssertEquals(2, attrResult.Properties.Length, "property count");

      // 2. Parse pattern to get segments
      Parser parser = new();
      ParseResult<TimeWarp.Nuru.Syntax> parseResult = parser.Parse(attrResult.Pattern!);
      if (!parseResult.Success)
      {
        throw new Exception("Pattern parse failed");
      }

      ImmutableArray<SegmentDefinition> segments =
        SegmentDefinitionConverter.FromSyntax(parseResult.Value!.Segments);

      AssertEquals(3, segments.Length, "segment count");

      // 3. Build bindings from properties
      ImmutableArray<ParameterBinding> bindings =
        AttributedRouteExtractor.BuildBindingsFromProperties(attrResult.Properties, segments);

      AssertEquals(2, bindings.Length, "binding count");
      AssertEquals("X", bindings[0].ParameterName, "binding 0 name");
      AssertEquals(BindingSource.Parameter, bindings[0].Source, "binding 0 source");
      AssertEquals("Y", bindings[1].ParameterName, "binding 1 name");

      // 4. Build handler definition (mediator style)
      HandlerDefinition handler = new HandlerDefinitionBuilder()
        .AsMediator(attrResult.RequestTypeName!)
        .ReturnsTaskOf("global::System.Int32", "int")
        .Build();

      AssertEquals(HandlerKind.Mediator, handler.HandlerKind, "handler kind");
      AssertEquals(true, handler.IsAsync, "is async");
      AssertEquals(true, handler.RequiresCancellationToken, "requires CT");
      AssertEquals(true, handler.RequiresServiceProvider, "requires SP");

      // 5. Assemble RouteDefinition
      RouteDefinition route = new RouteDefinitionBuilder()
        .WithPattern(attrResult.Pattern!)
        .WithSegments(segments)
        .WithHandler(handler)
        .WithMessageType(attrResult.MessageType!)
        .WithDescription(attrResult.Description)
        .Build();

      // 6. Verify complete model
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
