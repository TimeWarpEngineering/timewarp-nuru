// sandbox/sourcegen/tests/handler-definition-builder-tests.cs
// Tests for HandlerDefinitionBuilder
//
// Agent: Amina
// Task: #242-step-3

namespace TimeWarp.Nuru.SourceGen.Tests;

using System.Collections.Immutable;

/// <summary>
/// Tests for HandlerDefinitionBuilder.
/// Verifies we can correctly build handler definitions for various delegate signatures.
/// </summary>
public static class HandlerDefinitionBuilderTests
{
  public static int Run()
  {
    Console.WriteLine("=== HandlerDefinitionBuilder Tests ===");
    Console.WriteLine();

    int passed = 0;
    int failed = 0;

    passed += TestSimpleSyncHandler(ref failed);
    passed += TestSyncHandlerWithReturnValue(ref failed);
    passed += TestAsyncTaskHandler(ref failed);
    passed += TestAsyncTaskOfTHandler(ref failed);
    passed += TestHandlerWithOptions(ref failed);
    passed += TestHandlerWithCancellationToken(ref failed);
    passed += TestMediatorHandler(ref failed);

    Console.WriteLine();
    Console.WriteLine("==============================================");
    Console.WriteLine($"Results: {passed} passed, {failed} failed");
    Console.WriteLine("==============================================");

    return failed > 0 ? 1 : 0;
  }

  /// <summary>
  /// Test: (int x, int y) => { }
  /// Sync handler with void return.
  /// </summary>
  private static int TestSimpleSyncHandler(ref int failed)
  {
    Console.WriteLine("Test: (int x, int y) => { } - sync void");

    try
    {
      HandlerDefinition handler = new HandlerDefinitionBuilder()
        .AsDelegate()
        .WithParameter("x", "int")
        .WithParameter("y", "int")
        .ReturnsVoid()
        .Build();

      AssertEquals(HandlerKind.Delegate, handler.HandlerKind, "handler kind");
      AssertEquals(false, handler.IsAsync, "is async");
      AssertEquals(true, handler.ReturnType.IsVoid, "return is void");
      AssertEquals(2, handler.Parameters.Length, "parameter count");

      ParameterBinding paramX = handler.Parameters[0];
      AssertEquals("x", paramX.ParameterName, "param x name");
      AssertEquals("global::System.Int32", paramX.ParameterTypeName, "param x type");
      AssertEquals(true, paramX.RequiresConversion, "param x requires conversion");
      AssertEquals(BindingSource.Parameter, paramX.Source, "param x source");

      ParameterBinding paramY = handler.Parameters[1];
      AssertEquals("y", paramY.ParameterName, "param y name");

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
  /// Test: (int x, int y) => x + y
  /// Sync handler with int return.
  /// </summary>
  private static int TestSyncHandlerWithReturnValue(ref int failed)
  {
    Console.WriteLine("Test: (int x, int y) => x + y - sync int return");

    try
    {
      HandlerDefinition handler = new HandlerDefinitionBuilder()
        .AsDelegate()
        .WithParameter("x", "int")
        .WithParameter("y", "int")
        .Returns("int")
        .Build();

      AssertEquals(false, handler.IsAsync, "is async");
      AssertEquals(false, handler.ReturnType.IsVoid, "return is void");
      AssertEquals(false, handler.ReturnType.IsTask, "return is task");
      AssertEquals("global::System.Int32", handler.ReturnType.FullTypeName, "return type full name");
      AssertEquals("int", handler.ReturnType.ShortTypeName, "return type short name");

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
  /// Test: async (string name) => await Task.Delay(100)
  /// Async handler returning Task.
  /// </summary>
  private static int TestAsyncTaskHandler(ref int failed)
  {
    Console.WriteLine("Test: async (string name) => await ... - async Task");

    try
    {
      HandlerDefinition handler = new HandlerDefinitionBuilder()
        .AsDelegate()
        .WithParameter("name", "string")
        .ReturnsTask()
        .Build();

      AssertEquals(true, handler.IsAsync, "is async");
      AssertEquals(false, handler.ReturnType.IsVoid, "return is void");
      AssertEquals(true, handler.ReturnType.IsTask, "return is task");
      AssertEquals(null, handler.ReturnType.UnwrappedTypeName, "unwrapped type (Task has none)");

      ParameterBinding param = handler.Parameters[0];
      AssertEquals("name", param.ParameterName, "param name");
      AssertEquals("global::System.String", param.ParameterTypeName, "param type");
      AssertEquals(false, param.RequiresConversion, "string doesn't require conversion");

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
  /// Test: async (int id) => await GetNameAsync(id)
  /// Async handler returning Task&lt;string&gt;.
  /// </summary>
  private static int TestAsyncTaskOfTHandler(ref int failed)
  {
    Console.WriteLine("Test: async (int id) => await GetNameAsync(id) - async Task<string>");

    try
    {
      HandlerDefinition handler = new HandlerDefinitionBuilder()
        .AsDelegate()
        .WithParameter("id", "int")
        .Returns("Task<string>")
        .Build();

      AssertEquals(true, handler.IsAsync, "is async");
      AssertEquals(true, handler.ReturnType.IsTask, "return is task");
      AssertEquals("global::System.String", handler.ReturnType.UnwrappedTypeName, "unwrapped type");
      AssertEquals("Task<string>", handler.ReturnType.ShortTypeName, "short type name");

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
  /// Test: (string env, bool force, int count) => { }
  /// Handler with option parameters including a flag.
  /// </summary>
  private static int TestHandlerWithOptions(ref int failed)
  {
    Console.WriteLine("Test: (string env, bool force, int count) - with options");

    try
    {
      HandlerDefinition handler = new HandlerDefinitionBuilder()
        .AsDelegate()
        .WithParameter("env", "string")
        .WithFlagParameter("force", "force")
        .WithOptionParameter("count", "int", "count")
        .ReturnsVoid()
        .Build();

      AssertEquals(3, handler.Parameters.Length, "parameter count");

      ParameterBinding paramEnv = handler.Parameters[0];
      AssertEquals(BindingSource.Parameter, paramEnv.Source, "env source");

      ParameterBinding paramForce = handler.Parameters[1];
      AssertEquals("force", paramForce.ParameterName, "force name");
      AssertEquals(BindingSource.Flag, paramForce.Source, "force source");
      AssertEquals("global::System.Boolean", paramForce.ParameterTypeName, "force type");

      ParameterBinding paramCount = handler.Parameters[2];
      AssertEquals("count", paramCount.ParameterName, "count name");
      AssertEquals(BindingSource.Option, paramCount.Source, "count source");
      AssertEquals(true, paramCount.RequiresConversion, "count requires conversion");

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
  /// Test: async (string name, CancellationToken ct) => await ...
  /// Handler with CancellationToken.
  /// </summary>
  private static int TestHandlerWithCancellationToken(ref int failed)
  {
    Console.WriteLine("Test: async (string name, CancellationToken ct) - with cancellation");

    try
    {
      HandlerDefinition handler = new HandlerDefinitionBuilder()
        .AsDelegate()
        .WithParameter("name", "string")
        .WithCancellationToken("ct")
        .ReturnsTask()
        .Build();

      AssertEquals(true, handler.RequiresCancellationToken, "requires cancellation token");
      AssertEquals(2, handler.Parameters.Length, "parameter count");

      ParameterBinding ctParam = handler.Parameters[1];
      AssertEquals("ct", ctParam.ParameterName, "ct name");
      AssertEquals(BindingSource.CancellationToken, ctParam.Source, "ct source");
      AssertEquals("global::System.Threading.CancellationToken", ctParam.ParameterTypeName, "ct type");

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
  /// Test: Mediator-based handler for IRequest&lt;T&gt;.
  /// </summary>
  private static int TestMediatorHandler(ref int failed)
  {
    Console.WriteLine("Test: Mediator handler for AddCommand : IRequest<int>");

    try
    {
      HandlerDefinition handler = new HandlerDefinitionBuilder()
        .AsMediator("MyApp.Commands.AddCommand")
        .WithParameter("x", "int")
        .WithParameter("y", "int")
        .ReturnsTaskOf("global::System.Int32", "int")
        .Build();

      AssertEquals(HandlerKind.Mediator, handler.HandlerKind, "handler kind");
      AssertEquals("MyApp.Commands.AddCommand", handler.FullTypeName, "full type name");
      AssertEquals(true, handler.IsAsync, "is async");
      AssertEquals(true, handler.RequiresCancellationToken, "requires cancellation token");
      AssertEquals(true, handler.RequiresServiceProvider, "requires service provider");
      AssertEquals(true, handler.ReturnType.IsTask, "return is task");
      AssertEquals("global::System.Int32", handler.ReturnType.UnwrappedTypeName, "unwrapped type");

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

  #endregion
}
