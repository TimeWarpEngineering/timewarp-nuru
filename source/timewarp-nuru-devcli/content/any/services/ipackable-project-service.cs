#region Purpose
// Abstraction for deriving the set of packable projects (per MSBuild IsPackable)
// under a repo's source/ tree — replaces hand-maintained project/package-ID lists
// in pack, push, and check-version (kanban task 458-004).
#endregion

namespace DevCli;

/// <summary>
/// A packable MSBuild project discovered under <c>source/</c>.
/// </summary>
public sealed record PackableProject(string ProjectPath, string PackageId);

/// <summary>
/// Derives the set of packable projects (<c>IsPackable=true</c>) under a repo's
/// <c>source/</c> tree, so pack/push/check-version stop hand-maintaining lists.
/// </summary>
public interface IPackableProjectService
{
  /// <summary>
  /// Returns every packable project under <c>{repoRoot}/source/</c>, sorted by
  /// PackageId (ordinal). Returns an empty list if <c>source/</c> does not exist.
  /// </summary>
  ValueTask<IReadOnlyList<PackableProject>> GetPackableProjectsAsync(string repoRoot, CancellationToken cancellationToken = default);
}
