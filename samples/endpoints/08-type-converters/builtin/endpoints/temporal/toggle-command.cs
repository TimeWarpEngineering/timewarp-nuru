using TimeWarp.Nuru;

[NuruRoute("toggle", Description = "Toggle a boolean feature")]
public sealed class ToggleCommand : IQuery<bool>
{
  [Parameter] public bool State { get; set; }

  public sealed class Handler : IQueryHandler<ToggleCommand, bool>
  {
    public ValueTask<bool> Handle(ToggleCommand c, CancellationToken ct) =>
      new ValueTask<bool>(!c.State);
  }
}
