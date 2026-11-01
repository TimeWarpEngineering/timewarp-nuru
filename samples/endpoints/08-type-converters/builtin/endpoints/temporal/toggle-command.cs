using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("toggle", Description = "Toggle a boolean feature")]
public sealed class ToggleCommand : IQuery<bool>
{
  [Parameter] public bool State { get; set; }

  public sealed class Handler : IRequestHandler<ToggleCommand, bool>
  {
    public ValueTask<bool> Handle(ToggleCommand c, CancellationToken ct) =>
      ValueTask.FromResult<bool>(!c.State);
  }
}
