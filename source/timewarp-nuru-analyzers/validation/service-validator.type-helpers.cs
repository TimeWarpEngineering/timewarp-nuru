#region Purpose
// Type name normalization and registration lookup helpers (ServiceValidator partial).
#endregion

namespace TimeWarp.Nuru.Validation;

internal static partial class ServiceValidator
{
  /// <summary>
  /// Checks if a service type is registered.
  /// </summary>
  private static bool IsServiceRegistered(string typeName, HashSet<string> registeredServices)
  {
    return registeredServices.Contains(typeName)
        || registeredServices.Contains(NormalizeTypeName(typeName));
  }

  /// <summary>
  /// Normalizes a type name by removing global:: prefix.
  /// </summary>
  private static string NormalizeTypeName(string typeName)
  {
    return typeName.StartsWith("global::", StringComparison.Ordinal)
      ? typeName[8..]
      : typeName;
  }

  /// <summary>
  /// Checks if a type name is IOptions&lt;T&gt;.
  /// </summary>
  private static bool IsOptionsType(string typeName)
  {
    return typeName.Contains("IOptions<", StringComparison.Ordinal)
        || typeName.Contains("IOptionsSnapshot<", StringComparison.Ordinal)
        || typeName.Contains("IOptionsMonitor<", StringComparison.Ordinal);
  }

  /// <summary>
  /// Checks if a type name is <c>IOptions&lt;T&gt;</c>, the options type a handler parameter binds from configuration.
  /// </summary>
  private static bool IsBoundOptionsType(string typeName)
  {
    string normalized = NormalizeTypeName(typeName);
    return normalized.StartsWith("Microsoft.Extensions.Options.IOptions<", StringComparison.Ordinal)
        || normalized.StartsWith("IOptions<", StringComparison.Ordinal);
  }

  /// <summary>
  /// Gets the short type name for display (without namespace).
  /// </summary>
  private static string GetShortTypeName(string typeName)
  {
    string normalized = NormalizeTypeName(typeName);

    // Handle generic types: take everything after last dot but before <
    int genericIndex = normalized.IndexOf('<', StringComparison.Ordinal);
    if (genericIndex >= 0)
    {
      string beforeGeneric = normalized[..genericIndex];
      string genericPart = normalized[genericIndex..];
      int lastDot = beforeGeneric.LastIndexOf('.', StringComparison.Ordinal);
      return lastDot >= 0 ? beforeGeneric[(lastDot + 1)..] + genericPart : beforeGeneric + genericPart;
    }

    int lastDotIndex = normalized.LastIndexOf('.', StringComparison.Ordinal);
    return lastDotIndex >= 0 ? normalized[(lastDotIndex + 1)..] : normalized;
  }
}
