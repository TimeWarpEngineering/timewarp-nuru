// ═══════════════════════════════════════════════════════════════════════════════
// BACKUP COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Backup a source directory with optional destination.
// NOTE: Use nullable types (string?) for optional parameters, not IsOptional=true.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

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
