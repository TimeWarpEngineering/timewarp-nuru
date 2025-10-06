#!/usr/bin/dotnet --

return await RunTests<DiscoveryTests>(clearCache: true);

[TestTag("Kijaribu")]
[ClearRunfileCache]
public class DiscoveryTests
{
  /// <summary>
  /// Basic test method execution - Simple passing test.
  /// </summary>
  public static async Task BasicTest()
  {
    // No-op: passes by default
    await Task.CompletedTask;
  }

  /// <summary>
  /// Non-qualifying methods - These should be skipped.
  /// </summary>
  public static void NonAsyncVoidTest()
  {
    // Sync void: should skip
  }

  private static async Task PrivateAsyncTest()
  {
    // Private: should skip
    await Task.CompletedTask;
  }

  public static async Task Setup()
  {
    // Named Setup: invoked before tests
    WriteLine("Setup invoked - preparing test environment");
    await Task.CompletedTask;
  }

  public static async Task CleanUp()
  {
    // Named CleanUp: invoked after tests (async)
    // Log or assert something here for REPORT-03 validation
    WriteLine("CleanUp invoked");
    await Task.CompletedTask;
  }

  /// <summary>
  /// Failing test for multi-test validation.
  /// </summary>
  public static async Task FailingTest()
  {
    await Task.CompletedTask;
    throw new ArgumentException("Intentional failure");
  }

  /// <summary>
  /// Another passing test.
  /// </summary>
  public static async Task PassingTest2()
  {
    await Task.CompletedTask;
  }

  /// <summary>
  /// Async test with await.
  /// </summary>
  public static async Task AsyncAwaitTest()
  {
    await Task.Delay(1); // Simulates async work
  }

  /// <summary>
  /// ValueTask test (future enhancement - currently not supported).
  /// Uncomment when ValueTask support added.
  /// </summary>
  // public static ValueTask ValueTaskTest()
  // {
  //     return ValueTask.CompletedTask;
  // }
}