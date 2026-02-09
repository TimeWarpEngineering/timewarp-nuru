// ═══════════════════════════════════════════════════════════════════════════════
// OPTION EXAMPLES
// ═══════════════════════════════════════════════════════════════════════════════
// Boolean flags and value options using [Option].
// NOTE: For repeatable options, use array types without IsRepeatable property.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

/// <summary>
/// Build a project with optional configuration.
/// </summary>
[NuruRoute("build", Description = "Build a project")]
public sealed class BuildCommand : ICommand<Unit>
{
  [Parameter(Description = "Project to build")]
  public string Project { get; set; } = string.Empty;

  [Option("mode", "m", Description = "Build mode (Debug or Release)")]
  public string Mode { get; set; } = "Debug";

  [Option("verbose", "v", Description = "Verbose output")]
  public bool Verbose { get; set; }

  public sealed class Handler : ICommandHandler<BuildCommand, Unit>
  {
    public ValueTask<Unit> Handle(BuildCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Building {command.Project} ({command.Mode}, verbose: {command.Verbose})");
      return default;
    }
  }
}

/// <summary>
/// Deploy with environment, dry-run, and force options.
/// </summary>
[NuruRoute("deploy-full", Description = "Deploy with full options")]
public sealed class DeployFullCommand : ICommand<Unit>
{
  [Parameter(Description = "Target environment")]
  public string Env { get; set; } = string.Empty;

  [Option("dry-run", "d", Description = "Preview changes without applying")]
  public bool DryRun { get; set; }

  [Option("force", "f", Description = "Force deployment without confirmation")]
  public bool Force { get; set; }

  public sealed class Handler : ICommandHandler<DeployFullCommand, Unit>
  {
    public ValueTask<Unit> Handle(DeployFullCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Deploy to {command.Env} (dry-run: {command.DryRun}, force: {command.Force})");
      return default;
    }
  }
}

/// <summary>
/// Docker command with environment variables.
//  For repeatable options, use array types without IsRepeatable property.
/// </summary>
[NuruRoute("docker-env", Description = "Docker command with environment variables")]
public sealed class DockerEnvCommand : ICommand<Unit>
{
  [Option("env", "e", Description = "Environment variables")]
  public string[] Var { get; set; } = [];

  public sealed class Handler : ICommandHandler<DockerEnvCommand, Unit>
  {
    public ValueTask<Unit> Handle(DockerEnvCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Environment variables: {string.Join(", ", command.Var)}");
      return default;
    }
  }
}
