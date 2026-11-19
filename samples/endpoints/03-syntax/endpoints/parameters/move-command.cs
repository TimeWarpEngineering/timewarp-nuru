// ═══════════════════════════════════════════════════════════════════════════════
// MOVE COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Move a file from source to destination - demonstrates multiple parameters.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("move", Description = "Move a file from source to destination")]
public sealed class MoveCommand : ICommand<Unit>
{
  [Parameter(Order = 0, Description = "Source file path")]
  public string Source { get; set; } = string.Empty;

  [Parameter(Order = 1, Description = "Destination file path")]
  public string Destination { get; set; } = string.Empty;

  public sealed class Handler : IRequestHandler<MoveCommand, Unit>
  {
    public ValueTask<Unit> Handle(MoveCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Moving {command.Source} to {command.Destination}");
      return Unit.ValueTask;
    }
  }
}
