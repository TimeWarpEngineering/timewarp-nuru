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
internal sealed class RepoConfigService : IRepoConfigService
{
  private const string ConfigFileName = ".timewarp/dev.jsonc";

  // A context instance owns the .jsonc options, so its generated RepoConfig
  // property is a JsonTypeInfo<RepoConfig> that carries them.
  private static readonly DevCliJsonContext JsonContext = new(
    new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true,
      ReadCommentHandling = JsonCommentHandling.Skip,
      AllowTrailingCommas = true
    });

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
    // The generated JsonTypeInfo<RepoConfig> keeps the call on the source-generated
    // resolver; the generic Deserialize<T>(string, options) overload is
    // RequiresUnreferencedCode (IL2026/IL3050). Not options.GetTypeInfo(typeof(T)):
    // on net11 that trips CA2263 in consumers, and GetTypeInfo<T>() is net11-only.
    RepoConfig? config = JsonSerializer.Deserialize(json, JsonContext.RepoConfig);

    return config ?? new RepoConfig();
  }
}