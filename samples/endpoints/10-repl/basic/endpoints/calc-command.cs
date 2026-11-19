using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("calc", Description = "Simple calculator")]
public sealed class CalcCommand : ICommand<double>
{
  [Parameter(Order = 0)] public string Operation { get; set; } = "";
  [Parameter(Order = 1)] public double X { get; set; }
  [Parameter(Order = 2)] public double Y { get; set; }

  public sealed class Handler : IRequestHandler<CalcCommand, double>
  {
    public ValueTask<double> Handle(CalcCommand c, CancellationToken ct)
    {
      double result = c.Operation.ToLower() switch
      {
        "add" => c.X + c.Y,
        "sub" => c.X - c.Y,
        "mul" => c.X * c.Y,
        "div" => c.X / c.Y,
        _ => throw new ArgumentException($"Unknown operation: {c.Operation}")
      };

      Console.WriteLine($"Result: {result}");
      return ValueTask.FromResult<double>(result);
    }
  }
}
