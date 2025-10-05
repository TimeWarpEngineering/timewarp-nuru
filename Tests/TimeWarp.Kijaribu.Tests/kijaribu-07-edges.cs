namespace TimeWarp.Kijaribu.Tests;

using System;
using System.Threading.Tasks;
using TimeWarp.Kijaribu;

public static class EdgeTests
{
    /// <summary>
    /// DISC-EDGE-01: Generic test method - reflection should handle generics.
    /// </summary>
    public static async Task GenericTest<T>()
    {
        Console.WriteLine($"GenericTest: Running with type {typeof(T).Name}");
        await Task.CompletedTask;
    }

    /// <summary>
    /// PARAM-EDGE-01: Additional edge for [Input] with mismatched count (0 for 3 params).
    /// Should fail gracefully.
    /// </summary>
    [Input]
    public static async Task MismatchedParamCountTest(string p1, int p2, bool p3)
    {
        Console.WriteLine($"MismatchedParamCountTest: {p1}, {p2}, {p3} - Unexpected if mismatched");
        await Task.CompletedTask;
    }

    /// <summary>
    /// TAG-EDGE-01: Method with multiple tags - already in TagTests, but additional with no match.
    /// </summary>
    [TestTag("no-match1")]
    [TestTag("no-match2")]
    public static async Task MultiTagNoMatch()
    {
        Console.WriteLine("MultiTagNoMatch: Should skip if filter doesn't match any");
        await Task.CompletedTask;
    }

    /// <summary>
    /// CACHE-EDGE-01: Simulate permission issue - hard to automate, log for manual.
    /// </summary>
    public static async Task CachePermissionEdge()
    {
        // Manual: Run and check if IOException is handled without crash
        Console.WriteLine("CachePermissionEdge: Check ClearRunfileCache handles IO errors");
        await Task.CompletedTask;
    }

    /// <summary>
    /// REPORT-EDGE-01: Class with 0 qualifying tests - summary "0/0", exit=0.
    /// Include non-qualifying methods only.
    /// </summary>
    // No qualifying methods here - this class tests zero tests scenario

    public static void NonQualifyingMethod()
    {
        // Sync void: skipped
    }

    private static async Task PrivateMethod()
    {
        // Private: skipped
        await Task.CompletedTask;
    }

    /// <summary>
    /// Additional edge: Method with ValueTask (future support).
    /// Currently not run due to strict Task check.
    /// </summary>
    public static ValueTask ValueTaskEdge()
    {
        Console.WriteLine("ValueTaskEdge: Should not run until supported");
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Edge: Hanging test simulation (no timeout implemented).
    /// </summary>
    public static async Task HangingTestEdge()
    {
        // Infinite loop or long delay - manual timeout check
        await Task.Delay(Timeout.Infinite); // Simulates hang
    }

    /// <summary>
    /// Edge: Test with CancellationToken (future).
    /// </summary>
    public static async Task CancellationEdge(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested(); // If supported
        await Task.CompletedTask;
    }
}