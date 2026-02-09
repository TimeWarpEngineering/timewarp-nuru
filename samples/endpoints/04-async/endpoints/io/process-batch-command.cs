// ═══════════════════════════════════════════════════════════════════════════════
// PROCESS-BATCH COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Process items in batches asynchronously.
// NOTE: Property named IsParallel to avoid conflict with System.Threading.Tasks.Parallel

namespace AsyncExamples.Endpoints.IO;

using TimeWarp.Nuru;

[NuruRoute("process-batch", Description = "Process items in batches asynchronously")]
public sealed class ProcessBatchCommand : ICommand<Unit>
{
  [Parameter(IsCatchAll = true, Description = "Items to process")]
  public string[] Items { get; set; } = [];

  [Option("parallel", "p", Description = "Process in parallel")]
  public bool IsParallel { get; set; }

  public sealed class Handler : ICommandHandler<ProcessBatchCommand, Unit>
  {
    public async ValueTask<Unit> Handle(ProcessBatchCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Processing {command.Items.Length} items...");

      if (command.IsParallel)
      {
        await System.Threading.Tasks.Parallel.ForEachAsync(
          command.Items,
          new System.Threading.Tasks.ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = 4 },
          async (item, token) =>
          {
            await Task.Delay(10, token);
            Console.WriteLine($"  Processed: {item}");
          }
        );
      }
      else
      {
        foreach (string item in command.Items)
        {
          ct.ThrowIfCancellationRequested();
          await Task.Delay(10, ct);
          Console.WriteLine($"  Processed: {item}");
        }
      }

      Console.WriteLine("Batch processing complete!");
      return default;
    }
  }
}
