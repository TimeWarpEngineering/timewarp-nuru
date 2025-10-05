#!/usr/bin/dotnet --

return await RunTests<DiscoveryTests>(clearCache: true);

[TestTag("Kijaribu")]
[ClearRunfileCache]
public class DiscoveryTests
{
    /// <summary>
    /// DISC-01: Basic test method execution - Simple passing test.
    /// </summary>
    public static async Task BasicTest()
    {
        // No-op: passes by default
        await Task.CompletedTask;
    }

    /// <summary>
    /// DISC-02: Non-qualifying methods - These should be skipped.
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
        // Named Setup: should skip discovery but could be invoked separately if supported
        await Task.CompletedTask;
    }

    public static void CleanUp()
    {
        // Named CleanUp: should skip discovery, invoked post-run
        // Log or assert something here for REPORT-03 validation
        Console.WriteLine("CleanUp invoked");
    }

    /// <summary>
    /// DISC-03: Failing test for multi-test validation.
    /// </summary>
    public static async Task FailingTest()
    {
        await Task.CompletedTask;
        throw new ArgumentException("Intentional failure for DISC-03");
    }

    /// <summary>
    /// DISC-03: Another passing test.
    /// </summary>
    public static async Task PassingTest2()
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// DISC-04: Async test with await.
    /// </summary>
    public static async Task AsyncAwaitTest()
    {
        await Task.Delay(1); // Simulates async work
    }

    /// <summary>
    /// DISC-05: ValueTask test (future enhancement - currently not supported).
    /// Uncomment when ValueTask support added.
    /// </summary>
    // public static ValueTask ValueTaskTest()
    // {
    //     return ValueTask.CompletedTask;
    // }
}