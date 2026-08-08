#region Purpose
// Verify that the version in Directory.Build.props has not already been released
#endregion
#region Design
// One methodology: props-version membership in the published NuGet versions
// (NuGetVersionService, HttpClient-based — no NuGet.Protocol dependency).
// Package set precedence (kanban task 458-004): --package overrides everything
// for a single ad-hoc run; else .timewarp/dev.jsonc's checkVersionConfig.packages
// (an explicit, repo-level override); else the derived set of packable projects
// under source/ (IPackableProjectService, IsPackable=true via MSBuild evaluation)
// — so a repo with no override needs zero hand-maintained package lists. A
// --package value that parses to zero packages (e.g. ",") is still an explicit
// override and does NOT fall through to derivation — it hits the same "no
// packages" error as an empty/unconfigured repo.
#endregion

namespace DevCli;

using System.Xml.Linq;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

[NuruRoute("check-version", Description = "Verify version is ready to release")]
public sealed class CheckVersionCommand : ICommand<Unit>
{
  [Option("package", Description = "NuGet package ID to check (comma-separated)")]
  public string? Package { get; set; }

  public sealed class Handler : ICommandHandler<CheckVersionCommand, Unit>
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

    public async ValueTask<Unit> Handle(CheckVersionCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);

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
        return Value;
      }

      // Round-1 review finding #2: a repo hand-maintaining checkVersionConfig
      // .packages otherwise gets zero signal that derivation exists, so it
      // never learns to stop. Only for the config-override path — --package
      // is an intentional single-run override and needs no nudge.
      if (usingConfigOverride)
      {
        Terminal.WriteLine("Using configured package override; delete checkVersionConfig.packages to derive the set from IsPackable.");
      }

      string? version = GetVersionFromSource(repoRoot);
      if (version is null)
      {
        Terminal.WriteErrorLine("Error: could not read Version from source/Directory.Build.props");
        Environment.ExitCode = 1;
        return Value;
      }

      List<string> checkedPackages = [];
      List<string> alreadyPublished = [];
      string? latestNuGetVersion = null;

      foreach (string pkg in packages)
      {
        checkedPackages.Add(pkg);

        IReadOnlyList<string> versions = await NuGetVersionService
          .GetPackageVersionsAsync(pkg, cancellationToken)
          .ConfigureAwait(false);

        if (versions.Count == 0)
        {
          continue;
        }

        string highestVersion = versions[^1];

        if (latestNuGetVersion is null || NuGetVersionService.CompareVersions(highestVersion, latestNuGetVersion) > 0)
        {
          latestNuGetVersion = highestVersion;
        }

        if (NuGetVersionService.IsVersionPublished(version, versions))
        {
          alreadyPublished.Add(pkg);
        }
      }

      Terminal.WriteLine($"Version in source: {version}".Cyan());
      string latestDisplay = latestNuGetVersion ?? "(none)";
      Terminal.WriteLine($"Latest NuGet version: {latestDisplay}".Cyan());

      if (checkedPackages.Count > 0)
      {
        Terminal.WriteLine($"Packages checked: {string.Join(", ", checkedPackages)}");
      }

      Terminal.WriteLine("");

      PublishState publishState = PublishStateClassifier.Classify(checkedPackages.Count, alreadyPublished.Count);

      switch (publishState)
      {
        case PublishState.None:
          Terminal.WriteLine("✓ Version in source is new — safe to release.".Green());
          break;

        case PublishState.Partial:
          List<string> missingPackages = [.. checkedPackages.Except(alreadyPublished)];
          Terminal.WriteLine($"Partial publish detected: {alreadyPublished.Count} of {checkedPackages.Count} packages already have {version}.".Yellow());
          Terminal.WriteLine($"  Already published: {string.Join(", ", alreadyPublished)}".Yellow());
          Terminal.WriteLine($"  Missing: {string.Join(", ", missingPackages)}".Yellow());
          Terminal.WriteLine("  This run will resume the push (--skip-duplicate no-ops the already-published packages).".Yellow());
          Terminal.WriteLine("  Safe only if this is the SAME commit as the earlier partial push — the release gate's tag-pin check enforces that. Bump the version instead if the source has changed.".Yellow());
          break;

        case PublishState.All:
          Terminal.WriteLine($"✗ Version {version} was already released.".Red());
          Terminal.WriteLine("  Bump the version before releasing.".Yellow());
          Terminal.WriteLine($"  Already published: {string.Join(", ", alreadyPublished)}".Yellow());
          Environment.ExitCode = 1;
          break;

        default:
          throw new InvalidOperationException($"Unknown publish state '{publishState}'.");
      }

      return Value;
    }

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
      return (versionElement ?? doc.Descendants("Version").FirstOrDefault())?.Value;
    }
  }
}
