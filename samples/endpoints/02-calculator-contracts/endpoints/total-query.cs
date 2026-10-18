namespace CalculatorContracts.Endpoints;

using System.ComponentModel;
using System.Globalization;
using TimeWarp.Contracts;
using TimeWarp.Mediator;

/// <summary>
/// One labeled amount. Passed as JSON because it is not a scalar.
/// </summary>
public sealed class LabeledAmount
{
  public string Label { get; set; } = "";

  public double Amount { get; set; }
}

/// <summary>
/// Total the values. IncludeAverage keeps its true initializer when omitted.
/// Notes is one JSON value; argv cannot supply it.
/// </summary>
[Operation("total", Description = "Total a list of numbers")]
public sealed class TotalQuery : CalcGroup, IQuery<string>
{
  public double[] Values { get; set; } = [];

  [Description("Include the average in the output")]
  public bool IncludeAverage { get; set; } = true;

  public LabeledAmount[] Notes { get; set; } = [];

  public sealed class Handler : IQueryHandler<TotalQuery, string>
  {
    public Task<string> Handle(TotalQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      double[] values = query.Values ?? [];
      double total = values.Sum();
      string average = query.IncludeAverage && values.Length > 0
        ? (total / values.Length).ToString(CultureInfo.InvariantCulture)
        : "omitted";
      int notes = query.Notes?.Length ?? 0;
      return Task.FromResult(
        $"total:{total.ToString(CultureInfo.InvariantCulture)}|average:{average}|notes:{notes.ToString(CultureInfo.InvariantCulture)}");
    }
  }
}
