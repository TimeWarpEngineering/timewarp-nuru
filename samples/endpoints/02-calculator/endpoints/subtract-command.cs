// ═══════════════════════════════════════════════════════════════════════════════
// SUBTRACT COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Subtracts the second number from the first.

namespace EndpointCalculator.Endpoints;

using TimeWarp.Mediator;
using TimeWarp.Nuru;

/// <summary>
/// Subtract the second number from the first.
/// </summary>
[NuruRoute("subtract", Description = "Subtract the second number from the first")]
public sealed class SubtractCommand : IQuery<double>
{
  [Parameter(Description = "First number")]
  public double X { get; set; }

  [Parameter(Description = "Second number")]
  public double Y { get; set; }

  public sealed class Handler : IQueryHandler<SubtractCommand, double>
  {
    public Task<double> Handle(SubtractCommand command, CancellationToken ct)
    {
      return Task.FromResult<double>(command.X - command.Y);
    }
  }
}
