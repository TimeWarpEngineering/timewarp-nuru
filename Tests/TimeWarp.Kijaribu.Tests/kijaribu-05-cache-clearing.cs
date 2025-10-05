namespace TimeWarp.Kijaribu.Tests;

using System;
using System.Threading.Tasks;
using TimeWarp.Kijaribu;

[ClearRunfileCache(true)]
public static class CacheClearTest
{
    /// <summary>
    /// CACHE-01: Class with [ClearRunfileCache(Enabled=true)] - should clear cache and log.
    /// Verify output shows "✓ Clearing runfile cache:".
    /// </summary>
    public static async Task ClearTestMethod()
    {
        Console.WriteLine("CacheClearTest.ClearTestMethod: Running after cache clear");
        await Task.CompletedTask;
    }
}

public static class NoCacheTest
{
    /// <summary>
    /// CACHE-02: No attribute, default false - no clearing output.
    /// </summary>
    public static async Task NoClearTestMethod()
    {
        Console.WriteLine("NoCacheTest.NoClearTestMethod: Running without cache clear");
        await Task.CompletedTask;
    }
}

[ClearRunfileCache(true)]
public static class CurrentAssemblySkipTest
{
    /// <summary>
    /// CACHE-03: Clear enabled - should skip deleting current assembly's cache dir.
    /// Verify no deletion of current exe dir.
    /// </summary>
    public static async Task SkipCurrentTest()
    {
        Console.WriteLine("CurrentAssemblySkipTest.SkipCurrentTest: Running, cache should not delete self");
        await Task.CompletedTask;
    }
}

[ClearRunfileCache(true)]
public static class EmptyCacheTest
{
    /// <summary>
    /// CACHE-04: Run with clear=true param - handles empty cache dir gracefully.
    /// </summary>
    public static async Task EmptyCacheMethod()
    {
        Console.WriteLine("EmptyCacheTest.EmptyCacheMethod: No cache exists, should silent return");
        await Task.CompletedTask;
    }

    /// <summary>
    /// CACHE-EDGE-01: Simulate permission issue (manual verification needed).
    /// </summary>
    public static async Task PermissionEdge()
    {
        // Hard to test automatically; log for manual check
        Console.WriteLine("PermissionEdge: Check if IOException handled in ClearRunfileCache");
        await Task.CompletedTask;
    }
}