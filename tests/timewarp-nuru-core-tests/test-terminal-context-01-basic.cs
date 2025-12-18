#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using Microsoft.Extensions.DependencyInjection;

// Test TestTerminalContext AsyncLocal behavior and isolation

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.TerminalContext
{

[TestTag("Terminal")]
[TestTag("Testing")]
public class TestTerminalContextTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<TestTerminalContextTests>();

  public static async Task Should_provide_null_when_not_set()
  {
    // Arrange/Act
    TestTerminal? current = TestTerminalContext.Current;

    // Assert
    current.ShouldBeNull();
    TestTerminalContext.HasValue.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_return_set_terminal()
  {
    // Arrange
    using TestTerminal terminal = new();

    // Act
    TestTerminalContext.Current = terminal;

    // Assert
    TestTerminalContext.Current.ShouldBe(terminal);
    TestTerminalContext.HasValue.ShouldBeTrue();

    // Cleanup
    TestTerminalContext.Current = null;

    await Task.CompletedTask;
  }

  public static async Task Should_isolate_context_between_parallel_tasks()
  {
    // This test verifies that AsyncLocal properly isolates terminal contexts
    // across parallel async operations - critical for parallel test execution

    // Arrange
    const int taskCount = 5;
    Task[] tasks = new Task[taskCount];
    ConcurrentBag<(int TaskId, string Output)> results = [];

    // Act - run parallel tasks that each set their own context
    for (int i = 0; i < taskCount; i++)
    {
      int taskId = i;
      tasks[i] = Task.Run(async () =>
      {
        using TestTerminal terminal = new();
        TestTerminalContext.Current = terminal;

        // Simulate async work with deterministic delay
        await Task.Delay(10 + (taskId * 5));

        // Write unique output for this task
        await terminal.WriteLineAsync($"Task {taskId} output");

        // Verify we still have OUR terminal
        TestTerminalContext.Current.ShouldBe(terminal);

        // Capture the result
        results.Add((taskId, terminal.Output.Trim()));

        // Cleanup
        TestTerminalContext.Current = null;
      });
    }

    await Task.WhenAll(tasks);

    // Assert - each task should have captured its own unique output
    results.Count.ShouldBe(taskCount);

    foreach ((int taskId, string output) in results)
    {
      output.ShouldBe($"Task {taskId} output");
    }
  }

  public static async Task Should_not_leak_to_parent_context()
  {
    // Arrange
    TestTerminalContext.Current.ShouldBeNull();

    // Act - set context in child task
    await Task.Run(() =>
    {
      using TestTerminal terminal = new();
      TestTerminalContext.Current = terminal;
      TestTerminalContext.HasValue.ShouldBeTrue();
      // Note: We intentionally don't clean up to verify isolation
    });

    // Assert - parent context should not be affected
    TestTerminalContext.Current.ShouldBeNull();
  }

  public static async Task Should_flow_into_nested_async_calls()
  {
    // Arrange
    using TestTerminal terminal = new();
    TestTerminalContext.Current = terminal;

    // Act - verify context flows into nested async operations
    await NestedAsyncMethod();

    // Assert
    terminal.OutputContains("nested output").ShouldBeTrue();

    // Cleanup
    TestTerminalContext.Current = null;
  }

  private static async Task NestedAsyncMethod()
  {
    await Task.Delay(1);
    // Should access the same terminal from parent context
    await TestTerminalContext.Current!.WriteLineAsync("nested output");
  }

  public static async Task Should_use_context_in_nuru_app_when_set()
  {
    // Arrange
    using TestTerminal terminal = new();
    TestTerminalContext.Current = terminal;

    // Build an app WITHOUT explicitly setting terminal - it should pick up context
    NuruCoreAppBuilder builder = NuruCoreApp.CreateSlimBuilder();
    builder.Map("greet {name}", (string name, ITerminal t) => t.WriteLine($"Hello, {name}!"));
    NuruCoreApp app = builder.Build();

    // Act
    await app.RunAsync(["greet", "Context"]);

    // Assert - output should be captured in our test terminal
    terminal.OutputContains("Hello, Context!").ShouldBeTrue();

    // Cleanup
    TestTerminalContext.Current = null;
  }

  public static async Task Should_prefer_context_over_explicit_terminal()
  {
    // Arrange - set context terminal
    using TestTerminal contextTerminal = new();
    TestTerminalContext.Current = contextTerminal;

    // Build app with an explicitly configured terminal (simulates the DI scenario)
    using TestTerminal explicitTerminal = new();
    NuruCoreAppBuilder builder = NuruCoreApp.CreateSlimBuilder();
    builder.UseTerminal(explicitTerminal);  // Explicitly set a different terminal
    builder.Map("test", (ITerminal t) => t.WriteLine("test output"));
    NuruCoreApp app = builder.Build();

    // Verify context is still set after build
    TestTerminalContext.Current.ShouldBe(contextTerminal);

    // The app's Terminal property should be the context terminal, not the explicit one
    app.Terminal.ShouldBe(contextTerminal);

    // Act
    await app.RunAsync(["test"]);

    // Assert - context terminal should be used, not the explicitly configured one
    contextTerminal.OutputContains("test output").ShouldBeTrue();
    explicitTerminal.Output.ShouldBeEmpty();

    // Cleanup
    TestTerminalContext.Current = null;
  }
}

} // namespace TimeWarp.Nuru.Tests.Core.TerminalContext
