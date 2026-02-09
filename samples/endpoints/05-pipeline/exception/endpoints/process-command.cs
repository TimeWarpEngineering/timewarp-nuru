// ═══════════════════════════════════════════════════════════════════════════════
// PROCESS COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Process an operation (may fail with various exceptions).

namespace PipelineException.Endpoints;

using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("process", Description = "Process an operation (may fail)")]
public sealed class ProcessCommand : ICommand<Unit>
{
  [Parameter(Description = "Operation to perform")]
  public string Operation { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<ProcessCommand, Unit>
  {
    public ValueTask<Unit> Handle(ProcessCommand command, CancellationToken ct)
    {
      if (command.Operation == "fail")
      {
        throw new InvalidOperationException("Processing intentionally failed");
      }

      if (command.Operation == "deny")
      {
        throw new UnauthorizedAccessException("Access denied for this operation");
      }

      WriteLine($"✓ Processed: {command.Operation}");
      return default;
    }
  }
}
