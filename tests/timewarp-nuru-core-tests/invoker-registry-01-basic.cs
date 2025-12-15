#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Tests for InvokerRegistry signature key computation
// Verifies runtime signature keys match source generator output

using System.Reflection;
using TimeWarp.Jaribu;
using TimeWarp.Nuru;
using Shouldly;
using static System.Console;
using static TimeWarp.Jaribu.TestRunner;

return await RunTests<InvokerRegistryTests>();

/// <summary>
/// Tests for InvokerRegistry.ComputeSignatureKey
/// </summary>
[TestTag("InvokerRegistry")]
public sealed class InvokerRegistryTests
{
  /// <summary>
  /// Verify signature key for Action (no params, void return)
  /// </summary>
  public static async Task Should_compute_key_for_action_no_params()
  {
    // Arrange
    Action handler = () => { };
    MethodInfo method = handler.Method;

    // Act
    string key = InvokerRegistry.ComputeSignatureKey(method);

    // Assert
    key.ShouldBe("NoParams");
    WriteLine($"Action() -> {key}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Verify signature key for Action{string}
  /// </summary>
  public static async Task Should_compute_key_for_action_string()
  {
    // Arrange
    Action<string> handler = _ => { };
    MethodInfo method = handler.Method;

    // Act
    string key = InvokerRegistry.ComputeSignatureKey(method);

    // Assert
    key.ShouldBe("String");
    WriteLine($"Action<string> -> {key}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Verify signature key for Action{int}
  /// </summary>
  public static async Task Should_compute_key_for_action_int()
  {
    // Arrange
    Action<int> handler = _ => { };
    MethodInfo method = handler.Method;

    // Act
    string key = InvokerRegistry.ComputeSignatureKey(method);

    // Assert
    key.ShouldBe("Int");
    WriteLine($"Action<int> -> {key}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Verify signature key for Action{string, string}
  /// </summary>
  public static async Task Should_compute_key_for_action_string_string()
  {
    // Arrange
    Action<string, string> handler = (_, _) => { };
    MethodInfo method = handler.Method;

    // Act
    string key = InvokerRegistry.ComputeSignatureKey(method);

    // Assert
    key.ShouldBe("String_String");
    WriteLine($"Action<string, string> -> {key}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Verify signature key for Action{string[]}
  /// </summary>
  public static async Task Should_compute_key_for_action_string_array()
  {
    // Arrange
    Action<string[]> handler = _ => { };
    MethodInfo method = handler.Method;

    // Act
    string key = InvokerRegistry.ComputeSignatureKey(method);

    // Assert
    key.ShouldBe("StringArrayArray");
    WriteLine($"Action<string[]> -> {key}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Verify signature key for Action{int?}
  /// </summary>
  public static async Task Should_compute_key_for_action_nullable_int()
  {
    // Arrange
    Action<int?> handler = _ => { };
    MethodInfo method = handler.Method;

    // Act
    string key = InvokerRegistry.ComputeSignatureKey(method);

    // Assert
    key.ShouldBe("NullableInt");
    WriteLine($"Action<int?> -> {key}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Verify signature key for Func{int, int, int}
  /// </summary>
  public static async Task Should_compute_key_for_func_int_int_returns_int()
  {
    // Arrange
    Func<int, int, int> handler = (x, y) => x + y;
    MethodInfo method = handler.Method;

    // Act
    string key = InvokerRegistry.ComputeSignatureKey(method);

    // Assert
    key.ShouldBe("Int_Int_Returns_Int");
    WriteLine($"Func<int, int, int> -> {key}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Verify signature key for Func{Task} (async void)
  /// </summary>
  public static async Task Should_compute_key_for_func_task()
  {
    // Arrange
    Func<Task> handler = async () => await Task.CompletedTask;
    MethodInfo method = handler.Method;

    // Act
    string key = InvokerRegistry.ComputeSignatureKey(method);

    // Assert
    key.ShouldBe("_Returns_Task");
    WriteLine($"Func<Task> -> {key}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Verify signature key for Func{string, Task}
  /// </summary>
  public static async Task Should_compute_key_for_func_string_task()
  {
    // Arrange
    Func<string, Task> handler = async _ => await Task.CompletedTask;
    MethodInfo method = handler.Method;

    // Act
    string key = InvokerRegistry.ComputeSignatureKey(method);

    // Assert
    key.ShouldBe("String_Returns_Task");
    WriteLine($"Func<string, Task> -> {key}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Verify signature key for Func{string, Task{int}}
  /// </summary>
  public static async Task Should_compute_key_for_func_string_task_int()
  {
    // Arrange
    Func<string, Task<int>> handler = async _ => { await Task.CompletedTask; return 42; };
    MethodInfo method = handler.Method;

    // Act
    string key = InvokerRegistry.ComputeSignatureKey(method);

    // Assert
    key.ShouldBe("String_Returns_TaskInt");
    WriteLine($"Func<string, Task<int>> -> {key}");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Verify registry registration and lookup works
  /// </summary>
  public static async Task Should_register_and_lookup_sync_invoker()
  {
    // Arrange
    InvokerRegistry.Clear();
    bool invoked = false;
    SyncInvoker testInvoker = (_, _) => { invoked = true; return null; };

    // Act
    InvokerRegistry.RegisterSync("TestKey", testInvoker);
    bool found = InvokerRegistry.TryGetSync("TestKey", out SyncInvoker? retrieved);

    // Assert
    found.ShouldBeTrue();
    retrieved.ShouldNotBeNull();

    // Invoke it to verify it's the right one
    retrieved!(null!, []);
    invoked.ShouldBeTrue();

    WriteLine("Sync invoker registration and lookup works");
    InvokerRegistry.Clear();

    await Task.CompletedTask;
  }

  /// <summary>
  /// Verify registry returns false for unknown keys
  /// </summary>
  public static async Task Should_return_false_for_unknown_key()
  {
    // Arrange
    InvokerRegistry.Clear();

    // Act
    bool found = InvokerRegistry.TryGetSync("NonExistentKey", out SyncInvoker? invoker);

    // Assert
    found.ShouldBeFalse();
    invoker.ShouldBeNull();

    WriteLine("Returns false for unknown keys");

    await Task.CompletedTask;
  }
}
