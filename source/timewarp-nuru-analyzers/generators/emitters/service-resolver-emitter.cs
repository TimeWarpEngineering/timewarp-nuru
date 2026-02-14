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
  /// <param name="useRuntimeDI">When true, emits GetServiceProvider().GetRequiredService&lt;T&gt;() instead of static instantiation.</param>
  /// <param name="runtimeDISuffix">Suffix for per-app runtime DI methods (e.g., "_0" for multi-app assemblies).</param>
  public static void Emit(StringBuilder sb, HandlerDefinition handler, ImmutableArray<ServiceDefinition> services, int indent = 6, bool useRuntimeDI = false, string runtimeDISuffix = "")
  {
    string indentStr = new(' ', indent);

    foreach (ParameterBinding param in handler.ServiceParameters)
    {
      EmitServiceResolution(sb, param, services, indentStr, useRuntimeDI, runtimeDISuffix);
    }
  }

  /// <summary>
  /// Emits a single service resolution from a parameter binding.
  /// </summary>
  private static void EmitServiceResolution(StringBuilder sb, ParameterBinding param, ImmutableArray<ServiceDefinition> services, string indent, bool useRuntimeDI, string runtimeDISuffix)
  {
    string typeName = param.ParameterTypeName;
    string varName = CSharpIdentifierUtils.EscapeIfKeyword(param.ParameterName);

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

    // Special case: NuruApp - the app instance is directly available
    // Enables handlers to invoke other routes via app.RunAsync(["command"])
    if (IsNuruAppType(typeName))
    {
      sb.AppendLine(
        $"{indent}{typeName} {varName} = app;");
      return;
    }

    // Special case: IOptions<T> - bind from configuration
    // The configuration key is stored in SourceName (from handler extraction)
    if (TryGetOptionsInnerType(typeName, out string? innerTypeName))
    {
      // Use configuration key from binding (set during handler extraction with attribute/convention)
      string configurationKey = param.SourceName != typeName ? param.SourceName : GetConfigurationSectionKey(innerTypeName!);
      EmitOptionsBinding(sb, typeName, innerTypeName!, varName, configurationKey, param.ValidatorTypeName, indent);
      return;
    }

    // Runtime DI path: use GetServiceProvider{suffix}(app).GetRequiredService<T>()
    if (useRuntimeDI)
    {
      sb.AppendLine(
        $"{indent}{typeName} {varName} = global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<{typeName}>(GetServiceProvider{runtimeDISuffix}(app));");
      return;
    }

    // Source-gen DI path: static instantiation or Lazy<T> fields
    // Look up service in registered services
    ServiceDefinition? service = FindService(typeName, services);
    if (service is not null)
    {
      if (service.Lifetime is ServiceLifetime.Singleton or ServiceLifetime.Scoped)
      {
        // Singleton/Scoped: use Lazy<T> field (generated with constructor args if needed)
        string fieldName = InterceptorEmitter.GetServiceFieldName(service.ImplementationTypeName);
        sb.AppendLine(
          $"{indent}{typeName} {varName} = {fieldName}.Value;");
      }
      else if (service.HasConstructorDependencies)
      {
        // Transient with constructor deps: inline new T(resolvedDeps...)
        string args = ResolveConstructorArguments(service, services);
        sb.AppendLine(
          $"{indent}{typeName} {varName} = new {service.ImplementationTypeName}({args});");
      }
      else
      {
        // Transient without deps: new instance each time
        sb.AppendLine(
          $"{indent}{typeName} {varName} = new {service.ImplementationTypeName}();");
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
  /// Checks if a type name is NuruApp (built-in, available as the app parameter).
  /// Enables handlers to invoke other routes programmatically via app.RunAsync().
  /// </summary>
  private static bool IsNuruAppType(string typeName)
  {
    return typeName is "global::TimeWarp.Nuru.NuruApp"
        or "TimeWarp.Nuru.NuruApp"
        or "NuruApp";
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
  /// Emits code to bind IOptions&lt;T&gt; from configuration with optional validation.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="optionsTypeName">The full IOptions&lt;T&gt; type name.</param>
  /// <param name="innerTypeName">The inner options class type name.</param>
  /// <param name="varName">The variable name for the parameter.</param>
  /// <param name="sectionKey">The configuration section key (from [ConfigurationKey] or convention).</param>
  /// <param name="validatorTypeName">Optional validator type implementing IValidateOptions&lt;T&gt;.</param>
  /// <param name="indent">The indentation string.</param>
  private static void EmitOptionsBinding(StringBuilder sb, string optionsTypeName, string innerTypeName, string varName, string sectionKey, string? validatorTypeName, string indent)
  {
    string valueVarName = $"__{varName}Value";

    sb.AppendLine($"{indent}// Resolve {optionsTypeName} from configuration section \"{sectionKey}\"");
    sb.AppendLine($"{indent}{innerTypeName} {valueVarName} = configuration.GetSection(\"{sectionKey}\").Get<{innerTypeName}>() ?? new();");

    // Emit validation if a validator was found
    if (validatorTypeName is not null)
    {
      string validatorVarName = $"__{varName}Validator";
      string resultVarName = $"__{varName}ValidationResult";

      sb.AppendLine();
      sb.AppendLine($"{indent}// Validate options using {validatorTypeName}");
      sb.AppendLine($"{indent}var {validatorVarName} = new {validatorTypeName}();");
      sb.AppendLine($"{indent}global::Microsoft.Extensions.Options.ValidateOptionsResult {resultVarName} = {validatorVarName}.Validate(null, {valueVarName});");
      sb.AppendLine($"{indent}if ({resultVarName}.Failed)");
      sb.AppendLine($"{indent}{{");
      sb.AppendLine($"{indent}  throw new global::Microsoft.Extensions.Options.OptionsValidationException(");
      sb.AppendLine($"{indent}    \"{GetShortTypeName(innerTypeName)}\",");
      sb.AppendLine($"{indent}    typeof({innerTypeName}),");
      sb.AppendLine($"{indent}    {resultVarName}.Failures ?? [{resultVarName}.FailureMessage ?? \"Validation failed\"]);");
      sb.AppendLine($"{indent}}}");
      sb.AppendLine();
    }

    sb.AppendLine($"{indent}{optionsTypeName} {varName} = global::Microsoft.Extensions.Options.Options.Create({valueVarName});");
  }

  /// <summary>
  /// Gets the short type name (without namespace) for display purposes.
  /// </summary>
  private static string GetShortTypeName(string fullTypeName)
  {
    string typeName = fullTypeName;

    // Remove global:: prefix if present
    if (typeName.StartsWith("global::", StringComparison.Ordinal))
      typeName = typeName[8..];

    // Get just the type name (after last dot)
    int lastDot = typeName.LastIndexOf('.');
    if (lastDot >= 0)
      typeName = typeName[(lastDot + 1)..];

    return typeName;
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

  /// <summary>
  /// Resolves all constructor arguments for a service to their compile-time expressions.
  /// </summary>
  internal static string ResolveConstructorArguments(ServiceDefinition service, ImmutableArray<ServiceDefinition> services)
  {
    if (service.ConstructorDependencyTypes.IsDefaultOrEmpty)
      return "";

    return string.Join(", ", service.ConstructorDependencyTypes.Select(dep => ResolveDepExpression(dep, services)));
  }

  /// <summary>
  /// Resolves a single dependency type to its compile-time expression.
  /// Maps built-in types to their known sources and registered services to their instantiation.
  /// </summary>
  private static string ResolveDepExpression(string depType, ImmutableArray<ServiceDefinition> services)
  {
    // Built-in: IConfiguration / IConfigurationRoot
    if (IsConfigurationType(depType))
      return "configuration";

    // Built-in: ITerminal
    if (IsTerminalType(depType))
      return "app.Terminal";

    // Built-in: NuruApp
    if (IsNuruAppType(depType))
      return "app";

    // Built-in: ILogger<T> - use NullLogger when no logging configured
    if (depType.Contains("ILogger", StringComparison.Ordinal))
    {
      int start = depType.IndexOf('<', StringComparison.Ordinal);
      int end = depType.LastIndexOf('>');
      string typeArg = start >= 0 && end > start ? depType[(start + 1)..end] : "object";
      return $"global::Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance.CreateLogger<{typeArg}>()";
    }

    // Registered service - resolve by looking up in registered services
    ServiceDefinition? depService = FindService(depType, services);
    if (depService is not null)
    {
      // Singleton/Scoped: use Lazy<T> field (generated with constructor args if needed)
      if (depService.Lifetime is ServiceLifetime.Singleton or ServiceLifetime.Scoped)
      {
        string fieldName = InterceptorEmitter.GetServiceFieldName(depService.ImplementationTypeName);
        return $"{fieldName}.Value";
      }

      // Transient with constructor deps: inline new T(resolvedDeps...)
      if (depService.HasConstructorDependencies)
      {
        string innerArgs = ResolveConstructorArguments(depService, services);
        return $"new {depService.ImplementationTypeName}({innerArgs})";
      }

      // Transient without deps: new instance
      return $"new {depService.ImplementationTypeName}()";
    }

    // Unresolvable - NURU051 validator will report the error
    return $"default! /* ERROR: Cannot resolve {depType} */";
  }
}
