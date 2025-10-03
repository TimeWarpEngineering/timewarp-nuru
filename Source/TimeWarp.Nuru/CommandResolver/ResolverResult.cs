namespace TimeWarp.Nuru.CommandResolver;

/// <summary>
/// Result of command resolution.
/// </summary>
#pragma warning disable CA1815 // Override equals and operator equals on value types
// This is a one-time result object that's never compared. It exists only to pass
// resolution results from the resolver to the execution logic. No equality operations needed.
public readonly struct ResolverResult
#pragma warning restore CA1815
{
  public bool Success { get; }
  public Endpoint? MatchedEndpoint { get; }
  public Dictionary<string, string>? ExtractedValues { get; }
  public string? ErrorMessage { get; }

  public ResolverResult(bool success, Endpoint? matchedEndpoint = null,
    Dictionary<string, string>? extractedValues = null, string? errorMessage = null)
  {
    Success = success;
    MatchedEndpoint = matchedEndpoint;
    ExtractedValues = extractedValues;
    ErrorMessage = errorMessage;
  }
}
