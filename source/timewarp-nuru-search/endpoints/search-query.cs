namespace TimeWarp.Nuru.Search.Endpoints;

[NuruRoute("", Description = "Search indexed CLI endpoints")]
public sealed class SearchQuery : SearchGroup, IQuery<SearchResult[]>
{
  [Option("--cli", Description = "Filter results to a specific CLI")]
  public string? Cli { get; set; }

  [Option("--version", Description = "Show CLI version in results")]
  public bool Version { get; set; }

  [Option("--query", Description = "Search query (positional or named)")]
  public string? Query { get; set; }

  [Parameter(Order = 0, Description = "Search query terms", IsCatchAll = true)]
  public string[]? Terms { get; set; }

  [Option("--limit", Description = "Maximum number of results")]
  public int Limit { get; set; } = 50;

  public sealed class Handler(
    SearchIndex searchIndex,
    ITerminal terminal) : IQueryHandler<SearchQuery, SearchResult[]>
  {
    public async ValueTask<SearchResult[]> Handle(SearchQuery query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);

      string searchQuery = BuildSearchQuery(query);

      if (string.IsNullOrWhiteSpace(searchQuery))
      {
        await terminal.WriteLineAsync("Error: No search query provided. Use --query or pass search terms.").ConfigureAwait(false);
        return [];
      }

      IReadOnlyList<SearchResult> results = await searchIndex.SearchAsync(
        searchQuery,
        query.Cli,
        query.Limit,
        cancellationToken).ConfigureAwait(false);

      if (results.Count == 0)
      {
        await terminal.WriteLineAsync("No results found.").ConfigureAwait(false);
        return [];
      }

      await terminal.WriteLineAsync($"Found {results.Count} result(s):").ConfigureAwait(false);
      await terminal.WriteLineAsync().ConfigureAwait(false);

      foreach (SearchResult result in results)
      {
        string fullPattern = string.IsNullOrEmpty(result.GroupPath)
          ? result.Pattern
          : $"{result.GroupPath} {result.Pattern}".Trim();

        string versionInfo = query.Version ? $" [{result.CliName}@{result.Endpoint.Kind}]" : $" [{result.CliName}]";

        await terminal.WriteLineAsync($"  {fullPattern}{versionInfo}").ConfigureAwait(false);

        if (!string.IsNullOrEmpty(result.Description))
        {
          await terminal.WriteLineAsync($"    {result.Description}").ConfigureAwait(false);
        }
      }

      return [.. results];
    }

    private static string BuildSearchQuery(SearchQuery query)
    {
      if (!string.IsNullOrWhiteSpace(query.Query))
      {
        return query.Query;
      }

      if (query.Terms is { Length: > 0 })
      {
        return string.Join(" ", query.Terms);
      }

      return string.Empty;
    }
  }
}
