namespace CalculatorContracts.Endpoints;

using TimeWarp.Mediator;

/// <summary>
/// External handler for <see cref="AddQuery"/>. Registration is enough; it does not have to be nested.
/// </summary>
public sealed class AddHandler : IQueryHandler<AddQuery, double>
{
  public Task<double> Handle(AddQuery query, CancellationToken cancellationToken)
  {
    _ = cancellationToken;
    return Task.FromResult(query.X + query.Y);
  }
}
