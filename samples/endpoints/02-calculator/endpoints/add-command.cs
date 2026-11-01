// ═══════════════════════════════════════════════════════════════════════════════
// ADD COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Adds two numbers together.

namespace EndpointCalculator.Endpoints;

using TimeWarp.Mediator;
using TimeWarp.Nuru;

/// <summary>
/// Add two numbers together.
/// </summary>
[NuruRoute("add", Description = "Add two numbers together")]
public sealed class AddCommand : IQuery<double>
{
  [Parameter(Description = "First number")]
  public double X { get; set; }

  [Parameter(Description = "Second number")]
  public double Y { get; set; }

  public sealed class Handler : IRequestHandler<AddCommand, double>
  {
    public ValueTask<double> Handle(AddCommand command, CancellationToken ct)
    {
      return ValueTask.FromResult<double>(command.X + command.Y);
    }
  }
}
