namespace Editions.GroupFiltering;

using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("status", Description = "Show git status")]
public sealed class GitStatusCommand : GitGroup, ICommand<Unit>
{
  public sealed class Handler : IRequestHandler<GitStatusCommand, Unit>
  {
    public ValueTask<Unit> Handle(GitStatusCommand command, CancellationToken cancellationToken)
    {
      Console.WriteLine("[GIT] Status: working tree clean");
      return Unit.ValueTask;
    }
  }
}
