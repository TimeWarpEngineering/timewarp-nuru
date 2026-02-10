using Microsoft.Extensions.Logging;
using TimeWarp.Nuru;

[NuruRoute("process", Description = "Process an item with logging")]
public sealed class ProcessCommand : ICommand<Unit>
{
  [Parameter] public string Item { get; set; } = "";

  public sealed class Handler(ILogger<ProcessCommand> Logger) : ICommandHandler<ProcessCommand, Unit>
  {
    public ValueTask<Unit> Handle(ProcessCommand c, CancellationToken ct)
    {
      Logger.LogInformation("Starting processing of {Item}", c.Item);

      try
      {
        Logger.LogDebug("Validating item {Item}", c.Item);

        if (string.IsNullOrWhiteSpace(c.Item))
        {
          Logger.LogWarning("Empty item provided");
          throw new ArgumentException("Item cannot be empty");
        }

        Logger.LogInformation("Processing {Item}...", c.Item);
        Thread.Sleep(100);

        Logger.LogInformation("Successfully processed {Item}", c.Item);
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "Failed to process {Item}", c.Item);
        throw;
      }

      return default;
    }
  }
}
