namespace CalculatorContracts.Endpoints;

using System.ComponentModel;
using System.Globalization;
using TimeWarp.Contracts;
using TimeWarp.Mediator;
using TimeWarp.Nuru;

/// <summary>
/// One labeled amount. Passed as JSON because it is not a scalar.
/// </summary>
public sealed class LabeledAmount
{
  public required string Label { get; set; }

  public double Amount { get; set; }
}

/// <summary>
/// How to print the total. Passed as one JSON object.
/// </summary>
public sealed class TotalFormat
{
  [Description("Digits after the decimal point")]
  public int Decimals { get; set; } = 2;

  [Description("Text written before the total")]
  public string? Prefix { get; set; }
}

/// <summary>
/// Total the values. IncludeAverage keeps its true initializer when omitted.
/// Notes (an object array) and Format (an object) are JSON values; argv cannot supply them.
/// The `mcp` route publishes their real JSON schemas, and the class attributes become tool annotations.
/// </summary>
[Operation("total", Description = "Total a list of numbers")]
[DisplayName("Total numbers")]
[OpenWorld(false)]
public sealed class TotalQuery : CalcGroup, IQuery<string>
{
  public double[] Values { get; set; } = [];

  [Description("Include the average in the output")]
  public bool IncludeAverage { get; set; } = true;

  [Description("Labeled amounts added to the total")]
  public LabeledAmount[] Notes { get; set; } = [];

  [Description("How to print the total")]
  public TotalFormat? Format { get; set; }

  public sealed class Handler : IQueryHandler<TotalQuery, string>
  {
    public Task<string> Handle(TotalQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      double[] values = query.Values ?? [];
      LabeledAmount[] notes = query.Notes ?? [];
      double total = values.Sum() + notes.Sum(n => n.Amount);
      string format = query.Format is null ? "R" : "F" + query.Format.Decimals.ToString(CultureInfo.InvariantCulture);
      string average = query.IncludeAverage && values.Length > 0
        ? (values.Sum() / values.Length).ToString(CultureInfo.InvariantCulture)
        : "omitted";
      return Task.FromResult(
        $"total:{query.Format?.Prefix}{total.ToString(format, CultureInfo.InvariantCulture)}|average:{average}|notes:{notes.Length.ToString(CultureInfo.InvariantCulture)}");
    }
  }
}
