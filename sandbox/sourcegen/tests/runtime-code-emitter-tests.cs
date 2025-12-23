// sandbox/sourcegen/tests/runtime-code-emitter-tests.cs
// Tests for RuntimeCodeEmitter (Step-4)
//
// Agent: Amina
// Task: #242-step-4

namespace TimeWarp.Nuru.SourceGen.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TimeWarp.Nuru.SourceGen.Emitters;
using System.Collections.Immutable;

/// <summary>
/// Tests for emitting runtime C# code from RouteDefinition.
/// </summary>
public static class RuntimeCodeEmitterTests
{
  public static int Run()
  {
    Console.WriteLine("=== RuntimeCodeEmitter Tests (Step-4) ===");
    Console.WriteLine();

    int passed = 0;
    int failed = 0;

    passed += TestEmitSimpleRoute(ref failed);
    passed += TestEmitRouteWithTypedParameters(ref failed);
    passed += TestEmitMultipleRoutes(ref failed);
    passed += TestEmittedCodeCompiles(ref failed);
    passed += TestEmittedCodeHasCorrectStructure(ref failed);

    Console.WriteLine();
    Console.WriteLine("==============================================");
    Console.WriteLine($"Results: {passed} passed, {failed} failed");
    Console.WriteLine("==============================================");

    return failed > 0 ? 1 : 0;
  }

  /// <summary>
  /// Test emitting a simple route with string parameters
  /// </summary>
  private static int TestEmitSimpleRoute(ref int failed)
  {
    Console.WriteLine("Test: Emit simple route (greet {name})");

    try
    {
      // Build a simple route definition
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
        .WithMessageType("Command")
        .WithDescription("Greet someone")
        .Build();

      RuntimeCodeEmitter.EmitResult result = RuntimeCodeEmitter.EmitSourceFile(
        [route],
        new RuntimeCodeEmitter.EmitOptions(Namespace: "TestApp"));

      AssertContains(result.SourceCode, "greet {name}", "contains pattern");
      AssertContains(result.SourceCode, "LiteralMatcher(0, \"greet\")", "has literal matcher");
      AssertContains(result.SourceCode, "StringParameterMatcher(1, \"name\")", "has string param matcher");
      AssertContains(result.SourceCode, "TypeConverter.ToString", "has string converter");

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
  /// Test emitting a route with typed parameters
  /// </summary>
  private static int TestEmitRouteWithTypedParameters(ref int failed)
  {
    Console.WriteLine("Test: Emit route with typed parameters (add {x:int} {y:int})");

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
        .WithMessageType("Query")
        .WithDescription("Add two integers")
        .Build();

      RuntimeCodeEmitter.EmitResult result = RuntimeCodeEmitter.EmitSourceFile([route]);

      AssertContains(result.SourceCode, "IntParameterMatcher(1, \"x\")", "has int param matcher for x");
      AssertContains(result.SourceCode, "IntParameterMatcher(2, \"y\")", "has int param matcher for y");
      AssertContains(result.SourceCode, "TypeConverter.ToInt32", "has int converter");
      AssertContains(result.SourceCode, "(int)parameters[\"x\"]", "extracts x as int");
      AssertContains(result.SourceCode, "(int)parameters[\"y\"]", "extracts y as int");

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
  /// Test emitting multiple routes
  /// </summary>
  private static int TestEmitMultipleRoutes(ref int failed)
  {
    Console.WriteLine("Test: Emit multiple routes");

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

      RuntimeCodeEmitter.EmitResult result = RuntimeCodeEmitter.EmitSourceFile([route1, route2]);

      AssertContains(result.SourceCode, "Route_0", "has first route field");
      AssertContains(result.SourceCode, "Route_1", "has second route field");
      AssertContains(result.SourceCode, "CompiledRoute[] All", "has All array");

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
  /// Test that emitted code compiles without errors
  /// </summary>
  private static int TestEmittedCodeCompiles(ref int failed)
  {
    Console.WriteLine("Test: Emitted code compiles without errors");

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

      RuntimeCodeEmitter.EmitResult result = RuntimeCodeEmitter.EmitSourceFile([route]);

      // Try to compile the emitted code
      SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(result.SourceCode);
      CSharpCompilation compilation = CSharpCompilation.Create(
        "TestAssembly",
        [syntaxTree],
        [
          MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
          MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
          MetadataReference.CreateFromFile(typeof(Dictionary<,>).Assembly.Location),
          MetadataReference.CreateFromFile(typeof(ImmutableArray<>).Assembly.Location),
          MetadataReference.CreateFromFile(typeof(Func<>).Assembly.Location),
        ],
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

      ImmutableArray<Diagnostic> diagnostics = compilation.GetDiagnostics();
      ImmutableArray<Diagnostic> errors = diagnostics
        .Where(d => d.Severity == DiagnosticSeverity.Error)
        .ToImmutableArray();

      if (errors.Length > 0)
      {
        Console.WriteLine("    Compilation errors:");
        foreach (Diagnostic error in errors.Take(5))
        {
          Console.WriteLine($"      {error.GetMessage()}");
        }
        throw new Exception($"Emitted code has {errors.Length} compilation error(s)");
      }

      Console.WriteLine($"    No compilation errors");
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
  /// Test that emitted code has the correct structure
  /// </summary>
  private static int TestEmittedCodeHasCorrectStructure(ref int failed)
  {
    Console.WriteLine("Test: Emitted code has correct structure");

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

      RuntimeCodeEmitter.EmitResult result = RuntimeCodeEmitter.EmitSourceFile(
        [route],
        new RuntimeCodeEmitter.EmitOptions(
          Namespace: "MyApp.Generated",
          ClassName: "Routes"));

      // Check structure
      AssertContains(result.SourceCode, "// <auto-generated/>", "has auto-generated comment");
      AssertContains(result.SourceCode, "namespace MyApp.Generated", "has correct namespace");
      AssertContains(result.SourceCode, "internal static class Routes", "has correct class name");
      AssertContains(result.SourceCode, "internal sealed class CompiledRoute", "has CompiledRoute type");
      AssertContains(result.SourceCode, "internal interface ISegmentMatcher", "has ISegmentMatcher interface");
      AssertContains(result.SourceCode, "internal sealed class LiteralMatcher", "has LiteralMatcher type");
      AssertContains(result.SourceCode, "internal static class TypeConverter", "has TypeConverter type");

      AssertEquals("Routes.g.cs", result.FileName, "has correct filename");

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

  private static void AssertContains(string source, string substring, string description)
  {
    if (!source.Contains(substring, StringComparison.Ordinal))
    {
      throw new Exception($"{description}: source does not contain '{substring}'");
    }
    Console.WriteLine($"    {description}: ✓");
  }

  private static void AssertEquals<T>(T expected, T actual, string description)
  {
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
      throw new Exception($"{description}: expected '{expected}', got '{actual}'");
    }
    Console.WriteLine($"    {description}: {actual}");
  }

  #endregion
}
