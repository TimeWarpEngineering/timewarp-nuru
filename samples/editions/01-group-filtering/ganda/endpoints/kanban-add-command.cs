namespace Editions.GroupFiltering;

using TimeWarp.Nuru;

[NuruRoute("add", Description = "Add a kanban task")]
public sealed class KanbanAddCommand : KanbanGroup, ICommand<Unit>
{
  [Parameter(Description = "Task name")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<KanbanAddCommand, Unit>
  {
    public ValueTask<Unit> Handle(KanbanAddCommand command, CancellationToken cancellationToken)
    {
      Console.WriteLine($"[KANBAN] Added task: {command.Name}");
      return default;
    }
  }
}
