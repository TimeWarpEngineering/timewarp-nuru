// ═══════════════════════════════════════════════════════════════════════════════
// PING COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Simple ping (no retry needed).

namespace PipelineRetry.Endpoints;

using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("ping", Description = "Simple ping (no retry)")]
public sealed class PingCommand : ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<PingCommand, Unit>
  {
    public ValueTask<Unit> Handle(PingCommand command, CancellationToken ct)
    {
      WriteLine("Pong!");
      return default;
    }
  }
}
