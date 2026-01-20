namespace TimeWarp.Nuru;

/// <summary>
/// Provides an ambient context for test harnesses to take control of <see cref="NuruCoreApp"/> execution.
/// </summary>
/// <remarks>
/// <para>
/// This class enables zero-configuration testing of runfiles by allowing test code to be
/// included at build time via <c>Directory.Build.props</c>. When the test runner delegate
/// is set (typically via <c>[ModuleInitializer]</c>), <see cref="NuruCoreApp.RunAsync"/> 
/// hands control to the test harness instead of executing normally.
/// </para>
/// <para>
/// The test harness receives the fully configured <see cref="NuruCoreApp"/> instance and can
/// run multiple test scenarios against it using <see cref="TestTerminalContext"/> for output capture.
/// </para>
/// <example>
/// <code>
/// // test-my-app.cs - included via Directory.Build.props when NURU_TEST is set
/// public static class TestSetup
/// {
///     [ModuleInitializer]
///     public static void Initialize()
///     {
///         NuruTestContext.TestRunner = async (app) =>
///         {
///             // Test 1
///             using (var terminal = new TestTerminal())
///             {
///                 TestTerminalContext.Current = terminal;
///                 await app.RunAsync(["greet", "Alice"]);
///                 terminal.OutputContains("Hello, Alice!").ShouldBeTrue();
///             }
///             
///             Console.WriteLine("All tests passed!");
///             return 0;
///         };
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public static class NuruTestContext
{
  private static readonly AsyncLocal<Func<NuruApp, Task<int>>?> TestRunnerContext = new();
  private static readonly AsyncLocal<bool> IsExecutingTests = new();

  /// <summary>
  /// Gets or sets the test runner delegate that takes control of app execution.
  /// </summary>
  /// <remarks>
  /// <para>
  /// When set to a non-null value, <see cref="NuruApp.RunAsync"/> will invoke this
  /// delegate instead of executing the command normally. The delegate receives the
  /// fully configured <see cref="NuruApp"/> instance.
  /// </para>
  /// <para>
  /// The delegate is only invoked once per execution. Subsequent calls to
  /// <see cref="NuruApp.RunAsync"/> from within the test harness execute normally,
  /// allowing tests to run multiple scenarios.
  /// </para>
  /// </remarks>
  /// <value>
  /// The test runner delegate, or <c>null</c> if not in test mode.
  /// </value>
  public static Func<NuruApp, Task<int>>? TestRunner
  {
    get => TestRunnerContext.Value;
    set => TestRunnerContext.Value = value;
  }

  /// <summary>
  /// Gets a value indicating whether a test runner is configured.
  /// </summary>
  public static bool HasTestRunner => TestRunnerContext.Value is not null;

  /// <summary>
  /// Attempts to execute the test runner if one is configured and not already executing.
  /// </summary>
  /// <param name="app">The NuruApp instance to pass to the test runner.</param>
  /// <param name="exitCode">The exit code from the test runner, if executed.</param>
  /// <returns><c>true</c> if the test runner was executed; <c>false</c> if normal execution should proceed.</returns>
  internal static bool TryExecuteTestRunner(NuruApp app, out Task<int> exitCode)
  {
    // If no test runner, proceed with normal execution
    if (TestRunnerContext.Value is null)
    {
      exitCode = Task.FromResult(0);
      return false;
    }

    // If we're already executing tests, proceed with normal execution
    // This allows test code to call RunAsync for individual test cases
    if (IsExecutingTests.Value)
    {
      exitCode = Task.FromResult(0);
      return false;
    }

    // Mark that we're executing tests and invoke the runner
    IsExecutingTests.Value = true;
    exitCode = TestRunnerContext.Value(app);
    return true;
  }

  /// <summary>
  /// Resets the test context. Primarily used for testing the test infrastructure itself.
  /// </summary>
  internal static void Reset()
  {
    TestRunnerContext.Value = null;
    IsExecutingTests.Value = false;
  }
}
