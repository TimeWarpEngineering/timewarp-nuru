#region Purpose
// Cuts a release from the props version: reads <Version> from
// source/Directory.Build.props, runs every release-guard (working tree,
// branch, sync with origin, tag availability, publish state, CI run
// existence), then creates the annotated tag and the GitHub Release
// (kanban task 458-006, parent 458 finding F7). Humans type the version
// exactly once — in the props bump PR; the tag is derived, never hand-typed.
#endregion
#region Design
// D1 guard order (fail-fast, precise messages — see release-guard.cs Design
// region for the rationale behind each individual verdict): props version
// readable -> gh available/authed -> tree clean -> on master -> synced with
// origin -> tag v{Version} absent local AND remote -> check-version 3-state
// (None proceed; All/Partial refuse) -> a successful CI run of workflow.yml
// exists at HEAD (else the release event would fail at locate-run).
//
// D2: pure per-guard classifiers live in release-guard.cs (GuardVerdict);
// this handler owns the git/gh process orchestration and prints a short
// progress line per passed guard. workflow-command.cs's private release-gate
// helpers (CheckTagPinAsync, LocateCiRunAsync, ...) are repo-local and not
// reused directly — the small shell sequences below are re-implemented here,
// while the shared pure logic (PublishStateClassifier, CiRunPromotion,
// DevCliJsonContext) IS reused.
//
// D3: annotated local tag at the verified sha -> push tag -> `gh release
// create v{X} --title v{X} --generate-notes --verify-tag`. Not `--target`:
// gh-created tags are lightweight and branch targets resolve server-side
// (a race against anything landing on master between guard-check and
// release-create) — pinning to the verified, already-pushed tag avoids both.
// A push failure deletes the local tag and refuses (nothing was made public
// yet, so it is safe to unwind). A release-create failure AFTER the tag was
// pushed does NOT unwind the tag — the tag is now public and re-running
// `dev release` would only refuse at the tag-availability guard, which is by
// design (the tag is the source of truth for "this version has moved past
// the two-independent-human-acts stage"); the operator instead gets the
// exact `gh release create` command to retry by hand.
//
// D4: --dry-run runs every guard (all read-only — even `git fetch origin
// master` only reads remote state into the local tracking ref) and then
// prints the version/tag/target-sha/guard checklist/exact commands instead
// of executing them. No notes-related options: --generate-notes is the
// house convention, not a per-run choice.
//
// D5: this closes the 458-005/458-002 untagged-double-break-glass residual
// for tooling-cut releases — the tag is created BEFORE anything is
// published, so any resume has a concrete, tag-pinned commit to match
// against (workflow-command.cs's tag-pin check). No endpoint-level test:
// every guard needs real git/gh process execution to exercise meaningfully;
// see release-guard.cs's pure matrix tests and this task's --dry-run
// verification runs instead.
#endregion

namespace DevCli;

using System.Text.Json;
using System.Xml.Linq;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

[NuruRoute("release", Description = "Cut a release: tag + GitHub Release from source/Directory.Build.props")]
public sealed class ReleaseCommand : ICommand<Unit>
{
  [Option("dry-run", Description = "Run every guard and print what would be created, without creating anything")]
  public bool DryRun { get; set; }

  public sealed class Handler : ICommandHandler<ReleaseCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private readonly NuGetVersionService NuGetVersionService;
    private readonly IRepoConfigService ConfigService;
    private readonly IPackableProjectService PackableProjectService;

    public Handler
    (
      ITerminal terminal,
      NuGetVersionService nuGetVersionService,
      IRepoConfigService configService,
      IPackableProjectService packableProjectService
    )
    {
      Terminal = terminal;
      NuGetVersionService = nuGetVersionService;
      ConfigService = configService;
      PackableProjectService = packableProjectService;
    }

    public async ValueTask<Unit> Handle(ReleaseCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);

      // --- Step 1: props version -----------------------------------------
      string? repoRoot = Git.FindRoot();
      string? version = GetVersionFromSource(repoRoot);

      if (version is null)
      {
        Terminal.WriteErrorLine("Error: could not read <Version> from source/Directory.Build.props");
        Environment.ExitCode = 1;
        return Value;
      }

      string tag = $"v{version}";

      // --- Step 2: gh available/authed ------------------------------------
      CommandOutput ghAuthResult;
      try
      {
        ghAuthResult = await Shell.Builder("gh")
          .WithArguments("auth", "status")
          .WithNoValidation()
          .CaptureAsync(cancellationToken)
          .ConfigureAwait(false);
      }
      catch (System.ComponentModel.Win32Exception)
      {
        Terminal.WriteErrorLine("Error: gh CLI not found — install gh, then run 'gh auth login'.");
        Environment.ExitCode = 1;
        return Value;
      }

      if (ghAuthResult.ExitCode != 0)
      {
        Terminal.WriteErrorLine($"Error: gh is not authenticated — run 'gh auth login'.\n{ghAuthResult.Stderr.Trim()}");
        Environment.ExitCode = 1;
        return Value;
      }

      Terminal.WriteLine("✓ gh authenticated".Green());

      // --- Step 3: working tree clean --------------------------------------
      CommandOutput statusResult = await Shell.Builder("git")
        .WithArguments("status", "--porcelain")
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);

      GuardVerdict treeVerdict = ReleaseGuard.CheckWorkingTree(statusResult.Stdout);
      if (!treeVerdict.Ok)
      {
        Terminal.WriteErrorLine($"Error: {treeVerdict.Reason}");
        Environment.ExitCode = 1;
        return Value;
      }

      Terminal.WriteLine("✓ working tree clean".Green());

      // --- Step 4: on master -------------------------------------------------
      CommandOutput branchResult = await Shell.Builder("git")
        .WithArguments("rev-parse", "--abbrev-ref", "HEAD")
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);

      string? currentBranch = branchResult.ExitCode == 0 ? branchResult.Stdout.Trim() : null;

      GuardVerdict branchVerdict = ReleaseGuard.CheckBranch(currentBranch);
      if (!branchVerdict.Ok)
      {
        Terminal.WriteErrorLine($"Error: {branchVerdict.Reason}");
        Environment.ExitCode = 1;
        return Value;
      }

      Terminal.WriteLine("✓ on master".Green());

      // --- Step 5: synced with origin/master, then headSha --------------------
      CommandOutput fetchResult = await Shell.Builder("git")
        .WithArguments("fetch", "origin", "master")
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);

      if (fetchResult.ExitCode != 0)
      {
        Terminal.WriteErrorLine($"Error: git fetch origin master failed.\n{fetchResult.Stderr.Trim()}");
        Environment.ExitCode = 1;
        return Value;
      }

      CommandOutput aheadResult = await Shell.Builder("git")
        .WithArguments("rev-list", "--count", "origin/master..HEAD")
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);

      CommandOutput behindResult = await Shell.Builder("git")
        .WithArguments("rev-list", "--count", "HEAD..origin/master")
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);

      if (aheadResult.ExitCode != 0 || behindResult.ExitCode != 0 ||
          !int.TryParse(aheadResult.Stdout.Trim(), out int aheadCount) ||
          !int.TryParse(behindResult.Stdout.Trim(), out int behindCount))
      {
        string detail = aheadResult.ExitCode != 0 ? aheadResult.Stderr.Trim() : behindResult.Stderr.Trim();
        Terminal.WriteErrorLine($"Error: could not determine sync status with origin/master.\n{detail}");
        Environment.ExitCode = 1;
        return Value;
      }

      GuardVerdict syncVerdict = ReleaseGuard.CheckSync(aheadCount, behindCount);
      if (!syncVerdict.Ok)
      {
        Terminal.WriteErrorLine($"Error: {syncVerdict.Reason}");
        Environment.ExitCode = 1;
        return Value;
      }

      Terminal.WriteLine("✓ in sync with origin/master".Green());

      CommandOutput headShaResult = await Shell.Builder("git")
        .WithArguments("rev-parse", "HEAD")
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);

      if (headShaResult.ExitCode != 0)
      {
        Terminal.WriteErrorLine($"Error: could not resolve HEAD commit.\n{headShaResult.Stderr.Trim()}");
        Environment.ExitCode = 1;
        return Value;
      }

      string headSha = headShaResult.Stdout.Trim();

      // --- Step 6: tag v{Version} absent local AND remote --------------------
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
        Environment.ExitCode = 1;
        return Value;
      }

      CommandOutput remoteTagResult = await Shell.Builder("git")
        .WithArguments("ls-remote", "--tags", "origin", $"refs/tags/{tag}")
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);

      if (remoteTagResult.ExitCode != 0)
      {
        Terminal.WriteErrorLine($"Error: could not check for remote tag {tag}.\n{remoteTagResult.Stderr.Trim()}");
        Environment.ExitCode = 1;
        return Value;
      }

      bool existsOnRemote = !string.IsNullOrWhiteSpace(remoteTagResult.Stdout);

      GuardVerdict tagVerdict = ReleaseGuard.CheckTagAvailability(tag, existsLocally, existsOnRemote);
      if (!tagVerdict.Ok)
      {
        Terminal.WriteErrorLine($"Error: {tagVerdict.Reason}");
        Environment.ExitCode = 1;
        return Value;
      }

      Terminal.WriteLine($"✓ tag {tag} available".Green());

      // --- Step 7: check-version 3-state gate ---------------------------------
      RepoConfig config = await ConfigService.GetConfigAsync(cancellationToken).ConfigureAwait(false);
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
        Environment.ExitCode = 1;
        return Value;
      }

      List<string> alreadyPublished = [];
      foreach (string pkg in packages)
      {
        IReadOnlyList<string> versions = await NuGetVersionService
          .GetPackageVersionsAsync(pkg, cancellationToken)
          .ConfigureAwait(false);

        if (NuGetVersionService.IsVersionPublished(version, versions))
        {
          alreadyPublished.Add(pkg);
        }
      }

      PublishState publishState = PublishStateClassifier.Classify(packages.Count, alreadyPublished.Count);

      GuardVerdict publishVerdict = ReleaseGuard.CheckPublishState(publishState, version);
      if (!publishVerdict.Ok)
      {
        Terminal.WriteErrorLine($"Error: {publishVerdict.Reason}");
        Environment.ExitCode = 1;
        return Value;
      }

      Terminal.WriteLine($"✓ version {version} not yet published".Green());

      // --- Step 8: successful CI run of workflow.yml exists at HEAD ----------
      CommandOutput runListResult = await Shell.Builder("gh")
        .WithArguments("run", "list", "--workflow", "workflow.yml", "--commit", headSha, "--status", "success", "--json", "databaseId,event,headSha,createdAt")
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);

      if (runListResult.ExitCode != 0)
      {
        Terminal.WriteErrorLine($"Error: gh run list failed.\n{runListResult.Stderr.Trim()}");
        Environment.ExitCode = 1;
        return Value;
      }

      List<CiRunSummary>? runs = JsonSerializer.Deserialize(runListResult.Stdout, DevCliJsonContext.Default.ListCiRunSummary);
      IReadOnlyList<CiRunSummary> candidateRuns = CiRunPromotion.OrderCandidateRuns(runs ?? [], headSha);

      if (candidateRuns.Count == 0)
      {
        Terminal.WriteErrorLine($"Error: no successful CI run of workflow.yml exists for {headSha}. The Release event would fail at locate-run — wait for CI on this commit or rerun it (gh run rerun <id>).");
        Environment.ExitCode = 1;
        return Value;
      }

      Terminal.WriteLine($"✓ successful CI run found ({candidateRuns[0].DatabaseId})".Green());

      // --- Step 9: --dry-run stops here ---------------------------------------
      string tagCommand = $"git tag -a {tag} -m \"Release {tag}\" {headSha}";
      string pushCommand = $"git push origin {tag}";
      string releaseCommand = $"gh release create {tag} --title {tag} --generate-notes --verify-tag";

      if (command.DryRun)
      {
        Terminal.WriteLine("");
        Terminal.WriteLine("Dry run — no changes made.".Cyan());
        Terminal.WriteLine($"Version: {version}");
        Terminal.WriteLine($"Tag: {tag}");
        Terminal.WriteLine($"Target commit: {ShortSha(headSha)} ({headSha})");
        Terminal.WriteLine("All guards passed.");
        Terminal.WriteLine("Would run:");
        Terminal.WriteLine($"  {tagCommand}");
        Terminal.WriteLine($"  {pushCommand}");
        Terminal.WriteLine($"  {releaseCommand}");
        return Value;
      }

      // --- Step 10: execute — tag, push, gh release create --------------------
      CommandOutput tagCreateResult = await Shell.Builder("git")
        .WithArguments("tag", "-a", tag, "-m", $"Release {tag}", headSha)
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);

      if (tagCreateResult.ExitCode != 0)
      {
        Terminal.WriteErrorLine($"Error: failed to create tag {tag}.\n{tagCreateResult.Stderr.Trim()}");
        Environment.ExitCode = 1;
        return Value;
      }

      Terminal.WriteLine($"Created tag {tag} at {ShortSha(headSha)}.");

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
        return Value;
      }

      Terminal.WriteLine($"Pushed tag {tag} to origin.");

      CommandOutput releaseCreateResult = await Shell.Builder("gh")
        .WithArguments("release", "create", tag, "--title", tag, "--generate-notes", "--verify-tag")
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);

      if (releaseCreateResult.ExitCode != 0)
      {
        Terminal.WriteErrorLine($"Error: failed to create GitHub Release for {tag}.\n{releaseCreateResult.Stderr.Trim()}");
        Terminal.WriteErrorLine($"Recover with: {releaseCommand}");
        Terminal.WriteErrorLine("Re-running 'dev release' will refuse at the tag-availability guard by design — the tag now exists.");
        Environment.ExitCode = 1;
        return Value;
      }

      Terminal.WriteLine(releaseCreateResult.Stdout.Trim());
      Terminal.WriteLine($"✓ Released {tag}".Green());

      return Value;
    }

    private static string ShortSha(string sha) => sha.Length > 7 ? sha[..7] : sha;

    private static string? GetVersionFromSource(string? repoRoot)
    {
      if (repoRoot is null)
      {
        return null;
      }

      string sourceDir = Path.Combine(repoRoot, "source");
      if (!Directory.Exists(sourceDir))
      {
        return null;
      }

      string[] buildPropsFiles = Directory.GetFiles(sourceDir, "Directory.Build.props", SearchOption.TopDirectoryOnly);
      if (buildPropsFiles is not { Length: > 0 })
      {
        return null;
      }

      string xml = File.ReadAllText(buildPropsFiles[0]);
#pragma warning disable IDE0007
      XDocument doc = XDocument.Parse(xml);
#pragma warning restore IDE0007
      XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";

      XElement? versionElement = doc.Descendants(ns + "Version").FirstOrDefault();
      return (versionElement ?? doc.Descendants("Version").FirstOrDefault())?.Value.Trim();
    }
  }
}
