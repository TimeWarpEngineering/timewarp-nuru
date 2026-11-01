namespace CalculatorContracts.Endpoints;

using TimeWarp.Mediator;

/// <summary>
/// External handler for <see cref="AddQuery"/>. Registration is enough; it does not have to be nested.
/// </summary>
public sealed class AddHandler : IRequestHandler<AddQuery, double>
{
  public ValueTask<double> Handle(AddQuery query, CancellationToken cancellationToken)
  {
    _ = cancellationToken;
    return ValueTask.FromResult(query.X + query.Y);
  }
}
