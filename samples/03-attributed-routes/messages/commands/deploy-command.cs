namespace AttributedRoutes.Messages;

using TimeWarp.Nuru;
using TimeWarp.Terminal;

/// <summary>
/// Deploy to an environment.
/// This is a Command (C) - mutating, needs confirmation before running.
/// Demonstrates ITerminal injection for testable output.
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
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public ValueTask<Unit> Handle(DeployCommand command, CancellationToken ct)
    {
      Terminal.WriteLine($"Deploying to {command.Env}...");
      Terminal.WriteLine($"  Force: {command.Force}");
      Terminal.WriteLine($"  Config: {command.ConfigFile ?? "(default)"}");
      Terminal.WriteLine($"  Replicas: {command.Replicas}");
      return default;
    }
  }
}
