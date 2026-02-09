// ═══════════════════════════════════════════════════════════════════════════════
// TAIL COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Tail a file with optional line count using catch-all for extra arguments.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

[NuruRoute("tail", Description = "Tail a file")]
public sealed class TailCommand : ICommand<Unit>
{
  [Parameter(Description = "File to tail")]
  public string File { get; set; } = string.Empty;

  [Parameter(IsCatchAll = true, Description = "Additional tail arguments")]
  public string[] Args { get; set; } = [];

  public sealed class Handler : ICommandHandler<TailCommand, Unit>
  {
    public ValueTask<Unit> Handle(TailCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Tailing {command.File} with {command.Args.Length} extra args");
      return default;
    }
  }
}
