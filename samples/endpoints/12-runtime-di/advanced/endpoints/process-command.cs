using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("process", Description = "Process with selected mode")]
public sealed class ProcessCommand : ICommand<string>
{
  [Parameter] public string Mode { get; set; } = "fast";
  [Parameter] public string Input { get; set; } = "";

  public sealed class Handler(FastProcessor Fast, ThoroughProcessor Thorough) : ICommandHandler<ProcessCommand, string>
  {
    public async ValueTask<string> Handle(ProcessCommand c, CancellationToken ct)
    {
      IProcessor processor = c.Mode.ToLower() switch
      {
        "thorough" => Thorough,
        _ => Fast
      };

      WriteLine($"Using {processor.GetType().Name}");
      return await processor.ProcessAsync(c.Input);
    }
  }
}
