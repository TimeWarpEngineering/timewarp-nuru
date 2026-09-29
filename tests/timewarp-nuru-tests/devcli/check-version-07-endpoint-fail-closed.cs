#!/usr/bin/env -S dotnet --

// This file is guarded ENTIRELY by #if !JARIBU_MULTI (not just the runner entry
// point like sibling devcli test files): it references CheckVersionCommand, whose
// endpoint file is compiled here (tests/timewarp-nuru-tests/devcli/Directory.Build.props)
// but deliberately NOT compiled into the tests/ci-tests multi-mode assembly (endpoints
// are collected GLOBALLY by .DiscoverEndpoints() there — task 454-022 decision A2). Without
// this guard, the multi-mode compile would fail with CS0246 (CheckVersionCommand not found).
// Listed in CiTestExcludes and run by run-ci-tests.cs second phase (task 456).
// Manual: dotnet run tests/timewarp-nuru-tests/devcli/check-version-07-endpoint-fail-closed.cs

#if !JARIBU_MULTI
return await RunAllTests();

namespace TimeWarp.Nuru.Tests.DevCli
{

using System.Text;
using global::DevCli;

/// <summary>
/// Endpoint-level fail-closed coverage for CheckVersionCommand.Handler (kanban
/// task 470-007, parent-470 findings M9/M31): a NuGet outage must abort with
/// exit code 1 and never report "safe to release", and an invalid --package id
/// must be rejected before any HTTP lookup is attempted. This drives the real
/// Handler (not just NuGetVersionService in isolation) so the wiring is covered.
/// </summary>
[TestTag("DevCli")]
public class CheckVersionEndpointFailClosedTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CheckVersionEndpointFailClosedTests>();

  public static async Task Nuget_outage_fails_closed_instead_of_reporting_safe()
  {
    int originalExitCode = Environment.ExitCode;

    try
    {
      Environment.ExitCode = 0;

      using TestTerminal terminal = new();
      using NuGetVersionService nuGetVersionService = new(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
      RepoConfigService configService = new();
      PackableProjectService packableProjectService = new();

      CheckVersionCommand.Handler handler = new(terminal, nuGetVersionService, configService, packableProjectService);

      // Runs from inside the repo, so Git.FindRoot() finds this worktree's
      // source/Directory.Build.props and PropsVersionReader.Read succeeds —
      // the lookup itself is what must fail closed here.
      await handler.Handle(new CheckVersionCommand { Package = "TimeWarp.Nuru" }, CancellationToken.None);

      terminal.ErrorContains("NuGet lookup for 'TimeWarp.Nuru' failed").ShouldBeTrue();
      terminal.ErrorContains("refusing to report it as safe to release").ShouldBeTrue();
      terminal.OutputContains("safe to release").ShouldBeFalse();
      Environment.ExitCode.ShouldBe(1);
    }
    finally
    {
      Environment.ExitCode = originalExitCode;
    }

    await Task.CompletedTask;
  }

  public static async Task Invalid_package_id_is_rejected_before_lookup()
  {
    int originalExitCode = Environment.ExitCode;

    try
    {
      Environment.ExitCode = 0;

      using TestTerminal terminal = new();
      StubHandler stubHandler = new(_ => new HttpResponseMessage(HttpStatusCode.OK));
      using NuGetVersionService nuGetVersionService = new(stubHandler);
      RepoConfigService configService = new();
      PackableProjectService packableProjectService = new();

      CheckVersionCommand.Handler handler = new(terminal, nuGetVersionService, configService, packableProjectService);

      await handler.Handle(new CheckVersionCommand { Package = "../evil" }, CancellationToken.None);

      terminal.ErrorContains("invalid NuGet package id(s)").ShouldBeTrue();
      Environment.ExitCode.ShouldBe(1);
      stubHandler.RequestCount.ShouldBe(0);
    }
    finally
    {
      Environment.ExitCode = originalExitCode;
    }

    await Task.CompletedTask;
  }

  public static async Task Not_found_for_every_package_reports_safe()
  {
    int originalExitCode = Environment.ExitCode;

    try
    {
      Environment.ExitCode = 0;

      using TestTerminal terminal = new();
      using NuGetVersionService nuGetVersionService = new(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));
      RepoConfigService configService = new();
      PackableProjectService packableProjectService = new();

      CheckVersionCommand.Handler handler = new(terminal, nuGetVersionService, configService, packableProjectService);

      await handler.Handle(new CheckVersionCommand { Package = "TimeWarp.Nuru" }, CancellationToken.None);

      terminal.OutputContains("safe to release").ShouldBeTrue();
      terminal.OutputContains("Latest NuGet version: (none)").ShouldBeTrue();
      terminal.OutputContains("prerelease increment").ShouldBeFalse();
      Environment.ExitCode.ShouldBe(0);
    }
    finally
    {
      Environment.ExitCode = originalExitCode;
    }

    await Task.CompletedTask;
  }

  public static async Task Distance_zero_keeps_the_already_released_failure()
  {
    string source = ReadSourceVersion();
    await RunAsync
    (
      new CheckVersionCommand { Package = "TimeWarp.Nuru" },
      Versions(source),
      terminal =>
      {
        terminal.OutputContains("was already released").ShouldBeTrue();
        terminal.OutputContains("Bump the version before releasing.").ShouldBeTrue();
        terminal.OutputContains(PrereleaseDistance.FormatDistanceLine(0, source)).ShouldBeTrue();
        terminal.OutputContains("never released").ShouldBeFalse();
        terminal.OutputContains("safe to release").ShouldBeFalse();
        Environment.ExitCode.ShouldBe(1);
      }
    );

    await RunAsync
    (
      new CheckVersionCommand { Package = "TimeWarp.Nuru", Strict = true },
      Versions(source),
      terminal =>
      {
        terminal.OutputContains("was already released").ShouldBeTrue();
        terminal.OutputContains("never released").ShouldBeFalse();
        Environment.ExitCode.ShouldBe(1);
      }
    );
  }

  public static async Task Distance_one_prints_the_line_without_a_warning()
  {
    string source = ReadSourceVersion();
    int number = ReadPrereleaseNumber(source);
    string latest = WithPrereleaseNumber(source, number - 1);
    PrereleaseDistance.TryGetIncrements(source, latest).ShouldBe(1);
    string distanceLine = PrereleaseDistance.FormatDistanceLine(1, latest);

    await RunAsync
    (
      new CheckVersionCommand { Package = "TimeWarp.Nuru" },
      Versions(latest),
      terminal =>
      {
        terminal.OutputContains(distanceLine).ShouldBeTrue();
        terminal.OutputContains("never released").ShouldBeFalse();
        terminal.OutputContains("safe to release").ShouldBeTrue();
        Environment.ExitCode.ShouldBe(0);
      }
    );

    await RunAsync
    (
      new CheckVersionCommand { Package = "TimeWarp.Nuru", Strict = true },
      Versions(latest),
      _ => Environment.ExitCode.ShouldBe(0)
    );
  }

  public static async Task Distance_above_one_warns_and_strict_exits_non_zero()
  {
    string source = ReadSourceVersion();
    int number = ReadPrereleaseNumber(source);
    number.ShouldBeGreaterThanOrEqualTo(5);
    string latest = WithPrereleaseNumber(source, number - 5);
    int increments = PrereleaseDistance.TryGetIncrements(source, latest) ?? -1;
    increments.ShouldBe(5);
    string distanceLine = PrereleaseDistance.FormatDistanceLine(increments, latest);
    string warning = PrereleaseDistance.FormatSkippedReleaseWarning(increments) ?? "";

    await RunAsync
    (
      new CheckVersionCommand { Package = "TimeWarp.Nuru" },
      Versions(latest),
      terminal =>
      {
        int sourceAt = terminal.Output.IndexOf("Version in source", StringComparison.Ordinal);
        int latestAt = terminal.Output.IndexOf("Latest NuGet version", StringComparison.Ordinal);
        int distanceAt = terminal.Output.IndexOf(distanceLine, StringComparison.Ordinal);
        sourceAt.ShouldBeGreaterThanOrEqualTo(0);
        latestAt.ShouldBeGreaterThan(sourceAt);
        distanceAt.ShouldBeGreaterThan(latestAt);
        terminal.OutputContains(warning).ShouldBeTrue();
        terminal.OutputContains("safe to release").ShouldBeTrue();
        terminal.OutputContains("was already released").ShouldBeFalse();
        Environment.ExitCode.ShouldBe(0);
      }
    );

    await RunAsync
    (
      new CheckVersionCommand { Package = "TimeWarp.Nuru", Strict = true },
      Versions(latest),
      terminal =>
      {
        terminal.OutputContains(warning).ShouldBeTrue();
        terminal.OutputContains("safe to release").ShouldBeTrue();
        Environment.ExitCode.ShouldBe(1);
      }
    );
  }

  public static async Task Mismatched_shape_prints_both_versions_and_skips_the_distance()
  {
    string source = ReadSourceVersion();
    const string Latest = "1.0.0";
    PrereleaseDistance.TryGetIncrements(source, Latest).ShouldBeNull();

    await RunAsync
    (
      new CheckVersionCommand { Package = "TimeWarp.Nuru" },
      Versions(Latest),
      terminal =>
      {
        terminal.OutputContains($"Version in source: {source}").ShouldBeTrue();
        terminal.OutputContains($"Latest NuGet version: {Latest}").ShouldBeTrue();
        terminal.OutputContains("prerelease increment").ShouldBeFalse();
        terminal.OutputContains("never released").ShouldBeFalse();
        terminal.OutputContains("safe to release").ShouldBeTrue();
        Environment.ExitCode.ShouldBe(0);
      }
    );
  }

  private static string ReadSourceVersion()
  {
    string? source = PropsVersionReader.Read(Git.FindRoot());
    source.ShouldNotBeNullOrWhiteSpace();
    PrereleaseDistance.TryGetIncrements(source, source).ShouldBe(0);
    return source;
  }

  private static int ReadPrereleaseNumber(string version)
  {
    int plus = version.IndexOf('+');
    string noBuild = plus >= 0 ? version[..plus] : version;
    int dash = noBuild.IndexOf('-');
    string prerelease = noBuild[(dash + 1)..];
    string numberText = prerelease[(prerelease.LastIndexOf('.') + 1)..];
    return int.Parse(numberText, CultureInfo.InvariantCulture);
  }

  private static string WithPrereleaseNumber(string version, int number)
  {
    int plus = version.IndexOf('+');
    string noBuild = plus >= 0 ? version[..plus] : version;
    int dash = noBuild.IndexOf('-');
    string prerelease = noBuild[(dash + 1)..];
    int dot = prerelease.LastIndexOf('.');
    return $"{noBuild[..(dash + 1)]}{prerelease[..(dot + 1)]}{number.ToString(CultureInfo.InvariantCulture)}";
  }

  private static StubHandler Versions(string version) =>
    new(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent($"{{\"versions\":[\"{version}\"]}}", Encoding.UTF8, "application/json")
    });

  private static async Task RunAsync
  (
    CheckVersionCommand command,
    StubHandler handler,
    Action<TestTerminal> assert
  )
  {
    int originalExitCode = Environment.ExitCode;

    try
    {
      Environment.ExitCode = 0;

      using TestTerminal terminal = new();
      using NuGetVersionService nuGetVersionService = new(handler);
      CheckVersionCommand.Handler commandHandler = new
      (
        terminal,
        nuGetVersionService,
        new RepoConfigService(),
        new PackableProjectService()
      );

      await commandHandler.Handle(command, CancellationToken.None);
      assert(terminal);
    }
    finally
    {
      Environment.ExitCode = originalExitCode;
    }
  }

  private sealed class StubHandler : HttpMessageHandler
  {
    private readonly Func<HttpRequestMessage, HttpResponseMessage> Factory;

    public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> factory)
    {
      Factory = factory;
    }

    public int RequestCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      RequestCount++;
      return Task.FromResult(Factory(request));
    }
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
#endif
