#region Purpose
// Green-key IO: marker lookup and staging, the runner image, and the committed tree the ledger hashes.
#endregion

#region Design
// Split from green-key-ledger.cs so each file stays within the line limit. Lookup uses gh api.
// A run step has no ACTIONS_RUNTIME_TOKEN, so Stage writes a file and a GITHUB_OUTPUT name
// for the workflow upload step. It does not call the artifact service.
#endregion

namespace DevCli;

internal static partial class GreenKeyLedger
{
  private static async Task<GreenKeyLookup> FindAsync(
    string repository,
    string repoRoot,
    string key,
    CancellationToken cancellationToken)
  {
    if (!GreenKeyManifest.IsKey(key))
    {
      return new GreenKeyLookup(false, null, "unavailable", []);
    }

    string name = GreenKeyMarkers.ArtifactName(key);
    string path = "repos/" + repository + "/actions/artifacts?per_page=100&name=" + Uri.EscapeDataString(name);
    CommandOutput listed;
    try
    {
      listed = await Gh.Api(path)
        .WithPaginate()
        .WithJq(".artifacts[] | {id,expired,created_at,run_id:.workflow_run.id,head_sha:.workflow_run.head_sha}")
        .WithWorkingDirectory(repoRoot)
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);
    }
    catch (System.ComponentModel.Win32Exception)
    {
      return new GreenKeyLookup(false, null, "unavailable", []);
    }

    if (listed.ExitCode != 0)
    {
      return new GreenKeyLookup(false, null, "failed", []);
    }

    List<GreenKeyArtifactRow> rows = [];
    string text = (listed.Stdout ?? "").Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    foreach (string line in text.Split('\n'))
    {
      if (GreenKeyMarkers.TryParseArtifactRow(line, out GreenKeyArtifactRow row))
      {
        rows.Add(row);
      }
    }

    return new GreenKeyLookup(true, GreenKeyMarkers.SelectMarker(key, rows), "ok", rows);
  }

  private static async Task<PackageProbe> ProbePackagesAsync(
    string repository,
    string repoRoot,
    long runId,
    CancellationToken cancellationToken)
  {
    string path = "repos/" + repository + "/actions/runs/"
      + runId.ToString(CultureInfo.InvariantCulture) + "/artifacts?per_page=100";
    CommandOutput listed;
    try
    {
      listed = await Gh.Api(path)
        .WithWorkingDirectory(repoRoot)
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);
    }
    catch (System.ComponentModel.Win32Exception)
    {
      return PackageProbe.Unknown;
    }

    if (listed.ExitCode != 0 || string.IsNullOrWhiteSpace(listed.Stdout))
    {
      return PackageProbe.Unknown;
    }

    try
    {
      using JsonDocument document = JsonDocument.Parse(listed.Stdout);
    }
    catch (JsonException)
    {
      return PackageProbe.Unknown;
    }

    return GreenKeySkip.HasLivePackagesArtifact(listed.Stdout) ? PackageProbe.Live : PackageProbe.None;
  }

  private static GreenKeyStageResult Stage(string repoRoot, string key, string runId, string commit)
  {
    if (!GreenKeyManifest.IsKey(key))
    {
      return new GreenKeyStageResult(false, "reason=bad-key");
    }

    string? outputPath = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
    if (string.IsNullOrWhiteSpace(outputPath))
    {
      return new GreenKeyStageResult(false, "reason=no-github-output");
    }

    string relative = GreenKeyMarkers.MarkerRelativePath;
    string full = Path.Combine(repoRoot, relative.Replace('/', Path.DirectorySeparatorChar));
    try
    {
      string? directory = Path.GetDirectoryName(full);
      if (string.IsNullOrEmpty(directory))
      {
        return new GreenKeyStageResult(false, "reason=marker-file");
      }

      Directory.CreateDirectory(directory);
      File.WriteAllText(full, GreenKeyMarkers.MarkerDocument(key, runId, commit));
      File.AppendAllText(outputPath, GreenKeyMarkers.ArtifactOutputLine(key) + "\n");
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      return new GreenKeyStageResult(false, "reason=marker-file error=" + GreenKeyLog.Redact(exception.Message));
    }

    return new GreenKeyStageResult(true, "artifact=" + GreenKeyMarkers.ArtifactName(key) + " path=" + relative);
  }

  private static async Task WriteOutputsAsync(
    string key,
    bool writer,
    string earlier,
    CancellationToken cancellationToken)
  {
    string? outputPath = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
    if (string.IsNullOrEmpty(outputPath))
    {
      return;
    }

    string text = "green_key=" + key + "\n"
      + "green_key_writer=" + (writer ? "true" : "false") + "\n"
      + "green_key_earlier=" + earlier + "\n";
    try
    {
      await File.AppendAllTextAsync(outputPath, text, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      // No outputs means the ci job does not upload a marker.
    }
  }

  private static string? ReadEventJson()
  {
    string? eventPath = Environment.GetEnvironmentVariable("GITHUB_EVENT_PATH");
    if (string.IsNullOrWhiteSpace(eventPath) || !File.Exists(eventPath))
    {
      return null;
    }

    try
    {
      return File.ReadAllText(eventPath);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      return null;
    }
  }

  private static async Task<string> HeadShaAsync(string repoRoot, CancellationToken cancellationToken)
  {
    try
    {
      CommandOutput head = await Shell.Builder("git")
        .WithArguments("rev-parse", "HEAD")
        .WithWorkingDirectory(repoRoot)
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);
      return head.ExitCode == 0 ? head.Stdout.Trim() : "";
    }
    catch (System.ComponentModel.Win32Exception)
    {
      return "";
    }
  }

  private static async Task<string> SdkVersionAsync(CancellationToken cancellationToken)
  {
    try
    {
      CommandOutput version = await DotNet.WithVersion()
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);
      if (version.ExitCode == 0 && !string.IsNullOrWhiteSpace(version.Stdout))
      {
        return version.Stdout.Trim();
      }
    }
    catch (System.ComponentModel.Win32Exception)
    {
      return "unknown";
    }

    return "unknown";
  }

  private static async Task<string> RunnerImageAsync(CancellationToken cancellationToken)
  {
    string? overrideImage = Environment.GetEnvironmentVariable("GREEN_KEY_RUNNER_IMAGE");
    string? imageOs = Environment.GetEnvironmentVariable("ImageOS");
    string? imageVersion = Environment.GetEnvironmentVariable("ImageVersion");
    string? dpkgHash = null;
    if (string.IsNullOrWhiteSpace(overrideImage))
    {
      dpkgHash = await DpkgStatusHashAsync(cancellationToken).ConfigureAwait(false);
    }

    return GreenKeyRunnerImage.Describe(overrideImage, imageOs, imageVersion, dpkgHash);
  }

  private static async Task<string?> DpkgStatusHashAsync(CancellationToken cancellationToken)
  {
    const string Path = "/var/lib/dpkg/status";
    if (!File.Exists(Path))
    {
      return null;
    }

    await using FileStream stream = File.OpenRead(Path);
    byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
    return Convert.ToHexStringLower(hash);
  }

  private static List<string> ReadImagePulls(string repoRoot)
  {
    HashSet<string> tags = [];
    CollectImagePulls(Path.Combine(repoRoot, ".github", "workflows", "workflow.yml"), tags);
    CollectImageTree(Path.Combine(repoRoot, ".github", "scripts"), tags);
    CollectImageTree(Path.Combine(repoRoot, ".github", "actions"), tags);
    List<string> sorted = [.. tags];
    sorted.Sort(StringComparer.Ordinal);
    return sorted;
  }

  private static void CollectImageTree(string directory, HashSet<string> tags)
  {
    if (!Directory.Exists(directory))
    {
      return;
    }

    foreach (string file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
      .OrderBy(path => path, StringComparer.Ordinal))
    {
      CollectImagePulls(file, tags);
    }
  }

  private static void CollectImagePulls(string path, HashSet<string> tags)
  {
    if (!File.Exists(path))
    {
      return;
    }

    foreach (string tag in GreenKeyRunnerImage.FindImagePulls(File.ReadAllText(path)))
    {
      tags.Add(tag);
    }
  }

  private static async Task<Dictionary<string, string?>> ResolveDigestsAsync(
    IReadOnlyList<string> tags,
    CancellationToken cancellationToken)
  {
    Dictionary<string, string?> digests = [];
    foreach (string tag in tags)
    {
      digests[tag] = await ResolveDigestAsync(tag, cancellationToken).ConfigureAwait(false);
    }

    return digests;
  }

  private static async Task<string?> ResolveDigestAsync(string tag, CancellationToken cancellationToken)
  {
    try
    {
      CommandOutput inspected = await Shell.Builder("docker")
        .WithArguments("buildx", "imagetools", "inspect", "--format", "{{.Manifest.Digest}}", tag)
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);
      if (inspected.ExitCode == 0)
      {
        string digest = inspected.Stdout.Trim();
        if (digest.StartsWith("sha256:", StringComparison.Ordinal))
        {
          return digest;
        }
      }
    }
    catch (System.ComponentModel.Win32Exception)
    {
      return null;
    }

    return null;
  }

  private static void AppendSummary(string markdown)
  {
    string? path = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
    if (string.IsNullOrWhiteSpace(path))
    {
      return;
    }

    try
    {
      File.AppendAllText(path, markdown);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      // The terminal lines are the record. The summary is a copy.
    }
  }

  private enum ForceRead
  {
    NotSet,
    FullRun,
    Unreadable
  }

  private enum PackageProbe
  {
    None,
    Live,
    Unknown
  }
}
