using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("toggle", Description = "Toggle a boolean feature")]
public sealed class ToggleCommand : IQuery<bool>
{
  [Parameter] public bool State { get; set; }

  public sealed class Handler : IQueryHandler<ToggleCommand, bool>
  {
    public Task<bool> Handle(ToggleCommand c, CancellationToken ct) =>
      Task.FromResult<bool>(!c.State);
  }
}
