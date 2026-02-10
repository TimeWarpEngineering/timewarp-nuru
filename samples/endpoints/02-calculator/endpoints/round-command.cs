// ═══════════════════════════════════════════════════════════════════════════════
// ROUND COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Rounds a number to the nearest integer with optional rounding mode.

namespace EndpointCalculator.Endpoints;

using TimeWarp.Nuru;

/// <summary>
/// Round a number to the nearest integer.
/// </summary>
[NuruRoute("round", Description = "Round a number to the nearest integer")]
public sealed class RoundCommand : IQuery<double>
{
  [Parameter(Description = "Number to round")]
  public double Value { get; set; }

  [Option("mode", "m", Description = "Rounding mode: up, down, nearest, banker")]
  public string Mode { get; set; } = "nearest";

  public sealed class Handler : IQueryHandler<RoundCommand, double>
  {
    public ValueTask<double> Handle(RoundCommand command, CancellationToken ct)
    {
      double result = command.Mode.ToLower() switch
      {
        "up" => Math.Ceiling(command.Value),
        "down" => Math.Floor(command.Value),
        "nearest" => Math.Round(command.Value),
        "banker" or "accountancy" => Math.Round(command.Value, MidpointRounding.ToEven),
        _ => throw new ArgumentException($"Unknown rounding mode: {command.Mode}")
      };

      return new ValueTask<double>(result);
    }
  }
}
