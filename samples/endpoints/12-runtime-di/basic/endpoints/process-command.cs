using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("process", Description = "Process data through service chain")]
public sealed class ProcessCommand : ICommand<string>
{
  [Parameter] public string Input { get; set; } = "";

  public sealed class Handler(IProcessingService Processor) : ICommandHandler<ProcessCommand, string>
  {
    public async ValueTask<string> Handle(ProcessCommand c, CancellationToken ct)
    {
      WriteLine("=== Process Command ===");
      string result = await Processor.ProcessAsync(c.Input);
      WriteLine($"Result: {result}");
      return result;
    }
  }
}
