#!/usr/bin/env -S dotnet --

#region Purpose
// Pins DevAuditRunner, the engine behind `dev audit` (kanban task 600): the empty case,
// one line per check with wall time, a failing check's full message, the summary line,
// and the non-zero exit code.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// DevAuditRunner driven with fake checks. AuditCommand passes DevAuditChecks.All()
/// to the same runner and copies a non-zero ExitCode to Environment.ExitCode.
/// </summary>
[TestTag("DevCli")]
public class DevAuditRunnerTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DevAuditRunnerTests>();

  public static async Task No_checks_prints_nothing_to_run_and_exits_zero()
  {
    using TestTerminal terminal = new();

    DevAuditSummary summary = await DevAuditRunner.RunAsync([], terminal, CancellationToken.None);

    summary.ExitCode.ShouldBe(0);
    summary.Passed.ShouldBe(0);
    summary.Failed.ShouldBe(0);
    terminal.OutputContains(DevAuditRunner.NothingToRunLine).ShouldBeTrue();
  }

  public static async Task Empty_registry_has_no_checks()
  {
    // This test compilation implements no AddRepoChecks, like a repo without checks.
    DevAuditChecks.All().Count.ShouldBe(0);

    await Task.CompletedTask;
  }

  public static async Task Passing_checks_print_one_line_each_with_time_and_exit_zero()
  {
    using TestTerminal terminal = new();
    IDevAuditCheck[] checks =
    [
      new FakeAuditCheck("first-check", DevAuditCheckResult.Pass()),
      new FakeAuditCheck("second-check", DevAuditCheckResult.Pass())
    ];

    DevAuditSummary summary = await DevAuditRunner.RunAsync(checks, terminal, CancellationToken.None);

    summary.ExitCode.ShouldBe(0);
    summary.Passed.ShouldBe(2);
    terminal.OutputContains("PASS first-check (").ShouldBeTrue();
    terminal.OutputContains("PASS second-check (").ShouldBeTrue();
    terminal.OutputContains("ms)").ShouldBeTrue();
    terminal.OutputContains("dev audit: 2 passed, 0 failed").ShouldBeTrue();
  }

  public static async Task Failing_check_prints_full_message_and_exits_non_zero()
  {
    using TestTerminal terminal = new();
    string message = "deps check failed:\n  TimeWarp.Amuru 1.0 is behind 2.0\n  run: dev deps update";
    FakeAuditCheck after = new("after-check", DevAuditCheckResult.Pass());
    IDevAuditCheck[] checks =
    [
      new FakeAuditCheck("deps-check", DevAuditCheckResult.Fail(message)),
      after
    ];

    DevAuditSummary summary = await DevAuditRunner.RunAsync(checks, terminal, CancellationToken.None);

    summary.ExitCode.ShouldBe(1);
    summary.Passed.ShouldBe(1);
    summary.Failed.ShouldBe(1);
    terminal.OutputContains("FAIL deps-check (").ShouldBeTrue();
    terminal.OutputContains(message).ShouldBeTrue();
    // A failure does not stop the run: later checks still report.
    after.Ran.ShouldBeTrue();
    terminal.OutputContains("PASS after-check (").ShouldBeTrue();
    terminal.OutputContains("dev audit: 1 passed, 1 failed").ShouldBeTrue();
  }

  public static async Task Throwing_check_counts_as_failed_with_exception_text()
  {
    using TestTerminal terminal = new();
    IDevAuditCheck[] checks = [new ThrowingAuditCheck()];

    DevAuditSummary summary = await DevAuditRunner.RunAsync(checks, terminal, CancellationToken.None);

    summary.ExitCode.ShouldBe(1);
    terminal.OutputContains("FAIL throwing-check (").ShouldBeTrue();
    terminal.OutputContains("InvalidOperationException: config file missing").ShouldBeTrue();
    terminal.OutputContains("dev audit: 0 passed, 1 failed").ShouldBeTrue();
  }

  public static async Task Elapsed_formats_ms_under_a_second_and_seconds_above()
  {
    DevAuditRunner.FormatElapsed(TimeSpan.FromMilliseconds(42)).ShouldBe("42ms");
    DevAuditRunner.FormatElapsed(TimeSpan.FromMilliseconds(2345)).ShouldBe("2.3s");

    await Task.CompletedTask;
  }
}

internal sealed class FakeAuditCheck(string name, DevAuditCheckResult result) : IDevAuditCheck
{
  public string Name { get; } = name;

  public bool Ran { get; private set; }

  public ValueTask<DevAuditCheckResult> RunAsync(CancellationToken cancellationToken)
  {
    Ran = true;
    return ValueTask.FromResult(result);
  }
}

internal sealed class ThrowingAuditCheck : IDevAuditCheck
{
  public string Name => "throwing-check";

  public ValueTask<DevAuditCheckResult> RunAsync(CancellationToken cancellationToken) =>
    throw new InvalidOperationException("config file missing");
}

} // namespace TimeWarp.Nuru.Tests.DevCli
