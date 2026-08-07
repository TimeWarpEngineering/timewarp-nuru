#!/usr/bin/dotnet --

// This file is guarded ENTIRELY by #if !JARIBU_MULTI (not just the runner entry
// point like sibling devcli test files): it references CheckVersionCommand, whose
// endpoint file is compiled here (tests/timewarp-nuru-tests/devcli/Directory.Build.props)
// but deliberately NOT compiled into the tests/ci-tests multi-mode assembly (endpoints
// are collected GLOBALLY by .DiscoverEndpoints() there — task 454-022 decision A2). Without
// this guard, the multi-mode compile would fail with CS0246 (CheckVersionCommand not found).
// Run standalone only: dotnet run tests/timewarp-nuru-tests/devcli/check-version-04-endpoint-zero-package.cs

#if !JARIBU_MULTI
return await RunAllTests();

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// Endpoint-level regression test for round-1 review finding #1 (kanban task 458-005):
/// a delimiter-only --package value (e.g. ",") used to split to zero packages and reach
/// PublishStateClassifier.Classify(0, 0), which throws. The fix routes the parsed zero
/// count to the existing friendly "no packages" error instead. This test drives
/// the real CheckVersionCommand.Handler (not just the extracted PublishStateClassifier
/// unit) so the wiring — not just the classifier in isolation — is covered. The guard
/// (packages.Count == 0) fires before any HTTP call, so a real NuGetVersionService and a
/// real RepoConfigService are safe to construct here.
///
/// Kanban task 458-004 added a third package-set source (derived packable projects
/// under source/) below --package and checkVersionConfig.packages. An explicit
/// --package value that parses to zero packages (this test's ",") is still an
/// explicit override — CheckVersionCommand.Handler does NOT fall through to
/// derivation in that case, so a real PackableProjectService here never actually
/// runs (verified: this test passes without ever shelling out to `dotnet msbuild`).
/// The error text also changed ("no packable projects found under source/ and no
/// packages configured...") to describe all three sources being empty.
/// </summary>
[TestTag("DevCli")]
public class CheckVersionEndpointZeroPackageTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CheckVersionEndpointZeroPackageTests>();

  public static async Task Delimiter_only_package_input_prints_friendly_error_and_sets_exit_code_1()
  {
    int originalExitCode = Environment.ExitCode;

    try
    {
      using TestTerminal terminal = new();
      using NuGetVersionService nuGetVersionService = new();
      RepoConfigService configService = new();
      PackableProjectService packableProjectService = new();

      CheckVersionCommand.Handler handler = new(terminal, nuGetVersionService, configService, packableProjectService);

      await handler.Handle(new CheckVersionCommand { Package = "," }, CancellationToken.None);

      terminal.ErrorContains("no packable projects found under source/ and no packages configured").ShouldBeTrue();
      Environment.ExitCode.ShouldBe(1);
    }
    finally
    {
      // Reset so this passing test doesn't make the standalone runfile process exit non-zero.
      Environment.ExitCode = originalExitCode;
    }

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
#endif
