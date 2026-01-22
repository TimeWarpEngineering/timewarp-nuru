namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Records an extension method call that registers services internally.
/// These calls (like AddLogging, AddHttpClient) are opaque to source-gen DI.
/// </summary>
/// <param name="MethodName">The name of the extension method (e.g., "AddLogging").</param>
/// <param name="Location">Source location of the call for diagnostic reporting.</param>
public sealed record ExtensionMethodCall(
  string MethodName,
  Location Location);

/// <summary>
/// Result of service extraction from ConfigureServices().
/// Contains both extracted services and detected extension method calls.
/// </summary>
/// <param name="Services">Services extracted from AddTransient/AddScoped/AddSingleton calls.</param>
/// <param name="ExtensionMethods">Extension method calls detected (AddLogging, etc.).</param>
public sealed record ServiceExtractionResult(
  ImmutableArray<ServiceDefinition> Services,
  ImmutableArray<ExtensionMethodCall> ExtensionMethods)
{
  /// <summary>
  /// Empty extraction result (no services, no extension methods).
  /// </summary>
  public static ServiceExtractionResult Empty => new([], []);

  /// <summary>
  /// Creates a result with only services (no extension methods detected).
  /// </summary>
  public static ServiceExtractionResult FromServices(ImmutableArray<ServiceDefinition> services) =>
    new(services, []);

  /// <summary>
  /// Combines multiple extraction results into one.
  /// </summary>
  public ServiceExtractionResult Merge(ServiceExtractionResult other)
  {
    ArgumentNullException.ThrowIfNull(other);

    return new ServiceExtractionResult(
      Services.AddRange(other.Services),
      ExtensionMethods.AddRange(other.ExtensionMethods));
  }
}
