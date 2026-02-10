using TimeWarp.Nuru;

[NuruRoute("calc", Description = "Calculate simple expression")]
public sealed class CalcCommand : ICommand<double>
{
  [Parameter] public double X { get; set; }
  [Parameter] public string Op { get; set; } = "+";
  [Parameter] public double Y { get; set; }

  public sealed class Handler : ICommandHandler<CalcCommand, double>
  {
    public ValueTask<double> Handle(CalcCommand c, CancellationToken ct)
    {
      double result = c.Op switch
      {
        "+" => c.X + c.Y,
        "-" => c.X - c.Y,
        "*" or "x" => c.X * c.Y,
        "/" => c.X / c.Y,
        _ => throw new ArgumentException($"Unknown operator: {c.Op}")
      };

      Console.WriteLine($"{c.X} {c.Op} {c.Y} = {result}");
      return new ValueTask<double>(result);
    }
  }
}
