// ═══════════════════════════════════════════════════════════════════════════════
// ECHO COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Echo a message back - demonstrates basic pipeline.

namespace PipelineBasic.Endpoints;

using TimeWarp.Mediator;
using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("echo", Description = "Echo a message back (demonstrates pipeline)")]
public sealed class EchoCommand : ICommand<Unit>
{
  [Parameter(Description = "Message to echo")]
  public string Message { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<EchoCommand, Unit>
  {
    public Task<Unit> Handle(EchoCommand command, CancellationToken ct)
    {
      WriteLine($"Echo: {command.Message}");
      return Unit.Task;
    }
  }
}
