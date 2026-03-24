namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Centralized registry of framework service types that are always available.
/// These services are provided by the NuruApp runtime and don't require explicit registration.
/// </summary>
internal static class FrameworkServices
{
  /// <summary>
  /// Checks if a type name represents a framework service type.
  /// Framework services are always available without explicit registration.
  /// </summary>
  /// <param name="typeName">The fully qualified type name to check.</param>
  /// <returns>True if this is a framework service type.</returns>
  public static bool IsFrameworkServiceType(string typeName)
  {
    string normalized = typeName;

    if (normalized.StartsWith("global::", StringComparison.Ordinal))
      normalized = normalized[8..];

    return normalized.StartsWith("Microsoft.Extensions.Configuration.IConfiguration", StringComparison.Ordinal)
        || normalized.StartsWith("Microsoft.Extensions.Logging.ILogger", StringComparison.Ordinal)
        || normalized.StartsWith("TimeWarp.Terminal.ITerminal", StringComparison.Ordinal)
        || normalized.StartsWith("TimeWarp.Nuru.NuruApp", StringComparison.Ordinal)
        || normalized == "System.Threading.CancellationToken"
        || normalized.StartsWith("Microsoft.Extensions.Options.IOptions", StringComparison.Ordinal)
        || normalized.StartsWith("Microsoft.Extensions.Options.IOptionsSnapshot", StringComparison.Ordinal)
        || normalized.StartsWith("Microsoft.Extensions.Options.IOptionsMonitor", StringComparison.Ordinal);
  }

  /// <summary>
  /// Gets the static field name for a framework service type.
  /// </summary>
  /// <param name="typeName">The fully qualified type name.</param>
  /// <returns>The field name like "__fw_ITerminal".</returns>
  public static string GetFieldName(string typeName)
  {
    string normalized = typeName;

    if (normalized.StartsWith("global::", StringComparison.Ordinal))
      normalized = normalized[8..];

    // For generic types like ILogger<T>, create a unique field name
    // that includes the type argument to avoid collisions
    int genericIndex = normalized.IndexOf('<', StringComparison.Ordinal);
    if (genericIndex >= 0)
    {
      // Extract the generic type name and the type argument
      string genericTypeName = normalized[..genericIndex];
      string typeArg = normalized[(genericIndex + 1)..^1]; // Remove the closing >

      // Remove global:: prefix from type argument if present
      if (typeArg.StartsWith("global::", StringComparison.Ordinal))
        typeArg = typeArg[8..];

      // Get the simple name of the generic type (e.g., "ILogger" from "Microsoft.Extensions.Logging.ILogger")
      int lastDot = genericTypeName.LastIndexOf('.');
      string simpleGenericName = lastDot >= 0 ? genericTypeName[(lastDot + 1)..] : genericTypeName;

      // Get the simple name of the type argument (e.g., "SearchIndex" from "TimeWarp.Nuru.Search.Services.SearchIndex")
      int argLastDot = typeArg.LastIndexOf('.');
      string simpleTypeArg = argLastDot >= 0 ? typeArg[(argLastDot + 1)..] : typeArg;

      // Create a unique field name: __fw_ILogger_SearchIndex
      return $"__fw_{simpleGenericName}_{simpleTypeArg}";
    }

    // Non-generic types: just use the simple type name
    int simpleLastDot = normalized.LastIndexOf('.');
    string simpleName = simpleLastDot >= 0 ? normalized[(simpleLastDot + 1)..] : normalized;

    return $"__fw_{simpleName}";
  }

  /// <summary>
  /// Gets the initialization expression for a framework service type.
  /// This expression is used inside EnsureServicesInitialized.
  /// </summary>
  /// <param name="typeName">The fully qualified type name.</param>
  /// <returns>The initialization expression (e.g., "app.Terminal", "configuration").</returns>
  public static string GetInitExpression(string typeName)
  {
    string normalized = typeName;

    if (normalized.StartsWith("global::", StringComparison.Ordinal))
      normalized = normalized[8..];

    // ITerminal
    if (normalized.StartsWith("TimeWarp.Terminal.ITerminal", StringComparison.Ordinal))
      return "app.Terminal";

    // IConfiguration / IConfigurationRoot
    if (normalized.StartsWith("Microsoft.Extensions.Configuration.IConfiguration", StringComparison.Ordinal))
      return "configuration";

    // NuruApp
    if (normalized.StartsWith("TimeWarp.Nuru.NuruApp", StringComparison.Ordinal))
      return "app";

    // ILogger<T> - use NullLogger when no logging configured
    if (normalized.StartsWith("Microsoft.Extensions.Logging.ILogger", StringComparison.Ordinal))
    {
      int start = normalized.IndexOf('<', StringComparison.Ordinal);
      int end = normalized.LastIndexOf('>');
      string typeArg = start >= 0 && end > start ? normalized[(start + 1)..end] : "object";
      return $"global::Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance.CreateLogger<{typeArg}>()";
    }

    // CancellationToken
    if (normalized == "System.Threading.CancellationToken")
      return "cancellationToken";

    // IOptions<T> - handled separately in service resolution
    if (normalized.StartsWith("Microsoft.Extensions.Options.IOptions", StringComparison.Ordinal))
      return "default! /* IOptions<T> should be handled in service resolution */";

    return $"default! /* Unknown framework service: {typeName} */";
  }
}
