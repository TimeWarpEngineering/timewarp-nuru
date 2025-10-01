namespace TimeWarp.Kijaribu;

using System.Reflection;

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
  public static async Task RunTests<T>() where T : class
  {
    string testClassName = typeof(T).Name.Replace("Tests", "", StringComparison.Ordinal);
    Console.WriteLine($"🧪 Testing {testClassName}...");
    Console.WriteLine();

    // Get all public static methods in the class
    MethodInfo[] testMethods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Static);

    // Run them as tests
    foreach (MethodInfo method in testMethods)
    {
      await RunTest(method);
    }

    // Call cleanup method if it exists
    MethodInfo? cleanupMethod = typeof(T).GetMethod("CleanUp", BindingFlags.Public | BindingFlags.Static);
    if (cleanupMethod != null)
    {
      cleanupMethod.Invoke(null, null);
    }

    // Summary
    Console.WriteLine();
    Console.WriteLine("========================================");
    Console.WriteLine($"Results: {PassCount}/{TotalTests} tests passed");
    Console.WriteLine("========================================");

    Environment.Exit(PassCount == TotalTests ? 0 : 1);
  }

  private static async Task RunTest(MethodInfo method)
  {
    // Skip non-test methods (not public, not static, not Task, or named CleanUp/Setup)
    if (!method.IsPublic ||
        !method.IsStatic ||
        method.ReturnType != typeof(Task) ||
        method.Name is "CleanUp" or "Setup")
    {
      return;
    }

    // Check for [Skip] attribute
    SkipAttribute? skipAttr = method.GetCustomAttribute<SkipAttribute>();
    if (skipAttr != null)
    {
      TotalTests++;
      string testName = method.Name;
      Console.WriteLine($"Test: {TestHelpers.FormatTestName(testName)}");
      Console.WriteLine($"  ⚠ SKIPPED: {skipAttr.Reason}");
      Console.WriteLine();
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

    Console.WriteLine($"Test: {displayName}");

    try
    {
      var task = method.Invoke(null, parameters) as Task;
      if (task != null)
      {
        await task;
      }

      PassCount++;
      Console.WriteLine($"  ✓ PASSED");
    }
    catch (TargetInvocationException ex) when (ex.InnerException != null)
    {
      // Unwrap the TargetInvocationException to get the actual exception
      Console.WriteLine($"  ✗ FAILED: {ex.InnerException.GetType().Name}");
      Console.WriteLine($"           {ex.InnerException.Message}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"  ✗ FAILED: {ex.GetType().Name}");
      Console.WriteLine($"           {ex.Message}");
    }

    Console.WriteLine();
  }
}
