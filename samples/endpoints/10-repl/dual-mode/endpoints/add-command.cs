using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("add", Description = "Add two numbers")]
public sealed class AddCommand : IQuery<double>
{
  [Parameter] public double X { get; set; }
  [Parameter] public double Y { get; set; }

  public sealed class Handler : IQueryHandler<AddCommand, double>
  {
    public Task<double> Handle(AddCommand q, CancellationToken ct) =>
      Task.FromResult<double>(q.X + q.Y);
  }
}
