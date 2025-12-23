namespace AttributedRoutes.Messages;

using TimeWarp.Nuru;
using Mediator;
using TimeWarp.Terminal;

/// <summary>
/// Deploy to an environment.
/// This is a Command (C) - mutating, needs confirmation before running.
/// </summary>
[NuruRoute("deploy", Description = "Deploy to an environment")]
public sealed class DeployCommand : ICommand<Unit>
{
  [Parameter(Description = "Target environment (dev, staging, prod)")]
  public string Env { get; set; } = string.Empty;

  [Option("force", "f", Description = "Skip confirmation prompt")]
  public bool Force { get; set; }

  [Option("config", "c", Description = "Path to config file")]
  public string? ConfigFile { get; set; }

  [Option("replicas", "r", Description = "Number of replicas")]
  public int Replicas { get; set; } = 1;

  public sealed class Handler : ICommandHandler<DeployCommand, Unit>
  {
    public ValueTask<Unit> Handle(DeployCommand command, CancellationToken ct)
    {
      WriteLine($"Deploying to {command.Env}...");
      WriteLine($"  Force: {command.Force}");
      WriteLine($"  Config: {command.ConfigFile ?? "(default)"}");
      WriteLine($"  Replicas: {command.Replicas}");
      return default;
    }
  }
}
