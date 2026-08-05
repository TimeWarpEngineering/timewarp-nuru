#!/usr/bin/dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

// Tests for Ctrl+C cancellation of in-flight REPL commands (Task 454-017, M15).
// Semantics: Ctrl+C cancels the RUNNING command (REPL survives, re-prompts); Ctrl+C never
// exits the REPL (use `exit` / Ctrl+D); external-token cancellation still unwinds the loop.
//
// Handlers reference static fields (not captured locals) because the source generator
// lifts delegate bodies into generated code (H002).

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.CtrlCCancellation
{

[TestTag("REPL")]
[TestTag("Cancellation")]
public class CtrlCCancellationTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CtrlCCancellationTests>();

  private static TestTerminal? Terminal;
  private static TaskCompletionSource CommandStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

  public static Task Setup()
  {
    Terminal = new TestTerminal();
    CommandStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    return Task.CompletedTask;
  }

  public static Task CleanUp()
  {
    Terminal?.Dispose();
    Terminal = null;
    return Task.CompletedTask;
  }

  // Handler methods reference statics — safe for generator lifting.
  internal static async Task<string> HangHandler(CancellationToken cancellationToken)
  {
    CommandStarted.TrySetResult();
    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken); // only cancellation ends this early
    return "HANG-COMPLETED"; // must never appear
  }

  internal static string FirstWithCtrlC()
  {
    Terminal!.SimulateCancelKeyPress();
    return "FIRST-RAN";
  }

  internal static string FirstWithDoubleCtrlC()
  {
    Terminal!.SimulateCancelKeyPress();
    Terminal!.SimulateCancelKeyPress(); // double press — must be a harmless no-op
    return "FIRST-RAN";
  }

  internal static string ThrowUnrelatedTimeout() =>
    throw new TaskCanceledException("simulated HttpClient timeout"); // OCE with NO token cancelled

  public static async Task Ctrl_c_cancels_in_flight_command_and_repl_survives()
  {
    // Arrange
    Terminal!.QueueLine("hang");     // long-running command, awaits its cancellation token
    Terminal.QueueLine("after");     // must still run — proves the REPL survived Ctrl+C
    Terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(Terminal)
      .Map("hang")
        .WithHandler(HangHandler)
        .AsCommand()
        .Done()
      .Map("after")
        .WithHandler(() => "AFTER-RAN")
        .AsCommand()
        .Done()
      .AddRepl(options => { options.EnableColors = false; options.PersistHistory = false; })
      .Build();

    // Act: fire Ctrl+C only once the command is genuinely in flight (handshake, no sleeps).
    Task<int> repl = app.RunAsync(["--interactive"]);
    await CommandStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
    Terminal.SimulateCancelKeyPress();

    await repl.WaitAsync(TimeSpan.FromSeconds(10));

    // Assert
    Terminal.ErrorOutput.ShouldContain("Command cancelled", customMessage: "Ctrl+C must cancel the in-flight command");
    Terminal.OutputContains("HANG-COMPLETED").ShouldBeFalse("the hung command must not run to completion");
    Terminal.OutputContains("AFTER-RAN").ShouldBeTrue("the REPL must survive Ctrl+C and run the next command");
  }

  public static async Task Ctrl_c_does_not_exit_the_repl_loop()
  {
    // Arrange: pre-fix, OnCancelKeyPress set Running = false, so ANY Ctrl+C exited the
    // REPL after the current iteration — "second" would never run. The handler fires
    // Ctrl+C synchronously from inside "first" (deterministic — no timing races); "first"
    // ignores its token and completes, and the loop must continue to "second".
    Terminal!.QueueLine("first");
    Terminal.QueueLine("second");
    Terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(Terminal)
      .Map("first")
        .WithHandler(FirstWithCtrlC)
        .AsCommand()
        .Done()
      .Map("second")
        .WithHandler(() => "SECOND-RAN")
        .AsCommand()
        .Done()
      .AddRepl(options => { options.EnableColors = false; options.PersistHistory = false; })
      .Build();

    // Act
    await app.RunAsync(["--interactive"]).WaitAsync(TimeSpan.FromSeconds(10));

    // Assert
    Terminal.OutputContains("FIRST-RAN").ShouldBeTrue("sanity: first command ran");
    Terminal.OutputContains("SECOND-RAN").ShouldBeTrue("Ctrl+C must not exit the REPL loop");
  }

  public static async Task Double_ctrl_c_is_a_no_op_and_repl_continues()
  {
    // Arrange: two Ctrl+C presses against the same in-flight command — the second finds
    // the command already cancelled; it must not crash or exit the REPL.
    Terminal!.QueueLine("first");
    Terminal.QueueLine("second");
    Terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(Terminal)
      .Map("first")
        .WithHandler(FirstWithDoubleCtrlC)
        .AsCommand()
        .Done()
      .Map("second")
        .WithHandler(() => "SECOND-RAN")
        .AsCommand()
        .Done()
      .AddRepl(options => { options.EnableColors = false; options.PersistHistory = false; })
      .Build();

    // Act
    await app.RunAsync(["--interactive"]).WaitAsync(TimeSpan.FromSeconds(10));

    // Assert
    Terminal.OutputContains("SECOND-RAN").ShouldBeTrue("double Ctrl+C must not exit or crash the REPL");
  }

  public static async Task External_token_cancellation_still_exits_loop()
  {
    // Arrange: the EXTERNAL token (host shutdown) must still unwind the REPL — the
    // Ctrl+C conversion applies only to the per-command token.
    using CancellationTokenSource externalCts = new();

    Terminal!.QueueLine("hang");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(Terminal)
      .Map("hang")
        .WithHandler(HangHandler)
        .AsCommand()
        .Done()
      .AddRepl(options => { options.EnableColors = false; options.PersistHistory = false; })
      .Build();

    // Act: enter the REPL via the intercepted RunReplAsync overload that carries a token.
    Task repl = app.RunReplAsync(externalCts.Token);
    await CommandStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
    await externalCts.CancelAsync();

    // Assert: RunReplAsync unwinds with cancellation (does not hang, does not swallow).
    await Should.ThrowAsync<OperationCanceledException>(async () =>
      await repl.WaitAsync(TimeSpan.FromSeconds(10)));
  }

  public static async Task Unrelated_timeout_oce_is_a_command_failure_not_a_cancellation()
  {
    // Arrange: a handler throwing TaskCanceledException with NO token cancelled (e.g. an
    // HttpClient timeout) is an ordinary command failure — it must NOT be mislabeled
    // "Command cancelled", and the REPL continues (ContinueOnError default true).
    Terminal!.QueueLine("timeout");
    Terminal.QueueLine("after");
    Terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(Terminal)
      .Map("timeout")
        .WithHandler(ThrowUnrelatedTimeout)
        .AsCommand()
        .Done()
      .Map("after")
        .WithHandler(() => "AFTER-RAN")
        .AsCommand()
        .Done()
      .AddRepl(options => { options.EnableColors = false; options.PersistHistory = false; })
      .Build();

    // Act
    await app.RunAsync(["--interactive"]).WaitAsync(TimeSpan.FromSeconds(10));

    // Assert
    Terminal.ErrorOutput.Contains("Command cancelled", StringComparison.Ordinal)
      .ShouldBeFalse("an unrelated OCE must not be reported as a user cancellation");
    Terminal.ErrorOutput.ShouldContain("simulated HttpClient timeout");
    Terminal.OutputContains("AFTER-RAN").ShouldBeTrue("the REPL must survive the failed command");
  }

  public static async Task Cancelled_command_does_not_trip_continue_on_error_teardown()
  {
    // Arrange: ContinueOnError=false normally exits the REPL on a failed command.
    // User cancellation is NOT a failure — the REPL must survive it even in this mode.
    Terminal!.QueueLine("hang");
    Terminal.QueueLine("after");
    Terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(Terminal)
      .Map("hang")
        .WithHandler(HangHandler)
        .AsCommand()
        .Done()
      .Map("after")
        .WithHandler(() => "AFTER-RAN")
        .AsCommand()
        .Done()
      .AddRepl(options =>
      {
        options.EnableColors = false;
        options.PersistHistory = false;
        options.ContinueOnError = false;
      })
      .Build();

    // Act
    Task<int> repl = app.RunAsync(["--interactive"]);
    await CommandStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
    Terminal.SimulateCancelKeyPress();
    await repl.WaitAsync(TimeSpan.FromSeconds(10));

    // Assert
    Terminal.OutputContains("AFTER-RAN")
      .ShouldBeTrue("cancellation must not trigger ContinueOnError teardown");
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.CtrlCCancellation
