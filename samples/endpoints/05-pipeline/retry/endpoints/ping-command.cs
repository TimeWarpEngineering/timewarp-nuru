// ═══════════════════════════════════════════════════════════════════════════════
// PING COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Simple ping (no retry needed).

namespace PipelineRetry.Endpoints;

using TimeWarp.Mediator;
using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("ping", Description = "Simple ping (no retry)")]
public sealed class PingCommand : ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<PingCommand, Unit>
  {
    public Task<Unit> Handle(PingCommand command, CancellationToken ct)
    {
      WriteLine("Pong!");
      return Unit.Task;
    }
  }
}
