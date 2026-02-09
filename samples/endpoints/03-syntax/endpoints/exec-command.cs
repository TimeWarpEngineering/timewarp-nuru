// ═══════════════════════════════════════════════════════════════════════════════
// EXEC COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Execute a command with arguments using catch-all.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

[NuruRoute("exec", Description = "Execute a command")]
public sealed class ExecCommand : ICommand<Unit>
{
  [Parameter(Description = "Command to execute")]
  public string Command { get; set; } = string.Empty;

  [Parameter(IsCatchAll = true, Description = "Command arguments")]
  public string[] Args { get; set; } = [];

  public sealed class Handler : ICommandHandler<ExecCommand, Unit>
  {
    public ValueTask<Unit> Handle(ExecCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Executing {command.Command} {string.Join(" ", command.Args)}");
      return default;
    }
  }
}
