#region Purpose
// Nested object stand-in for Roslynk's MultiQueryOperation.
#endregion

namespace RoslynkNuru.Binding;

/// <summary>
/// One object inside a multi_query operations array.
/// </summary>
public sealed class ProbeOperation
{
  public string Tool { get; set; } = string.Empty;

  public string? Arguments { get; set; }
}
