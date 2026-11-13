#region Purpose
// Workflow handler: NuGet push and the private outcome enums/records used by the release gates.
#endregion

namespace DevCli;

public sealed partial class WorkflowCommand
{
  internal sealed partial class Handler
  {
    // Push order is cosmetic: NuGet does not validate inter-package dependencies
    // at push time (kanban task 458-004, decision D1). projects is the derived
    // packable set (IPackableProjectService), sorted by PackageId.
    private async Task PushPackagesAsync(string repoRoot, IReadOnlyList<PackableProject> projects, string? apiKey)
    {
      string artifactsDir = Path.Combine(repoRoot, "artifacts", "packages");

      // Read version to construct package names
      string? version = ReadPropsVersion(repoRoot);

      if (string.IsNullOrEmpty(version))
      {
        throw new InvalidOperationException("Could not determine version for push");
      }

      HashSet<string> expectedNupkgFileNames = [.. projects.Select(p => $"{p.PackageId}.{version}.nupkg")];

      // Cross-check: no *.{version}.nupkg file in the artifacts directory may
      // fall outside the derived packable set — stronger than a glob-only push,
      // catches a stray/leftover package (e.g. from a renamed or removed
      // project) that would otherwise be pushed unnoticed.
      string[] actualNupkgFiles = Directory.Exists(artifactsDir)
        ? Directory.GetFiles(artifactsDir, $"*.{version}.nupkg")
        : [];

      List<string> unexpectedNupkgFileNames = [];
      foreach (string filePath in actualNupkgFiles)
      {
        string fileName = Path.GetFileName(filePath);
        if (!expectedNupkgFileNames.Contains(fileName))
        {
          unexpectedNupkgFileNames.Add(fileName);
        }
      }

      if (unexpectedNupkgFileNames.Count > 0)
      {
        throw new InvalidOperationException($"Unexpected package(s) in {artifactsDir} not in the derived packable set: {string.Join(", ", unexpectedNupkgFileNames)}");
      }

      foreach (PackableProject project in projects)
      {
        string nupkgPath = Path.Combine(artifactsDir, $"{project.PackageId}.{version}.nupkg");

        if (!File.Exists(nupkgPath))
        {
          throw new FileNotFoundException($"Package not found: {nupkgPath}");
        }

        Terminal.WriteLine($"Pushing {project.PackageId}.{version}.nupkg...");

        // Build push arguments - include API key if provided (from OIDC Trusted Publishing)
        List<string> args = ["nuget", "push", nupkgPath, "--source", "https://api.nuget.org/v3/index.json", "--skip-duplicate"];

        if (!string.IsNullOrEmpty(apiKey))
        {
          args.AddRange(["--api-key", apiKey]);
        }

        int exitCode = await Shell.Builder("dotnet")
          .WithArguments([.. args])
          .WithWorkingDirectory(repoRoot)
          .RunAsync();

        if (exitCode != 0)
        {
          throw new InvalidOperationException($"Failed to push {project.PackageId}!");
        }
      }

      Terminal.WriteLine("\nAll packages pushed successfully!");
    }

    // Outcome of the release-gate ancestor check — a git-error verdict must
    // never be silently coerced into "not an ancestor".
    private enum AncestorCheckStatus
    {
      Ancestor,
      NotAncestor,
      MasterUnresolvable,
      GitError
    }

    private sealed record AncestorCheckOutcome(AncestorCheckStatus Status, string? Detail);

    // Outcome of the release-gate tag-pin check — Mismatch and GitError are
    // distinct verdicts and must not be collapsed into one message.
    private enum TagPinStatus
    {
      NoTag,
      Match,
      Mismatch,
      GitError
    }

    private sealed record TagPinOutcome(TagPinStatus Status, string? TagCommit, string? HeadCommit, string? Detail);

    // Outcome of locating the CI run to promote. GhUnavailable (gh could not
    // be launched at all), GhFailed (gh launched and ran but exited nonzero —
    // Detail carries its stderr), and NoMatchingRun (gh ran fine, exited 0,
    // nothing matched this commit) are three DIFFERENT verdicts requiring
    // different operator remediation and must not collapse into one message
    // (round-1 review finding #2).
    private enum LocateRunStatus
    {
      Found,
      GhUnavailable,
      GhFailed,
      NoMatchingRun
    }

    private sealed record LocateRunOutcome(LocateRunStatus Status, string HeadSha, IReadOnlyList<CiRunSummary> CandidateRuns, string? Detail);

    // Outcome of walking candidate runs for a downloadable Packages-* artifact.
    private enum DownloadArtifactStatus
    {
      Downloaded,
      Exhausted
    }

    private sealed record DownloadArtifactOutcome(DownloadArtifactStatus Status, CiRunSummary? Run, string? ArtifactName, IReadOnlyList<ExpiredArtifactEncounter> ExpiredEncounters);

    // One candidate run whose only matching artifact(s) were expired — carried
    // through so the final "every candidate exhausted" abort message can name
    // a specific run for `gh run rerun {RunId}` guidance.
    private sealed record ExpiredArtifactEncounter(long RunId, string Event, IReadOnlyList<string> ArtifactNames);

    // Outcome of VerifyAttestationAsync — one of every AttestationVerificationStatus
    // value EXCEPT ReadyToVerify (an internal-to-Evaluate intermediate state
    // that never escapes VerifyAttestationAsync; by the time this record is
    // returned, the openssl step has already resolved it to Valid or
    // BadSignature, or a pre-signature check already resolved it to something
    // else). CheckSet/Ts are populated whenever a note was parsed far enough
    // to have them (including failure paths like TreeMismatch/BadSignature),
    // so failure messages can still cite them when useful.
    private sealed record AttestationVerifyOutcome(AttestationVerificationStatus Status, string? Tree, string? CheckSet, string? Ts, string? Detail);

    // Outcome of RunAttestationPolicyStepAsync — whether the pipeline must
    // abort (mode=require + non-Valid) and, when in mode=warn with a
    // non-Valid outcome, the advisory line PR/merge repeats just before the
    // SUCCEEDED banner so it survives scrollback from the later steps.
    // Release mode ignores RepeatAdvisory (gate is early; one print is enough).
    private sealed record AttestationStepResult(bool ShouldAbort, string? RepeatAdvisory);
  }
}
