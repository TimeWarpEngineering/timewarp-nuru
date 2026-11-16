#region Purpose
// Per-repo additions to the pull request build rule (kanban task 595).
// Deserialized from .timewarp/dev.jsonc under the "pullRequestBuild" key and
// read by PullRequestBuildClassifier.DecideAsync.
#endregion
#region Design
// The extension lives in dev.jsonc, not in a caller argument, so the consumer's
// workflow command is one identical call in every repo (ganda's audit greps
// for it) and the repo's additions are data a human or an audit reads without
// parsing C#. RepoConfig is already what every dev CLI loads.
//
// Entry syntax, both lists: a repo-relative path. "dir", "dir/", and "dir/**"
// mean the directory tree; any other entry is one exact path ("readme.md" is
// the root readme only). No other wildcards: an entry with '*' or '?' anywhere
// but a trailing "/**" is invalid, and an invalid entry makes the classifier
// build (fail closed) with a message naming the entry.
//
// BuildPaths win over every non-build list. NonBuildPaths never apply to the
// protected build roots (source, tests, tools, msbuild), .github/workflows, or
// a file named .editorconfig: the classifier checks those first.
#endregion

namespace DevCli;

/// <summary>
/// Repo additions to the pull request build rule, from <c>.timewarp/dev.jsonc</c> key
/// <c>pullRequestBuild</c>. See <see cref="PullRequestBuildClassifier"/>.
/// </summary>
internal sealed class PullRequestBuildConfig
{
  /// <summary>
  /// Extra paths that always build, even when a default non-build path covers them
  /// (for example <c>readme.md</c> when the readme is packed into a package).
  /// </summary>
  public IReadOnlyList<string>? BuildPaths { get; init; }

  /// <summary>
  /// Extra non-build paths (for example <c>skills/**</c>). They cannot cover the
  /// protected build roots, <c>.github/workflows</c>, or <c>.editorconfig</c>.
  /// </summary>
  public IReadOnlyList<string>? NonBuildPaths { get; init; }
}
