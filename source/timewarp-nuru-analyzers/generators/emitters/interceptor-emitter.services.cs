#region Purpose
// InterceptorEmitter partial: service fields, source-gen mediator and service initialization emission.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

internal static partial class InterceptorEmitter
{
  /// <summary>
  /// Emits static nullable fields for Singleton and Scoped services.
  /// Each field is built on first read by its <c>__Resolve</c> accessor, so a call that never
  /// reaches a service (a built-in, a parse error, a forwarded route) never constructs it.
  /// </summary>
  private static void EmitServiceFields(StringBuilder sb, IEnumerable<ServiceDefinition> services, bool requiresMediator, string? generatedMediatorNamespace, string? loggerFactoryFieldName, ImmutableArray<HttpClientConfiguration> httpClientConfigurations, ImmutableArray<BehaviorDefinition> behaviors, ImmutableArray<ServiceDefinition> behaviorServices)
  {
    // Materialize to ImmutableArray for ResolveConstructorArguments compatibility
    ImmutableArray<ServiceDefinition> allServices = [.. services];

    // Collect framework service types needed by ALL services (including Transient)
    // Transient services are instantiated inline but still need framework service fields
    HashSet<string> frameworkServiceTypes = CollectFrameworkServiceTypes(allServices);

    // Filter to process-cached services (Singleton and non-REPL Scoped) and remove duplicates
    ServiceDefinition[] processServices =
    [
      .. allServices
        .Where(static service => service.Lifetime is ServiceLifetime.Singleton or ServiceLifetime.Scoped)
        .DistinctBy(static service => service.ImplementationTypeName)
    ];

    ServiceDefinition[] sessionServices =
    [
      .. allServices
        .Where(static service => service.Lifetime == ServiceLifetime.SessionScoped)
        .DistinctBy(static service => service.ImplementationTypeName)
    ];

    ServiceDefinition[] commandServices =
    [
      .. allServices
        .Where(static service => service.Lifetime == ServiceLifetime.CommandScoped)
        .DistinctBy(static service => service.ImplementationTypeName)
    ];

    // Sort services topologically (dependencies first)
    ImmutableArray<ServiceDefinition> sortedProcessServices =
      DependencyGraphBuilder.TopologicalSort([.. processServices]);

    // A Scoped service that injects a session or command service has no process field:
    // it lives in the run's command slot (508-007 review M1).
    ImmutableArray<ServiceDefinition> cachedProcessServices =
    [
      .. sortedProcessServices.Where(service => !ServiceScopeExpressions.IsPerRun(service, allServices))
    ];

    sb.AppendLine("  // Static service fields, built on first read by their __Resolve accessors");

    // Emit framework service fields (always needed)
    sb.AppendLine("  private static global::TimeWarp.Terminal.ITerminal? __fw_ITerminal;");
    sb.AppendLine("  private static global::TimeWarp.Nuru.NuruApp? __fw_NuruApp;");

    // Emit additional framework service fields needed by services
    HashSet<string> frameworkFieldNames = new(StringComparer.Ordinal) { "__fw_ITerminal", "__fw_NuruApp" };
    foreach (string frameworkType in frameworkServiceTypes)
    {
      string fieldName = FrameworkServices.GetFieldName(frameworkType);
      if (!frameworkFieldNames.Add(fieldName))
        continue; // Already emitted (or the same type spelled with and without global::)

      sb.AppendLine($"  private static {frameworkType}? {fieldName};");
    }

    if (loggerFactoryFieldName is not null)
      sb.AppendLine($"  private static global::Microsoft.Extensions.Logging.ILoggerFactory? {loggerFactoryFieldName};");

    // Emit user service fields
    foreach (ServiceDefinition service in cachedProcessServices)
    {
      string fieldName = GetServiceFieldName(service.ImplementationTypeName);
      sb.AppendLine(
        $"  private static {service.ImplementationTypeName}? {fieldName};");
    }

    // Session and command state lives on a __NuruCall: the process one outside a call context (a CLI run,
    // the REPL), or the call's own one for a daemon or MCP host call (NuruCallContext), so concurrent calls
    // never share or dispose each other's scope. The static properties keep the field names generated code uses.
    // REPL (and isolated-call) Scoped uses a per-command field. Non-REPL Scoped keeps the process field above,
    // except a Scoped service that injects a session or command service, which always uses the command field.
    List<(string TypeName, string Name)> callFields =
    [
      .. sessionServices.Select(static service => (service.ImplementationTypeName, ServiceScopeExpressions.SessionField(service.ImplementationTypeName))),
      .. commandServices.Select(static service => (service.ImplementationTypeName, ServiceScopeExpressions.CommandField(service.ImplementationTypeName))),
      .. sortedProcessServices
        .Where(static service => service.Lifetime == ServiceLifetime.Scoped)
        .Select(static service => (service.ImplementationTypeName, ServiceScopeExpressions.CommandField(service.ImplementationTypeName)))
    ];

    sb.AppendLine("  private sealed partial class __NuruCall");
    sb.AppendLine("  {");
    sb.AppendLine("    public bool ReplCommandActive;");
    sb.AppendLine("    public readonly global::System.Collections.Generic.List<object> CommandDisposables = new();");
    sb.AppendLine("    public readonly global::System.Collections.Generic.List<object> SessionDisposables = new();");
    foreach ((string typeName, string name) in callFields)
      sb.AppendLine($"    public {typeName}? {name};");
    sb.AppendLine("  }");
    sb.AppendLine();
    sb.AppendLine("  private static readonly __NuruCall __nuruProcessCall = new();");
    sb.AppendLine("  private static readonly global::System.Threading.AsyncLocal<__NuruCall?> __nuruCall = new();");
    sb.AppendLine("  private static readonly global::System.Threading.Lock __nuruInitLock = new();");
    sb.AppendLine("  private static __NuruCall __CurrentCall => __nuruCall.Value ?? __nuruProcessCall;");
    foreach ((string typeName, string name) in callFields)
      sb.AppendLine($"  private static {typeName}? {name} {{ get => __CurrentCall.{name}; set => __CurrentCall.{name} = value; }}");

    sb.AppendLine("  private static bool __replCommandActive { get => __CurrentCall.ReplCommandActive; set => __CurrentCall.ReplCommandActive = value; }");
    sb.AppendLine("  private static global::System.Collections.Generic.List<object> __commandDisposables => __CurrentCall.CommandDisposables;");
    sb.AppendLine("  private static global::System.Collections.Generic.List<object> __sessionDisposables => __CurrentCall.SessionDisposables;");

    sb.AppendLine();

    // Emit EnsureServicesInitialized method
    EmitEnsureServicesInitialized(sb, cachedProcessServices, frameworkServiceTypes, requiresMediator, loggerFactoryFieldName, behaviors, behaviorServices);
    EmitSourceGenScopeMethods(sb, allServices, cachedProcessServices);

    if (requiresMediator)
      EmitSourceGenMediator(sb, allServices, generatedMediatorNamespace, loggerFactoryFieldName, httpClientConfigurations);
  }

  /// <summary>
  /// Emits <c>__GetMediator()</c>, which resolves the TimeWarp.Mediator generated <c>IMediator</c>
  /// for source-gen DI apps. The generated mediator resolves handlers through an
  /// <see cref="System.IServiceProvider"/>, so this builds a small container per app holding
  /// <c>AddGeneratedMediator()</c> plus Nuru's framework services and the statically registered services.
  /// </summary>
  private static void EmitSourceGenMediator(StringBuilder sb, ImmutableArray<ServiceDefinition> allServices, string? generatedMediatorNamespace, string? loggerFactoryFieldName, ImmutableArray<HttpClientConfiguration> httpClientConfigurations = default)
  {
    const string Sce = "global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions";

    sb.AppendLine("  // Source-gen DI: ISender / IPublisher / IMediator from the TimeWarp.Mediator generated mediator");
    sb.AppendLine("  private static global::Microsoft.Extensions.Configuration.IConfigurationRoot? __mediatorConfiguration;");
    sb.AppendLine("  private static global::System.IServiceProvider? __mediatorServiceProvider;");
    sb.AppendLine();
    sb.AppendLine("  private static global::TimeWarp.Mediator.IMediator __GetMediator()");
    sb.AppendLine("  {");

    if (generatedMediatorNamespace is null)
    {
      sb.AppendLine("    throw new global::System.InvalidOperationException(\"The TimeWarp.Mediator generated mediator is not available in this compilation.\");");
      sb.AppendLine("  }");
      sb.AppendLine();
      return;
    }

    sb.AppendLine("    if (__mediatorServiceProvider is null)");
    sb.AppendLine("    lock (__nuruInitLock)");
    sb.AppendLine("    if (__mediatorServiceProvider is null)");
    sb.AppendLine("    {");
    sb.AppendLine("      var services = new global::Microsoft.Extensions.DependencyInjection.ServiceCollection();");
    sb.AppendLine($"      {Sce}.AddTransient<global::TimeWarp.Terminal.ITerminal>(services, static _ => __fw_ITerminal!);");
    sb.AppendLine($"      {Sce}.AddTransient<global::TimeWarp.Nuru.NuruApp>(services, static _ => __fw_NuruApp!);");
    sb.AppendLine($"      {Sce}.AddTransient<global::Microsoft.Extensions.Configuration.IConfigurationRoot>(services, static _ => __mediatorConfiguration!);");
    sb.AppendLine($"      {Sce}.AddTransient<global::Microsoft.Extensions.Configuration.IConfiguration>(services, static _ => __mediatorConfiguration!);");

    // The container is rebuilt per app, and the factory is read per resolve: the running app's (task 544)
    string loggerFactoryExpression = LoggerExpressions.Resolve("global::Microsoft.Extensions.Logging.ILoggerFactory", loggerFactoryFieldName);
    sb.AppendLine($"      {Sce}.AddTransient<global::Microsoft.Extensions.Logging.ILoggerFactory>(services, static _ => {loggerFactoryExpression});");
    sb.AppendLine($"      {Sce}.AddSingleton(services, typeof(global::Microsoft.Extensions.Logging.ILogger<>), typeof(global::Microsoft.Extensions.Logging.Logger<>));");

    foreach (ServiceDefinition service in allServices.DistinctBy(s => s.ServiceTypeName))
    {
      string factory = service.Lifetime == ServiceLifetime.Transient
        ? ServiceScopeExpressions.Construct(service, allServices, ServiceResolveMode.Command)
        : ServiceScopeExpressions.Read(service, allServices, ServiceResolveMode.Command);
      sb.AppendLine($"      {Sce}.AddTransient<{service.ServiceTypeName}>(services, static _ => {factory});");
    }

    if (!httpClientConfigurations.IsDefaultOrEmpty)
    {
      foreach (HttpClientConfiguration config in httpClientConfigurations)
      {
        if (config.ServiceTypeName is null || config.ImplementationTypeName is null)
          continue;

        string httpClientField = GetHttpClientFieldName(config.ServiceTypeName);
        sb.AppendLine($"      {Sce}.AddTransient<{config.ServiceTypeName}>(services, static _ => new {config.ImplementationTypeName}({httpClientField}));");
      }
    }

    sb.AppendLine($"      {GeneratedMediatorDetector.RegistrationMethod(generatedMediatorNamespace)}(services);");
    sb.AppendLine("      __mediatorServiceProvider = global::Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services);");
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    return global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::TimeWarp.Mediator.IMediator>(__mediatorServiceProvider);");
    sb.AppendLine("  }");
    sb.AppendLine();
  }

  /// <summary>
  /// Collects all framework service types needed by the given services.
  /// </summary>
  private static HashSet<string> CollectFrameworkServiceTypes(ImmutableArray<ServiceDefinition> services)
  {
    HashSet<string> frameworkTypes = new(StringComparer.Ordinal);

    foreach (ServiceDefinition service in services)
    {
      if (!service.ConstructorParameters.IsDefaultOrEmpty)
      {
        foreach (ConstructorParameter param in service.ConstructorParameters)
        {
          if (FrameworkServices.IsFrameworkServiceType(param.TypeName))
          {
            frameworkTypes.Add(param.TypeName);
          }
        }
      }

      if (!service.ConstructorDependencyTypes.IsDefaultOrEmpty)
      {
        foreach (string depType in service.ConstructorDependencyTypes)
        {
          if (FrameworkServices.IsFrameworkServiceType(depType))
          {
            frameworkTypes.Add(depType);
          }
        }
      }
    }

    return frameworkTypes;
  }

  /// <summary>
  /// Emits the EnsureServicesInitialized method that sets the framework service fields.
  /// This is called after configuration is built in ExecuteRouteAsync.
  /// Uses app instance check instead of boolean flag to support multiple app instances
  /// in the same process (e.g., CI multi-mode tests). User services are not built here:
  /// a new app clears the process fields and each <c>__Resolve</c> accessor builds on first read.
  /// </summary>
  private static void EmitEnsureServicesInitialized(StringBuilder sb, ImmutableArray<ServiceDefinition> sortedServices, HashSet<string> frameworkServiceTypes, bool requiresMediator, string? loggerFactoryFieldName, ImmutableArray<BehaviorDefinition> behaviors, ImmutableArray<ServiceDefinition> behaviorServices)
  {
    // The running app passes its own logger factory (task 544); see GetLoggerFactorySource.
    string loggerFactoryParameter = loggerFactoryFieldName is null
      ? ""
      : ", global::Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory";
    string loggerFactoryArgument = loggerFactoryFieldName is null ? "" : ", loggerFactory";

    sb.AppendLine($"  private static void EnsureServicesInitialized(NuruApp app, global::Microsoft.Extensions.Configuration.IConfigurationRoot configuration{loggerFactoryParameter})");
    sb.AppendLine("  {");
    // Concurrent first calls (a daemon or MCP host) must not set up an app twice.
    sb.AppendLine("    lock (__nuruInitLock)");
    sb.AppendLine($"      EnsureServicesInitializedLocked(app, configuration{loggerFactoryArgument});");
    sb.AppendLine("  }");
    sb.AppendLine();
    sb.AppendLine($"  private static void EnsureServicesInitializedLocked(NuruApp app, global::Microsoft.Extensions.Configuration.IConfigurationRoot configuration{loggerFactoryParameter})");
    sb.AppendLine("  {");
    sb.AppendLine("    if (__fw_NuruApp == app)");
    sb.AppendLine("      return;");
    sb.AppendLine();
    if (loggerFactoryFieldName is not null)
    {
      sb.AppendLine("    // Logging: the running app's factory, or null (NullLogger) when it configures none");
      sb.AppendLine($"    {loggerFactoryFieldName} = loggerFactory;");
    }

    // Initialize framework services
    sb.AppendLine("    // Framework services");
    sb.AppendLine("    __fw_NuruApp = app;");
    sb.AppendLine("    __fw_ITerminal = new global::TimeWarp.Nuru.NuruCallTerminal(app);");
    if (requiresMediator)
    {
      sb.AppendLine("    __mediatorConfiguration = configuration;");
      sb.AppendLine("    __mediatorServiceProvider = null;");
    }

    // Initialize additional framework services
    HashSet<string> frameworkFieldNames = new(StringComparer.Ordinal) { "__fw_ITerminal", "__fw_NuruApp" };
    foreach (string frameworkType in frameworkServiceTypes)
    {
      string fieldName = FrameworkServices.GetFieldName(frameworkType);
      if (!frameworkFieldNames.Add(fieldName))
        continue; // Already initialized

      // A service's ILogger<T> or ILoggerFactory comes from the AddLogging or telemetry factory, like a
      // handler's or behavior's does. It was always a NullLogger before task 545. It reads the running
      // app's factory, set above, so a second app never logs through the first app's (task 544).
      string initExpr = LoggerExpressions.IsLoggingServiceType(frameworkType)
        ? LoggerExpressions.Resolve(frameworkType, loggerFactoryFieldName)
        : FrameworkServices.GetInitExpression(frameworkType);
      sb.AppendLine($"    {fieldName} = {initExpr};");
    }

    sb.AppendLine();

    // One instance per app: drop the previous app's singletons and process-scoped services.
    sb.AppendLine("    // User services: built on first read; a new app drops the previous app's instances");
    foreach (ServiceDefinition service in sortedServices)
      sb.AppendLine($"    {GetServiceFieldName(service.ImplementationTypeName)} = null;");

    BehaviorEmitter.EmitLoggingBehaviorResets(sb, behaviors, behaviorServices, loggerFactoryFieldName);

    sb.AppendLine("  }");
    sb.AppendLine();
  }
}
