namespace GroupOptionsSample.Endpoints;

using TimeWarp.Nuru;

/// <summary>
/// Simple git status command showing repository status.
/// Inherits group options: --verbose, --dry-run, --config
/// </summary>
[NuruRoute("status", Description = "Show working tree status")]
public sealed class StatusCommand : GitGroupBase, ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal) : ICommandHandler<StatusCommand, Unit>
  {
    public async ValueTask<Unit> Handle(StatusCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);

      if (command.Verbose)
      {
        await terminal.WriteLineAsync("Verbose mode enabled").ConfigureAwait(false);
      }

      if (!string.IsNullOrEmpty(command.Config))
      {
        await terminal.WriteLineAsync($"Config: {command.Config}=sample-value").ConfigureAwait(false);
      }

      if (command.DryRun)
      {
        await terminal.WriteLineAsync("[DRY RUN] Would show status:").ConfigureAwait(false);
      }

      await terminal.WriteLineAsync("On branch main").ConfigureAwait(false);
      await terminal.WriteLineAsync("Your branch is up to date with 'origin/main'").ConfigureAwait(false);
      await terminal.WriteLineAsync("nothing to commit, working tree clean").ConfigureAwait(false);

      return default;
    }
  }
}
