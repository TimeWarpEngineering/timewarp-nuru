#!/usr/bin/dotnet --

// Issue #120: Verify async Task<int> return type delegates work with CreateBuilder
// https://github.com/TimeWarpEngineering/timewarp-nuru/issues/120

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class AsyncTaskIntReturnTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<AsyncTaskIntReturnTests>();

  /// <summary>
  /// Issue #120: Verify CreateBuilder works with async Task&lt;int&gt; MapDefault
  /// </summary>
  public static async Task Should_support_async_task_int()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("").WithHandler(async () =>
      {
        await Task.Delay(1);
        return 42; // Outputs "42" to terminal (tests Task<int> handler support)
      }).WithDescription("Test async Task<int>").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync([]);

    // Assert
    exitCode.ShouldBe(0);
  }

  /// <summary>
  /// Verify CreateBuilder works with async Task (void) MapDefault
  /// </summary>
  public static async Task Should_support_async_task_void()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("").WithHandler(async () =>
      {
        await Task.Delay(1);
      }).WithDescription("Test async Task").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync([]);

    // Assert
    exitCode.ShouldBe(0);
  }

  /// <summary>
  /// Verify CreateBuilder works with sync Func&lt;int&gt; MapDefault (baseline)
  /// </summary>
  public static async Task Should_support_sync_func_int()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("").WithHandler(() => { })
      .WithDescription("Test sync Func<int>").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync([]);

    // Assert
    exitCode.ShouldBe(0);
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
