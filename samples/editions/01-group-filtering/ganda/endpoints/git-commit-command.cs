namespace Editions.GroupFiltering;

using TimeWarp.Nuru;

[NuruRoute("commit", Description = "Commit changes")]
public sealed class GitCommitCommand : GitGroup, ICommand<Unit>
{
  [Option("message", "m", Description = "Commit message")]
  public string Message { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<GitCommitCommand, Unit>
  {
    public ValueTask<Unit> Handle(GitCommitCommand command, CancellationToken cancellationToken)
    {
      Console.WriteLine($"[GIT] Committed: {command.Message}");
      return default;
    }
  }
}
