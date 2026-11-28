#region Purpose
// Attest a green-key skip and, on master, stage the carried Packages-* for this run's upload.
#endregion

#region Design
// The observe job already chose skip and named the carry run. This stage does not look the key up
// again: the ci runner may not match the runner that hashed it. Attestation still runs, because the
// key does not hash the attestation note. A master skip downloads that run's live Packages-* and
// checks the nupkg names against this tree's packable set and version. A mismatch deletes the
// download and aborts, so the upload step cannot publish bytes release would reject. A pull request
// skip has no carry run and uploads nothing.
#endregion

namespace DevCli;

public sealed partial class WorkflowCommand
{
  internal sealed partial class Handler
  {
    /// <summary>
    /// Attests, and on a master skip downloads the carried packages into <c>artifacts/packages</c>.
    /// A failure sets the process exit code.
    /// </summary>
    private async Task RunGreenKeySkipAsync()
    {
      string repoRoot = ResolveRepoRoot();
      AttestationStepResult attestation = await RunAttestationPolicyStepAsync(
        repoRoot,
        cliAttestationMode: null,
        AttestationConfigResolver.DefaultPrMode).ConfigureAwait(false);
      if (attestation.ShouldAbort)
      {
        AbortPipeline("attestation required (mode=require) and not valid");
        return;
      }

      if (attestation.RepeatAdvisory is not null)
      {
        Terminal.WriteLine(attestation.RepeatAdvisory.Yellow());
      }

      string? carryText = Environment.GetEnvironmentVariable("GREEN_KEY_CARRY_RUN");
      if (string.IsNullOrWhiteSpace(carryText))
      {
        Terminal.WriteLine("green-key-v1 carry=not-needed");
        return;
      }

      if (!long.TryParse(carryText, NumberStyles.None, CultureInfo.InvariantCulture, out long carryRun) || carryRun <= 0)
      {
        AbortPipeline("green-key carry run is not a run id");
        return;
      }

      DownloadArtifactOutcome downloaded;
      try
      {
        CiRunSummary candidate = new() { DatabaseId = carryRun, Event = "push" };
        downloaded = await DownloadPackagesArtifactAsync(repoRoot, [candidate]).ConfigureAwait(false);
      }
      catch (Exception exception) when (exception is InvalidOperationException or Win32Exception or JsonException or IOException)
      {
        ClearCarriedPackages(repoRoot);
        Terminal.WriteLine("green-key-v1 carry error=" + GreenKeyLog.Redact(exception.Message));
        AbortPipeline("green-key carry download failed");
        return;
      }

      if (downloaded.Status != DownloadArtifactStatus.Downloaded || downloaded.ArtifactName is null)
      {
        ClearCarriedPackages(repoRoot);
        AbortPipeline("green-key carry found no live Packages-* artifact");
        return;
      }

      string? mismatch = await CarriedSetMismatchAsync(repoRoot).ConfigureAwait(false);
      if (mismatch is not null)
      {
        ClearCarriedPackages(repoRoot);
        Terminal.WriteLine("green-key-v1 carry " + mismatch);
        AbortPipeline("carried package set does not match this tree");
        return;
      }

      Terminal.WriteLine(
        "green-key-v1 carry=downloaded run="
        + carryRun.ToString(CultureInfo.InvariantCulture)
        + " artifact="
        + downloaded.ArtifactName);
    }

    private async Task<string?> CarriedSetMismatchAsync(string repoRoot)
    {
      IReadOnlyList<PackableProject> projects = await PackableProjectService
        .GetPackableProjectsAsync(repoRoot, CancellationToken.None)
        .ConfigureAwait(false);
      string? version = ReadPropsVersion(repoRoot);
      if (projects.Count == 0 || string.IsNullOrWhiteSpace(version))
      {
        return "derived=unreadable";
      }

      string directory = Path.Combine(repoRoot, "artifacts", "packages");
      string[] files = Directory.Exists(directory) ? Directory.GetFiles(directory, "*.nupkg") : [];
      IReadOnlyList<string> names = [.. files.Select(Path.GetFileName).OfType<string>()];
      PackageSetVerification verification = CiRunPromotion.VerifyPackageSet(names, projects, version);
      if (verification.IsMatch)
      {
        return null;
      }

      return "missing=" + string.Join(",", verification.Missing) + " unexpected=" + string.Join(",", verification.Unexpected);
    }

    private static void ClearCarriedPackages(string repoRoot)
    {
      string directory = Path.Combine(repoRoot, "artifacts", "packages");
      if (Directory.Exists(directory))
      {
        Directory.Delete(directory, recursive: true);
      }
    }
  }
}
