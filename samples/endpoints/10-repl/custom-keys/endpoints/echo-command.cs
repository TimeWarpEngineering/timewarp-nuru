using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("echo", Description = "Echo text back")]
public sealed class EchoCommand : ICommand<Unit>
{
  [Parameter] public string Text { get; set; } = "";

  public sealed class Handler : ICommandHandler<EchoCommand, Unit>
  {
    public Task<Unit> Handle(EchoCommand c, CancellationToken ct)
    {
      Console.WriteLine(c.Text);
      return Unit.Task;
    }
  }
}
