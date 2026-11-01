using TimeWarp.Mediator;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

[NuruRoute("error", Description = "Simulate an error")]
public sealed class ErrorCommand : ICommand<Unit>
{
  [Parameter] public string Message { get; set; } = "";

  public sealed class Handler(ITerminal T) : IRequestHandler<ErrorCommand, Unit>
  {
    public ValueTask<Unit> Handle(ErrorCommand c, CancellationToken ct)
    {
      T.WriteErrorLine($"{"✗".Red()} Error: {c.Message}".Red());
      throw new InvalidOperationException(c.Message);
    }
  }
}
