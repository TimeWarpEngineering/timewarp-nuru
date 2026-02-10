using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("analyze", Description = "Analyze using factory-created analyzer")]
public sealed class AnalyzeCommand : ICommand<string>
{
  [Parameter] public string Data { get; set; } = "";

  public sealed class Handler(Func<string, IAnalyzer> AnalyzerFactory) : ICommandHandler<AnalyzeCommand, string>
  {
    public ValueTask<string> Handle(AnalyzeCommand c, CancellationToken ct)
    {
      IAnalyzer analyzer = AnalyzerFactory("Smart");
      string result = analyzer.Analyze(c.Data);
      return new ValueTask<string>(result);
    }
  }
}
