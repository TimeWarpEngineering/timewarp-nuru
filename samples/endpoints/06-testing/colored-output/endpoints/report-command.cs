using TimeWarp.Nuru;
using TimeWarp.Terminal;

[NuruRoute("report", Description = "Show report with mixed colors")]
public sealed class ReportCommand : ICommand<Unit>
{
  public sealed class Handler(ITerminal T) : ICommandHandler<ReportCommand, Unit>
  {
    public ValueTask<Unit> Handle(ReportCommand c, CancellationToken ct)
    {
      T.WriteLine("System Report:".Bold().Underline());
      T.WriteLine($"  {"SUCCESS".Green()}: All systems operational");
      T.WriteLine($"  {"WARNING".Yellow()}: High memory usage");
      T.WriteLine($"  {"ERROR".Red()}: 1 failed job (non-critical)");
      return default;
    }
  }
}
