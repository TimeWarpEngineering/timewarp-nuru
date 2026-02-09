// ═══════════════════════════════════════════════════════════════════════════════
// ADD COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Adds two numbers together.

namespace EndpointCalculator.Endpoints;

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

  public sealed class Handler : IQueryHandler<AddCommand, double>
  {
    public ValueTask<double> Handle(AddCommand command, CancellationToken ct)
    {
      return new ValueTask<double>(command.X + command.Y);
    }
  }
}
