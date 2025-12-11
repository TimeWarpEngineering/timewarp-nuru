#!/usr/bin/dotnet --
#:package Mediator.SourceGenerator

// Issue #120: CreateBuilder does not support Task<int> return type delegates (but CreateSlimBuilder does)
// https://github.com/TimeWarpEngineering/timewarp-nuru/issues/120

using Microsoft.Extensions.DependencyInjection;

return await RunTests<AsyncTaskIntReturnTests>(clearCache: true);

[TestTag("Routing")]
[ClearRunfileCache]
public class AsyncTaskIntReturnTests
{
  /// <summary>
  /// Issue #120: Verify CreateSlimBuilder works with async Task&lt;int&gt; MapDefault
  /// </summary>
  public static async Task Should_support_async_task_int_with_slim_builder()
  {
    // Arrange
    bool executed = false;
    NuruCoreApp app = NuruApp.CreateSlimBuilder([])
      .MapDefault(async () =>
      {
        await Task.Delay(1);
        executed = true;
        return 0;
      }, "Test async Task<int>")
      .Build();

    // Act
    int exitCode = await app.RunAsync([]);

    // Assert
    exitCode.ShouldBe(0);
    executed.ShouldBeTrue();
  }

  /// <summary>
  /// Issue #120: Verify CreateBuilder works with async Task&lt;int&gt; MapDefault
  /// This is the failing case from the issue.
  /// </summary>
  public static async Task Should_support_async_task_int_with_create_builder()
  {
    // Arrange
    bool executed = false;
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .ConfigureServices(services => services.AddMediator())
      .MapDefault(async () =>
      {
        await Task.Delay(1);
        executed = true;
        return 0;
      }, "Test async Task<int>")
      .Build();

    // Act
    int exitCode = await app.RunAsync([]);

    // Assert
    exitCode.ShouldBe(0);
    executed.ShouldBeTrue();
  }

  /// <summary>
  /// Verify CreateSlimBuilder works with async Task (void) MapDefault
  /// </summary>
  public static async Task Should_support_async_task_void_with_slim_builder()
  {
    // Arrange
    bool executed = false;
    NuruCoreApp app = NuruApp.CreateSlimBuilder([])
      .MapDefault(async () =>
      {
        await Task.Delay(1);
        executed = true;
      }, "Test async Task")
      .Build();

    // Act
    int exitCode = await app.RunAsync([]);

    // Assert
    exitCode.ShouldBe(0);
    executed.ShouldBeTrue();
  }

  /// <summary>
  /// Verify CreateBuilder works with async Task (void) MapDefault
  /// </summary>
  public static async Task Should_support_async_task_void_with_create_builder()
  {
    // Arrange
    bool executed = false;
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .ConfigureServices(services => services.AddMediator())
      .MapDefault(async () =>
      {
        await Task.Delay(1);
        executed = true;
      }, "Test async Task")
      .Build();

    // Act
    int exitCode = await app.RunAsync([]);

    // Assert
    exitCode.ShouldBe(0);
    executed.ShouldBeTrue();
  }

  /// <summary>
  /// Verify CreateBuilder works with sync Func&lt;int&gt; MapDefault (baseline)
  /// </summary>
  public static async Task Should_support_sync_func_int_with_create_builder()
  {
    // Arrange
    bool executed = false;
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .ConfigureServices(services => services.AddMediator())
      .MapDefault(() =>
      {
        executed = true;
        return 0;
      }, "Test sync Func<int>")
      .Build();

    // Act
    int exitCode = await app.RunAsync([]);

    // Assert
    exitCode.ShouldBe(0);
    executed.ShouldBeTrue();
  }
}
