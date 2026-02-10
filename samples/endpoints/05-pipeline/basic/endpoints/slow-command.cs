// ═══════════════════════════════════════════════════════════════════════════════
// SLOW COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Simulate slow operation to demonstrate performance behavior.

namespace PipelineBasic.Endpoints;

using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("slow", Description = "Simulate slow operation (ms) to demonstrate performance behavior")]
public sealed class SlowCommand : ICommand<Unit>
{
  [Parameter(Description = "Milliseconds to delay")]
  public int Delay { get; set; }

  public sealed class Handler : ICommandHandler<SlowCommand, Unit>
  {
    public async ValueTask<Unit> Handle(SlowCommand command, CancellationToken ct)
    {
      WriteLine($"Starting slow operation ({command.Delay}ms)...");
      await Task.Delay(command.Delay, ct);
      WriteLine("Slow operation completed.");
      return default;
    }
  }
}
