namespace Editions.GroupFiltering;

using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("status", Description = "Show git status")]
public sealed class GitStatusCommand : GitGroup, ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<GitStatusCommand, Unit>
  {
    public Task<Unit> Handle(GitStatusCommand command, CancellationToken cancellationToken)
    {
      Console.WriteLine("[GIT] Status: working tree clean");
      return Unit.Task;
    }
  }
}
