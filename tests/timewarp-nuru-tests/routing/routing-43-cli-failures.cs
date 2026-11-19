#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Pins the three CLI failure behaviors from task 591-005: argument errors on stderr, a handler
// exception that returns 1, and Ctrl+C that cancels the handler token.
#endregion

#region Design
// Handlers reference static fields (not captured locals) because the source generator lifts delegate
// bodies into generated code. Each test builds its own app in the same method as RunAsync, which is
// what the generator intercepts. ExecuteAsync is the host path and still lets a handler exception
// propagate. A handler that ignores the token is released after the assertion so the test does not
// leave it waiting.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
[TestTag("Cancellation")]
public class CliFailureTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CliFailureTests>();

  private static TaskCompletionSource CommandStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
  private static TaskCompletionSource Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
  private static TaskCompletionSource HandlerFinished = new(TaskCreationOptions.RunContinuationsAsynchronously);
  private static bool TokenWasCancelled;

  public static Task Setup()
  {
    CommandStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    Gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    HandlerFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    TokenWasCancelled = false;
    return Task.CompletedTask;
  }

  internal static async Task CancellableHandler(CancellationToken cancellationToken)
  {
    CommandStarted.TrySetResult();
    await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
  }

  internal static async Task IgnoreCancellationHandler(CancellationToken cancellationToken)
  {
    CommandStarted.TrySetResult();
    await Gate.Task.ConfigureAwait(false);
    TokenWasCancelled = cancellationToken.IsCancellationRequested;
    HandlerFinished.TrySetResult();
  }

  internal static int ThrowHandler() =>
    throw new InvalidOperationException("handler failed");

  internal static int ThrowUnrelatedCancellation() =>
    throw new TaskCanceledException("simulated timeout");

  public static async Task Should_write_an_invalid_option_value_to_stderr()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("set --count {count:int}")
        .WithHandler((int count) => $"count:{count}")
        .AsCommand()
        .Done()
      .Build();

    int exitCode = await app.RunAsync(["set", "--count", "bogus"]);

    exitCode.ShouldBe(1);
    terminal.ErrorContains("Error: Invalid value 'bogus' for option '--count'").ShouldBeTrue();
    terminal.OutputContains("Error:").ShouldBeFalse();
    terminal.OutputContains("count:").ShouldBeFalse();
  }

  public static async Task Should_return_1_when_a_handler_throws()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("boom").WithHandler(ThrowHandler).AsCommand().Done()
      .Build();

    int exitCode = await app.RunAsync(["boom"]);

    exitCode.ShouldBe(1);
    terminal.ErrorContains("handler failed").ShouldBeTrue();
    terminal.ErrorContains("at TimeWarp").ShouldBeFalse();
    terminal.OutputContains("handler failed").ShouldBeFalse();
  }

  public static async Task Should_let_a_handler_exception_escape_execute_async()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("boom").WithHandler(ThrowHandler).AsCommand().Done()
      .Build();

    InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
      async () => await app.ExecuteAsync(["boom"]));

    exception.Message.ShouldContain("handler failed");
  }

  public static async Task Should_return_1_for_an_unrelated_task_cancellation()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("timeout").WithHandler(ThrowUnrelatedCancellation).AsCommand().Done()
      .Build();

    int exitCode = await app.RunAsync(["timeout"]);

    exitCode.ShouldBe(1);
    terminal.ErrorContains("simulated timeout").ShouldBeTrue();
  }

  public static async Task Should_cancel_the_handler_token_on_ctrl_c()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("hang").WithHandler(CancellableHandler).AsCommand().Done()
      .Build();

    Task<int> run = app.RunAsync(["hang"]);
    await CommandStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
    terminal.SimulateCancelKeyPress();

    int exitCode = await run.WaitAsync(TimeSpan.FromSeconds(10));
    exitCode.ShouldBe(130);
  }

  public static async Task Should_return_130_when_the_handler_ignores_ctrl_c_until_the_timeout()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("ignore").WithHandler(IgnoreCancellationHandler).AsCommand().Done()
      .Build();
    app.ProcessTerminationTimeout = TimeSpan.FromMilliseconds(200);

    try
    {
      Task<int> run = app.RunAsync(["ignore"]);
      await CommandStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
      terminal.SimulateCancelKeyPress();

      int exitCode = await run.WaitAsync(TimeSpan.FromSeconds(10));
      exitCode.ShouldBe(130);
      Gate.Task.IsCompleted.ShouldBeFalse();
    }
    finally
    {
      Gate.TrySetResult();
    }

    await HandlerFinished.Task.WaitAsync(TimeSpan.FromSeconds(10));
    TokenWasCancelled.ShouldBeTrue();
  }

  public static async Task Should_not_cancel_when_process_termination_timeout_is_null()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("ignore").WithHandler(IgnoreCancellationHandler).AsCommand().Done()
      .Build();
    app.ProcessTerminationTimeout = null;

    try
    {
      Task<int> run = app.RunAsync(["ignore"]);
      await CommandStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
      terminal.SimulateCancelKeyPress();
      run.IsCompleted.ShouldBeFalse();
      Gate.TrySetResult();

      int exitCode = await run.WaitAsync(TimeSpan.FromSeconds(10));
      exitCode.ShouldBe(0);
    }
    finally
    {
      Gate.TrySetResult();
    }

    await HandlerFinished.Task.WaitAsync(TimeSpan.FromSeconds(10));
    TokenWasCancelled.ShouldBeFalse();
  }
}

}
