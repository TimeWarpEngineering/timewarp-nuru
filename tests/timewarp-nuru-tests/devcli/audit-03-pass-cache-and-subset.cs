#!/usr/bin/env -S dotnet --

#region Purpose
// Pins the `dev audit` additions of kanban task 601 in DevAuditRunner and DevAuditChecks:
// the context reaches every check, the pass cache skips a run whose inputs already passed,
// --force runs anyway, a pass is recorded only when every check passed, a throwing cache
// never skips, and a named subset of checks runs in process without the cache.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// DevAuditRunner with a fake pass cache, and DevAuditChecks.Select for the subset run.
/// </summary>
[TestTag("DevCli")]
public class DevAuditPassCacheAndSubsetTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DevAuditPassCacheAndSubsetTests>();

  public static async Task Context_reaches_every_check()
  {
    using TestTerminal terminal = new();
    DevAuditContext context = new("/repo", Force: true, Verbose: true);
    ContextCheck first = new("first", DevAuditCheckResult.Pass());
    ContextCheck second = new("second", DevAuditCheckResult.Pass());

    await DevAuditRunner.RunAsync([first, second], context, terminal, passCache: null, CancellationToken.None);

    first.Seen.ShouldBe(context);
    second.Seen.ShouldBe(context);
    second.Seen!.Force.ShouldBeTrue();
    second.Seen.Verbose.ShouldBeTrue();
  }

  public static async Task Recorded_pass_skips_every_check()
  {
    using TestTerminal terminal = new();
    MemoryPassCache cache = new("abcdef0123456789");
    cache.RecordPass(new DevAuditContext("/repo"), "abcdef0123456789");
    ContextCheck check = new("only", DevAuditCheckResult.Pass());

    DevAuditSummary summary = await DevAuditRunner.RunAsync(
      [check], new DevAuditContext("/repo"), terminal, cache, CancellationToken.None);

    summary.Skipped.ShouldBeTrue();
    summary.ExitCode.ShouldBe(0);
    check.Seen.ShouldBeNull();
    terminal.OutputContains($"{DevAuditRunner.SkippedLine} (abcdef012345)").ShouldBeTrue();
    terminal.OutputContains("--force runs every check").ShouldBeTrue();
  }

  public static async Task Force_runs_every_check_despite_a_recorded_pass()
  {
    using TestTerminal terminal = new();
    MemoryPassCache cache = new("key-1");
    cache.RecordPass(new DevAuditContext("/repo"), "key-1");
    ContextCheck check = new("only", DevAuditCheckResult.Pass());

    DevAuditSummary summary = await DevAuditRunner.RunAsync(
      [check], new DevAuditContext("/repo", Force: true), terminal, cache, CancellationToken.None);

    summary.Skipped.ShouldBeFalse();
    summary.Passed.ShouldBe(1);
    check.Seen.ShouldNotBeNull();
    terminal.OutputContains(DevAuditRunner.SkippedLine).ShouldBeFalse();
  }

  public static async Task All_passing_run_records_the_key()
  {
    using TestTerminal terminal = new();
    MemoryPassCache cache = new("key-2");

    await DevAuditRunner.RunAsync(
      [new ContextCheck("a", DevAuditCheckResult.Pass()), new ContextCheck("b", DevAuditCheckResult.Pass())],
      new DevAuditContext("/repo"), terminal, cache, CancellationToken.None);

    cache.Recorded.ShouldBe("key-2");

    // The next run on the same inputs skips.
    using TestTerminal again = new();
    DevAuditSummary second = await DevAuditRunner.RunAsync(
      [new ContextCheck("a", DevAuditCheckResult.Pass())], new DevAuditContext("/repo"), again, cache, CancellationToken.None);
    second.Skipped.ShouldBeTrue();
  }

  public static async Task Failed_run_records_nothing()
  {
    using TestTerminal terminal = new();
    MemoryPassCache cache = new("key-3");

    DevAuditSummary summary = await DevAuditRunner.RunAsync(
      [new ContextCheck("a", DevAuditCheckResult.Pass()), new ContextCheck("b", DevAuditCheckResult.Fail("broken"))],
      new DevAuditContext("/repo"), terminal, cache, CancellationToken.None);

    summary.ExitCode.ShouldBe(1);
    cache.Recorded.ShouldBeNull();
  }

  public static async Task Forced_failed_run_keeps_the_earlier_pass_untouched()
  {
    using TestTerminal terminal = new();
    MemoryPassCache cache = new("key-4");
    cache.RecordPass(new DevAuditContext("/repo"), "older-key");

    await DevAuditRunner.RunAsync(
      [new ContextCheck("b", DevAuditCheckResult.Fail("broken"))],
      new DevAuditContext("/repo", Force: true), terminal, cache, CancellationToken.None);

    cache.Recorded.ShouldBe("older-key");
  }

  public static async Task No_key_runs_every_check_and_records_nothing()
  {
    using TestTerminal terminal = new();
    MemoryPassCache cache = new(key: null);
    ContextCheck check = new("only", DevAuditCheckResult.Pass());

    DevAuditSummary summary = await DevAuditRunner.RunAsync(
      [check], new DevAuditContext("/repo"), terminal, cache, CancellationToken.None);

    summary.Passed.ShouldBe(1);
    check.Seen.ShouldNotBeNull();
    cache.Recorded.ShouldBeNull();
  }

  public static async Task Throwing_cache_runs_every_check_and_says_so_when_verbose()
  {
    using TestTerminal terminal = new();
    ContextCheck check = new("only", DevAuditCheckResult.Pass());

    DevAuditSummary summary = await DevAuditRunner.RunAsync(
      [check], new DevAuditContext("/repo", Verbose: true), terminal, new ThrowingPassCache(), CancellationToken.None);

    summary.Passed.ShouldBe(1);
    check.Seen.ShouldNotBeNull();
    terminal.OutputContains("pass cache unavailable").ShouldBeTrue();
    terminal.OutputContains("IOException: git missing").ShouldBeTrue();
  }

  public static async Task Empty_check_list_never_reads_the_cache()
  {
    using TestTerminal terminal = new();
    ThrowingPassCache cache = new();

    DevAuditSummary summary = await DevAuditRunner.RunAsync(
      [], new DevAuditContext("/repo"), terminal, cache, CancellationToken.None);

    summary.ExitCode.ShouldBe(0);
    cache.Asked.ShouldBeFalse();
    terminal.OutputContains(DevAuditRunner.NothingToRunLine).ShouldBeTrue();
  }

  public static async Task Select_returns_named_checks_in_registry_order()
  {
    IDevAuditCheck[] registry =
    [
      new ContextCheck("deps-check", DevAuditCheckResult.Pass()),
      new ContextCheck("skills-compile", DevAuditCheckResult.Pass()),
      new ContextCheck("pair-test", DevAuditCheckResult.Pass())
    ];

    IReadOnlyList<IDevAuditCheck> subset = DevAuditChecks.Select(registry, ["pair-test", "deps-check"]);

    subset.Select(check => check.Name).ShouldBe(["deps-check", "pair-test"]);
    await Task.CompletedTask;
  }

  public static async Task Select_throws_on_an_unknown_name()
  {
    IDevAuditCheck[] registry = [new ContextCheck("deps-check", DevAuditCheckResult.Pass())];

    ArgumentException exception = Should.Throw<ArgumentException>(
      () => DevAuditChecks.Select(registry, ["deps-chek"]));

    exception.Message.ShouldContain("no check named deps-chek");
    exception.Message.ShouldContain("registered: deps-check");
    await Task.CompletedTask;
  }

  public static async Task Named_on_an_empty_registry_throws()
  {
    // This test compilation implements no AddRepoChecks.
    Should.Throw<ArgumentException>(() => DevAuditChecks.Named("deps-check"))
      .Message.ShouldContain("registered: none");
    DevAuditChecks.Named().Count.ShouldBe(0);
    await Task.CompletedTask;
  }

  public static async Task Subset_runs_in_process_without_the_cache()
  {
    using TestTerminal terminal = new();
    ContextCheck deps = new("deps-check", DevAuditCheckResult.Pass());
    ContextCheck pair = new("pair-test", DevAuditCheckResult.Pass());
    IReadOnlyList<IDevAuditCheck> subset = DevAuditChecks.Select([deps, pair], ["deps-check"]);

    DevAuditSummary summary = await DevAuditRunner.RunAsync(
      subset, new DevAuditContext("/repo"), terminal, passCache: null, CancellationToken.None);

    summary.Passed.ShouldBe(1);
    deps.Seen.ShouldNotBeNull();
    pair.Seen.ShouldBeNull();
    terminal.OutputContains("dev audit: 1 passed, 0 failed").ShouldBeTrue();
  }

  public static async Task Empty_registry_sets_no_pass_cache()
  {
    // This test compilation implements no ConfigurePassCache, like a repo that opts out.
    DevAuditChecks.PassCache().ShouldBeNull();
    await Task.CompletedTask;
  }

  private sealed class ContextCheck(string name, DevAuditCheckResult result) : IDevAuditCheck
  {
    public string Name { get; } = name;

    public DevAuditContext? Seen { get; private set; }

    public ValueTask<DevAuditCheckResult> RunAsync(DevAuditContext context, CancellationToken cancellationToken)
    {
      Seen = context;
      return ValueTask.FromResult(result);
    }
  }

  private sealed class MemoryPassCache(string? key) : IDevAuditPassCache
  {
    public string? Recorded { get; private set; }

    public ValueTask<string?> KeyAsync(DevAuditContext context, CancellationToken cancellationToken) =>
      ValueTask.FromResult(key);

    public bool HasPass(DevAuditContext context, string key) =>
      string.Equals(Recorded, key, StringComparison.Ordinal);

    public void RecordPass(DevAuditContext context, string key) => Recorded = key;
  }

  private sealed class ThrowingPassCache : IDevAuditPassCache
  {
    public bool Asked { get; private set; }

    public ValueTask<string?> KeyAsync(DevAuditContext context, CancellationToken cancellationToken)
    {
      Asked = true;
      throw new IOException("git missing");
    }

    public bool HasPass(DevAuditContext context, string key) => throw new InvalidOperationException("not reached");

    public void RecordPass(DevAuditContext context, string key) => throw new InvalidOperationException("not reached");
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
