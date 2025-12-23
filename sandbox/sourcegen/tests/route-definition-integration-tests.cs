// sandbox/sourcegen/tests/route-definition-integration-tests.cs
// Integration tests: Build complete RouteDefinition from pattern + handler
//
// Agent: Amina
// Task: #242-step-3

namespace TimeWarp.Nuru.SourceGen.Tests;

using System.Collections.Immutable;
using TimeWarp.Nuru;

/// <summary>
/// Integration tests that build complete RouteDefinition objects
/// by combining segment conversion with handler definition.
/// These represent what the source generator will produce.
/// </summary>
public static class RouteDefinitionIntegrationTests
{
  public static int Run()
  {
    Console.WriteLine("=== RouteDefinition Integration Tests ===");
    Console.WriteLine();

    int passed = 0;
    int failed = 0;

    passed += TestAddCommand(ref failed);
    passed += TestDeployWithOptions(ref failed);
    passed += TestAsyncHandler(ref failed);

    Console.WriteLine();
    Console.WriteLine("==============================================");
    Console.WriteLine($"Results: {passed} passed, {failed} failed");
    Console.WriteLine("==============================================");

    return failed > 0 ? 1 : 0;
  }

  /// <summary>
  /// Test: Map("add {x:int} {y:int}").WithHandler((int x, int y) => x + y).AsQuery()
  /// </summary>
  private static int TestAddCommand(ref int failed)
  {
    Console.WriteLine("Test: add {x:int} {y:int} with (int x, int y) => x + y");

    try
    {
      // 1. Parse pattern and convert segments
      Syntax syntax = Parse("add {x:int} {y:int}");
      ImmutableArray<SegmentDefinition> segments = SegmentDefinitionConverter.FromSyntax(syntax.Segments);

      // 2. Build handler definition (simulating what Roslyn extraction would produce)
      HandlerDefinition handler = new HandlerDefinitionBuilder()
        .AsDelegate()
        .WithParameter("x", "int")
        .WithParameter("y", "int")
        .Returns("int")
        .Build();

      // 3. Assemble complete RouteDefinition
      RouteDefinition route = new RouteDefinitionBuilder()
        .WithPattern("add {x:int} {y:int}")
        .WithSegments(segments)
        .WithHandler(handler)
        .WithMessageType("Query")
        .WithDescription("Add two integers")
        .Build();

      // 4. Verify the complete model
      AssertEquals("add {x:int} {y:int}", route.OriginalPattern, "pattern");
      AssertEquals("Query", route.MessageType, "message type");
      AssertEquals("Add two integers", route.Description, "description");
      AssertEquals(3, route.Segments.Length, "segment count");

      AssertEquals(HandlerKind.Delegate, route.Handler.HandlerKind, "handler kind");
      AssertEquals(2, route.Handler.Parameters.Length, "handler parameter count");
      AssertEquals(false, route.Handler.IsAsync, "handler is async");
      AssertEquals("global::System.Int32", route.Handler.ReturnType.FullTypeName, "return type");

      // Verify parameter binding matches segments
      ParameterBinding bindX = route.Handler.Parameters[0];
      AssertEquals("x", bindX.ParameterName, "binding x name");
      AssertEquals("x", bindX.SourceName, "binding x source");
      AssertEquals(BindingSource.Parameter, bindX.Source, "binding x source type");

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
  /// Test: Map("deploy {env} --force,-f").WithHandler((string env, bool force) => ...).AsCommand()
  /// </summary>
  private static int TestDeployWithOptions(ref int failed)
  {
    Console.WriteLine("Test: deploy {env} --force,-f with (string env, bool force) => ...");

    try
    {
      // 1. Parse pattern and convert segments
      Syntax syntax = Parse("deploy {env} --force,-f");
      ImmutableArray<SegmentDefinition> segments = SegmentDefinitionConverter.FromSyntax(syntax.Segments);

      // 2. Build handler definition
      HandlerDefinition handler = new HandlerDefinitionBuilder()
        .AsDelegate()
        .WithParameter("env", "string")
        .WithFlagParameter("force", "force")
        .ReturnsVoid()
        .Build();

      // 3. Assemble complete RouteDefinition
      RouteDefinition route = new RouteDefinitionBuilder()
        .WithPattern("deploy {env} --force,-f")
        .WithSegments(segments)
        .WithHandler(handler)
        .WithMessageType("Command")
        .Build();

      // 4. Verify
      AssertEquals("deploy {env} --force,-f", route.OriginalPattern, "pattern");
      AssertEquals("Command", route.MessageType, "message type");
      AssertEquals(3, route.Segments.Length, "segment count (literal + param + option)");

      // Verify option segment
      OptionDefinition optionSeg = (OptionDefinition)route.Segments[2];
      AssertEquals("force", optionSeg.LongForm, "option long form");
      AssertEquals("f", optionSeg.ShortForm, "option short form");

      // Verify handler has flag binding
      ParameterBinding flagBinding = route.Handler.Parameters[1];
      AssertEquals("force", flagBinding.ParameterName, "flag param name");
      AssertEquals(BindingSource.Flag, flagBinding.Source, "flag binding source");

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
  /// Test: Map("fetch {url}").WithHandler(async (string url, CancellationToken ct) => await ...).AsQuery()
  /// </summary>
  private static int TestAsyncHandler(ref int failed)
  {
    Console.WriteLine("Test: fetch {url} with async (string url, CancellationToken ct) => ...");

    try
    {
      // 1. Parse pattern and convert segments
      Syntax syntax = Parse("fetch {url}");
      ImmutableArray<SegmentDefinition> segments = SegmentDefinitionConverter.FromSyntax(syntax.Segments);

      // 2. Build handler definition
      HandlerDefinition handler = new HandlerDefinitionBuilder()
        .AsDelegate()
        .WithParameter("url", "string")
        .WithCancellationToken("ct")
        .Returns("Task<string>")
        .Build();

      // 3. Assemble complete RouteDefinition
      RouteDefinition route = new RouteDefinitionBuilder()
        .WithPattern("fetch {url}")
        .WithSegments(segments)
        .WithHandler(handler)
        .WithMessageType("Query")
        .Build();

      // 4. Verify async handling
      AssertEquals(true, route.Handler.IsAsync, "handler is async");
      AssertEquals(true, route.Handler.RequiresCancellationToken, "requires cancellation token");
      AssertEquals(true, route.Handler.ReturnType.IsTask, "return is task");
      AssertEquals("global::System.String", route.Handler.ReturnType.UnwrappedTypeName, "unwrapped return type");

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

  private static Syntax Parse(string pattern)
  {
    Parser parser = new();
    ParseResult<Syntax> result = parser.Parse(pattern);

    if (!result.Success)
    {
      string errors = string.Join(", ", result.ParseErrors?.Select(e => e.ToString()) ?? []);
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

  #endregion
}
