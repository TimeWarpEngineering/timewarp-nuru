#region Purpose
// Implements the `index clear` command: removes one CLI's entries, or the whole search index, from the local
// database.
#endregion

namespace TimeWarp.Nuru.Search.Endpoints;

using TimeWarp.Mediator;

[NuruRoute("clear", Description = "Clear the search index")]
public sealed class IndexClearCommand : IndexGroup, ICommand<Unit>
{
  [Option("--cli", Description = "Clear index for one CLI only, by app name or executable name")]
  public string? Cli { get; set; }

  [Option("--force", Description = "Skip confirmation prompt")]
  public bool Force { get; set; }

  public sealed class Handler(
    SearchIndex searchIndex,
    ITerminal terminal) : IRequestHandler<IndexClearCommand, Unit>
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

      // --cli is the app name or the executable name, the same key search --cli takes (534).
      IReadOnlyList<string> cliNames = await searchIndex.ResolveCliNamesAsync(command.Cli!, cancellationToken).ConfigureAwait(false);

      if (cliNames.Count == 0)
      {
        await terminal.WriteLineAsync($"No indexed CLI matches: {command.Cli}").ConfigureAwait(false);
        return;
      }

      foreach (string cliName in cliNames)
      {
        await searchIndex.ClearCliAsync(cliName, cancellationToken).ConfigureAwait(false);
        await terminal.WriteLineAsync($"Cleared index for CLI: {cliName}").ConfigureAwait(false);
      }
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
