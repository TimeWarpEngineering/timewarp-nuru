namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Represents a service registration in the DI container.
/// Used for validation and code generation.
/// </summary>
/// <param name="ServiceTypeName">Fully qualified interface/abstract type name</param>
/// <param name="ImplementationTypeName">Fully qualified implementation type name</param>
/// <param name="Lifetime">Service lifetime (Singleton, Scoped, Transient)</param>
public sealed record ServiceDefinition(
  string ServiceTypeName,
  string ImplementationTypeName,
  ServiceLifetime Lifetime)
{
  /// <summary>
  /// Creates a singleton service registration.
  /// </summary>
  public static ServiceDefinition Singleton(string serviceType, string implementationType) => new(
    ServiceTypeName: serviceType,
    ImplementationTypeName: implementationType,
    Lifetime: ServiceLifetime.Singleton);

  /// <summary>
  /// Creates a scoped service registration.
  /// </summary>
  public static ServiceDefinition Scoped(string serviceType, string implementationType) => new(
    ServiceTypeName: serviceType,
    ImplementationTypeName: implementationType,
    Lifetime: ServiceLifetime.Scoped);

  /// <summary>
  /// Creates a transient service registration.
  /// </summary>
  public static ServiceDefinition Transient(string serviceType, string implementationType) => new(
    ServiceTypeName: serviceType,
    ImplementationTypeName: implementationType,
    Lifetime: ServiceLifetime.Transient);

  /// <summary>
  /// Gets the short service type name for display.
  /// </summary>
  public string ShortServiceTypeName => GetShortName(ServiceTypeName);

  /// <summary>
  /// Gets the short implementation type name for display.
  /// </summary>
  public string ShortImplementationTypeName => GetShortName(ImplementationTypeName);

  private static string GetShortName(string typeName)
  {
    if (typeName.StartsWith("global::", StringComparison.Ordinal))
      typeName = typeName[8..];

    int lastDot = typeName.LastIndexOf('.');
    return lastDot >= 0 ? typeName[(lastDot + 1)..] : typeName;
  }
}

/// <summary>
/// Service lifetime for DI container.
/// </summary>
public enum ServiceLifetime
{
  /// <summary>
  /// Single instance for the application lifetime.
  /// </summary>
  Singleton,

  /// <summary>
  /// One instance per scope (typically per request).
  /// </summary>
  Scoped,

  /// <summary>
  /// New instance every time.
  /// </summary>
  Transient
}
