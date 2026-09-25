using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("multiply", Description = "Multiply two doubles")]
public sealed class MultiplyCommand : IQuery<double>
{
  [Parameter] public double X { get; set; }
  [Parameter] public double Y { get; set; }

  public sealed class Handler : IQueryHandler<MultiplyCommand, double>
  {
    public Task<double> Handle(MultiplyCommand c, CancellationToken ct) =>
      Task.FromResult<double>(c.X * c.Y);
  }
}
