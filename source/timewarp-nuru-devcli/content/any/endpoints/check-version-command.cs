#region Purpose
// Verify that the version in Directory.Build.props has not already been released
#endregion
#region Design
// One methodology: props-version membership in the published NuGet versions
// (NuGetVersionService, HttpClient-based — no NuGet.Protocol dependency).
// The props version comes from the shared PropsVersionReader (trimmed, same
// reader as release-command) and a NuGet lookup that fails for any reason
// other than 404 aborts the gate with exit code 1 rather than being counted
// as "not published" (kanban task 470-007, findings M9/M10/M31).
// Package set precedence (kanban task 458-004): --package overrides everything
// for a single ad-hoc run; else .timewarp/dev.jsonc's checkVersionConfig.packages
// (an explicit, repo-level override); else the derived set of packable projects
// under source/ (IPackableProjectService, IsPackable=true via MSBuild evaluation)
// — so a repo with no override needs zero hand-maintained package lists. A
// --package value that parses to zero packages (e.g. ",") is still an explicit
// override and does NOT fall through to derivation — it hits the same "no
// packages" error as an empty/unconfigured repo.
//
// After the two version lines, PrereleaseDistance reports how many prerelease
// increments separate source from the latest published version when that gap
// is honest (same core, same label, only the number differs). A gap greater
// than 1 warns that intermediate numbers were bumped and never published, and
// exits 0 — a deliberate jump is legitimate. --strict turns that warning into
// exit code 1 for CI. Every other shape (core change, different label, no
// published version) keeps the two version lines and omits the distance.
//
// Both org feeds (kanban task 536): each package is read from nuget.org and
// the org GitHub Packages feed (NuGetVersionService.LookupAsync), and a
// version on either is published. "Latest NuGet version" is the highest
// version on either feed. A version that is not on nuget.org while GitHub
// Packages could not be read refuses with its own message (feed, HTTP status,
// fix), never "already released". The handler records Outcome and
// GitHubPackagesFailure so a pipeline running the gate can name the cause
// (CheckVersionAbort.FormatAbortReason).
#endregion

namespace DevCli;

using TimeWarp.Mediator;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

[NuruRoute("check-version", Description = "Verify version is ready to release")]
[NuruRouteExample("check-version --strict", Description = "Fail when source is more than one prerelease increment ahead of the latest published version")]
public sealed class CheckVersionCommand : ICommand<Unit>
{
  [Option("package", Description = "NuGet package ID to check (comma-separated)")]
  public string? Package { get; set; }

  [Option("strict", Description = "Exit non-zero when source is more than one prerelease increment ahead of the latest published version")]
  public bool Strict { get; set; }

  /// <summary>
  /// Test seam, not a CLI option: when set, used instead of the Version read from
  /// source/Directory.Build.props so tests can pin a prerelease source version.
  /// </summary>
  public string? SourceVersion { get; set; }

  public sealed class Handler : ICommandHandler<CheckVersionCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private readonly NuGetVersionService NuGetVersionService;
    private readonly IRepoConfigService ConfigService;
    private readonly IPackableProjectService PackableProjectService;

    /// <summary>How the last <see cref="Handle"/> call ended.</summary>
    public CheckVersionOutcome Outcome { get; private set; }

    /// <summary>The GitHub Packages failure behind <see cref="CheckVersionOutcome.GitHubPackagesUnreadable"/>.</summary>
    public PackageFeedUnreadableException? GitHubPackagesFailure { get; private set; }

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

    public async Task<Unit> Handle(CheckVersionCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);

      Outcome = CheckVersionOutcome.InvalidInput;
      GitHubPackagesFailure = null;

      string? repoRoot = Git.FindRoot();

      RepoConfig config = await ConfigService
        .GetConfigAsync(cancellationToken)
        .ConfigureAwait(false);

      bool usingConfigOverride = command.Package is null && config.CheckVersionConfig?.Packages is not null;
      string? packageInput = command.Package ?? config.CheckVersionConfig?.Packages;
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
        Terminal.WriteErrorLine("Error: no packable projects found under source/ and no packages configured. Use --package or checkVersionConfig.packages.");
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      // Round-1 review finding #2: a repo hand-maintaining checkVersionConfig
      // .packages otherwise gets zero signal that derivation exists, so it
      // never learns to stop. Only for the config-override path — --package
      // is an intentional single-run override and needs no nudge.
      if (usingConfigOverride)
      {
        Terminal.WriteLine("Using configured package override; delete checkVersionConfig.packages to derive the set from IsPackable.");
      }

      // M31 (470-007): --package is user input that becomes a URL path segment;
      // reject anything outside NuGet id grammar before any HTTP call.
      List<string> invalidPackages = [.. packages.Where(p => !NuGetVersionService.IsValidPackageId(p))];
      if (invalidPackages.Count > 0)
      {
        Terminal.WriteErrorLine($"Error: invalid NuGet package id(s): {string.Join(", ", invalidPackages)}");
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      string? version = command.SourceVersion ?? PropsVersionReader.Read(repoRoot);
      if (version is null)
      {
        Terminal.WriteErrorLine("Error: could not read Version from source/Directory.Build.props");
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      List<string> checkedPackages = [];
      List<string> alreadyPublished = [];
      string? latestNuGetVersion = null;

      foreach (string pkg in packages)
      {
        checkedPackages.Add(pkg);

        PackageFeedLookup lookup;
        try
        {
          lookup = await NuGetVersionService
            .LookupAsync(pkg, cancellationToken)
            .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
          // M9 (470-007): fail closed. An empty list means "never published"
          // downstream, so a 429/5xx/network failure must NOT be reported as
          // "safe to release" — refuse to answer instead.
          Terminal.WriteErrorLine($"Error: NuGet lookup for '{pkg}' failed: {ex.Message}");
          Terminal.WriteErrorLine("  Cannot determine whether this version is already published; refusing to report it as safe to release. Retry when NuGet is reachable.");
          Outcome = CheckVersionOutcome.NuGetOrgLookupFailed;
          Environment.ExitCode = 1;
          return Unit.Value;
        }

        PackagePublishVerdict verdict = lookup.Check(version);

        if (verdict == PackagePublishVerdict.Unknown)
        {
          // 536: not on nuget.org and GitHub Packages unreadable. Unknown, so
          // refuse; never "not published", and never "already released".
          foreach (string line in lookup.FormatUnknownRefusal(version))
          {
            Terminal.WriteErrorLine(line);
          }

          Outcome = CheckVersionOutcome.GitHubPackagesUnreadable;
          GitHubPackagesFailure = lookup.GitHubPackagesFailure;
          Environment.ExitCode = 1;
          return Unit.Value;
        }

        string? highestVersion = lookup.LatestVersion;

        if (highestVersion is not null &&
            (latestNuGetVersion is null || NuGetVersionService.CompareVersions(highestVersion, latestNuGetVersion) > 0))
        {
          latestNuGetVersion = highestVersion;
        }

        if (verdict == PackagePublishVerdict.Published)
        {
          alreadyPublished.Add(pkg);
        }
      }

      Terminal.WriteLine($"Version in source: {version}".Cyan());
      string latestDisplay = latestNuGetVersion ?? "(none)";
      Terminal.WriteLine($"Latest NuGet version: {latestDisplay}".Cyan());

      int? prereleaseIncrements = PrereleaseDistance.TryGetIncrements(version, latestNuGetVersion);
      if (prereleaseIncrements is int increments && latestNuGetVersion is not null)
      {
        Terminal.WriteLine(PrereleaseDistance.FormatDistanceLine(increments, latestNuGetVersion));
        string? skippedReleaseWarning = PrereleaseDistance.FormatSkippedReleaseWarning(increments);
        if (skippedReleaseWarning is not null)
        {
          Terminal.WriteLine(skippedReleaseWarning.Yellow());
        }
      }

      if (checkedPackages.Count > 0)
      {
        Terminal.WriteLine($"Packages checked: {string.Join(", ", checkedPackages)}");
      }

      Terminal.WriteLine("");

      PublishState publishState = PublishStateClassifier.Classify(checkedPackages.Count, alreadyPublished.Count);

      switch (publishState)
      {
        case PublishState.None:
          Outcome = CheckVersionOutcome.NewVersion;
          Terminal.WriteLine("✓ Version in source is new — safe to release.".Green());
          break;

        case PublishState.Partial:
          Outcome = CheckVersionOutcome.PartiallyPublished;
          List<string> missingPackages = [.. checkedPackages.Except(alreadyPublished)];
          Terminal.WriteLine($"Partial publish detected: {alreadyPublished.Count} of {checkedPackages.Count} packages already have {version}.".Yellow());
          Terminal.WriteLine($"  Already published: {string.Join(", ", alreadyPublished)}".Yellow());
          Terminal.WriteLine($"  Missing: {string.Join(", ", missingPackages)}".Yellow());
          Terminal.WriteLine("  This run will resume the push (--skip-duplicate no-ops the already-published packages).".Yellow());
          Terminal.WriteLine("  Safe only if this is the SAME commit as the earlier partial push — the release gate's tag-pin check enforces that. Bump the version instead if the source has changed.".Yellow());
          break;

        case PublishState.All:
          Outcome = CheckVersionOutcome.AlreadyReleased;
          Terminal.WriteLine($"✗ Version {version} was already released.".Red());
          Terminal.WriteLine("  Bump the version before releasing.".Yellow());
          Terminal.WriteLine($"  Already published: {string.Join(", ", alreadyPublished)}".Yellow());
          Environment.ExitCode = 1;
          break;

        default:
          throw new InvalidOperationException($"Unknown publish state '{publishState}'.");
      }

      if (command.Strict && PrereleaseDistance.IsStrictFailure(prereleaseIncrements))
      {
        Environment.ExitCode = 1;
      }

      return Unit.Value;
    }
  }
}
