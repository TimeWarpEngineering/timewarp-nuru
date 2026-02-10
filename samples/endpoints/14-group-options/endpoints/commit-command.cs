namespace GroupOptionsSample.Endpoints;

using TimeWarp.Nuru;

/// <summary>
/// Git commit command with both group options and route-level options.
/// Group options inherited: --verbose, --dry-run, --config
/// Route options: --message, --all
/// </summary>
[NuruRoute("commit", Description = "Record changes to the repository")]
public sealed class CommitCommand : GitGroupBase, ICommand<Unit>
{
  [Option("message", "m", Description = "Commit message")]
  public string Message { get; set; } = string.Empty;

  [Option("all", "a", Description = "Stage all modified and deleted files")]
  public bool All { get; set; }

  public sealed class Handler(ITerminal terminal) : ICommandHandler<CommitCommand, Unit>
  {
    public async ValueTask<Unit> Handle(CommitCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);

      if (command.Verbose)
      {
        await terminal.WriteLineAsync("Verbose mode enabled").ConfigureAwait(false);
      }

      if (string.IsNullOrEmpty(command.Message))
      {
        await terminal.WriteLineAsync("Error: Commit message required (--message)").ConfigureAwait(false);
        return new Unit(1);
      }

      if (command.DryRun)
      {
        await terminal.WriteLineAsync("[DRY RUN] Would create commit:").ConfigureAwait(false);
        await terminal.WriteLineAsync($"  Message: {command.Message}").ConfigureAwait(false);
        await terminal.WriteLineAsync($"  Stage all: {command.All}").ConfigureAwait(false);
      }
      else
      {
        await terminal.WriteLineAsync($"Created commit: {command.Message}").ConfigureAwait(false);
        if (command.All)
        {
          await terminal.WriteLineAsync("(with all modified files staged)").ConfigureAwait(false);
        }
      }

      return default;
    }
  }
}
