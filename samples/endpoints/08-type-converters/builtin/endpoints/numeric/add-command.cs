using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("add", Description = "Add two integers")]
public sealed class AddCommand : IQuery<int>
{
  [Parameter(Description = "First number")]
  public int X { get; set; }

  [Parameter(Description = "Second number")]
  public int Y { get; set; }

  public sealed class Handler : IQueryHandler<AddCommand, int>
  {
    public Task<int> Handle(AddCommand c, CancellationToken ct) =>
      Task.FromResult<int>(c.X + c.Y);
  }
}
