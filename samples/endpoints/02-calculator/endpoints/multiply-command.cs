// ═══════════════════════════════════════════════════════════════════════════════
// MULTIPLY COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Multiplies two numbers together.

namespace EndpointCalculator.Endpoints;

using TimeWarp.Mediator;
using TimeWarp.Nuru;

/// <summary>
/// Multiply two numbers together.
/// </summary>
[NuruRoute("multiply", Description = "Multiply two numbers together")]
public sealed class MultiplyCommand : IQuery<double>
{
  [Parameter(Description = "First number")]
  public double X { get; set; }

  [Parameter(Description = "Second number")]
  public double Y { get; set; }

  public sealed class Handler : IQueryHandler<MultiplyCommand, double>
  {
    public Task<double> Handle(MultiplyCommand command, CancellationToken ct)
    {
      return Task.FromResult<double>(command.X * command.Y);
    }
  }
}
