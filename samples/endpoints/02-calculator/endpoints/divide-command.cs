// ═══════════════════════════════════════════════════════════════════════════════
// DIVIDE COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Divides the first number by the second.

namespace EndpointCalculator.Endpoints;

using TimeWarp.Nuru;

/// <summary>
/// Divide the first number by the second.
/// </summary>
[NuruRoute("divide", Description = "Divide the first number by the second")]
public sealed class DivideCommand : IQuery<double>
{
  [Parameter(Description = "Dividend")]
  public double X { get; set; }

  [Parameter(Description = "Divisor")]
  public double Y { get; set; }

  public sealed class Handler : IQueryHandler<DivideCommand, double>
  {
    public ValueTask<double> Handle(DivideCommand command, CancellationToken ct)
    {
      if (command.Y == 0)
      {
        throw new DivideByZeroException("Cannot divide by zero");
      }

      return new ValueTask<double>(command.X / command.Y);
    }
  }
}
