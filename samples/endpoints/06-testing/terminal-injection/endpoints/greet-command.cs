using TimeWarp.Nuru;
using TimeWarp.Terminal;

[NuruRoute("greet", Description = "Greet someone using injected terminal")]
public sealed class GreetCommand : ICommand<Unit>
{
  [Parameter(Description = "Name to greet")]
  public string Name { get; set; } = "";

  public sealed class Handler(ITerminal Terminal) : ICommandHandler<GreetCommand, Unit>
  {
    public ValueTask<Unit> Handle(GreetCommand c, CancellationToken ct)
    {
      Terminal.WriteLine($"Hello, {c.Name}!");
      return default;
    }
  }
}
