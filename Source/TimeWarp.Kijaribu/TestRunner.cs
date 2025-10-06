namespace TimeWarp.Kijaribu;

using System.Reflection;
using static System.Console;

/// <summary>
/// Simple test runner for single-file C# programs.
/// Discovers and executes public static async Task methods as tests.
/// </summary>
public static class TestRunner
{
  private static int PassCount;
  private static int TotalTests;

  /// <summary>
  /// Runs all public static async Task methods in the specified test class.
  /// </summary>
  /// <typeparam name="T">The test class containing test methods.</typeparam>
  /// <param name="clearCache">Whether to clear .NET runfile cache before running tests. Defaults to false for performance. Set true to ensure latest source changes are picked up.</param>
  /// <param name="filterTag">Optional tag to filter tests. Only runs tests with this tag. Checks both class-level and method-level TestTag attributes. If not specified, checks KIJARIBU_FILTER_TAG environment variable.</param>
  /// <returns>Exit code: 0 if all tests passed, 1 if any tests failed.</returns>
  public static async Task<int> RunTests<T>(bool? clearCache = null, string? filterTag = null) where T : class
  {
    // Check environment variable if filterTag not explicitly provided
    filterTag ??= Environment.GetEnvironmentVariable("KIJARIBU_FILTER_TAG");

    // Check if test class matches filter tag (if specified)
    if (filterTag is not null)
    {
      TestTagAttribute[] classTags = typeof(T).GetCustomAttributes<TestTagAttribute>().ToArray();
      if (classTags.Length > 0 && !classTags.Any(t => t.Tag.Equals(filterTag, StringComparison.OrdinalIgnoreCase)))
      {
        // Class has tags but none match the filter - skip entire class
        return 0;
      }
    }

    // Determine whether to clear cache: attribute wins, then parameter, then default (false)
    bool shouldClearCache = false;
    ClearRunfileCacheAttribute? cacheAttr = typeof(T).GetCustomAttribute<ClearRunfileCacheAttribute>();
    if (cacheAttr is not null)
    {
      shouldClearCache = cacheAttr.Enabled;
    }
    else if (clearCache.HasValue)
    {
      shouldClearCache = clearCache.Value;
    }

    if (shouldClearCache)
    {
      ClearRunfileCache();
    }

    string testClassName = typeof(T).Name.Replace("Tests", "", StringComparison.Ordinal);
    WriteLine($"🧪 Testing {testClassName}...");

    if (filterTag is not null)
    {
      WriteLine($"   (filtered by tag: {filterTag})");
    }

    WriteLine();

    // Get all public static methods in the class
    MethodInfo[] testMethods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Static);

    // Run them as tests
    await InvokeSetup<T>();

    foreach (MethodInfo method in testMethods)
    {
      await RunTest(method, filterTag);
    }

    await InvokeCleanup<T>();

    // Summary
    WriteLine();
    WriteLine("========================================");
    WriteLine($"Results: {PassCount}/{TotalTests} tests passed");
    WriteLine("========================================");

    return PassCount == TotalTests ? 0 : 1;
  }

  private static async Task RunTest(MethodInfo method, string? filterTag)
  {
    // Skip non-test methods (not public, not static, not Task, or named CleanUp/Setup)
    if (!method.IsPublic ||
        !method.IsStatic ||
        method.ReturnType != typeof(Task) ||
        method.Name is "CleanUp" or "Setup")
    {
      return;
    }

    // Check for method tag filter if specified
    if (filterTag is not null)
    {
      TestTagAttribute[] methodTags = method.GetCustomAttributes<TestTagAttribute>().ToArray();
      if (methodTags.Length > 0 && !methodTags.Any(t => t.Tag.Equals(filterTag, StringComparison.OrdinalIgnoreCase)))
      {
        TotalTests++;
        WriteLine($"Test: {TestHelpers.FormatTestName(method.Name)}");
        TestHelpers.TestSkipped($"No matching tag '{filterTag}'");
        WriteLine();
        return;
      }
    }

    // Check for [Skip] attribute
    SkipAttribute? skipAttr = method.GetCustomAttribute<SkipAttribute>();
    if (skipAttr is not null)
    {
      TotalTests++;
      string testName = method.Name;
      WriteLine($"Test: {TestHelpers.FormatTestName(testName)}");
      TestHelpers.TestSkipped(skipAttr.Reason);
      WriteLine();
      return;
    }

    // Check for [Input] attributes for parameterized tests
    InputAttribute[] inputAttrs = method.GetCustomAttributes<InputAttribute>().ToArray();

    if (inputAttrs.Length > 0)
    {
      // Run test once for each [Input]
      foreach (InputAttribute inputAttr in inputAttrs)
      {
        await RunSingleTest(method, inputAttr.Parameters);
      }
    }
    else
    {
      // No [Input] attributes - run once with no parameters
      await RunSingleTest(method, []);
    }
  }

  private static async Task RunSingleTest(MethodInfo method, object?[] parameters)
  {
    TotalTests++;
    string testName = method.Name;

    // Format test name with parameters if provided
    string displayName = parameters.Length > 0
      ? $"{TestHelpers.FormatTestName(testName)} ({string.Join(", ", parameters.Select(p => p?.ToString() ?? "null"))})"
      : TestHelpers.FormatTestName(testName);

    WriteLine($"Test: {displayName}");

    try
    {
      var testTask = method.Invoke(null, parameters) as Task;
      if (testTask is not null)
      {
        TimeoutAttribute? timeoutAttr = method.GetCustomAttribute<TimeoutAttribute>();
        if (timeoutAttr is not null)
        {
          var timeoutTask = Task.Delay(timeoutAttr.Milliseconds);
          Task completedTask = await Task.WhenAny(testTask, timeoutTask);
          if (completedTask == timeoutTask)
          {
            TestHelpers.TestFailed($"Timeout after {timeoutAttr.Milliseconds}ms");
            WriteLine();
            return;
          }

          await testTask; // Propagate any exceptions from the test task

        }
        else
        {
          await testTask;
        }
      }

      PassCount++;
      TestHelpers.TestPassed();
    }
    catch (TargetInvocationException ex) when (ex.InnerException is not null)
    {
      // Unwrap the TargetInvocationException to get the actual exception
      TestHelpers.TestFailed($"{ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
    }
    catch (Exception ex)
    {
      TestHelpers.TestFailed($"{ex.GetType().Name}: {ex.Message}");
    }

    WriteLine();
  }

  private static async Task InvokeSetup<T>() where T : class
  {
    MethodInfo? setupMethod = typeof(T).GetMethod("Setup", BindingFlags.Public | BindingFlags.Static);
    if (setupMethod is not null && setupMethod.ReturnType == typeof(Task))
    {
      if (setupMethod.Invoke(null, null) is Task task)
      {
        await task;
      }
    }
  }

  private static async Task InvokeCleanup<T>() where T : class
  {
    MethodInfo? cleanupMethod = typeof(T).GetMethod("CleanUp", BindingFlags.Public | BindingFlags.Static);
    if (cleanupMethod is not null && cleanupMethod.ReturnType == typeof(Task))
    {
      if (cleanupMethod.Invoke(null, null) is Task task)
      {
        await task;
      }
    }
  }

  /// <summary>
  /// Clears the .NET runfile cache to ensure tests pick up latest source changes.
  /// Skips the currently executing test to avoid deleting itself.
  /// </summary>
  private static void ClearRunfileCache()
  {
    string runfileCacheRoot = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      ".local", "share", "dotnet", "runfile"
    );

    if (!Directory.Exists(runfileCacheRoot))
    {
      return;
    }

    string? currentExeDir = AppContext.BaseDirectory;
    bool anyDeleted = false;

    foreach (string cacheDir in Directory.GetDirectories(runfileCacheRoot))
    {
      // Don't delete if currentExeDir STARTS WITH cacheDir (parent-child relationship)
      if (currentExeDir?.StartsWith(cacheDir, StringComparison.OrdinalIgnoreCase) == true)
      {
        continue;
      }

      if (!anyDeleted)
      {
        WriteLine("✓ Clearing runfile cache:");
        anyDeleted = true;
      }

      string cacheDirName = Path.GetFileName(cacheDir);
      Directory.Delete(cacheDir, recursive: true);
      WriteLine($"  - {cacheDirName}");
    }

    if (anyDeleted)
    {
      WriteLine();
    }
  }
}
