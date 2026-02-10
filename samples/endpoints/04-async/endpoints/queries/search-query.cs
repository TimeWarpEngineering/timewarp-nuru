// ═══════════════════════════════════════════════════════════════════════════════
// SEARCH QUERY
// ═══════════════════════════════════════════════════════════════════════════════
// Search with async results.

namespace AsyncExamples.Endpoints.Queries;

using TimeWarp.Nuru;

/// <summary>
/// Result item from search query.
/// </summary>
public class SearchResult
{
  public int Id { get; set; }
  public string Title { get; set; } = string.Empty;
}

[NuruRoute("search", Description = "Search with async results")]
public sealed class SearchQuery : IQuery<SearchResult[]>
{
  [Parameter(Description = "Search query")]
  public string Query { get; set; } = string.Empty;

  [Option("limit", "l", Description = "Maximum results")]
  public int Limit { get; set; } = 10;

  public sealed class Handler : IQueryHandler<SearchQuery, SearchResult[]>
  {
    public async ValueTask<SearchResult[]> Handle(SearchQuery query, CancellationToken ct)
    {
      Console.WriteLine($"Searching for: {query.Query}");
      await Task.Delay(100, ct); // Simulate search

      // Generate fake results
      SearchResult[] results = Enumerable.Range(1, Math.Min(query.Limit, 5))
        .Select(i => new SearchResult { Id = i, Title = $"Result {i} for '{query.Query}'" })
        .ToArray();

      return results;
    }
  }
}
