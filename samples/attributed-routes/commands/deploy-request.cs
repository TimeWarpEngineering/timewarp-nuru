namespace AttributedRoutes.Commands;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Deploy request with a required parameter and options.
/// This is a Command (C) - mutating, needs confirmation before running.
/// </summary>
[NuruRoute("deploy", Description = "Deploy to an environment")]
public sealed class DeployRequest : ICommand<Unit>
{
  [Parameter(Description = "Target environment (dev, staging, prod)")]
  public string Env { get; set; } = string.Empty;

  [Option("force", "f", Description = "Skip confirmation prompt")]
  public bool Force { get; set; }

  [Option("config", "c", Description = "Path to config file")]
  public string? ConfigFile { get; set; }

  [Option("replicas", "r", Description = "Number of replicas")]
  public int Replicas { get; set; } = 1;

  public sealed class Handler : ICommandHandler<DeployRequest, Unit>
  {
    public ValueTask<Unit> Handle(DeployRequest request, CancellationToken ct)
    {
      WriteLine($"Deploying to {request.Env}...");
      WriteLine($"  Force: {request.Force}");
      WriteLine($"  Config: {request.ConfigFile ?? "(default)"}");
      WriteLine($"  Replicas: {request.Replicas}");
      return default;
    }
  }
}
