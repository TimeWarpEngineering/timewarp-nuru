namespace Editions.GroupFiltering;

using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("add", Description = "Add a kanban task")]
public sealed class KanbanAddCommand : KanbanGroup, ICommand<Unit>
{
  [Parameter(Description = "Task name")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<KanbanAddCommand, Unit>
  {
    public Task<Unit> Handle(KanbanAddCommand command, CancellationToken cancellationToken)
    {
      Console.WriteLine($"[KANBAN] Added task: {command.Name}");
      return Unit.Task;
    }
  }
}
