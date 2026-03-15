namespace TimeWarp.Nuru.Search.Endpoints;

[NuruRoute("rebuild", Description = "Rebuild the search index for one or more CLIs")]
public sealed class IndexRebuildCommand : IndexGroup, ICommand<Unit>
{
  [Option("--cli", Description = "Path to CLI executable to index")]
  public string? Cli { get; set; }

  [Option("--all", Description = "Rebuild all currently indexed CLIs")]
  public bool All { get; set; }

  public sealed class Handler(
    SearchIndex searchIndex,
    CapabilitiesClient capabilitiesClient,
    ITerminal terminal) : ICommandHandler<IndexRebuildCommand, Unit>
  {
    public async ValueTask<Unit> Handle(IndexRebuildCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);

      if (string.IsNullOrEmpty(command.Cli) && !command.All)
      {
        await terminal.WriteLineAsync("Error: Specify --cli <path> or --all").ConfigureAwait(false);
        return default;
      }

      if (command.All)
      {
        await RebuildAllAsync(cancellationToken).ConfigureAwait(false);
      }
      else
      {
        await RebuildSingleAsync(command, cancellationToken).ConfigureAwait(false);
      }

      return default;
    }

    private async Task RebuildAllAsync(CancellationToken cancellationToken)
    {
      IReadOnlyList<CliInfo> existingClis = await searchIndex.ListClisAsync(cancellationToken).ConfigureAwait(false);

      if (existingClis.Count == 0)
      {
        await terminal.WriteLineAsync("No CLIs currently indexed. Use --cli <path> to index a new CLI.").ConfigureAwait(false);
        return;
      }

      await terminal.WriteLineAsync($"Rebuilding index for {existingClis.Count} CLI(s)...").ConfigureAwait(false);

      int successCount = 0;
      int failCount = 0;

      foreach (CliInfo cli in existingClis)
      {
        bool success = await TryIndexCliAsync(cli.Name, cancellationToken).ConfigureAwait(false);

        if (success)
        {
          successCount++;
        }
        else
        {
          failCount++;
        }
      }

      await terminal.WriteLineAsync($"Rebuild complete: {successCount} succeeded, {failCount} failed.").ConfigureAwait(false);
    }

    private async Task RebuildSingleAsync(IndexRebuildCommand command, CancellationToken cancellationToken)
    {
      string cliPath = command.Cli!;

      if (!File.Exists(cliPath))
      {
        await terminal.WriteLineAsync($"Error: CLI not found: {cliPath}").ConfigureAwait(false);
        return;
      }

      await terminal.WriteLineAsync($"Indexing CLI: {cliPath}").ConfigureAwait(false);

      bool success = await TryIndexCliAsync(cliPath, cancellationToken).ConfigureAwait(false);

      if (success)
      {
        await terminal.WriteLineAsync("Index complete.").ConfigureAwait(false);
      }
      else
      {
        await terminal.WriteLineAsync("Error: Failed to index CLI. Check that it supports --capabilities.").ConfigureAwait(false);
      }
    }

    private async Task<bool> TryIndexCliAsync(string cliPathOrName, CancellationToken cancellationToken)
    {
      CliCapabilities? capabilities = await capabilitiesClient.GetCapabilitiesAsync(cliPathOrName, cancellationToken).ConfigureAwait(false);

      if (capabilities is null)
      {
        return false;
      }

      await searchIndex.IndexCliAsync(
        capabilities.Name,
        capabilities.Version,
        capabilities.RawJson,
        capabilities.Endpoints,
        cancellationToken).ConfigureAwait(false);

      return true;
    }
  }
}
