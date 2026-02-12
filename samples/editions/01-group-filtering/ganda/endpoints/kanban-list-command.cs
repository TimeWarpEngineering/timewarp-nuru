namespace Editions.GroupFiltering;

using TimeWarp.Nuru;

[NuruRoute("list", Description = "List kanban tasks")]
public sealed class KanbanListCommand : KanbanGroup, ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<KanbanListCommand, Unit>
  {
    public ValueTask<Unit> Handle(KanbanListCommand command, CancellationToken cancellationToken)
    {
      Console.WriteLine("[KANBAN] Tasks: (none yet)");
      return default;
    }
  }
}
