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
      sb.AppendLine(
        $"{indent}{typeName} {varName} = configuration;");
      return;
    }

    // Special case: ITerminal uses app.Terminal (built-in service)
    if (IsTerminalType(typeName))
    {
      sb.AppendLine(
        $"{indent}{typeName} {varName} = app.Terminal;");
      return;
    }

    // Special case: IOptions<T> - bind from configuration
    // The configuration key is stored in SourceName (from handler extraction)
    if (TryGetOptionsInnerType(typeName, out string? innerTypeName))
    {
      // Use configuration key from binding (set during handler extraction with attribute/convention)
      string configurationKey = param.SourceName != typeName ? param.SourceName : GetConfigurationSectionKey(innerTypeName!);
      EmitOptionsBinding(sb, typeName, innerTypeName!, varName, configurationKey, indent);
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
        sb.AppendLine(
          $"{indent}{typeName} {varName} = new {service.ImplementationTypeName}();");
      }
      else
      {
        // Singleton/Scoped - thread-safe cached via Lazy<T>
        string fieldName = InterceptorEmitter.GetServiceFieldName(service.ImplementationTypeName);
        sb.AppendLine(
          $"{indent}{typeName} {varName} = {fieldName}.Value;");
      }

      return;
    }

    // Service not found - emit error comment
    sb.AppendLine(
      $"{indent}// ERROR: Service {typeName} not registered. Use ConfigureServices to register it.");
    sb.AppendLine(
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

  /// <summary>
  /// Checks if a type name is IOptions&lt;T&gt; and extracts the inner type name.
  /// </summary>
  /// <param name="typeName">The full type name to check.</param>
  /// <param name="innerTypeName">The inner type name if it's an IOptions type.</param>
  /// <returns>True if the type is IOptions&lt;T&gt;, false otherwise.</returns>
  private static bool TryGetOptionsInnerType(string typeName, out string? innerTypeName)
  {
    innerTypeName = null;

    // Match patterns like:
    // - global::Microsoft.Extensions.Options.IOptions<global::DatabaseOptions>
    // - Microsoft.Extensions.Options.IOptions<DatabaseOptions>
    // - IOptions<DatabaseOptions>
    string[] prefixes =
    [
      "global::Microsoft.Extensions.Options.IOptions<",
      "Microsoft.Extensions.Options.IOptions<",
      "IOptions<"
    ];

    foreach (string prefix in prefixes)
    {
      if (typeName.StartsWith(prefix, StringComparison.Ordinal) && typeName.EndsWith('>'))
      {
        innerTypeName = typeName[prefix.Length..^1];
        return true;
      }
    }

    return false;
  }

  /// <summary>
  /// Emits code to bind IOptions&lt;T&gt; from configuration.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="optionsTypeName">The full IOptions&lt;T&gt; type name.</param>
  /// <param name="innerTypeName">The inner options class type name.</param>
  /// <param name="varName">The variable name for the parameter.</param>
  /// <param name="sectionKey">The configuration section key (from [ConfigurationKey] or convention).</param>
  /// <param name="indent">The indentation string.</param>
  private static void EmitOptionsBinding(StringBuilder sb, string optionsTypeName, string innerTypeName, string varName, string sectionKey, string indent)
  {
    string valueVarName = $"__{varName}Value";

    sb.AppendLine($"{indent}// Resolve {optionsTypeName} from configuration section \"{sectionKey}\"");
    sb.AppendLine($"{indent}{innerTypeName} {valueVarName} = configuration.GetSection(\"{sectionKey}\").Get<{innerTypeName}>() ?? new();");
    sb.AppendLine($"{indent}{optionsTypeName} {varName} = global::Microsoft.Extensions.Options.Options.Create({valueVarName});");
  }

  /// <summary>
  /// Gets the configuration section key for an options class.
  /// Convention: strip "Options" suffix from class name.
  /// </summary>
  /// <param name="innerTypeName">The fully qualified options class name.</param>
  /// <returns>The configuration section key.</returns>
  private static string GetConfigurationSectionKey(string innerTypeName)
  {
    // Extract just the class name (without namespace/global::)
    string className = innerTypeName;

    // Remove global:: prefix if present
    if (className.StartsWith("global::", StringComparison.Ordinal))
      className = className[8..];

    // Get just the class name (after last dot)
    int lastDot = className.LastIndexOf('.');
    if (lastDot >= 0)
      className = className[(lastDot + 1)..];

    // Strip "Options" suffix if present
    const string optionsSuffix = "Options";
    if (className.EndsWith(optionsSuffix, StringComparison.Ordinal) && className.Length > optionsSuffix.Length)
    {
      return className[..^optionsSuffix.Length];
    }

    return className;
  }
}
