namespace TimeWarp.Nuru.Search.Endpoints;

[NuruRoute("", Description = "Search indexed CLI endpoints")]
public sealed partial class SearchQuery : SearchGroup, IQuery<SearchResult[]>
{
  [Option("--cli", Description = "Filter results to a specific CLI (will auto-index if not found)")]
  public string? Cli { get; set; }

  [Option("--version", Description = "Show CLI version in results")]
  public bool Version { get; set; }

  [Option("--query", Description = "Search query (positional or named)")]
  public string? Query { get; set; }

  [Option("--group", Description = "Filter results by group path prefix")]
  public string? Group { get; set; }

  [Parameter(Order = 0, Description = "Search query terms", IsCatchAll = true)]
  public string[]? Terms { get; set; }

  [Option("--limit", Description = "Maximum number of results")]
  public int Limit { get; set; } = 50;

  public sealed partial class Handler(
    SearchIndex searchIndex,
    CapabilitiesClient capabilitiesClient,
    ITerminal terminal,
    ILogger<Handler> logger) : IQueryHandler<SearchQuery, SearchResult[]>
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

      // On-demand indexing: if --cli is specified, ensure it's indexed
      if (!string.IsNullOrEmpty(query.Cli))
      {
        await EnsureCliIndexedAsync(query.Cli, cancellationToken).ConfigureAwait(false);
      }

      IReadOnlyList<SearchResult> results = await searchIndex.SearchAsync(
        searchQuery,
        query.Cli,
        query.Group,
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

    private async Task EnsureCliIndexedAsync(string cliName, CancellationToken cancellationToken)
    {
      // Check if already indexed
      string? indexedVersion = await searchIndex.GetCliVersionAsync(cliName, cancellationToken).ConfigureAwait(false);

      if (indexedVersion is not null)
      {
        LogCliAlreadyIndexed(logger, cliName, indexedVersion);
        return;
      }

      // Find CLI in PATH using Amuru's PathResolver
      string? cliPath = PathResolver.ResolveExecutable(cliName);
      if (cliPath is null)
      {
        await terminal.WriteLineAsync($"Warning: CLI '{cliName}' not found in PATH. Cannot auto-index.").ConfigureAwait(false);
        return;
      }

      LogAutoIndexingCli(logger, cliName, cliPath);

      // Get capabilities from CLI
      CliCapabilities? capabilities = await capabilitiesClient.GetCapabilitiesAsync(cliPath, cancellationToken).ConfigureAwait(false);
      if (capabilities is null)
      {
        await terminal.WriteLineAsync($"Warning: Failed to get capabilities from '{cliName}'. Cannot auto-index.").ConfigureAwait(false);
        return;
      }

      // Index the CLI
      await searchIndex.IndexCliAsync(
        capabilities.Name,
        capabilities.Version,
        capabilities.RawJson,
        capabilities.Endpoints,
        cancellationToken).ConfigureAwait(false);

      await terminal.WriteLineAsync($"Auto-indexed {capabilities.Name} v{capabilities.Version} ({capabilities.Endpoints.Count} endpoints)").ConfigureAwait(false);
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

    [LoggerMessage(LogLevel.Debug, "CLI {CliName} already indexed at version {Version}")]
    private static partial void LogCliAlreadyIndexed(ILogger logger, string cliName, string version);

    [LoggerMessage(LogLevel.Information, "Auto-indexing CLI {CliName} from {CliPath}")]
    private static partial void LogAutoIndexingCli(ILogger logger, string cliName, string cliPath);
  }
}
