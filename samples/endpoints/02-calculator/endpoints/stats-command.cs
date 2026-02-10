// ═══════════════════════════════════════════════════════════════════════════════
// STATS COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Calculates statistics for a set of numbers.

namespace EndpointCalculator.Endpoints;

using TimeWarp.Nuru;

/// <summary>
/// Response type for stats command.
/// </summary>
public class StatsResponse
{
  public double Sum { get; set; }
  public double Average { get; set; }
  public double Min { get; set; }
  public double Max { get; set; }
  public int Count { get; set; }
}

/// <summary>
/// Calculate statistics for a set of numbers.
/// </summary>
[NuruRoute("stats", Description = "Calculate statistics for a set of numbers")]
public sealed class StatsCommand : IQuery<StatsResponse>
{
  [Parameter(IsCatchAll = true, Description = "Numbers to analyze")]
  public string[] Values { get; set; } = [];

  public sealed class Handler : IQueryHandler<StatsCommand, StatsResponse>
  {
    public ValueTask<StatsResponse> Handle(StatsCommand command, CancellationToken ct)
    {
      if (command.Values.Length == 0)
      {
        return new ValueTask<StatsResponse>(new StatsResponse());
      }

      double[] values =
      [
        .. command.Values
          .Select(v => double.TryParse(v, out double d) ? d : 0)
          .Where(v => v != 0)
      ];

      if (values.Length == 0)
      {
        return new ValueTask<StatsResponse>(new StatsResponse());
      }

      return new ValueTask<StatsResponse>
      (
        new StatsResponse
        {
          Sum = values.Sum(),
          Average = values.Average(),
          Min = values.Min(),
          Max = values.Max(),
          Count = values.Length
        }
      );
    }
  }
}
