namespace TimeWarp.Nuru;

/// <summary>
/// Diagnostic descriptors for service/DI validation errors.
/// These are reported when source-gen DI cannot handle a registration pattern.
/// All validation is skipped when UseMicrosoftDependencyInjection is enabled.
/// </summary>
internal static partial class DiagnosticDescriptors
{
  internal const string ServiceCategory = "Service.Validation";

  /// <summary>
  /// NURU050: Handler requires an unregistered service.
  /// </summary>
  public static readonly DiagnosticDescriptor UnregisteredService = new(
    id: "NURU050",
    title: "Handler requires unregistered service",
    messageFormat: "Handler requires service '{0}' but it is not registered in ConfigureServices. Register it or use .UseMicrosoftDependencyInjection() for runtime DI.",
    category: ServiceCategory,
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Source-generated DI requires all services to be explicitly registered. Either add the registration or opt into runtime DI with .UseMicrosoftDependencyInjection().");

  /// <summary>
  /// NURU051: Service implementation has constructor dependencies.
  /// </summary>
  public static readonly DiagnosticDescriptor ServiceHasConstructorDependencies = new(
    id: "NURU051",
    title: "Service has constructor dependencies",
    messageFormat: "Service '{0}' has constructor dependencies ({1}). Register these dependencies or use .UseMicrosoftDependencyInjection() for automatic resolution.",
    category: ServiceCategory,
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Source-generated DI uses static instantiation (new T()). Services with constructor parameters require either explicit dependency registration or runtime DI with .UseMicrosoftDependencyInjection().");

  /// <summary>
  /// NURU052: Extension method registration detected (opaque to source-gen).
  /// </summary>
  public static readonly DiagnosticDescriptor ExtensionMethodRegistration = new(
    id: "NURU052",
    title: "Extension method registration not analyzable",
    messageFormat: "Cannot analyze registrations inside '{0}()'. Services registered there may not be visible to source-gen DI. Consider using .UseMicrosoftDependencyInjection().",
    category: ServiceCategory,
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true,
    description: "Extension methods like AddLogging() or AddHttpClient() register services internally. Source-gen DI cannot see these registrations. Consider using .UseMicrosoftDependencyInjection() for full runtime DI support.");

  /// <summary>
  /// NURU053: Factory delegate registration not supported.
  /// </summary>
  public static readonly DiagnosticDescriptor FactoryDelegateNotSupported = new(
    id: "NURU053",
    title: "Factory delegate registration not supported",
    messageFormat: "Service '{0}' uses factory delegate registration. Use .UseMicrosoftDependencyInjection() for factory support or register with type mapping.",
    category: ServiceCategory,
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Source-generated DI cannot execute factory delegates at compile time. Use .UseMicrosoftDependencyInjection() for runtime DI with factory-based registrations.");

  /// <summary>
  /// NURU054: Internal type not accessible from generated code.
  /// </summary>
  public static readonly DiagnosticDescriptor InternalTypeNotAccessible = new(
    id: "NURU054",
    title: "Internal type not accessible",
    messageFormat: "Cannot instantiate internal type '{0}' from generated code. Make it public, add [InternalsVisibleTo], or use .UseMicrosoftDependencyInjection().",
    category: ServiceCategory,
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Generated code runs in a different assembly context and cannot access internal types without [InternalsVisibleTo]. Either expose the type publicly or use .UseMicrosoftDependencyInjection() for runtime DI.");
}
