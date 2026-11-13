#region Purpose
// ReleaseCommand.Handler steps split out of Handle: the tag-availability guard (step 6),
// the check-version publish-state gate (step 7), the release-target walk (step 8) and
// the tag/push/gh release create execution (step 10). Guard helpers print their own
// refusal and return false; Handle sets the exit code.
#endregion

namespace DevCli;

using System.Text.Json;
using TimeWarp.Terminal;

internal sealed partial class ReleaseCommand
{
  internal sealed partial class Handler
  {
    /// <summary>
    /// Step 6: refuses when tag <paramref name="tag"/> already exists locally or on origin.
    /// Prints the refusal and returns <see langword="false"/>.
    /// </summary>
    private async Task<bool> CheckTagAvailableAsync(string tag, CancellationToken cancellationToken)
    {
      CommandOutput localTagResult = await Shell.Builder("git")
        .WithArguments("rev-parse", "-q", "--verify", $"refs/tags/{tag}")
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);

      bool existsLocally;
      if (localTagResult.ExitCode == 0)
      {
        existsLocally = true;
      }
      else if (string.IsNullOrWhiteSpace(localTagResult.Stderr))
      {
        existsLocally = false;
      }
      else
      {
        Terminal.WriteErrorLine($"Error: could not check for local tag {tag}.\n{localTagResult.Stderr.Trim()}");
        return false;
      }

      CommandOutput remoteTagResult = await Shell.Builder("git")
        .WithArguments("ls-remote", "--tags", "origin", $"refs/tags/{tag}")
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);

      if (remoteTagResult.ExitCode != 0)
      {
        Terminal.WriteErrorLine($"Error: could not check for remote tag {tag}.\n{remoteTagResult.Stderr.Trim()}");
        return false;
      }

      bool existsOnRemote = !string.IsNullOrWhiteSpace(remoteTagResult.Stdout);

      GuardVerdict tagVerdict = ReleaseGuard.CheckTagAvailability(tag, existsLocally, existsOnRemote);
      if (!tagVerdict.Ok)
      {
        Terminal.WriteErrorLine($"Error: {tagVerdict.Reason}");
        return false;
      }

      Terminal.WriteLine($"✓ tag {tag} available".Green());
      return true;
    }

    /// <summary>
    /// Step 7: the check-version 3-state gate over the declared feeds. Prints the refusal
    /// and returns <see langword="false"/> unless no package has <paramref name="version"/> yet.
    /// </summary>
    private async Task<bool> CheckNotYetPublishedAsync
    (
      RepoConfig config,
      string? repoRoot,
      string version,
      PackageFeedsResolution feedsResolution,
      CancellationToken cancellationToken
    )
    {
      string? packageInput = config.CheckVersionConfig?.Packages;
      IReadOnlyList<string> packages;

      if (packageInput is not null)
      {
        packages = PublishStateClassifier.ParsePackageList(packageInput);
      }
      else if (repoRoot is not null)
      {
        IReadOnlyList<PackableProject> packableProjects = await PackableProjectService
          .GetPackableProjectsAsync(repoRoot, cancellationToken)
          .ConfigureAwait(false);

        packages = [.. packableProjects.Select(p => p.PackageId)];
      }
      else
      {
        packages = [];
      }

      if (packages.Count == 0)
      {
        Terminal.WriteErrorLine("Error: no packable projects found under source/ and no packages configured. Use checkVersionConfig.packages in .timewarp/dev.jsonc.");
        return false;
      }

      List<string> invalidPackages = [.. packages.Where(p => !NuGetVersionService.IsValidPackageId(p))];
      if (invalidPackages.Count > 0)
      {
        Terminal.WriteErrorLine($"Error: invalid NuGet package id(s): {string.Join(", ", invalidPackages)}");
        return false;
      }

      List<string> alreadyPublished = [];
      foreach (string pkg in packages)
      {
        PackageFeedLookup lookup;
        try
        {
          lookup = await NuGetVersionService
            .LookupAsync(pkg, feedsResolution.Feeds, cancellationToken)
            .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
          // Fail closed (470-007 M9): an unreachable/erroring NuGet must not
          // be read as "not yet published".
          Terminal.WriteErrorLine($"Error: NuGet lookup for '{pkg}' failed: {ex.Message}");
          Terminal.WriteErrorLine("  Cannot determine whether this version is already published; refusing to release. Retry when NuGet is reachable.");
          return false;
        }

        PackagePublishVerdict verdict = lookup.Check(version);

        if (verdict == PackagePublishVerdict.Unknown)
        {
          // 536: on no declared feed and GitHub Packages unreadable — refuse
          // with the feed's own message, not the already-released one.
          foreach (string line in lookup.FormatUnknownRefusal(version))
          {
            Terminal.WriteErrorLine(line);
          }

          return false;
        }

        if (verdict == PackagePublishVerdict.Published)
        {
          alreadyPublished.Add(pkg);
        }
      }

      PublishState publishState = PublishStateClassifier.Classify(packages.Count, alreadyPublished.Count);

      GuardVerdict publishVerdict = ReleaseGuard.CheckPublishState(publishState, version);
      if (!publishVerdict.Ok)
      {
        Terminal.WriteErrorLine($"Error: {publishVerdict.Reason}");
        return false;
      }

      Terminal.WriteLine($"✓ version {version} not yet published".Green());
      return true;
    }

    /// <summary>
    /// Step 10: creates the annotated tag at <paramref name="targetSha"/>, pushes it, then runs
    /// gh release create. Sets <see cref="Environment.ExitCode"/> to 1 on failure.
    /// </summary>
    private async Task ExecuteReleaseAsync
    (
      string tag,
      string targetSha,
      IReadOnlyList<string> releaseCreateArguments,
      string releaseCommand,
      CancellationToken cancellationToken
    )
    {
      CommandOutput tagCreateResult = await Shell.Builder("git")
        .WithArguments("tag", "-a", tag, "-m", $"Release {tag}", targetSha)
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);

      if (tagCreateResult.ExitCode != 0)
      {
        Terminal.WriteErrorLine($"Error: failed to create tag {tag}.\n{tagCreateResult.Stderr.Trim()}");
        Environment.ExitCode = 1;
        return;
      }

      Terminal.WriteLine($"Created tag {tag} at {ShortSha(targetSha)}.");

      CommandOutput pushResult = await Shell.Builder("git")
        .WithArguments("push", "origin", tag)
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);

      if (pushResult.ExitCode != 0)
      {
        CommandOutput deleteResult = await Shell.Builder("git")
          .WithArguments("tag", "-d", tag)
          .WithNoValidation()
          .CaptureAsync(cancellationToken)
          .ConfigureAwait(false);

        string cleanupNote = deleteResult.ExitCode == 0
          ? "local tag deleted."
          : $"WARNING: could not delete local tag {tag} — remove it manually with 'git tag -d {tag}' before retrying. ({deleteResult.Stderr.Trim()})";

        Terminal.WriteErrorLine($"Error: failed to push tag {tag} to origin — {cleanupNote}\n{pushResult.Stderr.Trim()}");
        Environment.ExitCode = 1;
        return;
      }

      Terminal.WriteLine($"Pushed tag {tag} to origin.");

      CommandOutput releaseCreateResult = await Shell.Builder("gh")
        .WithArguments([.. releaseCreateArguments])
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);

      if (releaseCreateResult.ExitCode != 0)
      {
        Terminal.WriteErrorLine($"Error: failed to create GitHub Release for {tag}.\n{releaseCreateResult.Stderr.Trim()}");
        Terminal.WriteErrorLine($"Recover with: {releaseCommand}");
        Terminal.WriteErrorLine("Re-running 'dev release' will refuse at the tag-availability guard by design — the tag now exists.");
        Environment.ExitCode = 1;
        return;
      }

      Terminal.WriteLine(releaseCreateResult.Stdout.Trim());
      Terminal.WriteLine($"✓ Released {tag}".Green());
    }

    /// <summary>
    /// Walks first-parent history back from <paramref name="headSha"/> (at most
    /// <see cref="ReleaseGuard.ReleaseTargetWalkLimit"/> commits) and returns
    /// <see cref="ReleaseGuard.SelectReleaseTarget"/>'s verdict over what it found. The walk
    /// stops at the first commit with a successful run or a path outside kanban/. Returns
    /// <see langword="null"/> after printing the error when git or gh fails.
    /// </summary>
    private async Task<ReleaseTargetVerdict?> LocateReleaseTargetAsync(string headSha, CancellationToken cancellationToken)
    {
      CommandOutput revListResult = await Shell.Builder("git")
        .WithArguments("rev-list", "--first-parent", $"--max-count={ReleaseGuard.ReleaseTargetWalkLimit}", headSha)
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);

      if (revListResult.ExitCode != 0)
      {
        Terminal.WriteErrorLine($"Error: git rev-list failed.\n{revListResult.Stderr.Trim()}");
        return null;
      }

      string[] shas = revListResult.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
      List<ReleaseTargetCommit> commits = [];

      foreach (string sha in shas)
      {
        CommandOutput runListResult = await Shell.Builder("gh")
          .WithArguments("run", "list", "--workflow", "workflow.yml", "--commit", sha, "--status", "success", "--json", "databaseId,event,headSha,createdAt")
          .WithNoValidation()
          .CaptureAsync(cancellationToken)
          .ConfigureAwait(false);

        if (runListResult.ExitCode != 0)
        {
          Terminal.WriteErrorLine($"Error: gh run list failed.\n{runListResult.Stderr.Trim()}");
          return null;
        }

        List<CiRunSummary>? runs = JsonSerializer.Deserialize(runListResult.Stdout, DevCliJsonContext.Default.ListCiRunSummary);
        IReadOnlyList<CiRunSummary> candidateRuns = CiRunPromotion.OrderCandidateRuns(runs ?? [], sha);

        if (candidateRuns.Count > 0)
        {
          commits.Add(new ReleaseTargetCommit(sha, [], candidateRuns[0].DatabaseId));
          break;
        }

        // --no-renames lists both sides of a rename, so a move from source/ into
        // kanban/ still shows the source/ path, whatever diff.renames is set to.
        CommandOutput diffResult = await Shell.Builder("git")
          .WithArguments("diff", "--name-only", "--no-renames", $"{sha}^1", sha)
          .WithNoValidation()
          .CaptureAsync(cancellationToken)
          .ConfigureAwait(false);

        if (diffResult.ExitCode != 0)
        {
          Terminal.WriteErrorLine($"Error: could not list the paths changed by {sha}.\n{diffResult.Stderr.Trim()}");
          return null;
        }

        string[] changedPaths = diffResult.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        commits.Add(new ReleaseTargetCommit(sha, changedPaths, null));

        if (ReleaseGuard.FindShippingPath(changedPaths) is not null)
        {
          break;
        }
      }

      return ReleaseGuard.SelectReleaseTarget(headSha, commits);
    }

    private static string ShortSha(string sha) => sha.Length > 7 ? sha[..7] : sha;
  }
}
