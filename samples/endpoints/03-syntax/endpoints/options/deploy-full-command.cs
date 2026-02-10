// ═══════════════════════════════════════════════════════════════════════════════
// DEPLOY-FULL COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Deploy with environment, dry-run, and force options.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

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
