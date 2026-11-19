// ═══════════════════════════════════════════════════════════════════════════════
// COPY COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Copy a file from source to destination - demonstrates multiple parameters.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("copy", Description = "Copy a file from source to destination")]
public sealed class CopyCommand : ICommand<Unit>
{
  [Parameter(Order = 0, Description = "Source file path")]
  public string Source { get; set; } = string.Empty;

  [Parameter(Order = 1, Description = "Destination file path")]
  public string Destination { get; set; } = string.Empty;

  public sealed class Handler : IRequestHandler<CopyCommand, Unit>
  {
    public ValueTask<Unit> Handle(CopyCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Copying {command.Source} to {command.Destination}");
      return Unit.ValueTask;
    }
  }
}
