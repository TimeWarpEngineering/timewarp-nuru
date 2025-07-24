namespace TimeWarp.Nuru.CommandResolver;

/// <summary>
/// Result of command resolution.
/// </summary>
public class ResolverResult
{
  public bool Success { get; init; }
  public RouteEndpoint? MatchedEndpoint { get; init; }
  public Dictionary<string, string> ExtractedValues { get; init; } = new();
  public string? ErrorMessage { get; init; }
}
