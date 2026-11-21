#region Purpose
// The git half of `dev benchmark run --base <ref>` (kanban task 578): resolve the base
// ref, check it out into a temporary detached worktree, read the head revision, and
// remove the worktree afterwards.
#endregion
#region Design
// A linked worktree (`git worktree add --detach`), not a clone: it shares the object
// store, so it is fast and needs no network. It lives under the system temp directory,
// not under the repo, so the base tree's MSBuild never walks up into the head tree's
// Directory.Build.props.
//
// Repo hooks are off for the add (`-c core.hooksPath=<a path with no hooks>`): a
// post-checkout hook (ganda repo attest in Nuru) has nothing to do on a throwaway tree.
//
// The ref is resolved with `rev-parse --verify --end-of-options <ref>^{commit}`, so a
// ref that starts with a dash is not read as an option and a tree or blob is refused.
// The ref is not fetched: pass `origin/master` after a fetch, a tag, or a SHA.
//
// DisposeAsync removes the worktree with --force (the build wrote bin/ and obj/ into
// it), deletes the temp directory, and prunes. Cleanup is best effort and never throws:
// a leftover temp directory must not hide the benchmark's own result or error.
#endregion

namespace DevCli;

/// <summary>A temporary detached worktree of the base ref for <c>dev benchmark run</c>.</summary>
internal sealed class BenchmarkBaseCheckout : IAsyncDisposable
{
  private readonly string RepoRoot;
  private readonly string Container;

  private BenchmarkBaseCheckout(string repoRoot, string container, string root, string commit)
  {
    RepoRoot = repoRoot;
    Container = container;
    Root = root;
    Commit = commit;
  }

  /// <summary>The base tree's root.</summary>
  public string Root { get; }

  /// <summary>The full SHA the base ref resolved to.</summary>
  public string Commit { get; }

  /// <summary>Resolves <paramref name="gitRef"/> and checks it out into a new temporary worktree.</summary>
  /// <exception cref="InvalidOperationException">The ref names no commit, or the worktree cannot be added.</exception>
  public static async Task<BenchmarkBaseCheckout> CreateAsync(string repoRoot, string gitRef, CancellationToken cancellationToken)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(repoRoot);
    ArgumentException.ThrowIfNullOrWhiteSpace(gitRef);

    string commit = await ResolveCommitAsync(repoRoot, gitRef, cancellationToken).ConfigureAwait(false)
      ?? throw new InvalidOperationException
      (
        $"--base '{gitRef}' does not name a commit in {repoRoot}. Pass a branch, tag or SHA; fetch a remote ref first."
      );

    DirectoryInfo container = Directory.CreateTempSubdirectory("dev-benchmark-base-");
    string root = Path.Combine(container.FullName, "tree");
    string noHooks = Path.Combine(container.FullName, "no-hooks");

    CommandOutput add = await Shell.Builder("git")
      .WithArguments("-c", $"core.hooksPath={noHooks}", "worktree", "add", "--detach", root, commit)
      .WithWorkingDirectory(repoRoot)
      .WithNoValidation()
      .CaptureAsync(cancellationToken)
      .ConfigureAwait(false);

    if (add.ExitCode != 0)
    {
      DeleteDirectory(container.FullName);
      throw new InvalidOperationException($"git worktree add for base {commit} failed: {add.Stderr.Trim()}");
    }

    return new BenchmarkBaseCheckout(repoRoot, container.FullName, root, commit);
  }

  /// <summary>The full SHA <paramref name="gitRef"/> names, or null when it names no commit.</summary>
  public static async Task<string?> ResolveCommitAsync(string repoRoot, string gitRef, CancellationToken cancellationToken)
  {
    CommandOutput result = await Shell.Builder("git")
      .WithArguments("rev-parse", "--verify", "--quiet", "--end-of-options", $"{gitRef}^{{commit}}")
      .WithWorkingDirectory(repoRoot)
      .WithNoValidation()
      .CaptureAsync(cancellationToken)
      .ConfigureAwait(false);

    string sha = result.Stdout.Trim();
    return result.ExitCode == 0 && sha.Length > 0 ? sha : null;
  }

  /// <summary>The working tree's HEAD commit and whether it has uncommitted changes.</summary>
  /// <exception cref="InvalidOperationException">HEAD cannot be resolved.</exception>
  public static async Task<DevBenchmarkRevision> ReadHeadAsync(string repoRoot, CancellationToken cancellationToken)
  {
    string commit = await ResolveCommitAsync(repoRoot, "HEAD", cancellationToken).ConfigureAwait(false)
      ?? throw new InvalidOperationException($"Could not resolve HEAD in {repoRoot}.");

    CommandOutput status = await Shell.Builder("git")
      .WithArguments("status", "--porcelain")
      .WithWorkingDirectory(repoRoot)
      .WithNoValidation()
      .CaptureAsync(cancellationToken)
      .ConfigureAwait(false);

    return new DevBenchmarkRevision("HEAD", commit, Dirty: status.ExitCode != 0 || status.Stdout.Trim().Length > 0);
  }

  /// <summary>Removes the worktree and its temp directory. Never throws.</summary>
  public async ValueTask DisposeAsync()
  {
    try
    {
      await Shell.Builder("git")
        .WithArguments("worktree", "remove", "--force", Root)
        .WithWorkingDirectory(RepoRoot)
        .WithNoValidation()
        .CaptureAsync(CancellationToken.None)
        .ConfigureAwait(false);
    }
    catch (Exception exception) when (exception is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
    {
      // Best effort: the directory delete and prune below still run.
    }

    DeleteDirectory(Container);

    try
    {
      await Shell.Builder("git")
        .WithArguments("worktree", "prune")
        .WithWorkingDirectory(RepoRoot)
        .WithNoValidation()
        .CaptureAsync(CancellationToken.None)
        .ConfigureAwait(false);
    }
    catch (Exception exception) when (exception is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
    {
      // Best effort: a stale worktree entry is pruned by the next `git worktree prune`.
    }
  }

  private static void DeleteDirectory(string path)
  {
    try
    {
      if (Directory.Exists(path))
      {
        Directory.Delete(path, recursive: true);
      }
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      // Best effort: the system temp directory is cleaned eventually.
    }
  }
}
