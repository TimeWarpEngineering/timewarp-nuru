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
/// Represents HttpClient configuration extracted from AddHttpClient() calls.
/// </summary>
/// <param name="ClientName">The name of the HttpClient (for named clients), or null for typed clients.</param>
/// <param name="ServiceTypeName">The service type name for typed clients (e.g., "IMyService").</param>
/// <param name="ImplementationTypeName">The implementation type name for typed clients (e.g., "MyService").</param>
/// <param name="ConfigurationLambdaBody">The lambda body text to emit in HttpClient configuration (e.g., "client.BaseAddress = ...").</param>
/// <param name="LambdaParameterName">The parameter name from the user's AddHttpClient lambda (defaults to "client").</param>
public sealed record HttpClientConfiguration(
  string? ClientName,
  string? ServiceTypeName,
  string? ImplementationTypeName,
  string? ConfigurationLambdaBody,
  string LambdaParameterName = "client"
);

/// <summary>
/// Result of service extraction from ConfigureServices().
/// Contains both extracted services and detected extension method calls.
/// </summary>
/// <param name="Services">Services extracted from AddTransient/AddScoped/AddSingleton calls.</param>
/// <param name="ExtensionMethods">Extension method calls detected (AddLogging, etc.).</param>
/// <param name="HttpClientConfigurations">HttpClient configurations extracted from AddHttpClient calls.</param>
public sealed record ServiceExtractionResult(
  ImmutableArray<ServiceDefinition> Services,
  ImmutableArray<ExtensionMethodCall> ExtensionMethods,
  ImmutableArray<HttpClientConfiguration> HttpClientConfigurations)
{
  /// <summary>
  /// Empty extraction result (no services, no extension methods, no HttpClient configurations).
  /// </summary>
  public static ServiceExtractionResult Empty => new([], [], []);

  /// <summary>
  /// Creates a result with only services (no extension methods or HttpClient configurations detected).
  /// </summary>
  public static ServiceExtractionResult FromServices(ImmutableArray<ServiceDefinition> services) =>
    new(services, [], []);

  /// <summary>
  /// Combines multiple extraction results into one.
  /// </summary>
  public ServiceExtractionResult Merge(ServiceExtractionResult other)
  {
    ArgumentNullException.ThrowIfNull(other);

    return new ServiceExtractionResult(
      Services.AddRange(other.Services),
      ExtensionMethods.AddRange(other.ExtensionMethods),
      HttpClientConfigurations.AddRange(other.HttpClientConfigurations));
  }
}
