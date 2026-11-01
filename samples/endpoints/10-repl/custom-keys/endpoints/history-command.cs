using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("history", Description = "Show command history")]
public sealed class HistoryCommand : ICommand<Unit>
{
  [Parameter] public int? Count { get; set; }

  public sealed class Handler : IRequestHandler<HistoryCommand, Unit>
  {
    public ValueTask<Unit> Handle(HistoryCommand c, CancellationToken ct)
    {
      Console.WriteLine("Recent commands:");
      Console.WriteLine("  (history would appear here)");
      return Unit.ValueTask;
    }
  }
}
