namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Represents a constructor parameter for dependency resolution.
/// </summary>
/// <param name="ParameterName">Name of the constructor parameter.</param>
/// <param name="TypeName">Fully qualified type name of the parameter.</param>
/// <param name="HasDefaultValue">True if the parameter has a default value.</param>
/// <param name="DefaultValue">The default value expression (as string) if HasDefaultValue is true.</param>
public sealed record ConstructorParameter(
  string ParameterName,
  string TypeName,
  bool HasDefaultValue = false,
  string? DefaultValue = null)
{
  /// <summary>
  /// Gets the short type name for display.
  /// </summary>
  public string ShortTypeName => GetShortName(TypeName);

  private static string GetShortName(string typeName)
  {
    if (typeName.StartsWith("global::", StringComparison.Ordinal))
      typeName = typeName[8..];

    // Handle generic types like ILogger<T>
    int genericIndex = typeName.IndexOf('<', StringComparison.Ordinal);
    if (genericIndex >= 0)
    {
      string beforeGeneric = typeName[..genericIndex];
      int lastDot = beforeGeneric.LastIndexOf('.');
      return lastDot >= 0 ? beforeGeneric[(lastDot + 1)..] + typeName[genericIndex..] : typeName;
    }

    int lastDotIndex = typeName.LastIndexOf('.');
    return lastDotIndex >= 0 ? typeName[(lastDotIndex + 1)..] : typeName;
  }
}

/// <summary>
/// Represents a service registration in the DI container.
/// Used for validation and code generation.
/// </summary>
/// <param name="ServiceTypeName">Fully qualified interface/abstract type name</param>
/// <param name="ImplementationTypeName">Fully qualified implementation type name</param>
/// <param name="Lifetime">Service lifetime (Singleton, Scoped, Transient)</param>
/// <param name="ConstructorDependencyTypes">Fully qualified type names of constructor parameters (legacy, for backward compatibility)</param>
/// <param name="ConstructorParameters">Detailed constructor parameter information.</param>
/// <param name="IsFactoryRegistration">True if registered with a factory delegate</param>
/// <param name="IsInternalType">True if implementation type is internal</param>
/// <param name="RegistrationLocation">Source location of the registration for error reporting</param>
public sealed record ServiceDefinition(
  string ServiceTypeName,
  string ImplementationTypeName,
  ServiceLifetime Lifetime,
  ImmutableArray<string> ConstructorDependencyTypes = default,
  ImmutableArray<ConstructorParameter> ConstructorParameters = default,
  bool IsFactoryRegistration = false,
  bool IsInternalType = false,
  Location? RegistrationLocation = null)
{
  /// <summary>
  /// Gets whether this service has constructor dependencies.
  /// </summary>
  public bool HasConstructorDependencies =>
    (!ConstructorDependencyTypes.IsDefaultOrEmpty && ConstructorDependencyTypes.Length > 0) ||
    (!ConstructorParameters.IsDefaultOrEmpty && ConstructorParameters.Length > 0);

  /// <summary>
  /// Creates a singleton service registration.
  /// </summary>
  public static ServiceDefinition Singleton(
    string serviceType,
    string implementationType,
    ImmutableArray<string> constructorDependencyTypes = default,
    ImmutableArray<ConstructorParameter> constructorParameters = default,
    bool isFactoryRegistration = false,
    bool isInternalType = false,
    Location? registrationLocation = null) => new(
    ServiceTypeName: serviceType,
    ImplementationTypeName: implementationType,
    Lifetime: ServiceLifetime.Singleton,
    ConstructorDependencyTypes: constructorDependencyTypes,
    ConstructorParameters: constructorParameters,
    IsFactoryRegistration: isFactoryRegistration,
    IsInternalType: isInternalType,
    RegistrationLocation: registrationLocation);

  /// <summary>
  /// Creates a scoped service registration.
  /// </summary>
  public static ServiceDefinition Scoped(
    string serviceType,
    string implementationType,
    ImmutableArray<string> constructorDependencyTypes = default,
    ImmutableArray<ConstructorParameter> constructorParameters = default,
    bool isFactoryRegistration = false,
    bool isInternalType = false,
    Location? registrationLocation = null) => new(
    ServiceTypeName: serviceType,
    ImplementationTypeName: implementationType,
    Lifetime: ServiceLifetime.Scoped,
    ConstructorDependencyTypes: constructorDependencyTypes,
    ConstructorParameters: constructorParameters,
    IsFactoryRegistration: isFactoryRegistration,
    IsInternalType: isInternalType,
    RegistrationLocation: registrationLocation);

  /// <summary>
  /// Creates a transient service registration.
  /// </summary>
  public static ServiceDefinition Transient(
    string serviceType,
    string implementationType,
    ImmutableArray<string> constructorDependencyTypes = default,
    ImmutableArray<ConstructorParameter> constructorParameters = default,
    bool isFactoryRegistration = false,
    bool isInternalType = false,
    Location? registrationLocation = null) => new(
    ServiceTypeName: serviceType,
    ImplementationTypeName: implementationType,
    Lifetime: ServiceLifetime.Transient,
    ConstructorDependencyTypes: constructorDependencyTypes,
    ConstructorParameters: constructorParameters,
    IsFactoryRegistration: isFactoryRegistration,
    IsInternalType: isInternalType,
    RegistrationLocation: registrationLocation);

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
