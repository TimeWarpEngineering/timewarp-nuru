namespace GroupOptionsSample.Endpoints;

using TimeWarp.Nuru;

/// <summary>
/// Base class for all git commands with shared group options.
/// All commands inheriting from this base automatically get:
///   --verbose, -v    : Enable verbose output
///   --dry-run, -d    : Show what would be done without making changes
///   --config {key}   : Read configuration value
/// </summary>
[NuruRouteGroup("git", Description = "Git version control commands")]
public abstract class GitGroupBase
{
  [GroupOption("verbose", "v", Description = "Enable verbose output")]
  public bool Verbose { get; set; }

  [GroupOption("dry-run", "d", Description = "Show what would be done without making changes")]
  public bool DryRun { get; set; }

  [GroupOption("config", Description = "Configuration key to display")]
  public string? Config { get; set; }
}
