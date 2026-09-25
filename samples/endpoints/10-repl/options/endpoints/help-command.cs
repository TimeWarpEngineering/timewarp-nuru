using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("help", Description = "Show help information")]
public sealed class HelpCommand : ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<HelpCommand, Unit>
  {
    public Task<Unit> Handle(HelpCommand c, CancellationToken ct)
    {
      Console.WriteLine("""
        Available Commands:
          greet <name>     - Greet someone
          calc <expr>      - Calculate expression
          date             - Show current date
          exit             - Exit REPL
        """);
      return Unit.Task;
    }
  }
}
