using Microsoft.Extensions.Logging;
using TimeWarp.Nuru;

[NuruRoute("batch", Description = "Process multiple items with batch logging")]
public sealed class BatchCommand : ICommand<Unit>
{
  [Parameter(IsCatchAll = true)] public string[] Items { get; set; } = [];

  public sealed class Handler(ILogger<BatchCommand> Logger) : ICommandHandler<BatchCommand, Unit>
  {
    public ValueTask<Unit> Handle(BatchCommand c, CancellationToken ct)
    {
      Logger.LogInformation("Starting batch processing of {Count} items", c.Items.Length);

      int successCount = 0;
      int failCount = 0;

      foreach (string item in c.Items)
      {
        Logger.LogDebug("Processing {Item}", item);

        if (Random.Shared.Next(10) > 7)
        {
          Logger.LogError("Failed to process {Item}", item);
          failCount++;
        }
        else
        {
          Logger.LogInformation("Successfully processed {Item}", item);
          successCount++;
        }
      }

      Logger.LogInformation(
        "Batch complete: {Success} succeeded, {Failed} failed",
        successCount,
        failCount
      );

      return default;
    }
  }
}
