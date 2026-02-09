// ═══════════════════════════════════════════════════════════════════════════════
// DELETE COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Delete a file at the specified path.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

[NuruRoute("delete", Description = "Delete a file")]
public sealed class DeleteCommand : ICommand<Unit>
{
  [Parameter(Description = "File path to delete")]
  public string Path { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<DeleteCommand, Unit>
  {
    public ValueTask<Unit> Handle(DeleteCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Deleting {command.Path}");
      return default;
    }
  }
}
