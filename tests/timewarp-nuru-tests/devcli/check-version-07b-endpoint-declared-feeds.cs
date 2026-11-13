#!/usr/bin/env -S dotnet --

#region Purpose
// Pins that CheckVersionCommand honors the declared feeds (564): a nuget.org-only config never contacts GitHub
// Packages, and an unknown feed name fails before any request.
#endregion

#region Design
// Guarded ENTIRELY by #if !JARIBU_MULTI: CheckVersionCommand's endpoint file compiles here but not into the
// ci-tests multi-mode assembly, which collects endpoints globally (task 454-022 A2), so multi-mode would fail
// with CS0246. CiTestExcludes lists this file; run-ci-tests.cs runs it in its second phase (task 456).
#endregion

#if !JARIBU_MULTI
return await RunAllTests();

namespace TimeWarp.Nuru.Tests.DevCliFeeds
{

using global::DevCli;

/// <summary>
/// Declared-feed coverage for CheckVersionCommand.Handler (kanban task 564), split from
/// check-version-07 to keep each file under the line cap.
/// </summary>
[TestTag("DevCli")]
public class CheckVersionEndpointDeclaredFeedsTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CheckVersionEndpointDeclaredFeedsTests>();

  // --- Declared feeds (kanban task 564) ------------------------------------

  public static async Task Nuget_org_only_config_never_contacts_github_packages()
  {
    // The Mediator v15.0.0-beta.3 shape: not on nuget.org, no token. With the
    // default feed set (no feeds key, and no section at all) the gate says
    // "safe to release" and never asks GitHub Packages or a credential source.
    foreach (RepoConfig config in new[]
    {
      new RepoConfig(),
      new RepoConfig { CheckVersionConfig = new CheckVersionConfig { Packages = "TimeWarp.Mediator" } },
      new RepoConfig { CheckVersionConfig = new CheckVersionConfig { Feeds = "nuget.org" } }
    })
    {
      int originalExitCode = Environment.ExitCode;

      try
      {
        Environment.ExitCode = 0;

        int credentialCalls = 0;
        List<Uri> requests = [];
        using TestTerminal terminal = new();
        using NuGetVersionService nuGetVersionService = new
        (
          new StubHandler(request =>
          {
            requests.Add(request.RequestUri!);
            return new HttpResponseMessage(HttpStatusCode.NotFound);
          }),
          _ =>
          {
            credentialCalls++;
            return Task.FromResult<GitHubPackagesCredential?>(null);
          }
        );

        CheckVersionCommand.Handler handler = new(terminal, nuGetVersionService, new FixedConfig(config), new PackableProjectService(terminal));

        await handler.Handle(new CheckVersionCommand { Package = "TimeWarp.Mediator", SourceVersion = "15.0.0-beta.3" }, CancellationToken.None);

        terminal.OutputContains("safe to release").ShouldBeTrue();
        handler.Outcome.ShouldBe(CheckVersionOutcome.NewVersion);
        Environment.ExitCode.ShouldBe(0);
        requests.ShouldAllBe(uri => uri.Host == "api.nuget.org");
        requests.Count.ShouldBe(1);
        credentialCalls.ShouldBe(0);
      }
      finally
      {
        Environment.ExitCode = originalExitCode;
      }
    }
  }

  public static async Task Unknown_feed_name_fails_before_any_request()
  {
    int originalExitCode = Environment.ExitCode;

    try
    {
      Environment.ExitCode = 0;

      int credentialCalls = 0;
      using TestTerminal terminal = new();
      StubHandler stubHandler = new(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
      using NuGetVersionService nuGetVersionService = new(stubHandler, _ =>
      {
        credentialCalls++;
        return Task.FromResult<GitHubPackagesCredential?>(null);
      });

      CheckVersionCommand.Handler handler = new(terminal, nuGetVersionService, new FixedConfig("nuget.org,myget"), new PackableProjectService(terminal));

      await handler.Handle(new CheckVersionCommand { Package = "TimeWarp.Nuru", SourceVersion = "1.0.0" }, CancellationToken.None);

      terminal.ErrorContains("checkVersionConfig.feeds").ShouldBeTrue();
      terminal.ErrorContains("'myget'").ShouldBeTrue();
      terminal.OutputContains("safe to release").ShouldBeFalse();
      handler.Outcome.ShouldBe(CheckVersionOutcome.InvalidInput);
      Environment.ExitCode.ShouldBe(1);
      stubHandler.RequestCount.ShouldBe(0);
      credentialCalls.ShouldBe(0);
    }
    finally
    {
      Environment.ExitCode = originalExitCode;
    }
  }

  /// <summary>
  /// A fixed .timewarp/dev.jsonc (kanban task 564), so a test chooses the declared
  /// feeds instead of reading this repo's config (which declares nuget.org only).
  /// </summary>
  private sealed class FixedConfig : IRepoConfigService
  {
    private readonly RepoConfig Config;

    public FixedConfig(string? feeds)
    {
      Config = new RepoConfig { CheckVersionConfig = new CheckVersionConfig { Feeds = feeds } };
    }

    public FixedConfig(RepoConfig config)
    {
      Config = config;
    }

    public ValueTask<RepoConfig> GetConfigAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(Config);
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

} // namespace TimeWarp.Nuru.Tests.DevCliFeeds
#endif
