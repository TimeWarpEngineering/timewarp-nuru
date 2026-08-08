#region Purpose
// Reads per-repo dev-cli configuration from .timewarp/dev.jsonc.
// Returns an empty RepoConfig with defaults if the file does not exist.
// Uses source-generated JSON context for AOT compatibility.
// Unknown keys (e.g. a removed legacy `checkVersionStrategy`) are ignored by
// design — System.Text.Json's default deserialize behavior — so old configs
// keep parsing without a breaking error.
#endregion

namespace DevCli;

using System.Text.Json;

/// <summary>
/// Reads per-repo configuration from <c>.timewarp/dev.jsonc</c>.
/// </summary>
public sealed class RepoConfigService : IRepoConfigService
{
  private const string ConfigFileName = ".timewarp/dev.jsonc";

  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
    TypeInfoResolver = DevCliJsonContext.Default
  };

  /// <inheritdoc />
  public async ValueTask<RepoConfig> GetConfigAsync(CancellationToken cancellationToken = default)
  {
    string? repoRoot = Git.FindRoot();

    if (repoRoot is null)
    {
      return new RepoConfig();
    }

    string configPath = Path.Combine(repoRoot, ConfigFileName);

    if (!File.Exists(configPath))
    {
      return new RepoConfig();
    }

    string json = await File.ReadAllTextAsync(configPath, cancellationToken).ConfigureAwait(false);

    return Parse(json);
  }

  /// <summary>
  /// Deserializes a <see cref="RepoConfig"/> from raw <c>.timewarp/dev.jsonc</c> content.
  /// Unknown keys are ignored. Returns defaults when the JSON deserializes to <c>null</c>.
  /// </summary>
  public static RepoConfig Parse(string json)
  {
    RepoConfig? config = JsonSerializer.Deserialize<RepoConfig>(json, JsonOptions);

    return config ?? new RepoConfig();
  }
}