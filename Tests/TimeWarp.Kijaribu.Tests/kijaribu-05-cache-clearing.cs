#!/usr/bin/dotnet --

await RunTests<CacheClearTest>();
await RunTests<NoCacheTest>(clearCache: false);
await RunTests<CurrentAssemblySkipTest>();
await RunTests<EmptyCacheTest>();

[TestTag("Kijaribu")]
[ClearRunfileCache(true)]
public class CacheClearTest
{
  /// <summary>
  /// Class with [ClearRunfileCache(Enabled=true)] - should clear cache and log.
  /// Verify output shows "✓ Clearing runfile cache:".
  /// </summary>
  public static async Task ClearTestMethod()
  {
    WriteLine("CacheClearTest.ClearTestMethod: Running after cache clear");
    await Task.CompletedTask;
  }
}

[TestTag("Kijaribu")]
public class NoCacheTest
{
  /// <summary>
  /// No attribute, default false - no clearing output.
  /// </summary>
  public static async Task NoClearTestMethod()
  {
    WriteLine("NoCacheTest.NoClearTestMethod: Running without cache clear");
    await Task.CompletedTask;
  }
}

[TestTag("Kijaribu")]
[ClearRunfileCache(true)]
public class CurrentAssemblySkipTest
{
  /// <summary>
  /// Clear enabled - should skip deleting current assembly's cache dir.
  /// Verify no deletion of current exe dir.
  /// </summary>
  public static async Task SkipCurrentTest()
  {
    WriteLine("CurrentAssemblySkipTest.SkipCurrentTest: Running, cache should not delete self");
    await Task.CompletedTask;
  }
}

[TestTag("Kijaribu")]
[ClearRunfileCache(true)]
public class EmptyCacheTest
{
  /// <summary>
  /// Run with clear=true param - handles empty cache dir gracefully.
  /// </summary>
  public static async Task EmptyCacheMethod()
  {
    WriteLine("EmptyCacheTest.EmptyCacheMethod: No cache exists, should silent return");
    await Task.CompletedTask;
  }

  /// <summary>
  /// Simulate permission issue (manual verification needed).
  /// </summary>
  public static async Task PermissionEdge()
  {
    // Hard to test automatically; log for manual check
    WriteLine("PermissionEdge: Check if IOException handled in ClearRunfileCache");
    await Task.CompletedTask;
  }
}