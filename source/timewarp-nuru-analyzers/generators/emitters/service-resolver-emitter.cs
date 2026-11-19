#region Purpose
// Emits service resolution code for handler parameters.
// Generates instantiation code for registered services (static DI).
// Supports constructor parameters with optional default values.
#endregion
#region Design
// IOptions<T> is not a container service under source-generated DI. Delegate parameters and
// endpoint Handler constructors share EmitOptionsBinding: section key from the binding
// (or the type name minus a trailing Options), then Options.Create. A found IValidateOptions<T>
// runs before the value is wrapped. The configuration expression is the route's configuration
// local, or an IConfiguration resolved from a service provider inside the source-gen mediator
// container. IOptionsSnapshot<T> and IOptionsMonitor<T> are not this path.
#endregion

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
  /// <param name="loggerFactoryFieldName">Logger factory source (static field name or "app.LoggerFactory"), or null if no logging.</param>
  /// <param name="httpClientConfigurations">Typed clients registered with AddHttpClient.</param>
  public static void Emit(StringBuilder sb, HandlerDefinition handler, ImmutableArray<ServiceDefinition> services, int indent = 6, bool useRuntimeDI = false, string runtimeDISuffix = "", string? loggerFactoryFieldName = null, ImmutableArray<HttpClientConfiguration> httpClientConfigurations = default)
  {
    string indentStr = new(' ', indent);

    foreach (ParameterBinding param in handler.ServiceParameters)
    {
      EmitServiceResolution(sb, param, services, httpClientConfigurations, loggerFactoryFieldName, indentStr, useRuntimeDI, runtimeDISuffix);
    }
  }

  /// <summary>
  /// Emits a single service resolution from a parameter binding.
  /// </summary>
  private static void EmitServiceResolution(StringBuilder sb, ParameterBinding param, ImmutableArray<ServiceDefinition> services, ImmutableArray<HttpClientConfiguration> httpClientConfigurations, string? loggerFactoryFieldName, string indent, bool useRuntimeDI, string runtimeDISuffix)
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

    // Special case: ITerminal uses framework service field
    if (IsTerminalType(typeName))
    {
      sb.AppendLine(
        $"{indent}{typeName} {varName} = __fw_ITerminal;");
      return;
    }

    // Special case: NuruApp - use framework service field
    // Enables handlers to invoke other routes via app.RunAsync(["command"])
    if (IsNuruAppType(typeName))
    {
      sb.AppendLine(
        $"{indent}{typeName} {varName} = __fw_NuruApp;");
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

    string expression = ResolveServiceExpression(typeName, services, httpClientConfigurations, loggerFactoryFieldName, useRuntimeDI, runtimeDISuffix);
    sb.AppendLine(
      $"{indent}{typeName} {varName} = {expression};");
  }

  /// <summary>
  /// Resolves a handler service to its expression. Delegate handler parameters and endpoint handler
  /// constructors share this, so a type resolves the same way at either injection site.
  /// Built-in services (ITerminal, IConfiguration, NuruApp) are resolved by the caller first.
  /// </summary>
  /// <returns>
  /// The expression, or <c>default! /* ERROR: Service T not registered */</c> when nothing provides
  /// the type. Source-gen DI reports NURU050 for that case.
  /// </returns>
  internal static string ResolveServiceExpression
  (
    string typeName,
    ImmutableArray<ServiceDefinition> services,
    ImmutableArray<HttpClientConfiguration> httpClientConfigurations,
    string? loggerFactoryFieldName,
    bool useRuntimeDI,
    string runtimeDISuffix
  )
  {
    // ILogger<T>, ILogger, ILoggerFactory come from the app's logger factory, also under runtime DI
    if (LoggerExpressions.IsLoggingServiceType(typeName))
      return LoggerExpressions.Resolve(typeName, loggerFactoryFieldName);

    // Runtime DI path: use the command scope when one is active.
    // Session-scoped services stay on the root provider so a command scope does not dispose them.
    if (useRuntimeDI)
    {
      ServiceDefinition? runtimeService = FindService(typeName, services);
      string providerMethod = runtimeService?.Lifetime == ServiceLifetime.SessionScoped
        ? $"GetServiceProvider{runtimeDISuffix}"
        : $"GetResolutionProvider{runtimeDISuffix}";
      return $"global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<{typeName}>({providerMethod}(app))";
    }

    // Source-gen DI path: ISender / IPublisher / IMediator come from the generated mediator
    if (FrameworkServices.IsMediatorServiceType(typeName))
      return FrameworkServices.MediatorExpression;

    // Source-gen DI path: static instantiation or static fields
    ServiceDefinition? service = FindService(typeName, services);
    if (service is not null)
    {
      return service.Lifetime == ServiceLifetime.Transient
        ? ServiceScopeExpressions.Construct(service, services, ServiceResolveMode.Command)
        : ServiceScopeExpressions.Read(service, services, ServiceResolveMode.Command);
    }

    // Typed HttpClient service: construct the implementation with its static HttpClient field
    if (!httpClientConfigurations.IsDefaultOrEmpty)
    {
      string normalizedTypeName = NormalizeTypeName(typeName);
      HttpClientConfiguration? httpClientConfig = httpClientConfigurations.FirstOrDefault(c =>
        c.ServiceTypeName is not null && NormalizeTypeName(c.ServiceTypeName) == normalizedTypeName);

      if (httpClientConfig?.ImplementationTypeName is not null)
      {
        string httpClientField = InterceptorEmitter.GetHttpClientFieldName(httpClientConfig.ServiceTypeName!);
        return $"new {httpClientConfig.ImplementationTypeName}({httpClientField})";
      }
    }

    return $"default! /* ERROR: Service {typeName} not registered */";
  }

  /// <summary>
  /// Finds a service definition by its service type name.
  /// </summary>
  private static ServiceDefinition? FindService(string typeName, ImmutableArray<ServiceDefinition> services)
  {
    // Try exact match first
    foreach (ServiceDefinition service in services)
    {
      if (service.IsInternalType)
        continue;
      if (service.ServiceTypeName == typeName)
        return service;
    }

    // Try matching without global:: prefix
    string normalizedTypeName = NormalizeTypeName(typeName);
    foreach (ServiceDefinition service in services)
    {
      if (service.IsInternalType)
        continue;
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
  /// True when <paramref name="binding"/> is <c>IOptions&lt;T&gt;</c> and this emitter can bind it
  /// from configuration. Writes the inner type and the section key.
  /// </summary>
  internal static bool TryDescribeBoundOptions(
    ParameterBinding binding,
    out string innerTypeName,
    out string sectionKey)
  {
    innerTypeName = "";
    sectionKey = "";
    if (!TryGetOptionsInnerType(binding.ParameterTypeName, out string? inner) || inner is null)
      return false;

    innerTypeName = inner;
    sectionKey = binding.SourceName != binding.ParameterTypeName
      ? binding.SourceName
      : GetConfigurationSectionKey(inner);
    return true;
  }

  /// <summary>
  /// Emits the configuration binding for an endpoint <c>Handler</c> constructor dependency
  /// into <c>__{parameter name}</c>. Returns false when the dependency is not <c>IOptions&lt;T&gt;</c>.
  /// The route method's <c>configuration</c> local must be in scope.
  /// </summary>
  internal static bool TryEmitConstructorOptions(StringBuilder sb, ParameterBinding binding, string indent)
  {
    if (!TryDescribeBoundOptions(binding, out string innerTypeName, out string sectionKey))
      return false;

    EmitOptionsBinding(
      sb,
      binding.ParameterTypeName,
      innerTypeName,
      $"__{binding.ParameterName}",
      sectionKey,
      binding.ValidatorTypeName,
      indent);
    return true;
  }

  /// <summary>
  /// Checks if a type name is IOptions&lt;T&gt; and extracts the inner type name.
  /// </summary>
  /// <param name="typeName">The full type name to check.</param>
  /// <param name="innerTypeName">The inner type name if it's an IOptions type.</param>
  /// <returns>True if the type is IOptions&lt;T&gt;, false otherwise.</returns>
  internal static bool TryGetOptionsInnerType(string typeName, out string? innerTypeName)
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
      if (typeName.StartsWith(prefix, StringComparison.Ordinal) && typeName.EndsWith('>', StringComparison.Ordinal))
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
  internal static void EmitOptionsBinding(
    StringBuilder sb,
    string optionsTypeName,
    string innerTypeName,
    string varName,
    string sectionKey,
    string? validatorTypeName,
    string indent,
    string configurationExpression = "configuration")
  {
    // A constructor local is already prefixed (__name). A delegate parameter is not (name),
    // and its value local keeps the historical __nameValue spelling.
    // A keyword parameter arrives as @type. Suffixing that token yields @typeValue (task 591-006).
    string compoundName = CSharpIdentifierUtils.Bare(varName);
    string valueVarName = compoundName.StartsWith("__", StringComparison.Ordinal)
      ? $"{compoundName}Value"
      : $"__{compoundName}Value";

    sb.AppendLine($"{indent}// Resolve {optionsTypeName} from configuration section \"{sectionKey}\"");
    sb.AppendLine($"{indent}{innerTypeName} {valueVarName} = {configurationExpression}.GetSection(\"{sectionKey}\").Get<{innerTypeName}>() ?? new();");

    // Emit validation if a validator was found
    if (validatorTypeName is not null)
    {
      string validatorVarName = $"__{compoundName}Validator";
      string resultVarName = $"__{compoundName}ValidationResult";

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
    int lastDot = typeName.LastIndexOf('.', StringComparison.Ordinal);
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
    int lastDot = className.LastIndexOf('.', StringComparison.Ordinal);
    if (lastDot >= 0)
      className = className[(lastDot + 1)..];

    // Strip "Options" suffix if present
    const string OptionsSuffix = "Options";
    if (className.EndsWith(OptionsSuffix, StringComparison.Ordinal) && className.Length > OptionsSuffix.Length)
    {
      return className[..^OptionsSuffix.Length];
    }

    return className;
  }

  /// <summary>
  /// Resolves all constructor arguments for a service to their compile-time expressions.
  /// Uses ConstructorParameters if available for detailed resolution including optional params.
  /// </summary>
  internal static string ResolveConstructorArguments
  (
    ServiceDefinition service,
    ImmutableArray<ServiceDefinition> services,
    ServiceResolveMode mode = ServiceResolveMode.Command
  )
  {
    // Prefer ConstructorParameters if available (has detailed info)
    if (!service.ConstructorParameters.IsDefaultOrEmpty && service.ConstructorParameters.Length > 0)
    {
      return string.Join(", ", service.ConstructorParameters
        .Select(param => ResolveParameterExpression(param, services, mode)));
    }

    // Fallback to ConstructorDependencyTypes
    if (service.ConstructorDependencyTypes.IsDefaultOrEmpty)
      return "";

    return string.Join(", ", service.ConstructorDependencyTypes.Select(dep => ResolveDepExpression(dep, services, mode)));
  }

  /// <summary>
  /// Resolves a single constructor parameter to its compile-time expression.
  /// Handles optional parameters with default values.
  /// </summary>
  private static string ResolveParameterExpression(ConstructorParameter param, ImmutableArray<ServiceDefinition> services, ServiceResolveMode mode)
  {
    // Framework service types (ITerminal, IConfiguration, NuruApp, etc.)
    if (FrameworkServices.IsFrameworkServiceType(param.TypeName))
    {
      return FrameworkServices.GetFieldName(param.TypeName);
    }

    if (FrameworkServices.IsMediatorServiceType(param.TypeName))
      return FrameworkServices.MediatorExpression;

    // Check if this is a registered service
    ServiceDefinition? depService = FindService(param.TypeName, services);
    if (depService is not null)
    {
      if (depService.Lifetime == ServiceLifetime.Transient)
        return ServiceScopeExpressions.Construct(depService, services, mode);

      return ServiceScopeExpressions.Read(depService, services, mode);
    }

    // Optional parameter with default value - use the default
    if (param.HasDefaultValue && param.DefaultValue is not null)
    {
      return param.DefaultValue;
    }

    // Unresolvable - NURU051 validator will report the error
    return $"default! /* ERROR: Cannot resolve {param.TypeName} */";
  }

  /// <summary>
  /// Resolves a single dependency type to its compile-time expression.
  /// Maps framework types to their known sources and registered services to their instantiation.
  /// </summary>
  private static string ResolveDepExpression(string depType, ImmutableArray<ServiceDefinition> services, ServiceResolveMode mode)
  {
    // Framework service types (ITerminal, IConfiguration, NuruApp, etc.)
    if (FrameworkServices.IsFrameworkServiceType(depType))
    {
      return FrameworkServices.GetFieldName(depType);
    }

    if (FrameworkServices.IsMediatorServiceType(depType))
      return FrameworkServices.MediatorExpression;

    // Registered service - resolve by looking up in registered services
    ServiceDefinition? depService = FindService(depType, services);
    if (depService is not null)
    {
      if (depService.Lifetime == ServiceLifetime.Transient)
        return ServiceScopeExpressions.Construct(depService, services, mode);

      return ServiceScopeExpressions.Read(depService, services, mode);
    }

    // Unresolvable - NURU051 validator will report the error
    return $"default! /* ERROR: Cannot resolve {depType} */";
  }
}
