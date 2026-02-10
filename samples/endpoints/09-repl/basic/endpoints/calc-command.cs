using TimeWarp.Nuru;

[NuruRoute("calc", Description = "Simple calculator")]
public sealed class CalcCommand : ICommand<double>
{
  [Parameter] public string Operation { get; set; } = "";
  [Parameter] public double X { get; set; }
  [Parameter] public double Y { get; set; }

  public sealed class Handler : ICommandHandler<CalcCommand, double>
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
      return new ValueTask<double>(result);
    }
  }
}
