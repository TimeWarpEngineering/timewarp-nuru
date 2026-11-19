using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("multiply", Description = "Multiply two doubles")]
public sealed class MultiplyCommand : IQuery<double>
{
  [Parameter(Order = 0)] public double X { get; set; }
  [Parameter(Order = 1)] public double Y { get; set; }

  public sealed class Handler : IRequestHandler<MultiplyCommand, double>
  {
    public ValueTask<double> Handle(MultiplyCommand c, CancellationToken ct) =>
      ValueTask.FromResult<double>(c.X * c.Y);
  }
}
