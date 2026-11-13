#region Purpose
// InterceptorEmitter partial: scope helpers, runtime DI infrastructure, logging and HttpClient fields.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

internal static partial class InterceptorEmitter
{
  /// <summary>
  /// Emits session and command scope helpers for source-generated DI.
  /// </summary>
  private static void EmitSourceGenScopeMethods
  (
    StringBuilder sb,
    ImmutableArray<ServiceDefinition> allServices,
    ImmutableArray<ServiceDefinition> cachedProcessServices
  )
  {
    ImmutableArray<ServiceDefinition> sessionServices = DependencyGraphBuilder.TopologicalSort
    (
      [.. allServices.Where(static service => service.Lifetime == ServiceLifetime.SessionScoped).DistinctBy(static service => service.ImplementationTypeName)]
    );
    ImmutableArray<ServiceDefinition> commandServices = DependencyGraphBuilder.TopologicalSort
    (
      [.. allServices.Where(static service => service.Lifetime == ServiceLifetime.CommandScoped).DistinctBy(static service => service.ImplementationTypeName)]
    );
    ImmutableArray<ServiceDefinition> replCommandServices = DependencyGraphBuilder.TopologicalSort
    (
      [
        .. allServices
          .Where(static service => service.Lifetime is ServiceLifetime.CommandScoped or ServiceLifetime.Scoped)
          .DistinctBy(static service => service.ImplementationTypeName)
      ]
    );

    sb.AppendLine("  private static T __TrackCommand<T>(T instance) where T : class");
    sb.AppendLine("  {");
    sb.AppendLine("    if (instance is global::System.IDisposable or global::System.IAsyncDisposable)");
    sb.AppendLine("      __commandDisposables.Add(instance);");
    sb.AppendLine("    return instance;");
    sb.AppendLine("  }");
    sb.AppendLine();
    sb.AppendLine("  private static T __TrackSession<T>(T instance) where T : class");
    sb.AppendLine("  {");
    sb.AppendLine("    if (instance is global::System.IDisposable or global::System.IAsyncDisposable)");
    sb.AppendLine("      __sessionDisposables.Add(instance);");
    sb.AppendLine("    return instance;");
    sb.AppendLine("  }");
    sb.AppendLine();
    sb.AppendLine("  private static async global::System.Threading.Tasks.ValueTask __DisposeTrackedAsync(global::System.Collections.Generic.List<object> items)");
    sb.AppendLine("  {");
    sb.AppendLine("    for (int i = items.Count - 1; i >= 0; i--)");
    sb.AppendLine("    {");
    sb.AppendLine("      object instance = items[i];");
    sb.AppendLine("      if (instance is global::System.IAsyncDisposable asyncDisposable)");
    sb.AppendLine("        await asyncDisposable.DisposeAsync().ConfigureAwait(false);");
    sb.AppendLine("      else if (instance is global::System.IDisposable disposable)");
    sb.AppendLine("        disposable.Dispose();");
    sb.AppendLine("    }");
    sb.AppendLine("    items.Clear();");
    sb.AppendLine("  }");
    sb.AppendLine();

    // A Scoped service that injects a session or command service is built per run in its command
    // slot on the call, tracked with the command, and cleared when the run ends.
    ImmutableArray<ServiceDefinition> lateScoped =
    [
      .. replCommandServices.Where(service =>
        service.Lifetime == ServiceLifetime.Scoped &&
        ServiceScopeExpressions.IsPerRun(service, allServices))
    ];

    // Process instances (singletons, non-REPL Scoped) take the init lock so concurrent served calls build one.
    foreach (ServiceDefinition service in cachedProcessServices)
    {
      string fieldName = GetServiceFieldName(service.ImplementationTypeName);
      string constructed = ServiceScopeExpressions.Construct(service, allServices, ServiceResolveMode.ProcessInit);

      sb.AppendLine($"  private static {service.ImplementationTypeName} {ServiceScopeExpressions.ResolverName(fieldName)}()");
      sb.AppendLine("  {");
      sb.AppendLine($"    {service.ImplementationTypeName}? instance = {fieldName};");
      sb.AppendLine("    if (instance is not null)");
      sb.AppendLine("      return instance;");
      sb.AppendLine("    lock (__nuruInitLock)");
      sb.AppendLine($"      return {fieldName} ??= {constructed};");
      sb.AppendLine("  }");
      sb.AppendLine();
    }

    // Session and command fields belong to the call (__NuruCall), so they need no lock.
    EmitScopeResolvers(sb, sessionServices, allServices, ServiceResolveMode.Session, ServiceScopeExpressions.SessionField);
    EmitScopeResolvers(sb, replCommandServices, allServices, ServiceResolveMode.Command, ServiceScopeExpressions.CommandField);

    sb.AppendLine("  private static async global::System.Threading.Tasks.ValueTask EndReplCommandServicesAsync()");
    sb.AppendLine("  {");
    sb.AppendLine("    await __DisposeTrackedAsync(__commandDisposables).ConfigureAwait(false);");
    EmitNullAssignments(sb, replCommandServices, ServiceScopeExpressions.CommandField, "    ");
    sb.AppendLine("    __replCommandActive = false;");
    sb.AppendLine("  }");
    sb.AppendLine();

    sb.AppendLine("  private static async global::System.Threading.Tasks.ValueTask EndSingleRunServicesAsync()");
    sb.AppendLine("  {");
    sb.AppendLine("    await __DisposeTrackedAsync(__commandDisposables).ConfigureAwait(false);");
    sb.AppendLine("    await __DisposeTrackedAsync(__sessionDisposables).ConfigureAwait(false);");
    EmitNullAssignments(sb, commandServices, ServiceScopeExpressions.CommandField, "    ");
    EmitNullAssignments(sb, sessionServices, ServiceScopeExpressions.SessionField, "    ");
    EmitNullAssignments(sb, lateScoped, ServiceScopeExpressions.CommandField, "    ");
    sb.AppendLine("    __replCommandActive = false;");
    sb.AppendLine("  }");
    sb.AppendLine();

    sb.AppendLine("  private static async global::System.Threading.Tasks.ValueTask EndReplSessionServicesAsync()");
    sb.AppendLine("  {");
    sb.AppendLine("    await __DisposeTrackedAsync(__sessionDisposables).ConfigureAwait(false);");
    EmitNullAssignments(sb, sessionServices, ServiceScopeExpressions.SessionField, "    ");
    sb.AppendLine("  }");
    sb.AppendLine();
  }

  private static void EmitScopeResolvers
  (
    StringBuilder sb,
    ImmutableArray<ServiceDefinition> services,
    ImmutableArray<ServiceDefinition> allServices,
    ServiceResolveMode mode,
    Func<string, string> fieldName
  )
  {
    foreach (ServiceDefinition service in services)
    {
      string field = fieldName(service.ImplementationTypeName);
      string constructed = ServiceScopeExpressions.Construct(service, allServices, mode);
      sb.AppendLine($"  private static {service.ImplementationTypeName} {ServiceScopeExpressions.ResolverName(field)}() => {field} ??= {constructed};");
    }

    if (services.Length > 0)
      sb.AppendLine();
  }

  private static void EmitNullAssignments
  (
    StringBuilder sb,
    ImmutableArray<ServiceDefinition> services,
    Func<string, string> fieldName,
    string indent
  )
  {
    foreach (ServiceDefinition service in services)
      sb.AppendLine($"{indent}{fieldName(service.ImplementationTypeName)} = null;");
  }

  /// <summary>
  /// Emits runtime DI infrastructure when UseMicrosoftDependencyInjection() is called.
  /// Each app gets its own __ConfigureServices and GetServiceProvider methods.
  /// This provides full MS DI container support at the cost of runtime overhead.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="apps">All app models (will filter to those using runtime DI).</param>
  /// <param name="generatedMediatorNamespace">The TimeWarp.Mediator generated namespace to register, or null when there is none.</param>
  private static void EmitRuntimeDIInfrastructure(StringBuilder sb, ImmutableArray<AppModel> apps, string? generatedMediatorNamespace)
  {
    sb.AppendLine("  // ═══════════════════════════════════════════════════════════════════════════════");
    sb.AppendLine("  // RUNTIME DI INFRASTRUCTURE (UseMicrosoftDependencyInjection was called)");
    sb.AppendLine("  // ═══════════════════════════════════════════════════════════════════════════════");
    sb.AppendLine("  // This enables full MS DI container support including:");
    sb.AppendLine("  // - Services with constructor dependencies (MS DI resolves automatically)");
    sb.AppendLine("  // - All service lifetimes (Singleton, Scoped, Transient)");
    sb.AppendLine("  // - Extension methods like AddLogging(), AddDbContext(), etc.");
    sb.AppendLine("  // Trade-off: Slightly slower startup (~2-10ms for ServiceProvider.Build())");
    sb.AppendLine();

    // Emit per-app infrastructure for each app that uses runtime DI
    for (int appIndex = 0; appIndex < apps.Length; appIndex++)
    {
      AppModel app = apps[appIndex];
      if (!app.UseMicrosoftDependencyInjection)
        continue;

      string suffix = apps.Length > 1 ? $"_{appIndex}" : "";
      string? lambdaBody = app.ConfigureServicesLambdaBody;

      // Emit static __ConfigureServices method if lambda body is available
      if (!string.IsNullOrEmpty(lambdaBody))
      {
        sb.AppendLine("  /// <summary>");
        sb.AppendLine($"  /// User's ConfigureServices delegate for app {appIndex}, invoked at runtime.");
        sb.AppendLine("  /// This preserves extension method calls (AddLogging, AddDbContext, etc.).");
        sb.AppendLine("  /// </summary>");
        sb.AppendLine($"  private static void __ConfigureServices{suffix}(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)");
        sb.AppendLine($"  {lambdaBody}");
        sb.AppendLine();
      }

      sb.AppendLine($"  private static global::System.IServiceProvider? __serviceProvider{suffix};");
      // The command scope belongs to the call (see __NuruCall): concurrent served calls each have one.
      sb.AppendLine("  private sealed partial class __NuruCall");
      sb.AppendLine("  {");
      sb.AppendLine($"    public global::Microsoft.Extensions.DependencyInjection.IServiceScope? CommandScope{suffix};");
      sb.AppendLine("  }");
      sb.AppendLine();
      sb.AppendLine($"  private static global::Microsoft.Extensions.DependencyInjection.IServiceScope? __commandScope{suffix} {{ get => __CurrentCall.CommandScope{suffix}; set => __CurrentCall.CommandScope{suffix} = value; }}");
      sb.AppendLine();
      sb.AppendLine($"  private static global::System.IServiceProvider GetServiceProvider{suffix}(NuruApp app)");
      sb.AppendLine("  {");
      sb.AppendLine($"    if (__serviceProvider{suffix} is not null) return __serviceProvider{suffix};");
      sb.AppendLine("    lock (__nuruInitLock)");
      sb.AppendLine($"      return __serviceProvider{suffix} ?? BuildServiceProvider{suffix}(app);");
      sb.AppendLine("  }");
      sb.AppendLine();
      sb.AppendLine($"  private static global::System.IServiceProvider BuildServiceProvider{suffix}(NuruApp app)");
      sb.AppendLine("  {");
      sb.AppendLine("    var services = new global::Microsoft.Extensions.DependencyInjection.ServiceCollection();");
      sb.AppendLine();
      sb.AppendLine("    // Register Nuru built-ins so they're available for user services");
      sb.AppendLine("    global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::TimeWarp.Terminal.ITerminal>(services, new global::TimeWarp.Nuru.NuruCallTerminal(app));");
      sb.AppendLine("    global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<NuruApp>(services, app);");
      sb.AppendLine();

      if (generatedMediatorNamespace is not null)
      {
        sb.AppendLine("    // Register the TimeWarp.Mediator source-generated IMediator / ISender / IPublisher");
        sb.AppendLine($"    {GeneratedMediatorDetector.RegistrationMethod(generatedMediatorNamespace)}(services);");
        sb.AppendLine();
      }

      if (!string.IsNullOrEmpty(lambdaBody))
      {
        sb.AppendLine("    // Invoke user's ConfigureServices delegate at runtime");
        sb.AppendLine("    // This ensures extension methods (AddLogging, AddDbContext, etc.) work correctly");
        sb.AppendLine($"    __ConfigureServices{suffix}(services);");
      }
      else if (app.Services.Length > 0)
      {
        // Fallback: emit service registrations extracted at compile time
        sb.AppendLine("    // Services extracted from ConfigureServices at compile time");
        sb.AppendLine("    // MS DI handles constructor dependency resolution automatically");
        foreach (ServiceDefinition service in app.Services)
        {
          string owner = service.Lifetime is ServiceLifetime.SessionScoped or ServiceLifetime.CommandScoped
            ? "global::TimeWarp.Nuru.NuruServiceCollectionExtensions"
            : "global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions";
          string methodName = service.Lifetime switch
          {
            ServiceLifetime.Singleton => "AddSingleton",
            ServiceLifetime.Scoped => "AddScoped",
            ServiceLifetime.Transient => "AddTransient",
            ServiceLifetime.SessionScoped => "AddSessionScoped",
            ServiceLifetime.CommandScoped => "AddCommandScoped",
            _ => "AddSingleton"
          };

          // If service type equals implementation type, use single type parameter form
          if (service.ServiceTypeName == service.ImplementationTypeName)
          {
            sb.AppendLine($"    {owner}.{methodName}<{service.ImplementationTypeName}>(services);");
          }
          else
          {
            sb.AppendLine($"    {owner}.{methodName}<{service.ServiceTypeName}, {service.ImplementationTypeName}>(services);");
          }
        }
      }

      sb.AppendLine();
      sb.AppendLine($"    __serviceProvider{suffix} = global::Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services);");
      sb.AppendLine($"    return __serviceProvider{suffix};");
      sb.AppendLine("  }");
      sb.AppendLine();
      sb.AppendLine($"  private static global::System.IServiceProvider GetResolutionProvider{suffix}(NuruApp app)");
      sb.AppendLine("  {");
      sb.AppendLine($"    if (__commandScope{suffix} is not null) return __commandScope{suffix}.ServiceProvider;");
      sb.AppendLine($"    return GetServiceProvider{suffix}(app);");
      sb.AppendLine("  }");
      sb.AppendLine();
    }
  }

  /// <summary>
  /// Emits static LoggerFactory fields for apps with explicit AddLogging() configuration.
  /// This is the AOT-optimized path - factory created at static init, zero runtime cost.
  /// </summary>
  private static void EmitLoggingFactoryFields(StringBuilder sb, GeneratorModel model)
  {
    // Only emit for explicit AddLogging() - telemetry uses runtime app.LoggerFactory instead
    bool hasAnyLogging = model.Apps.Any(a => a.HasLogging);
    if (!hasAnyLogging)
      return;

    sb.AppendLine("  // Static LoggerFactory - AOT path: compile-time deterministic, zero runtime cost");

    for (int appIndex = 0; appIndex < model.Apps.Length; appIndex++)
    {
      AppModel app = model.Apps[appIndex];
      if (app.LoggingConfiguration is null)
        continue;

      string fieldName = GetLoggerFactoryFieldName(appIndex, model.Apps.Length);
      string lambdaBody = app.LoggingConfiguration.ConfigurationLambdaBody.TrimEnd();
      string paramName = app.LoggingConfiguration.LambdaParameterName;

      // Ensure lambda body ends with semicolon
      if (lambdaBody.Length > 0 && !lambdaBody.EndsWith(';', StringComparison.Ordinal))
        lambdaBody += ";";

      sb.AppendLine($"  private static readonly global::Microsoft.Extensions.Logging.ILoggerFactory {fieldName} =");
      sb.AppendLine($"    global::Microsoft.Extensions.Logging.LoggerFactory.Create({paramName} =>");
      sb.AppendLine("    {");

      // AddLogging() with no configure delegate: a factory with no providers (task 546)
      if (lambdaBody.Length > 0)
        sb.AppendLine($"      {lambdaBody}");

      sb.AppendLine("    });");
    }

    sb.AppendLine();
  }

  /// <summary>
  /// Emits static HttpClient fields for apps with AddHttpClient() configurations.
  /// Each typed client gets a static field with its configuration lambda applied.
  /// </summary>
  private static void EmitHttpClientFields(StringBuilder sb, GeneratorModel model)
  {
    bool hasAny = model.Apps.Any(a => a.HasHttpClients);
    if (!hasAny)
      return;

    sb.AppendLine("  // Static HttpClient fields - AOT path: compile-time deterministic");

    foreach (AppModel app in model.Apps)
    {
      if (!app.HasHttpClients)
        continue;

      foreach (HttpClientConfiguration config in app.HttpClientConfigurations)
      {
        if (config.ServiceTypeName is null)
          continue;

        string fieldName = GetHttpClientFieldName(config.ServiceTypeName);

        sb.AppendLine($"  private static readonly global::System.Net.Http.HttpClient {fieldName} = Create_{fieldName}();");
        sb.AppendLine($"  private static global::System.Net.Http.HttpClient Create_{fieldName}()");
        sb.AppendLine("  {");
        sb.AppendLine("    var client = new global::System.Net.Http.HttpClient();");

        if (config.ConfigurationLambdaBody is not null)
        {
          string lambdaBody = config.ConfigurationLambdaBody.TrimEnd();

          // Handle block body: { stmt1; stmt2; }
          if (lambdaBody.StartsWith('{', StringComparison.Ordinal) && lambdaBody.EndsWith('}', StringComparison.Ordinal))
          {
            string inner = lambdaBody[1..^1].Trim();
            inner = inner.Replace(config.LambdaParameterName + ".", "client.", StringComparison.Ordinal);
            sb.AppendLine($"    {inner}");
          }
          else
          {
            // Expression body: client.Timeout = TimeSpan.FromSeconds(30)
            string body = lambdaBody.Replace(config.LambdaParameterName + ".", "client.", StringComparison.Ordinal);
            if (!body.EndsWith(';', StringComparison.Ordinal))
              body += ";";
            sb.AppendLine($"    {body}");
          }
        }

        sb.AppendLine("    return client;");
        sb.AppendLine("  }");
        sb.AppendLine();
      }
    }
  }
}
