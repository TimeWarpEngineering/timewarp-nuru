// Emits service resolution code for handler parameters.
// Generates instantiation code for registered services (static DI).

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits code to resolve services for handler parameters.
/// Uses static instantiation (new T()) instead of runtime DI container.
/// Built-in services (ITerminal, IConfiguration) are wired to app properties.
/// </summary>
internal static class ServiceResolverEmitter
{
  /// <summary>
  /// Emits service resolution code for all service parameters in a handler.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="handler">The handler definition containing service parameters.</param>
  /// <param name="services">Registered services from ConfigureServices.</param>
  /// <param name="indent">Number of spaces for indentation.</param>
  public static void Emit(StringBuilder sb, HandlerDefinition handler, ImmutableArray<ServiceDefinition> services, int indent = 6)
  {
    string indentStr = new(' ', indent);

    foreach (ParameterBinding param in handler.ServiceParameters)
    {
      EmitServiceResolution(sb, param, services, indentStr);
    }
  }

  /// <summary>
  /// Emits a single service resolution from a parameter binding.
  /// </summary>
  private static void EmitServiceResolution(StringBuilder sb, ParameterBinding param, ImmutableArray<ServiceDefinition> services, string indent)
  {
    string typeName = param.ParameterTypeName;
    string varName = param.ParameterName;

    // Special case: IConfiguration and IConfigurationRoot use the local configuration variable
    // (built by ConfigurationEmitter when AddConfiguration() is called)
    if (IsConfigurationType(typeName))
    {
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"{indent}{typeName} {varName} = configuration;");
      return;
    }

    // Special case: ITerminal uses app.Terminal (built-in service)
    if (IsTerminalType(typeName))
    {
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"{indent}{typeName} {varName} = app.Terminal;");
      return;
    }

    // Look up service in registered services
    ServiceDefinition? service = FindService(typeName, services);
    if (service is not null)
    {
      // Found registered service - emit instantiation based on lifetime
      // Note: Phase 4 will add constructor dependency resolution for services with dependencies.
      if (service.Lifetime == ServiceLifetime.Transient)
      {
        // Transient - new instance each time
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"{indent}{typeName} {varName} = new {service.ImplementationTypeName}();");
      }
      else
      {
        // Singleton/Scoped - thread-safe cached via Lazy<T>
        string fieldName = InterceptorEmitter.GetServiceFieldName(service.ImplementationTypeName);
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"{indent}{typeName} {varName} = {fieldName}.Value;");
      }

      return;
    }

    // Service not found - emit error comment
    sb.AppendLine(CultureInfo.InvariantCulture,
      $"{indent}// ERROR: Service {typeName} not registered. Use ConfigureServices to register it.");
    sb.AppendLine(CultureInfo.InvariantCulture,
      $"{indent}{typeName} {varName} = default!; // Service not registered");
  }

  /// <summary>
  /// Finds a service definition by its service type name.
  /// </summary>
  private static ServiceDefinition? FindService(string typeName, ImmutableArray<ServiceDefinition> services)
  {
    // Try exact match first
    foreach (ServiceDefinition service in services)
    {
      if (service.ServiceTypeName == typeName)
        return service;
    }

    // Try matching without global:: prefix
    string normalizedTypeName = NormalizeTypeName(typeName);
    foreach (ServiceDefinition service in services)
    {
      if (NormalizeTypeName(service.ServiceTypeName) == normalizedTypeName)
        return service;
    }

    return null;
  }

  /// <summary>
  /// Normalizes a type name by removing the global:: prefix.
  /// </summary>
  private static string NormalizeTypeName(string typeName)
  {
    return typeName.StartsWith("global::", StringComparison.Ordinal)
      ? typeName[8..]
      : typeName;
  }

  /// <summary>
  /// Checks if a type name is IConfiguration or IConfigurationRoot.
  /// </summary>
  private static bool IsConfigurationType(string typeName)
  {
    return typeName is "global::Microsoft.Extensions.Configuration.IConfiguration"
        or "global::Microsoft.Extensions.Configuration.IConfigurationRoot"
        or "Microsoft.Extensions.Configuration.IConfiguration"
        or "Microsoft.Extensions.Configuration.IConfigurationRoot"
        or "IConfiguration"
        or "IConfigurationRoot";
  }

  /// <summary>
  /// Checks if a type name is ITerminal (built-in service available via app.Terminal).
  /// </summary>
  private static bool IsTerminalType(string typeName)
  {
    return typeName is "global::TimeWarp.Terminal.ITerminal"
        or "TimeWarp.Terminal.ITerminal"
        or "ITerminal";
  }
}
