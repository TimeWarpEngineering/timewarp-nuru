#!/usr/bin/env -S dotnet --

// This file is guarded ENTIRELY by #if !JARIBU_MULTI (not just the runner entry
// point like sibling devcli test files): it references CheckVersionCommand, whose
// endpoint file is compiled here (tests/timewarp-nuru-tests/devcli/Directory.Build.props)
// but deliberately NOT compiled into the tests/ci-tests multi-mode assembly (endpoints
// are collected GLOBALLY by .DiscoverEndpoints() there — task 454-022 decision A2). Without
// this guard, the multi-mode compile would fail with CS0246 (CheckVersionCommand not found).
// Run standalone only: dotnet run tests/timewarp-nuru-tests/devcli/check-version-07-endpoint-fail-closed.cs

#if !JARIBU_MULTI
return await RunAllTests();

namespace TimeWarp.Nuru.Tests.DevCli
{

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
      Environment.ExitCode.ShouldBe(0);
    }
    finally
    {
      Environment.ExitCode = originalExitCode;
    }

    await Task.CompletedTask;
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
