namespace Editions.GroupFiltering;

using TimeWarp.Nuru;

[NuruRoute("status", Description = "Show git status")]
public sealed class GitStatusCommand : GitGroup, ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<GitStatusCommand, Unit>
  {
    public ValueTask<Unit> Handle(GitStatusCommand command, CancellationToken cancellationToken)
    {
      Console.WriteLine("[GIT] Status: working tree clean");
      return default;
    }
  }
}
