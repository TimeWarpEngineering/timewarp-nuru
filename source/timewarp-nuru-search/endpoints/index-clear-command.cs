namespace TimeWarp.Nuru.Search.Endpoints;

[NuruRoute("clear", Description = "Clear the search index")]
public sealed class IndexClearCommand : IndexGroup, ICommand<Unit>
{
  [Option("--cli", Description = "Clear index for a specific CLI only")]
  public string? Cli { get; set; }

  [Option("--force", Description = "Skip confirmation prompt")]
  public bool Force { get; set; }

  public sealed class Handler(
    SearchIndex searchIndex,
    ITerminal terminal) : ICommandHandler<IndexClearCommand, Unit>
  {
    public async ValueTask<Unit> Handle(IndexClearCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);

      if (!string.IsNullOrEmpty(command.Cli))
      {
        await ClearSingleCliAsync(command, cancellationToken).ConfigureAwait(false);
      }
      else
      {
        await ClearAllAsync(command, cancellationToken).ConfigureAwait(false);
      }

      return default;
    }

    private async Task ClearSingleCliAsync(IndexClearCommand command, CancellationToken cancellationToken)
    {
      if (!command.Force)
      {
        terminal.Write($"Clear index for '{command.Cli}'? [y/N] ");
        string? response = terminal.ReadLine();

        if (response?.ToLowerInvariant() != "y")
        {
          await terminal.WriteLineAsync("Cancelled.").ConfigureAwait(false);
          return;
        }
      }

      await searchIndex.ClearCliAsync(command.Cli!, cancellationToken).ConfigureAwait(false);
      await terminal.WriteLineAsync($"Cleared index for CLI: {command.Cli}").ConfigureAwait(false);
    }

    private async Task ClearAllAsync(IndexClearCommand command, CancellationToken cancellationToken)
    {
      if (!command.Force)
      {
        terminal.Write("Clear entire search index? [y/N] ");
        string? response = terminal.ReadLine();

        if (response?.ToLowerInvariant() != "y")
        {
          await terminal.WriteLineAsync("Cancelled.").ConfigureAwait(false);
          return;
        }
      }

      await searchIndex.ClearIndexAsync(cancellationToken).ConfigureAwait(false);
      await terminal.WriteLineAsync("Search index cleared.").ConfigureAwait(false);
    }
  }
}
