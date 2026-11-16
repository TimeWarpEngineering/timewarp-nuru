#region Purpose
// The fleet pull request build decision (kanban task 595): does this pull
// request need build and test? One implementation, shipped to every repo's
// dev CLI as DevCli content, so a rule change reaches every repo through the
// TimeWarp.Nuru.DevCli pin instead of hand copies.
#endregion
#region Design
// RequiresBuild is ganda's WorkflowChangeFilter.RequiresBuild, ported exactly
// (timewarp-ganda source/timewarp-ganda/services/workflow-change-filter.cs).
// A pull request skips build and test only when every changed path is a
// declared non-build path: kanban/**, documentation/**, assets/**, notes/**,
// .github/** except .github/workflows/**, a file named .gitignore, or a path
// ending in .md outside source/, tests/, tools/, and msbuild/. Those four roots
// always build, markdown included. .editorconfig builds (warnings are errors).
// An empty diff does not require a build. An empty or whitespace path does.
//
// Repo additions (PullRequestBuildConfig, dev.jsonc "pullRequestBuild") slot
// into that order without loosening it:
//   1. .github/workflows/** builds.
//   2. The protected roots build.
//   3. The repo's BuildPaths build.
//   4. The default non-build paths skip.
//   5. A file named .editorconfig builds.
//   6. The repo's NonBuildPaths skip.
//   7. .gitignore and markdown skip; anything else builds.
// Steps 1, 2 and 5 come before step 6, so no repo entry can make the
// protected roots, the workflows, or .editorconfig non-build. Step 5 sits
// after step 4 so the default rule stays the ganda rule byte for byte.
//
// DecideAsync is the one call a repo's workflow command makes. It reads
// GITHUB_EVENT_NAME and GITHUB_EVENT_PATH, takes pull_request.base.sha and
// head.sha from the event, runs git diff --name-only base...head in the repo
// root, reads pullRequestBuild from .timewarp/dev.jsonc, and applies the rule.
// Every path that cannot answer builds: a push, a release, a dispatch, a local
// run (no event name), an unreadable event, a failed diff, an unreadable or
// invalid dev.jsonc. Only a definite all-non-build diff skips.
// The checkout needs the history between base and head (fetch-depth: 0) for
// the three-dot diff; a shallow checkout fails the diff and so builds.
#endregion

namespace DevCli;

using System.Text.Json;

/// <summary>
/// Outcome of <see cref="PullRequestBuildClassifier.DecideAsync(string, CancellationToken)"/>.
/// </summary>
/// <param name="RequiresBuild">False only when a pull request changes declared non-build paths only.</param>
/// <param name="Message">One line to print: why the run builds or skips.</param>
/// <param name="ChangedPaths">The pull request diff, or empty when it was not read.</param>
internal sealed record PullRequestBuildDecision(
  bool RequiresBuild,
  string Message,
  IReadOnlyList<string> ChangedPaths);

/// <summary>
/// Decides whether a pull request needs build and test. See the Design region for the rule.
/// </summary>
internal static class PullRequestBuildClassifier
{
  /// <summary>The event name that can skip. Every other event builds.</summary>
  public const string PullRequestEventName = "pull_request";

  /// <summary>Printed when build and test are skipped.</summary>
  public const string SkipMessage =
    "Pull request changes only declared non-build paths. Skipping build and test. The rule is PullRequestBuildClassifier (TimeWarp.Nuru.DevCli).";

  /// <summary>Roots that always build, including markdown. No repo entry can change them.</summary>
  public static IReadOnlyList<string> ProtectedBuildRoots { get; } = ["source", "tests", "tools", "msbuild"];

  /// <summary>Directory trees that are non-build by default (<c>.github</c> minus <c>.github/workflows</c>).</summary>
  public static IReadOnlyList<string> DefaultNonBuildRoots { get; } = ["kanban", "documentation", "assets", "notes", ".github"];

  private const string WorkflowsRoot = ".github/workflows";
  private const string ConfigFileName = ".timewarp/dev.jsonc";

  /// <summary>
  /// The one call for a repo's <c>workflow</c> command. Reads the GitHub event from the
  /// environment and <c>pullRequestBuild</c> from <c>&lt;repoRoot&gt;/.timewarp/dev.jsonc</c>.
  /// </summary>
  public static async Task<PullRequestBuildDecision> DecideAsync(
    string repoRoot,
    CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(repoRoot);
    string? eventName = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME");
    if (!IsPullRequest(eventName))
    {
      return NotPullRequest(eventName);
    }

    PullRequestBuildConfig? config;
    try
    {
      config = await ReadConfigAsync(repoRoot, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
    {
      return Build($"{ConfigFileName} could not be read ({exception.Message}). Full build.");
    }

    return await DecideAsync(
      repoRoot,
      eventName,
      Environment.GetEnvironmentVariable("GITHUB_EVENT_PATH"),
      config,
      cancellationToken).ConfigureAwait(false);
  }

  /// <summary>
  /// The decision with every input explicit (tests, and callers that already hold the config).
  /// </summary>
  public static async Task<PullRequestBuildDecision> DecideAsync(
    string repoRoot,
    string? eventName,
    string? eventPath,
    PullRequestBuildConfig? config,
    CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(repoRoot);
    if (!IsPullRequest(eventName))
    {
      return NotPullRequest(eventName);
    }

    string? invalid = FindInvalidEntry(config);
    if (invalid is not null)
    {
      return Build($"{ConfigFileName} pullRequestBuild entry '{invalid}' is not a path, 'dir/**', or 'dir/'. Full build.");
    }

    if (string.IsNullOrWhiteSpace(eventPath))
    {
      return Build("GITHUB_EVENT_PATH is not set. Full build.");
    }

    string json;
    try
    {
      json = await File.ReadAllTextAsync(eventPath, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      return Build($"The GitHub event file {eventPath} could not be read ({exception.Message}). Full build.");
    }

    if (!TryReadPullRequestShas(json, out string baseSha, out string headSha))
    {
      return Build("The GitHub event has no pull_request.base.sha and head.sha. Full build.");
    }

    IReadOnlyList<string>? changed = await ListChangedPathsAsync(repoRoot, baseSha, headSha, cancellationToken)
      .ConfigureAwait(false);
    if (changed is null)
    {
      return Build($"git diff --name-only {baseSha}...{headSha} failed. Full build.");
    }

    string? buildPath = FirstBuildPath(changed, config);
    if (buildPath is not null)
    {
      return new PullRequestBuildDecision(true, $"Build path changed: {buildPath}. Full build.", changed);
    }

    return new PullRequestBuildDecision(false, $"{SkipMessage} ({changed.Count} changed paths)", changed);
  }

  /// <summary>
  /// True when any path is outside the default non-build allowlist. An empty list does not require a build.
  /// </summary>
  public static bool RequiresBuild(IEnumerable<string> relativePaths) => RequiresBuild(relativePaths, null);

  /// <summary>
  /// True when any path is outside the non-build allowlist plus the repo's additions.
  /// An empty list does not require a build.
  /// </summary>
  public static bool RequiresBuild(IEnumerable<string> relativePaths, PullRequestBuildConfig? config)
  {
    ArgumentNullException.ThrowIfNull(relativePaths);
    return FirstBuildPath(relativePaths, config) is not null;
  }

  /// <summary>
  /// Reads <c>pull_request.base.sha</c> and <c>pull_request.head.sha</c>. Both must be hex SHAs.
  /// </summary>
  public static bool TryReadPullRequestShas(string? json, out string baseSha, out string headSha)
  {
    baseSha = "";
    headSha = "";
    if (string.IsNullOrWhiteSpace(json))
    {
      return false;
    }

    try
    {
      using JsonDocument document = JsonDocument.Parse(json);
      JsonElement root = document.RootElement;
      if (root.ValueKind != JsonValueKind.Object
        || !root.TryGetProperty("pull_request", out JsonElement pullRequest)
        || pullRequest.ValueKind != JsonValueKind.Object)
      {
        return false;
      }

      return TrySha(pullRequest, "base", out baseSha) && TrySha(pullRequest, "head", out headSha);
    }
    catch (JsonException)
    {
      baseSha = "";
      headSha = "";
      return false;
    }
  }

  /// <summary>
  /// <c>git diff --name-only base...head</c> in <paramref name="repoRoot"/>. Null when git cannot answer.
  /// </summary>
  public static async Task<IReadOnlyList<string>?> ListChangedPathsAsync(
    string repoRoot,
    string baseSha,
    string headSha,
    CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(repoRoot);
    ArgumentNullException.ThrowIfNull(baseSha);
    ArgumentNullException.ThrowIfNull(headSha);
    if (!IsSha(baseSha) || !IsSha(headSha))
    {
      return null;
    }

    CommandOutput result;
    try
    {
      result = await Shell.Builder("git")
        .WithArguments("diff", "--name-only", baseSha + "..." + headSha)
        .WithWorkingDirectory(repoRoot)
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);
    }
    catch (System.ComponentModel.Win32Exception)
    {
      return null;
    }

    if (result.ExitCode != 0)
    {
      return null;
    }

    string text = result.Stdout
      .Replace("\r\n", "\n", StringComparison.Ordinal)
      .Replace('\r', '\n');
    List<string> paths = [];
    foreach (string line in text.Split('\n'))
    {
      string trimmed = line.Trim();
      if (trimmed.Length > 0)
      {
        paths.Add(trimmed);
      }
    }

    return paths;
  }

  private static bool IsPullRequest(string? eventName) =>
    string.Equals(eventName, PullRequestEventName, StringComparison.Ordinal);

  private static PullRequestBuildDecision NotPullRequest(string? eventName) =>
    string.IsNullOrEmpty(eventName)
      ? Build("No GITHUB_EVENT_NAME (local run). Full build.")
      : Build($"Event {eventName} is not a pull request. Full build.");

  private static PullRequestBuildDecision Build(string message) => new(true, message, []);

  private static async Task<PullRequestBuildConfig?> ReadConfigAsync(string repoRoot, CancellationToken cancellationToken)
  {
    string path = Path.Combine(repoRoot, ConfigFileName);
    if (!File.Exists(path))
    {
      return null;
    }

    string json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
    return RepoConfigService.Parse(json).PullRequestBuild;
  }

  private static string? FirstBuildPath(IEnumerable<string> relativePaths, PullRequestBuildConfig? config)
  {
    foreach (string raw in relativePaths)
    {
      if (!IsNonBuildPath(raw, config))
      {
        return raw;
      }
    }

    return null;
  }

  private static bool IsNonBuildPath(string? raw, PullRequestBuildConfig? config)
  {
    string? path = Normalize(raw);
    if (path is null)
    {
      return false;
    }

    if (IsUnder(path, WorkflowsRoot))
    {
      return false;
    }

    foreach (string root in ProtectedBuildRoots)
    {
      if (IsUnder(path, root))
      {
        return false;
      }
    }

    if (MatchesAny(path, config?.BuildPaths))
    {
      return false;
    }

    foreach (string root in DefaultNonBuildRoots)
    {
      if (path.StartsWith(root + "/", StringComparison.Ordinal))
      {
        return true;
      }
    }

    string name = FileName(path);
    if (string.Equals(name, ".editorconfig", StringComparison.Ordinal))
    {
      return false;
    }

    if (MatchesAny(path, config?.NonBuildPaths))
    {
      return true;
    }

    if (string.Equals(name, ".gitignore", StringComparison.Ordinal))
    {
      return true;
    }

    return name.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
  }

  private static string? Normalize(string? raw)
  {
    if (string.IsNullOrWhiteSpace(raw))
    {
      return null;
    }

    string path = raw.Replace('\\', '/').Trim();
    if (path.StartsWith("./", StringComparison.Ordinal))
    {
      path = path[2..];
    }

    return path.Length == 0 ? null : path;
  }

  private static string FileName(string path)
  {
    int slash = path.LastIndexOf('/');
    return slash >= 0 ? path[(slash + 1)..] : path;
  }

  private static bool IsUnder(string path, string root) =>
    string.Equals(path, root, StringComparison.Ordinal)
    || path.StartsWith(root + "/", StringComparison.Ordinal);

  private static bool MatchesAny(string path, IReadOnlyList<string>? entries)
  {
    if (entries is null)
    {
      return false;
    }

    foreach (string entry in entries)
    {
      string? root = EntryRoot(entry);
      if (root is not null && IsUnder(path, root))
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>"dir", "dir/", "dir/**", "./dir" → "dir". Null when the entry is not a supported path.</summary>
  private static string? EntryRoot(string? entry)
  {
    string? root = Normalize(entry);
    if (root is null)
    {
      return null;
    }

    if (root.EndsWith("/**", StringComparison.Ordinal))
    {
      root = root[..^3];
    }

    root = root.TrimEnd('/');
    if (root.Length == 0 || root.Contains('*', StringComparison.Ordinal) || root.Contains('?', StringComparison.Ordinal))
    {
      return null;
    }

    return root;
  }

  private static string? FindInvalidEntry(PullRequestBuildConfig? config)
  {
    if (config is null)
    {
      return null;
    }

    return FirstInvalidEntry(config.BuildPaths) ?? FirstInvalidEntry(config.NonBuildPaths);
  }

  private static string? FirstInvalidEntry(IReadOnlyList<string>? entries)
  {
    if (entries is null)
    {
      return null;
    }

    foreach (string? entry in entries)
    {
      if (EntryRoot(entry) is null)
      {
        return entry ?? "null";
      }
    }

    return null;
  }

  private static bool TrySha(JsonElement pullRequest, string side, out string sha)
  {
    sha = "";
    if (!pullRequest.TryGetProperty(side, out JsonElement element)
      || element.ValueKind != JsonValueKind.Object
      || !element.TryGetProperty("sha", out JsonElement shaElement)
      || shaElement.ValueKind != JsonValueKind.String)
    {
      return false;
    }

    string? value = shaElement.GetString();
    if (value is null || !IsSha(value))
    {
      return false;
    }

    sha = value;
    return true;
  }

  private static bool IsSha(string value)
  {
    if (value.Length is < 7 or > 64)
    {
      return false;
    }

    foreach (char character in value)
    {
      bool hex = character is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F');
      if (!hex)
      {
        return false;
      }
    }

    return true;
  }
}
