// ═══════════════════════════════════════════════════════════════════════════════
// GIT COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Git commit with message and options.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

[NuruRoute("git", Description = "Git commit with message and options")]
public sealed class GitCommand : ICommand<Unit>
{
  [Parameter(Description = "Commit message")]
  public string Message { get; set; } = string.Empty;

  [Option("amend", "a", Description = "Amend previous commit")]
  public bool Amend { get; set; }

  [Option("no-verify", "n", Description = "Bypass pre-commit hooks")]
  public bool NoVerify { get; set; }

  public sealed class Handler : ICommandHandler<GitCommand, Unit>
  {
    public ValueTask<Unit> Handle(GitCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Commit: {command.Message} (amend: {command.Amend}, no-verify: {command.NoVerify})");
      return default;
    }
  }
}
