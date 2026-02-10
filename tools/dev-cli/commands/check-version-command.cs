// ═══════════════════════════════════════════════════════════════════════════════
// CHECK VERSION COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Verifies that NuGet packages are not already published before release.
// Migrated from runfiles/check-version.cs to endpoints pattern.

namespace DevCli.Commands;

/// <summary>
/// Check if NuGet packages are already published.
/// </summary>
[NuruRoute("check-version", Description = "Check if packages are already published on NuGet")]
internal sealed class CheckVersionCommand : ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<CheckVersionCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(CheckVersionCommand command, CancellationToken ct)
    {
      // Get repo root using Git.FindRoot
      string? repoRoot = Git.FindRoot();

      if (repoRoot is null)
      {
        throw new InvalidOperationException("Could not find git repository root (.git not found)");
      }

      // Verify we're in the right place
      if (!File.Exists(Path.Combine(repoRoot, "timewarp-nuru.slnx")))
      {
        throw new InvalidOperationException("Could not find repository root (timewarp-nuru.slnx not found)");
      }

      // Read version from source/Directory.Build.props
      string propsPath = Path.Combine(repoRoot, "source", "Directory.Build.props");

      if (!File.Exists(propsPath))
      {
        throw new FileNotFoundException($"Could not find {propsPath}");
      }

      XDocument doc = XDocument.Load(propsPath);
      string? version = doc.Descendants("Version").FirstOrDefault()?.Value;

      if (string.IsNullOrEmpty(version))
      {
        throw new InvalidOperationException("Could not find version in source/Directory.Build.props");
      }

      Terminal.WriteLine($"Checking if packages with version {version} are already published on NuGet.org...");

      // Packages to check (dependency order)
      string[] packages =
      [
        "TimeWarp.Nuru.Analyzers",
        "TimeWarp.Nuru.Mcp",
        "TimeWarp.Nuru"
      ];

      List<string> alreadyPublished = [];

      foreach (string package in packages)
      {
        Terminal.WriteLine($"\nChecking {package}...");

        CommandOutput result = await DotNet.PackageSearch(package)
          .WithExactMatch()
          .WithPrerelease()
          .WithSource("https://api.nuget.org/v3/index.json")
          .Build()
          .CaptureAsync();

        // Check if the version appears in the output
        if (result.Stdout.Contains($"| {version} |", StringComparison.Ordinal))
        {
          Terminal.WriteLine($"  WARNING: {package} {version} is already published to NuGet.org");
          alreadyPublished.Add(package);
        }
        else
        {
          Terminal.WriteLine($"  OK: {package} {version} is not yet published");
        }
      }

      if (alreadyPublished.Count > 0)
      {
        throw new InvalidOperationException(
          $"Package(s) already published: {string.Join(", ", alreadyPublished)}. " +
          "Please increment the version in source/Directory.Build.props");
      }

      Terminal.WriteLine("\nAll packages are ready to publish!");
      return Unit.Value;
    }
  }
}
