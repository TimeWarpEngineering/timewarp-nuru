namespace CalculatorContracts.Endpoints;

using TimeWarp.Contracts;
using TimeWarp.Mediator;

/// <summary>
/// How to round <see cref="RoundQuery.Value"/>.
/// </summary>
public enum RoundingMode
{
  Nearest,
  Up,
  Down
}

/// <summary>
/// Round a number. Mode is a non-nullable enum with an initializer.
/// </summary>
[Operation("round", Description = "Round a number")]
public sealed class RoundQuery : CalcGroup, IQuery<double>
{
  public double Value { get; set; }

  public RoundingMode Mode { get; set; } = RoundingMode.Nearest;

  public sealed class Handler : IQueryHandler<RoundQuery, double>
  {
    public Task<double> Handle(RoundQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      double result = query.Mode switch
      {
        RoundingMode.Up => Math.Ceiling(query.Value),
        RoundingMode.Down => Math.Floor(query.Value),
        _ => Math.Round(query.Value)
      };
      return Task.FromResult(result);
    }
  }
}
