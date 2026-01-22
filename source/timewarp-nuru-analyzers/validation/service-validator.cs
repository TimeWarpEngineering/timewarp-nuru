// Service validator that detects:
// NURU050: Handler requires unregistered service
// NURU051: Service has constructor dependencies
// NURU052: Extension method registration (warning)
// NURU053: Factory delegate registration
// NURU054: Internal type not accessible
//
// All validation is skipped when UseMicrosoftDependencyInjection = true.

namespace TimeWarp.Nuru.Validation;

using TimeWarp.Nuru.Generators;

/// <summary>
/// Validates service registrations for source-generated DI compatibility.
/// Skips all validation when UseMicrosoftDependencyInjection is enabled.
/// </summary>
internal static class ServiceValidator
{
  /// <summary>
  /// Validates services and handler requirements, returning diagnostics.
  /// </summary>
  /// <param name="app">The app model to validate.</param>
  /// <returns>Diagnostics for any issues found.</returns>
  public static ImmutableArray<Diagnostic> Validate(AppModel app)
  {
    // Skip ALL validation when runtime DI is enabled
    if (app.UseMicrosoftDependencyInjection)
      return [];

    List<Diagnostic> diagnostics = [];

    // Build a set of registered service type names for lookup
    HashSet<string> registeredServices = BuildRegisteredServiceSet(app.Services);

    // NURU050: Validate handler service requirements
    ValidateHandlerServiceRequirements(app, registeredServices, diagnostics);

    // NURU051: Validate service constructor dependencies
    ValidateServiceConstructorDependencies(app.Services, registeredServices, diagnostics);

    // NURU053: Validate factory registrations
    ValidateFactoryRegistrations(app.Services, diagnostics);

    // NURU054: Validate type accessibility
    ValidateTypeAccessibility(app.Services, diagnostics);

    return [.. diagnostics];
  }

  /// <summary>
  /// Reports warnings for extension method calls that may register services.
  /// NURU052: Extension method registration.
  /// </summary>
  public static ImmutableArray<Diagnostic> ValidateExtensionMethods(
    ImmutableArray<ExtensionMethodCall> extensionMethods,
    bool useMicrosoftDependencyInjection)
  {
    if (useMicrosoftDependencyInjection || extensionMethods.IsDefaultOrEmpty)
      return [];

    return [.. extensionMethods.Select(ext =>
      Diagnostic.Create(
        DiagnosticDescriptors.ExtensionMethodRegistration,
        ext.Location,
        ext.MethodName))];
  }

  /// <summary>
  /// Builds a set of registered service type names for fast lookup.
  /// Includes built-in services that are always available.
  /// </summary>
  private static HashSet<string> BuildRegisteredServiceSet(ImmutableArray<ServiceDefinition> services)
  {
    HashSet<string> set = new(StringComparer.Ordinal);

    foreach (ServiceDefinition service in services)
    {
      // Add both with and without global:: prefix for matching
      set.Add(service.ServiceTypeName);
      set.Add(NormalizeTypeName(service.ServiceTypeName));
    }

    // Add built-in services that are always available
    AddBuiltInServices(set);

    return set;
  }

  /// <summary>
  /// Adds built-in services that are always available without explicit registration.
  /// </summary>
  private static void AddBuiltInServices(HashSet<string> set)
  {
    // ITerminal
    set.Add("global::TimeWarp.Terminal.ITerminal");
    set.Add("TimeWarp.Terminal.ITerminal");
    set.Add("ITerminal");

    // IConfiguration
    set.Add("global::Microsoft.Extensions.Configuration.IConfiguration");
    set.Add("Microsoft.Extensions.Configuration.IConfiguration");
    set.Add("IConfiguration");

    // IConfigurationRoot
    set.Add("global::Microsoft.Extensions.Configuration.IConfigurationRoot");
    set.Add("Microsoft.Extensions.Configuration.IConfigurationRoot");
    set.Add("IConfigurationRoot");

    // NuruApp
    set.Add("global::TimeWarp.Nuru.NuruApp");
    set.Add("TimeWarp.Nuru.NuruApp");
    set.Add("NuruApp");

    // CancellationToken (special parameter, always available)
    set.Add("global::System.Threading.CancellationToken");
    set.Add("System.Threading.CancellationToken");
    set.Add("CancellationToken");
  }

  /// <summary>
  /// NURU050: Validates handler service requirements against registered services.
  /// </summary>
  private static void ValidateHandlerServiceRequirements(
    AppModel app,
    HashSet<string> registeredServices,
    List<Diagnostic> diagnostics)
  {
    foreach (RouteDefinition route in app.Routes)
    {
      // Check handler service parameters
      foreach (ParameterBinding param in route.Handler.ServiceParameters)
      {
        ValidateServiceRequirement(param.ParameterTypeName, registeredServices, diagnostics);
      }

      // Check constructor dependencies for endpoint handlers
      foreach (ParameterBinding dep in route.Handler.ConstructorDependencies)
      {
        ValidateServiceRequirement(dep.ParameterTypeName, registeredServices, diagnostics);
      }
    }

    // Also check behavior constructor dependencies
    foreach (BehaviorDefinition behavior in app.Behaviors)
    {
      foreach (ParameterBinding dep in behavior.ConstructorDependencies)
      {
        ValidateServiceRequirement(dep.ParameterTypeName, registeredServices, diagnostics);
      }
    }
  }

  /// <summary>
  /// Validates a single service requirement and adds diagnostic if not registered.
  /// </summary>
  private static void ValidateServiceRequirement(
    string? typeName,
    HashSet<string> registeredServices,
    List<Diagnostic> diagnostics)
  {
    if (typeName is null)
      return;

    // Skip IOptions<T> - validated separately
    if (IsOptionsType(typeName))
      return;

    // Skip ILogger<T> - handled by NURU_H007
    if (IsLoggerType(typeName))
      return;

    if (!IsServiceRegistered(typeName, registeredServices))
    {
      diagnostics.Add(Diagnostic.Create(
        DiagnosticDescriptors.UnregisteredService,
        Location.None,
        GetShortTypeName(typeName)));
    }
  }

  /// <summary>
  /// NURU051: Validates services with constructor dependencies.
  /// </summary>
  private static void ValidateServiceConstructorDependencies(
    ImmutableArray<ServiceDefinition> services,
    HashSet<string> registeredServices,
    List<Diagnostic> diagnostics)
  {
    foreach (ServiceDefinition service in services)
    {
      if (!service.HasConstructorDependencies)
        continue;

      // Check if all constructor dependencies are registered
      List<string> missingDeps = [];
      foreach (string depType in service.ConstructorDependencyTypes)
      {
        if (!IsServiceRegistered(depType, registeredServices))
        {
          // Skip built-in types that are always available
          if (IsBuiltInServiceType(depType))
            continue;

          // Skip ILogger<T> - always available via NullLogger
          if (IsLoggerType(depType))
            continue;

          // Skip IOptions<T> - handled separately
          if (IsOptionsType(depType))
            continue;

          missingDeps.Add(GetShortTypeName(depType));
        }
      }

      if (missingDeps.Count > 0)
      {
        Location location = service.RegistrationLocation ?? Location.None;

        diagnostics.Add(Diagnostic.Create(
          DiagnosticDescriptors.ServiceHasConstructorDependencies,
          location,
          GetShortTypeName(service.ImplementationTypeName),
          string.Join(", ", missingDeps)));
      }
    }
  }

  /// <summary>
  /// NURU053: Validates factory delegate registrations.
  /// </summary>
  private static void ValidateFactoryRegistrations(
    ImmutableArray<ServiceDefinition> services,
    List<Diagnostic> diagnostics)
  {
    foreach (ServiceDefinition service in services)
    {
      if (!service.IsFactoryRegistration)
        continue;

      Location location = service.RegistrationLocation ?? Location.None;

      diagnostics.Add(Diagnostic.Create(
        DiagnosticDescriptors.FactoryDelegateNotSupported,
        location,
        GetShortTypeName(service.ServiceTypeName)));
    }
  }

  /// <summary>
  /// NURU054: Validates type accessibility.
  /// </summary>
  private static void ValidateTypeAccessibility(
    ImmutableArray<ServiceDefinition> services,
    List<Diagnostic> diagnostics)
  {
    foreach (ServiceDefinition service in services)
    {
      if (!service.IsInternalType)
        continue;

      Location location = service.RegistrationLocation ?? Location.None;

      diagnostics.Add(Diagnostic.Create(
        DiagnosticDescriptors.InternalTypeNotAccessible,
        location,
        GetShortTypeName(service.ImplementationTypeName)));
    }
  }

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
  /// Checks if a type name is ILogger&lt;T&gt; or ILogger.
  /// </summary>
  private static bool IsLoggerType(string typeName)
  {
    return typeName.Contains("ILogger", StringComparison.Ordinal);
  }

  /// <summary>
  /// Checks if a type is a built-in service type.
  /// </summary>
  private static bool IsBuiltInServiceType(string typeName)
  {
    string normalized = NormalizeTypeName(typeName);
    return normalized.StartsWith("Microsoft.Extensions.Configuration.", StringComparison.Ordinal)
        || normalized.StartsWith("Microsoft.Extensions.Logging.", StringComparison.Ordinal)
        || normalized.StartsWith("TimeWarp.Terminal.", StringComparison.Ordinal)
        || normalized.StartsWith("TimeWarp.Nuru.NuruApp", StringComparison.Ordinal)
        || normalized == "System.Threading.CancellationToken";
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
      int lastDot = beforeGeneric.LastIndexOf('.');
      return lastDot >= 0 ? beforeGeneric[(lastDot + 1)..] + genericPart : beforeGeneric + genericPart;
    }

    int lastDotIndex = normalized.LastIndexOf('.');
    return lastDotIndex >= 0 ? normalized[(lastDotIndex + 1)..] : normalized;
  }
}
