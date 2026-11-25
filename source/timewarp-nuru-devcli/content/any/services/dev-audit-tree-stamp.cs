#region Purpose
// DevCli's ready-made `dev audit` pass cache (kanban task 601): a pass recorded for the
// committed tree, so the next run on the same tree skips every check. Ported from ganda's
// DevAuditStamp (ganda task 707).
#endregion
#region Design
// The key is SHA-256 of HEAD's root tree listing without the ignored top-level entries
// (default: kanban), plus the SDK `dotnet --version` reports. Checks read source, tests,
// tools and the build props, not kanban/, so a kanban-only commit keeps the key. The SDK
// line is there because global.json rolls forward.
// A worktree with a change outside the ignored entries, untracked files included, has no
// key: the checks would read that change, and the committed tree does not name it.
// The file lives under artifacts/ (gitignored) and holds one key: the last tree that
// passed. Recording overwrites it; a failed run never reaches RecordPass (the runner's
// rule), so a fail is never read back as a pass.
// A static Create, not a public constructor call in each repo: this file compiles into
// every consumer, and an internal class no code constructs fails CA1812 there.
#endregion

namespace DevCli;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// A <see cref="IDevAuditPassCache"/> keyed by the committed tree and the .NET SDK version.
/// Turn it on with <c>passCache = DevAuditTreeStamp.Create();</c> in <c>ConfigurePassCache</c>.
/// </summary>
internal sealed class DevAuditTreeStamp : IDevAuditPassCache
{
  /// <summary>Path of the pass file, relative to the repository root.</summary>
  public const string RelativePath = "artifacts/dev-audit/passed.key";

  /// <summary>The top-level entries ignored when no list is given.</summary>
  public static readonly IReadOnlyList<string> DefaultIgnoredEntries = ["kanban"];

  private readonly HashSet<string> IgnoredEntries;

  private DevAuditTreeStamp(IEnumerable<string> ignoredEntries)
  {
    IgnoredEntries = new HashSet<string>(ignoredEntries, StringComparer.Ordinal);
  }

  /// <summary>
  /// A tree stamp that ignores <paramref name="ignoredEntries"/> (top-level file or folder
  /// names), or <see cref="DefaultIgnoredEntries"/> when none are given.
  /// </summary>
  public static DevAuditTreeStamp Create(params IEnumerable<string>? ignoredEntries)
  {
    List<string> entries = ignoredEntries is null ? [] : [.. ignoredEntries];
    return new DevAuditTreeStamp(entries.Count == 0 ? DefaultIgnoredEntries : entries);
  }

  /// <inheritdoc />
  public async ValueTask<string?> KeyAsync(DevAuditContext context, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(context);

    CommandOutput status = await GitAsync(
      context.RepoRoot,
      ["-c", "core.quotepath=false", "status", "--porcelain", "--untracked-files=all"],
      cancellationToken).ConfigureAwait(false);
    if (!status.Success || !OnlyIgnoredChanges(status.Stdout))
    {
      return null;
    }

    CommandOutput tree = await GitAsync(context.RepoRoot, ["ls-tree", "HEAD"], cancellationToken).ConfigureAwait(false);
    if (!tree.Success || string.IsNullOrWhiteSpace(tree.Stdout))
    {
      return null;
    }

    CommandOutput sdk = await DotNet.WithVersion()
      .WithWorkingDirectory(context.RepoRoot)
      .WithNoValidation()
      .CaptureAsync(cancellationToken)
      .ConfigureAwait(false);
    if (!sdk.Success)
    {
      return null;
    }

    return Compute(tree.Stdout, sdk.Stdout.Trim());
  }

  /// <inheritdoc />
  public bool HasPass(DevAuditContext context, string key)
  {
    ArgumentNullException.ThrowIfNull(context);
    ArgumentNullException.ThrowIfNull(key);

    string path = Path.Combine(context.RepoRoot, RelativePath);
    return File.Exists(path)
      && string.Equals(File.ReadAllText(path).Trim(), key, StringComparison.Ordinal);
  }

  /// <inheritdoc />
  public void RecordPass(DevAuditContext context, string key)
  {
    ArgumentNullException.ThrowIfNull(context);
    ArgumentNullException.ThrowIfNull(key);

    string path = Path.Combine(context.RepoRoot, RelativePath);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, key + "\n");
  }

  /// <summary>Key from a <c>git ls-tree HEAD</c> listing and an SDK version, without the ignored entries.</summary>
  public string Compute(string rootTreeListing, string sdkVersion)
  {
    ArgumentNullException.ThrowIfNull(rootTreeListing);
    ArgumentNullException.ThrowIfNull(sdkVersion);

    StringBuilder builder = new();
    builder.Append("dev-audit-inputs\nsdk=").Append(sdkVersion).Append('\n');
    foreach (string raw in rootTreeListing.Split('\n', StringSplitOptions.RemoveEmptyEntries))
    {
      string line = raw.TrimEnd('\r');
      int tab = line.IndexOf('\t', StringComparison.Ordinal);
      if (tab >= 0 && IgnoredEntries.Contains(line[(tab + 1)..]))
      {
        continue;
      }

      builder.Append(line).Append('\n');
    }

    byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
    return Convert.ToHexStringLower(hash);
  }

  /// <summary>
  /// True when every path in a <c>git status --porcelain</c> listing sits under an ignored
  /// top-level entry (an empty listing is true).
  /// </summary>
  public bool OnlyIgnoredChanges(string porcelain)
  {
    ArgumentNullException.ThrowIfNull(porcelain);

    foreach (string raw in porcelain.Split('\n', StringSplitOptions.RemoveEmptyEntries))
    {
      string line = raw.TrimEnd('\r');
      if (line.Length <= 3)
      {
        continue;
      }

      foreach (string path in line[3..].Split(" -> ", StringSplitOptions.RemoveEmptyEntries))
      {
        string trimmed = path.Trim().Trim('"');
        int slash = trimmed.IndexOf('/', StringComparison.Ordinal);
        string top = slash < 0 ? trimmed : trimmed[..slash];
        if (!IgnoredEntries.Contains(top))
        {
          return false;
        }
      }
    }

    return true;
  }

  private static Task<CommandOutput> GitAsync(string repoRoot, string[] arguments, CancellationToken cancellationToken) =>
    Shell.Builder("git")
      .WithArguments(arguments)
      .WithWorkingDirectory(repoRoot)
      .WithEnvironmentVariable("GIT_TERMINAL_PROMPT", "0")
      .WithNoValidation()
      .CaptureAsync(cancellationToken);
}
