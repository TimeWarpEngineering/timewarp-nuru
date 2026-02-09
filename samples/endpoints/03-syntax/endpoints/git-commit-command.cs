// ═══════════════════════════════════════════════════════════════════════════════
// GIT COMMIT COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Git commit endpoint - demonstrates hyphenated literal route.
// NOTE: Multi-word routes must be hyphenated (git-commit), not spaced (git commit).

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

[NuruRoute("git-commit", Description = "Commit changes")]
public sealed class GitCommitCommand : ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<GitCommitCommand, Unit>
  {
    public ValueTask<Unit> Handle(GitCommitCommand command, CancellationToken ct)
    {
      Console.WriteLine("Committing...");
      return default;
    }
  }
}
