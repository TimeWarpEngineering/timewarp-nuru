namespace TimeWarp.Nuru.Search.Endpoints;

[NuruRoute("list", Description = "List all indexed CLIs")]
public sealed class IndexListQuery : IndexGroup, IQuery<Unit>
{
  public sealed class Handler(
    SearchIndex searchIndex,
    ITerminal terminal) : IQueryHandler<IndexListQuery, Unit>
  {
    public async ValueTask<Unit> Handle(IndexListQuery query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);

      IReadOnlyList<CliInfo> clis = await searchIndex.ListClisAsync(cancellationToken).ConfigureAwait(false);

      if (clis.Count == 0)
      {
        await terminal.WriteLineAsync("No CLIs indexed. Use 'index rebuild --cli <path>' to index a CLI.").ConfigureAwait(false);
        return default;
      }

      await terminal.WriteLineAsync($"Indexed CLIs ({clis.Count}):").ConfigureAwait(false);
      await terminal.WriteLineAsync().ConfigureAwait(false);

      foreach (CliInfo cli in clis)
      {
        await terminal.WriteLineAsync($"  {cli.Name} v{cli.Version}").ConfigureAwait(false);
        await terminal.WriteLineAsync($"    Endpoints: {cli.EndpointCount}").ConfigureAwait(false);
        await terminal.WriteLineAsync($"    Indexed: {cli.IndexedAt:yyyy-MM-dd HH:mm:ss} UTC").ConfigureAwait(false);
      }

      return default;
    }
  }
}
