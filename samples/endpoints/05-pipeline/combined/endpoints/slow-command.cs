// ═══════════════════════════════════════════════════════════════════════════════
// SLOW COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Slow operation triggers performance warning.

namespace PipelineCombined.Endpoints;

using TimeWarp.Nuru;

[NuruRoute("slow", Description = "Slow operation (triggers performance warning)")]
public sealed class SlowCommand : ICommand<Unit>
{
  [Parameter] public int Ms { get; set; } = 600;

  public sealed class Handler : ICommandHandler<SlowCommand, Unit>
  {
    public async ValueTask<Unit> Handle(SlowCommand c, CancellationToken ct)
    {
      await Task.Delay(c.Ms, ct);
      Console.WriteLine("Slow operation complete");
      return default;
    }
  }
}
