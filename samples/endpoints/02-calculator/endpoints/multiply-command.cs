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
  [Parameter(Order = 0, Description = "First number")]
  public double X { get; set; }

  [Parameter(Order = 1, Description = "Second number")]
  public double Y { get; set; }

  public sealed class Handler : IRequestHandler<MultiplyCommand, double>
  {
    public ValueTask<double> Handle(MultiplyCommand command, CancellationToken ct)
    {
      return ValueTask.FromResult<double>(command.X * command.Y);
    }
  }
}
