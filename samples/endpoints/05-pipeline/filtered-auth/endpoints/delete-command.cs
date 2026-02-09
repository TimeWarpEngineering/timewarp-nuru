// ═══════════════════════════════════════════════════════════════════════════════
// DELETE COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Delete an item (admin only, requires IRequireAuthorization).

namespace PipelineFilteredAuth.Endpoints;

using PipelineFilteredAuth.Behaviors;
using TimeWarp.Nuru;

[NuruRoute("delete", Description = "Delete an item (admin only)")]
public sealed class DeleteCommand : ICommand<Unit>, IRequireAuthorization
{
  [Parameter(Description = "Item ID to delete")]
  public string Id { get; set; } = string.Empty;

  [Option("force", "f", Description = "Force deletion without confirmation")]
  public bool Force { get; set; }

  public sealed class Handler : ICommandHandler<DeleteCommand, Unit>
  {
    public ValueTask<Unit> Handle(DeleteCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Deleting item {command.Id} (force: {command.Force})");
      return default;
    }
  }
}
