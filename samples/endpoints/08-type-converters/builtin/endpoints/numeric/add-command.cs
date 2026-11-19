using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("add", Description = "Add two integers")]
public sealed class AddCommand : IQuery<int>
{
  [Parameter(Order = 0, Description = "First number")]
  public int X { get; set; }

  [Parameter(Order = 1, Description = "Second number")]
  public int Y { get; set; }

  public sealed class Handler : IRequestHandler<AddCommand, int>
  {
    public ValueTask<int> Handle(AddCommand c, CancellationToken ct) =>
      ValueTask.FromResult<int>(c.X + c.Y);
  }
}
