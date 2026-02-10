using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("deploy", Description = "Deploy application to environment")]
public sealed class DeployCommand : ICommand<Unit>
{
  [Parameter(Description = "Target environment")]
  public string Env { get; set; } = "";

  [Option("version", "v", Description = "Version to deploy")]
  public string? Version { get; set; }

  [Option("force", "f", Description = "Force deployment")]
  public bool Force { get; set; }

  public sealed class Handler : ICommandHandler<DeployCommand, Unit>
  {
    public ValueTask<Unit> Handle(DeployCommand c, CancellationToken ct)
    {
      WriteLine($"Deploying to {c.Env}");
      if (c.Version != null)
        WriteLine($"  Version: {c.Version}");
      WriteLine($"  Force: {c.Force}");
      return default;
    }
  }
}
