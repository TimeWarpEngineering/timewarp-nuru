#!/usr/bin/dotnet --
await RunTests<ReportTests>();
await RunTests<ZeroTestsClass>();

[TestTag("Kijaribu")]
public class ReportTests
{
  /// <summary>
  /// REPORT-01: 2 passing, 1 failing - verify summary "2/3 passed".
  /// </summary>
  public static async Task PassingTest1()
  {
    await Task.CompletedTask;
  }

  public static async Task PassingTest2()
  {
    await Task.CompletedTask;
  }

  public static async Task FailingTest()
  {
    await Task.CompletedTask;
    throw new InvalidOperationException("Intentional fail for REPORT-01");
  }

  /// <summary>
  /// REPORT-02: Test for filtered summary - combine with tag filter.
  /// This untagged method should run in filtered context.
  /// </summary>
  public static async Task FilteredPassingTest()
  {
    await Task.CompletedTask;
  }

  /// <summary>
  /// REPORT-03: CleanUp method - should invoke after tests, log message.
  /// </summary>
  public static void CleanUp()
  {
    WriteLine("ReportTests.CleanUp: Invoked after tests");
  }

  /// <summary>
  /// REPORT-04: Async CleanUp (future) - if Task, should await.
  /// Currently sync only; uncomment when supported.
  /// </summary>
  // public static async Task AsyncCleanUp()
  // {
  //     await Task.Delay(1);
  //     Console.WriteLine("AsyncCleanUp: Invoked and awaited");
  // }

  /// <summary>
  /// REPORT-05: Test to highlight counter accumulation (if not reset).
  /// Run multiple times to see if counters accumulate.
  /// </summary>
  public static async Task CounterTest()
  {
    await Task.CompletedTask;
  }

  /// <summary>
  /// REPORT-EDGE-01: Class with 0 tests - summary "0/0", exit=0.
  /// No methods here.
  /// </summary>
}

[TestTag("Kijaribu")]
public class ZeroTestsClass
{
  // No test methods - should TotalTests=0, exit=0
  public static void CleanUp()
  {
    WriteLine("ZeroTestsClass.CleanUp: Invoked even with no tests");
  }
}