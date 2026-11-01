namespace Editions.GroupFiltering;

using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("list", Description = "List kanban tasks")]
public sealed class KanbanListCommand : KanbanGroup, ICommand<Unit>
{
  public sealed class Handler : IRequestHandler<KanbanListCommand, Unit>
  {
    public ValueTask<Unit> Handle(KanbanListCommand command, CancellationToken cancellationToken)
    {
      Console.WriteLine("[KANBAN] Tasks: (none yet)");
      return Unit.ValueTask;
    }
  }
}
