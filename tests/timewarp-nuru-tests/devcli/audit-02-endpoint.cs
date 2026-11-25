#!/usr/bin/env -S dotnet --

#region Purpose
// Pins that the shared `audit` route accepts --force and --verbose/-v (kanban task 601).
#endregion

// Guarded ENTIRELY by #if !JARIBU_MULTI, like check-version-04: it maps AuditCommand, whose
// endpoint file is compiled into this directory's standalone runfiles
// (tests/timewarp-nuru-tests/devcli/Directory.Build.props) but not into the tests/ci-tests
// multi-mode assembly. Listed in CiTestExcludes and run by
// tests/ci-standalone/run-standalone-tests.cs. Manual:
//   dotnet run --file tests/timewarp-nuru-tests/devcli/audit-02-endpoint.cs

#if !JARIBU_MULTI
return await RunAllTests();

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// Routes <c>audit</c> through a NuruApp. This compilation registers no checks, so a parsed
/// route prints the nothing-to-run line and exits 0; the options' effect on checks and the
/// pass cache is pinned in audit-03-pass-cache-and-subset.cs.
/// </summary>
[TestTag("DevCli")]
public class AuditEndpointTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<AuditEndpointTests>();

  public static async Task Audit_accepts_force_and_verbose()
  {
    await RunsCleanAsync(["audit", "--force", "--verbose"]);
  }

  public static async Task Audit_accepts_force_and_short_verbose()
  {
    await RunsCleanAsync(["audit", "--force", "-v"]);
  }

  public static async Task Audit_still_runs_with_no_options()
  {
    await RunsCleanAsync(["audit"]);
  }

  public static async Task Audit_help_lists_both_options()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<AuditCommand>()
      .Build();

    await app.RunAsync(["audit", "--help"]);

    terminal.OutputContains("--force").ShouldBeTrue();
    terminal.OutputContains("--verbose").ShouldBeTrue();
  }

  private static async Task RunsCleanAsync(string[] args)
  {
    int originalExitCode = Environment.ExitCode;
    try
    {
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map<AuditCommand>()
        .Build();

      int exitCode = await app.RunAsync(args);

      exitCode.ShouldBe(0);
      terminal.OutputContains(DevAuditRunner.NothingToRunLine).ShouldBeTrue();
    }
    finally
    {
      Environment.ExitCode = originalExitCode;
    }
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
#endif
