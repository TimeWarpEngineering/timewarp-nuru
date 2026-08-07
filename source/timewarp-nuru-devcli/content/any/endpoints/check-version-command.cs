#region Purpose
// Verify that the version in Directory.Build.props has not already been released
#endregion
#region Design
// One methodology: props-version membership in the published NuGet versions
// (NuGetVersionService, HttpClient-based — no NuGet.Protocol dependency).
// Per-repo config (.timewarp/dev.jsonc) supplies the package list via
// IRepoConfigService; --package overrides it for a single ad-hoc run.
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

    public Handler
    (
      ITerminal terminal,
      NuGetVersionService nuGetVersionService,
      IRepoConfigService configService
    )
    {
      Terminal = terminal;
      NuGetVersionService = nuGetVersionService;
      ConfigService = configService;
    }

    public async ValueTask<Unit> Handle(CheckVersionCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);

      RepoConfig config = await ConfigService
        .GetConfigAsync(cancellationToken)
        .ConfigureAwait(false);

      string? packageInput = command.Package ?? config.CheckVersionConfig?.Packages;
      if (string.IsNullOrWhiteSpace(packageInput))
      {
        Terminal.WriteErrorLine("Error: no packages specified. Use --package or configure Packages in .timewarp/dev.jsonc");
        Environment.ExitCode = 1;
        return Value;
      }

      IReadOnlyList<string> packages = packageInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

      string? version = GetVersionFromSource();
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

      bool isNewVersion = alreadyPublished.Count == 0;

      if (isNewVersion)
      {
        Terminal.WriteLine("✓ Version in source is new — safe to release.".Green());
      }
      else
      {
        Terminal.WriteLine($"✗ Version {version} was already released.".Red());
        Terminal.WriteLine("  Bump the version before releasing.".Yellow());

        if (alreadyPublished.Count > 0)
        {
          Terminal.WriteLine($"  Already published: {string.Join(", ", alreadyPublished)}".Yellow());
        }

        Environment.ExitCode = 1;
      }

      return Value;
    }

    private static string? GetVersionFromSource()
    {
      string? repoRoot = Git.FindRoot();
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
