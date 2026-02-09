// ═══════════════════════════════════════════════════════════════════════════════
// WORKFLOW COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Multi-step workflow with nested telemetry.

namespace PipelineTelemetry.Endpoints;

using TimeWarp.Nuru;

[NuruRoute("workflow", Description = "Multi-step workflow with nested telemetry")]
public sealed class WorkflowCommand : ICommand<Unit>
{
  [Parameter(Description = "Workflow name")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<WorkflowCommand, Unit>
  {
    public async ValueTask<Unit> Handle(WorkflowCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Starting workflow: {command.Name}");

      await Task.Delay(50, ct);
      Console.WriteLine("  ✓ Step 1 complete");

      await Task.Delay(50, ct);
      Console.WriteLine("  ✓ Step 2 complete");

      await Task.Delay(50, ct);
      Console.WriteLine("  ✓ Step 3 complete");

      Console.WriteLine($"Workflow '{command.Name}' complete!");
      return default;
    }
  }
}
