// ═══════════════════════════════════════════════════════════════════════════════
// WAIT COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Wait for specified seconds (optional, defaults to 5).
// NOTE: Use nullable types (int?) for optional parameters, not IsOptional=true.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

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
