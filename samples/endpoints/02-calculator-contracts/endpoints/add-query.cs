namespace CalculatorContracts.Endpoints;

using TimeWarp.Contracts;
using TimeWarp.Mediator;

/// <summary>
/// Add two numbers. The handler is a separate class, not a nested Handler.
/// </summary>
[Operation("add", Description = "Add two numbers")]
public sealed class AddQuery : CalcGroup, IQuery<double>
{
  public double X { get; set; }

  public double Y { get; set; }
}
