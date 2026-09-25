using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("greet", Description = "Greet someone")]
public sealed class GreetCommand : ICommand<Unit>
{
  [Parameter] public string Name { get; set; } = "";

  public sealed class Handler : ICommandHandler<GreetCommand, Unit>
  {
    public Task<Unit> Handle(GreetCommand c, CancellationToken ct)
    {
      Console.WriteLine($"Hello, {c.Name}!");
      return Unit.Task;
    }
  }
}
