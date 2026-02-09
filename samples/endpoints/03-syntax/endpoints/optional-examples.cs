// ═══════════════════════════════════════════════════════════════════════════════
// OPTIONAL PARAMETER EXAMPLES
// ═══════════════════════════════════════════════════════════════════════════════
// Make parameters optional with nullable types.
// NOTE: The IsOptional property on [Parameter] is no longer needed - use nullable types.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

/// <summary>
/// Deploy to an environment with optional tag parameter.
/// </summary>
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

/// <summary>
/// Wait for specified seconds (optional, defaults to 5).
/// </summary>
[NuruRoute("wait", Description = "Wait for specified seconds")]
public sealed class WaitCommand : ICommand<Unit>
{
  [Parameter(Description = "Seconds to wait (optional)")]
  public int? Seconds { get; set; }

  public sealed class Handler : ICommandHandler<WaitCommand, Unit>
  {
    public async ValueTask<Unit> Handle(WaitCommand command, CancellationToken ct)
    {
      int seconds = command.Seconds ?? 5;
      Console.WriteLine($"Waiting {seconds} seconds");
      await Task.Delay(seconds * 1000, ct);
      return default;
    }
  }
}

/// <summary>
/// Backup a source directory with optional destination.
/// </summary>
[NuruRoute("backup", Description = "Backup a source directory")]
public sealed class BackupCommand : ICommand<Unit>
{
  [Parameter(Description = "Source directory to backup")]
  public string Source { get; set; } = string.Empty;

  [Parameter(Description = "Optional destination path")]
  public string? Destination { get; set; }

  public sealed class Handler : ICommandHandler<BackupCommand, Unit>
  {
    public ValueTask<Unit> Handle(BackupCommand command, CancellationToken ct)
    {
      string dest = command.Destination ?? "default location";
      Console.WriteLine($"Backing up {command.Source} to {dest}");
      return default;
    }
  }
}
