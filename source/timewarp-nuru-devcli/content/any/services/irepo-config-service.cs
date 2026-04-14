#region Purpose
// Abstraction for reading per-repo dev-cli configuration from .timewarp/dev.jsonc.
#endregion

namespace DevCli;

/// <summary>
/// Reads per-repo configuration from <c>.timewarp/dev.jsonc</c>.
/// </summary>
public interface IRepoConfigService
{
  /// <summary>
  /// Loads the repo configuration.
  /// Returns an empty <see cref="RepoConfig"/> with defaults if the config file does not exist.
  /// </summary>
  ValueTask<RepoConfig> GetConfigAsync(CancellationToken cancellationToken = default);
}