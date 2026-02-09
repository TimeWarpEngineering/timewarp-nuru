// ═══════════════════════════════════════════════════════════════════════════════
// DEPLOY COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Deploy to an environment with optional tag parameter.
// NOTE: Use nullable types (string?) for optional parameters, not IsOptional=true.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

[NuruRoute("deploy", Description = "Deploy to an environment")]
public sealed class DeployCommand : ICommand<Unit>
{
  [Parameter(Description = "Target environment")]
  public string Env { get; set; } = string.Empty;

  [Parameter(Description = "Optional tag to deploy")]
  public string? Tag { get; set; }

  public sealed class Handler : ICommandHandler<DeployCommand, Unit>
  {
    public ValueTask<Unit> Handle(DeployCommand command, CancellationToken ct)
    {
      string message = $"Deploying to {command.Env}";
      if (!string.IsNullOrEmpty(command.Tag))
      {
        message += $" with tag {command.Tag}";
      }
      Console.WriteLine(message);
      return default;
    }
  }
}
