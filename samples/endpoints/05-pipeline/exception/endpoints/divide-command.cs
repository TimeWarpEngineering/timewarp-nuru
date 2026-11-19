// ═══════════════════════════════════════════════════════════════════════════════
// DIVIDE COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Divide two numbers (handles divide by zero).

namespace PipelineException.Endpoints;

using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("divide", Description = "Divide two numbers (handles divide by zero)")]
public sealed class DivideCommand : ICommand<double>
{
  [Parameter(Order = 0, Description = "Dividend")]
  public double X { get; set; }

  [Parameter(Order = 1, Description = "Divisor")]
  public double Y { get; set; }

  public sealed class Handler : IRequestHandler<DivideCommand, double>
  {
    public ValueTask<double> Handle(DivideCommand command, CancellationToken ct)
    {
      if (command.Y == 0)
      {
        throw new DivideByZeroException("Cannot divide by zero");
      }

      return ValueTask.FromResult<double>(command.X / command.Y);
    }
  }
}
