#region Purpose
// Why a check-version run ended, so a release pipeline that runs the gate can
// name the real cause when it aborts (kanban task 536).
#endregion
#region Design
// The `dev workflow` release pipeline used to print "version already released"
// for every non-zero check-version exit. Once the gate reads GitHub Packages,
// a job token without packages: read makes it refuse, and that abort line
// sent operators to bump a version that was never published. The handler now
// records an outcome and the GitHub Packages failure, and FormatAbortReason
// turns them into the pipeline's abort line. A forked workflow command can
// call it too.
#endregion

namespace DevCli;

internal enum CheckVersionOutcome
{
  NotRun,
  InvalidInput,
  NewVersion,
  PartiallyPublished,
  AlreadyReleased,
  NuGetOrgLookupFailed,
  GitHubPackagesUnreadable
}

internal static class CheckVersionAbort
{
  public static string FormatAbortReason(CheckVersionOutcome outcome, PackageFeedUnreadableException? gitHubPackagesFailure)
  {
    return outcome switch
    {
      CheckVersionOutcome.AlreadyReleased => "version already released",
      CheckVersionOutcome.GitHubPackagesUnreadable =>
        $"GitHub Packages feed {NuGetVersionService.GitHubPackagesServiceIndexUrl} unreadable ({gitHubPackagesFailure?.StatusDisplay ?? "no HTTP response"}) — add 'packages: read' to the job permissions",
      CheckVersionOutcome.NuGetOrgLookupFailed => "nuget.org lookup failed — retry when nuget.org is reachable",
      _ => "check-version failed (see the error above)"
    };
  }
}
