// Main emitter that generates the RunAsync interceptor methods.
// This is the entry point for code emission, coordinating all other emitters.
//
// Key design: Each app instance gets its own interceptor method with isolated routes.
// This ensures route patterns don't leak between different NuruApp instances.

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits the complete generated interceptor source file.
/// This is the main entry point for the emission phase.
/// </summary>
internal static class InterceptorEmitter
{
  /// <summary>
  /// Generates the complete source code for the interceptor.
  /// </summary>
  /// <param name="model">The generator model containing all apps and routes.</param>
  /// <param name="compilation">The Roslyn compilation for type resolution (used for enum completions).</param>
  /// <returns>The generated C# source code.</returns>
  public static string Emit(GeneratorModel model, Compilation compilation)
  {
    StringBuilder sb = new();

    EmitHeader(sb);
    EmitInterceptsLocationAttribute(sb);
    EmitNamespaceAndUsings(sb, model);
    EmitClassStart(sb);

    // Emit shared infrastructure (command classes, service fields, behaviors, logging, telemetry)
    EmitCommandClasses(sb, model);
    EmitServiceFields(sb, model.AllServices);
    EmitLoggingFactoryFields(sb, model);

    // Emit telemetry infrastructure if any app has telemetry enabled
    if (model.HasTelemetry)
    {
      TelemetryEmitter.EmitTelemetryFields(sb);
    }

    // Determine the logger factory source for ILogger<T> injection
    // (static field when explicit logging configured, or app.LoggerFactory when telemetry enabled)
    string? loggerFactoryFieldName = GetFirstLoggerFactoryFieldName(model);
    BehaviorEmitter.EmitBehaviorFields(sb, [.. model.AllBehaviors], [.. model.AllServices], loggerFactoryFieldName);

    // Emit per-app interceptor methods
    for (int appIndex = 0; appIndex < model.Apps.Length; appIndex++)
    {
      AppModel app = model.Apps[appIndex];
      EmitAppInterceptorMethod(sb, app, appIndex, model, compilation, loggerFactoryFieldName);
      EmitRunReplAsyncInterceptorMethod(sb, app, appIndex, model);
    }

    EmitClassEnd(sb, model, compilation);

    return sb.ToString();
  }

  /// <summary>
  /// Emits a single interceptor method for one app.
  /// Each app gets its own method with its own [InterceptsLocation] attributes and routes.
  /// </summary>
  private static void EmitAppInterceptorMethod(StringBuilder sb, AppModel app, int appIndex, GeneratorModel model, Compilation compilation, string? loggerFactoryFieldName)
  {
    string methodSuffix = model.Apps.Length > 1 ? $"_{appIndex}" : "";

    // Get RunAsync intercept sites from the dictionary
    bool hasRunAsyncSites = app.InterceptSitesByMethod.TryGetValue("RunAsync", out ImmutableArray<InterceptSiteModel> runAsyncSites);

    // Emit ExecuteRouteAsync when:
    // - App has routes (needs route matching)
    // - App has REPL (REPL calls ExecuteRouteAsync)
    // - App has RunAsync intercept sites (RunAsync_Intercepted calls ExecuteRouteAsync)
    if (app.HasRoutes || app.HasRepl || hasRunAsyncSites)
    {
      EmitExecuteRouteAsyncMethod(sb, app, appIndex, model, methodSuffix, loggerFactoryFieldName);
    }

    // Only emit RunAsync_Intercepted when there are actual intercept sites
    if (!hasRunAsyncSites)
      return;

    // Emit [InterceptsLocation] attributes for this app's RunAsync calls
    foreach (InterceptSiteModel site in runAsyncSites)
    {
      sb.AppendLine($"  {site.GetAttributeSyntax()}");
    }

    // Method signature - use index suffix for uniqueness when multiple apps
    sb.AppendLine($"  public static async Task<int> RunAsync_Intercepted{methodSuffix}");
    sb.AppendLine("  (");
    sb.AppendLine("    this NuruApp app,");
    sb.AppendLine("    string[] args");
    sb.AppendLine("  )");
    sb.AppendLine("  {");

    // Emit telemetry setup if this app has telemetry enabled
    if (app.HasTelemetry)
    {
      TelemetryEmitter.EmitTelemetrySetup(sb);
      sb.AppendLine("    try");
      sb.AppendLine("    {");
      sb.AppendLine($"      return await ExecuteRouteAsync{methodSuffix}(app, args).ConfigureAwait(false);");
      sb.AppendLine("    }");
      sb.AppendLine("    finally");
      sb.AppendLine("    {");
      TelemetryEmitter.EmitTelemetryFlush(sb);
      sb.AppendLine("    }");
    }
    else
    {
      sb.AppendLine($"    return await ExecuteRouteAsync{methodSuffix}(app, args).ConfigureAwait(false);");
    }

    sb.AppendLine("  }");
    sb.AppendLine();
  }

  /// <summary>
  /// Emits the core route execution method that handles all route matching.
  /// This is called by both RunAsync_Intercepted and the REPL command executor.
  /// </summary>
  private static void EmitExecuteRouteAsyncMethod(StringBuilder sb, AppModel app, int appIndex, GeneratorModel model, string methodSuffix, string? loggerFactoryFieldName)
  {
    sb.AppendLine($"  private static async Task<int> ExecuteRouteAsync{methodSuffix}");
    sb.AppendLine("  (");
    sb.AppendLine("    NuruApp app,");
    sb.AppendLine("    string[] args");
    sb.AppendLine("  )");
    sb.AppendLine("  {");

    // Set up completion support (only when completion or REPL is enabled)
    if (app.HasCompletion || app.HasRepl)
    {
      sb.AppendLine($"    app.ShellCompletionProvider ??= __shellCompletionProvider{methodSuffix};");
      sb.AppendLine("    app.ConfigureCompletionRegistry();");
      sb.AppendLine();
    }

    // Method body with this app's routes only
    // Note: LoggerFactory is static and should NOT be disposed after each command
    // (this would break REPL mode). Disposal happens at app shutdown via NuruApp.
    EmitMethodBody(sb, app, appIndex, model, loggerFactoryFieldName);

    sb.AppendLine("  }");
    sb.AppendLine();
  }

  /// <summary>
  /// Emits the RunReplAsync interceptor method for one app.
  /// This intercepts direct RunReplAsync() calls and delegates to the REPL session.
  /// </summary>
  private static void EmitRunReplAsyncInterceptorMethod(StringBuilder sb, AppModel app, int appIndex, GeneratorModel model)
  {
    // Get RunReplAsync intercept sites from the dictionary
    if (!app.InterceptSitesByMethod.TryGetValue("RunReplAsync", out ImmutableArray<InterceptSiteModel> replSites))
      return;

    // Only emit if app has REPL enabled
    if (!app.HasRepl)
      return;

    string methodSuffix = model.Apps.Length > 1 ? $"_{appIndex}" : "";

    // Emit [InterceptsLocation] attributes for this app's RunReplAsync calls
    foreach (InterceptSiteModel site in replSites)
    {
      sb.AppendLine($"  {site.GetAttributeSyntax()}");
    }

    // Method signature - matches NuruApp.RunReplAsync signature
    sb.AppendLine($"  public static async global::System.Threading.Tasks.Task RunReplAsync_Intercepted{methodSuffix}");
    sb.AppendLine("  (");
    sb.AppendLine("    this NuruApp app,");
    sb.AppendLine("    global::System.Threading.CancellationToken cancellationToken = default");
    sb.AppendLine("  )");
    sb.AppendLine("  {");
    sb.AppendLine($"    await RunReplAsync{methodSuffix}(app).ConfigureAwait(false);");
    sb.AppendLine("  }");
    sb.AppendLine();
  }

  /// <summary>
  /// Emits the InterceptsLocationAttribute definition.
  /// In .NET 10 / C# 14, interceptors use the new versioned constructor:
  /// InterceptsLocationAttribute(int version, string data)
  /// We use 'file' scope so it doesn't conflict with other generators that may define it.
  /// </summary>
  private static void EmitInterceptsLocationAttribute(StringBuilder sb)
  {
    sb.AppendLine("namespace System.Runtime.CompilerServices");
    sb.AppendLine("{");
    sb.AppendLine("  [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]");
    sb.AppendLine("  file sealed class InterceptsLocationAttribute : global::System.Attribute");
    sb.AppendLine("  {");
    sb.AppendLine("    public InterceptsLocationAttribute(int version, string data)");
    sb.AppendLine("    {");
    sb.AppendLine("      Version = version;");
    sb.AppendLine("      Data = data;");
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    public int Version { get; }");
    sb.AppendLine("    public string Data { get; }");
    sb.AppendLine("  }");
    sb.AppendLine("}");
    sb.AppendLine();
  }

  /// <summary>
  /// Emits the auto-generated header comment, pragma warning disable, and nullable enable.
  /// </summary>
  private static void EmitHeader(StringBuilder sb)
  {
    sb.AppendLine("// <auto-generated/>");
    sb.AppendLine("// This code was generated by TimeWarp.Nuru V2 source generator.");
    sb.AppendLine("// Do not modify this file directly.");
    sb.AppendLine("#pragma warning disable");
    sb.AppendLine("#nullable enable");
    sb.AppendLine();
  }

  /// <summary>
  /// Emits the namespace declaration and using statements.
  /// Uses block-scoped namespace to be compatible with the InterceptsLocationAttribute
  /// which is in a separate namespace block in the same file.
  /// </summary>
  private static void EmitNamespaceAndUsings(StringBuilder sb, GeneratorModel model)
  {
    sb.AppendLine("namespace TimeWarp.Nuru.Generated");
    sb.AppendLine("{");
    sb.AppendLine();

    // Default usings required by generated code
    sb.AppendLine("using global::System.Linq;");
    sb.AppendLine("using global::System.Net.Http;");
    sb.AppendLine("using global::System.Reflection;");
    sb.AppendLine("using global::System.Runtime.CompilerServices;");
    sb.AppendLine("using global::System.Text.Json;");
    sb.AppendLine("using global::System.Text.Json.Serialization;");
    sb.AppendLine("using global::System.Text.RegularExpressions;");
    sb.AppendLine("using global::System.Threading.Tasks;");
    sb.AppendLine("using global::Microsoft.Extensions.Configuration;");
    sb.AppendLine("using global::Microsoft.Extensions.Configuration.Json;");
    sb.AppendLine("using global::Microsoft.Extensions.Configuration.EnvironmentVariables;");
    sb.AppendLine("#if DEBUG");
    sb.AppendLine("using global::Microsoft.Extensions.Configuration.UserSecrets;");
    sb.AppendLine("#endif");
    sb.AppendLine("using global::TimeWarp.Nuru;");
    sb.AppendLine("using global::TimeWarp.Terminal;");

    // OpenTelemetry usings (only when telemetry is enabled)
    if (model.HasTelemetry)
    {
      sb.AppendLine("using global::OpenTelemetry;");
      sb.AppendLine("using global::OpenTelemetry.Extensions.Hosting;");
      sb.AppendLine("using global::OpenTelemetry.Logs;");
      sb.AppendLine("using global::OpenTelemetry.Metrics;");
      sb.AppendLine("using global::OpenTelemetry.Resources;");
      sb.AppendLine("using global::OpenTelemetry.Trace;");
    }

    // User-defined usings from source file
    if (model.UserUsings.Length > 0)
    {
      sb.AppendLine();
      sb.AppendLine("// User-defined usings");
      foreach (string userUsing in model.UserUsings)
      {
        sb.AppendLine(userUsing);
      }
    }

    sb.AppendLine();
  }

  /// <summary>
  /// Emits the file-scoped static partial class declaration.
  /// Partial is always emitted to support GeneratedRegex and other source generators.
  /// </summary>
  private static void EmitClassStart(StringBuilder sb)
  {
    sb.AppendLine("file static partial class GeneratedInterceptor");
    sb.AppendLine("{");
  }

  /// <summary>
  /// Emits generated command classes for delegate routes.
  /// These provide command instances for behaviors.
  /// </summary>
  private static void EmitCommandClasses(StringBuilder sb, GeneratorModel model)
  {
    // Emit command classes for all routes across all apps
    CommandClassEmitter.EmitCommandClasses(sb, model.AllRoutes);
  }

  /// <summary>
  /// Emits static Lazy fields for Singleton and Scoped services.
  /// These provide thread-safe lazy initialization for cached service instances.
  /// </summary>
  private static void EmitServiceFields(StringBuilder sb, IEnumerable<ServiceDefinition> services)
  {
    // Only emit fields for Singleton and Scoped services (not Transient)
    // Materialize to array to avoid multiple enumeration
    ServiceDefinition[] cachedServices =
    [
      .. services
        .Where(s => s.Lifetime is ServiceLifetime.Singleton or ServiceLifetime.Scoped)
        .DistinctBy(s => s.ImplementationTypeName) // Avoid duplicates if same impl registered multiple times
    ];

    if (cachedServices.Length == 0)
      return;

    sb.AppendLine("  // Static service fields (thread-safe lazy initialization)");

    foreach (ServiceDefinition service in cachedServices)
    {
      string fieldName = GetServiceFieldName(service.ImplementationTypeName);
      sb.AppendLine(
        $"  private static readonly global::System.Lazy<{service.ImplementationTypeName}> {fieldName} = new(() => new {service.ImplementationTypeName}());");
    }

    sb.AppendLine();
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

      // Ensure lambda body ends with semicolon
      if (!lambdaBody.EndsWith(';'))
        lambdaBody += ";";

      sb.AppendLine($"  private static readonly global::Microsoft.Extensions.Logging.ILoggerFactory {fieldName} =");
      sb.AppendLine("    global::Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>");
      sb.AppendLine("    {");
      sb.AppendLine($"      {lambdaBody}");
      sb.AppendLine("    });");
    }

    sb.AppendLine();
  }

  /// <summary>
  /// Gets the field name for a LoggerFactory instance.
  /// </summary>
  /// <param name="appIndex">Index of the app (for unique naming with multiple apps).</param>
  /// <param name="totalApps">Total number of apps.</param>
  /// <returns>Field name like "__loggerFactory" or "__loggerFactory_0".</returns>
  internal static string GetLoggerFactoryFieldName(int appIndex, int totalApps)
  {
    // Use simple name if only one app, otherwise add suffix for uniqueness
    return totalApps > 1 ? $"__loggerFactory_{appIndex}" : "__loggerFactory";
  }

  // ═══════════════════════════════════════════════════════════════════════════════
  // LOGGER FACTORY RESOLUTION - TWO PATHS FOR AOT OPTIMIZATION
  // ═══════════════════════════════════════════════════════════════════════════════
  //
  // Path 1: STATIC FIELD (__loggerFactory)
  //   - Used when: User calls ConfigureServices(s => s.AddLogging(...))
  //   - Created at: Static initialization (compile-time deterministic)
  //   - AOT benefit: Zero runtime cost, trimmer sees exact types
  //   - Generated as: private static readonly ILoggerFactory __loggerFactory = ...
  //
  // Path 2: INSTANCE PROPERTY (app.LoggerFactory)
  //   - Used when: UseTelemetry() is called (OTEL endpoint is runtime config)
  //   - Created at: Runtime in RunAsync_Intercepted (checks OTEL_EXPORTER_OTLP_ENDPOINT)
  //   - AOT tradeoff: Runtime null checks, dynamic factory creation
  //   - Necessary because: OTLP endpoint is environment variable, unknown at compile time
  //
  // Priority: Static field wins if both are configured (better AOT characteristics)
  // ═══════════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Gets the logger factory source for ILogger&lt;T&gt; injection.
  /// </summary>
  /// <param name="model">The generator model.</param>
  /// <returns>
  /// Static field name (AOT-optimized) when explicit logging is configured,
  /// "app.LoggerFactory" (runtime) when only telemetry is enabled,
  /// or null if no logging source is available.
  /// </returns>
  private static string? GetFirstLoggerFactoryFieldName(GeneratorModel model)
  {
    // Prefer static field - AOT-friendly, zero runtime cost
    for (int i = 0; i < model.Apps.Length; i++)
    {
      if (model.Apps[i].HasLogging)
      {
        return GetLoggerFactoryFieldName(i, model.Apps.Length);
      }
    }

    // Fall back to runtime path for telemetry (OTEL endpoint is runtime config)
    if (model.HasTelemetry)
    {
      return "app.LoggerFactory";
    }

    return null;
  }

  /// <summary>
  /// Gets the static field name for a service implementation type.
  /// </summary>
  /// <param name="implementationTypeName">Fully qualified implementation type name.</param>
  /// <returns>Field name like "__svc_MyApp_Services_Greeter".</returns>
  internal static string GetServiceFieldName(string implementationTypeName)
  {
    string name = implementationTypeName;

    // Remove global:: prefix
    if (name.StartsWith("global::", StringComparison.Ordinal))
      name = name[8..];

    // Replace dots with underscores for valid C# identifier
    return "__svc_" + name.Replace(".", "_", StringComparison.Ordinal);
  }

  /// <summary>
  /// Emits the complete method body including all route matching logic.
  /// </summary>
  private static void EmitMethodBody(StringBuilder sb, AppModel app, int appIndex, GeneratorModel model, string? loggerFactoryFieldName)
  {
    // Configuration setup (if AddConfiguration was called)
    if (app.HasConfiguration)
    {
      ConfigurationEmitter.Emit(sb);
    }

    // Filter out configuration override args before route matching
    // Config overrides follow pattern: --Section:Key=value (starts with -- and contains :)
    // This allows AddCommandLine(args) to process them while route matching ignores them
    EmitConfigArgFiltering(sb);

    // --interactive / -i must be checked BEFORE user routes so catch-all routes don't intercept it
    string methodSuffix = model.Apps.Length > 1 ? $"_{appIndex}" : "";
    EmitInteractiveFlag(sb, app, methodSuffix);

    // Route matching - emit this app's routes in specificity order (highest first)
    // User routes are emitted BEFORE built-ins so users can override --help, --version, etc.
    // Filter endpoints based on app's discovery mode (DiscoverEndpoints or Map<T> calls)
    ImmutableArray<RouteDefinition> endpointsForApp = FilterEndpointsForApp(app, model.Endpoints);
    IEnumerable<RouteDefinition> allRoutes = app.RoutesBySpecificity.Concat(endpointsForApp);

    // Build a lookup from route to its original index for command class naming
    // IMPORTANT: Use only this app's routes (plus filtered endpoints), not all routes from all apps
    // Using model.AllRoutes would cause route index collisions between different apps
    List<RouteDefinition> allRoutesOrdered = [.. app.Routes.Concat(endpointsForApp)];

    foreach (RouteDefinition route in allRoutes.OrderByDescending(r => r.ComputedSpecificity))
    {
      // Find the route's original index in model.AllRoutes
      int routeIndex = allRoutesOrdered.FindIndex(r => ReferenceEquals(r, route));
      if (routeIndex < 0)
      {
        // For endpoints not in allRoutesOrdered, use a new index
        routeIndex = allRoutesOrdered.Count;
        allRoutesOrdered.Add(route);
      }

      RouteMatcherEmitter.Emit(sb, route, routeIndex, app.Services, app.Behaviors, app.CustomConverters, loggerFactoryFieldName);
    }

    // Built-in flags: --help, --version, --capabilities
    // Emitted AFTER user routes so users can override default behavior
    EmitBuiltInFlags(sb, app, methodSuffix);

    // No match fallback
    EmitNoMatch(sb);
  }

  /// <summary>
  /// Filters endpoints based on the app's discovery mode.
  /// </summary>
  /// <param name="app">The app model with discovery settings.</param>
  /// <param name="allEndpoints">All discovered endpoint classes.</param>
  /// <returns>Endpoints that should be included in this app.</returns>
  private static ImmutableArray<RouteDefinition> FilterEndpointsForApp
  (
    AppModel app,
    ImmutableArray<RouteDefinition> allEndpoints
  )
  {
    // If DiscoverEndpoints() was called, include all endpoints
    if (app.DiscoverEndpoints)
      return allEndpoints;

    // If explicit Map<T>() calls, include only those endpoints
    if (!app.ExplicitEndpointTypes.IsDefaultOrEmpty && app.ExplicitEndpointTypes.Length > 0)
    {
      return
      [
        .. allEndpoints.Where
        (
          e => app.ExplicitEndpointTypes.Any
          (
            t => e.Handler.FullTypeName?.EndsWith(t, StringComparison.Ordinal) == true ||
                 e.Handler.FullTypeName == t
          )
        )
      ];
    }

    // Default: no endpoints (test isolation)
    return [];
  }

  /// <summary>
  /// Emits handling for built-in flags (--help, --version, --capabilities).
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="app">The application model.</param>
  /// <param name="methodSuffix">Suffix for per-app helper methods (e.g., "_0" for multi-app assemblies).</param>
  private static void EmitBuiltInFlags(StringBuilder sb, AppModel app, string methodSuffix)
  {
    // --help flag
    if (app.HasHelp)
    {
      sb.AppendLine("    // Built-in: --help");
      sb.AppendLine("    if (routeArgs is [\"--help\" or \"-h\"])");
      sb.AppendLine("    {");
      sb.AppendLine($"      PrintHelp{methodSuffix}(app.Terminal);");
      sb.AppendLine("      return 0;");
      sb.AppendLine("    }");
      sb.AppendLine();
    }

    // --version flag (always available) - shared across apps (assembly-level)
    sb.AppendLine("    // Built-in: --version");
    sb.AppendLine("    if (routeArgs is [\"--version\"])");
    sb.AppendLine("    {");
    sb.AppendLine("      PrintVersion(app.Terminal);");
    sb.AppendLine("      return 0;");
    sb.AppendLine("    }");
    sb.AppendLine();

    // --capabilities flag (always available for AI tools)
    sb.AppendLine("    // Built-in: --capabilities (for AI tools)");
    sb.AppendLine("    if (routeArgs is [\"--capabilities\"])");
    sb.AppendLine("    {");
    sb.AppendLine($"      PrintCapabilities{methodSuffix}(app.Terminal);");
    sb.AppendLine("      return 0;");
    sb.AppendLine("    }");
    sb.AppendLine();

    // --check-updates flag (opt-in via AddCheckUpdatesRoute())
    if (app.HasCheckUpdatesRoute)
    {
      sb.AppendLine("    // Built-in: --check-updates (opt-in via AddCheckUpdatesRoute())");
      sb.AppendLine("    if (routeArgs is [\"--check-updates\"])");
      sb.AppendLine("    {");
      sb.AppendLine("      await CheckForUpdatesAsync(app.Terminal).ConfigureAwait(false);");
      sb.AppendLine("      return 0;");
      sb.AppendLine("    }");
      sb.AppendLine();
    }

    // Shell completion routes (opt-in via EnableCompletion())
    if (app.HasCompletion)
    {
      EmitCompletionRoutes(sb, methodSuffix);
    }
  }

  /// <summary>
  /// Emits shell completion route handlers.
  /// </summary>
  private static void EmitCompletionRoutes(StringBuilder sb, string methodSuffix)
  {
    // __complete {index} {*words} - callback route for shell scripts
    sb.AppendLine("    // Built-in: __complete (shell completion callback)");
    sb.AppendLine("    if (routeArgs.Length >= 2 && routeArgs[0] == \"__complete\" && int.TryParse(routeArgs[1], out int completionIndex))");
    sb.AppendLine("    {");
    sb.AppendLine("      string[] completionWords = routeArgs.Length > 2 ? routeArgs[2..] : [];");
    sb.AppendLine("      var completionContext = new global::TimeWarp.Nuru.CompletionContext(completionWords, completionIndex);");
    sb.AppendLine($"      return global::TimeWarp.Nuru.DynamicCompletionHandler.HandleCompletion(completionContext, app.CompletionSourceRegistry, app.ShellCompletionProvider ?? global::TimeWarp.Nuru.EmptyShellCompletionProvider.Instance, app.Terminal);");
    sb.AppendLine("    }");
    sb.AppendLine();

    // --generate-completion {shell} - generates shell completion script
    sb.AppendLine("    // Built-in: --generate-completion (generates shell script)");
    sb.AppendLine("    if (routeArgs is [\"--generate-completion\", var genShell])");
    sb.AppendLine("    {");
    sb.AppendLine("      string appName = global::System.IO.Path.GetFileNameWithoutExtension(global::System.Environment.ProcessPath ?? \"app\");");
    sb.AppendLine("      string script = genShell.ToLowerInvariant() switch");
    sb.AppendLine("      {");
    sb.AppendLine("        \"bash\" => global::TimeWarp.Nuru.DynamicCompletionScriptGenerator.GenerateBash(appName),");
    sb.AppendLine("        \"zsh\" => global::TimeWarp.Nuru.DynamicCompletionScriptGenerator.GenerateZsh(appName),");
    sb.AppendLine("        \"fish\" => global::TimeWarp.Nuru.DynamicCompletionScriptGenerator.GenerateFish(appName),");
    sb.AppendLine("        \"pwsh\" or \"powershell\" => global::TimeWarp.Nuru.DynamicCompletionScriptGenerator.GeneratePowerShell(appName),");
    sb.AppendLine("        _ => throw new global::System.ArgumentException($\"Unknown shell: {genShell}. Supported: bash, zsh, fish, pwsh\")");
    sb.AppendLine("      };");
    sb.AppendLine("      await app.Terminal.WriteLineAsync(script).ConfigureAwait(false);");
    sb.AppendLine("      return 0;");
    sb.AppendLine("    }");
    sb.AppendLine();

    // --install-completion --dry-run {shell?} - preview installation (more specific, check first)
    sb.AppendLine("    // Built-in: --install-completion --dry-run (preview installation)");
    sb.AppendLine("    if (routeArgs is [\"--install-completion\", \"--dry-run\"])");
    sb.AppendLine("    {");
    sb.AppendLine("      string appName = global::System.IO.Path.GetFileNameWithoutExtension(global::System.Environment.ProcessPath ?? \"app\");");
    sb.AppendLine("      return global::TimeWarp.Nuru.InstallCompletionHandler.Install(app.Terminal, appName, null, dryRun: true);");
    sb.AppendLine("    }");
    sb.AppendLine();

    sb.AppendLine("    if (routeArgs is [\"--install-completion\", \"--dry-run\", var dryRunShell])");
    sb.AppendLine("    {");
    sb.AppendLine("      string appName = global::System.IO.Path.GetFileNameWithoutExtension(global::System.Environment.ProcessPath ?? \"app\");");
    sb.AppendLine("      return global::TimeWarp.Nuru.InstallCompletionHandler.Install(app.Terminal, appName, dryRunShell, dryRun: true);");
    sb.AppendLine("    }");
    sb.AppendLine();

    // --install-completion {shell?} - installs completion to shell config (less specific, check after --dry-run)
    sb.AppendLine("    // Built-in: --install-completion (installs shell script)");
    sb.AppendLine("    if (routeArgs is [\"--install-completion\"])");
    sb.AppendLine("    {");
    sb.AppendLine("      string appName = global::System.IO.Path.GetFileNameWithoutExtension(global::System.Environment.ProcessPath ?? \"app\");");
    sb.AppendLine("      return global::TimeWarp.Nuru.InstallCompletionHandler.Install(app.Terminal, appName, null, dryRun: false);");
    sb.AppendLine("    }");
    sb.AppendLine();

    sb.AppendLine("    if (routeArgs is [\"--install-completion\", var installShell])");
    sb.AppendLine("    {");
    sb.AppendLine("      string appName = global::System.IO.Path.GetFileNameWithoutExtension(global::System.Environment.ProcessPath ?? \"app\");");
    sb.AppendLine("      return global::TimeWarp.Nuru.InstallCompletionHandler.Install(app.Terminal, appName, installShell, dryRun: false);");
    sb.AppendLine("    }");
    sb.AppendLine();
  }

  /// <summary>
  /// Emits --interactive / -i flag check. Called BEFORE user routes so catch-all routes don't intercept it.
  /// </summary>
  private static void EmitInteractiveFlag(StringBuilder sb, AppModel app, string methodSuffix)
  {
    if (app.HasRepl)
    {
      sb.AppendLine("    // Built-in: --interactive / -i (REPL mode, opt-in via AddRepl())");
      sb.AppendLine("    // Emitted BEFORE user routes so catch-all routes don't intercept it");
      sb.AppendLine("    if (routeArgs is [\"--interactive\"] or [\"-i\"])");
      sb.AppendLine("    {");
      sb.AppendLine($"      await RunReplAsync{methodSuffix}(app).ConfigureAwait(false);");
      sb.AppendLine("      return 0;");
      sb.AppendLine("    }");
      sb.AppendLine();
    }
  }

  /// <summary>
  /// Emits code to filter out configuration override args before route matching.
  /// Config overrides are identified by: --key=value, --Section:Key=value, /key=value, /Section:Key=value
  /// This allows AddCommandLine(args) to process them while route matching ignores them.
  /// </summary>
  private static void EmitConfigArgFiltering(StringBuilder sb)
  {
    sb.AppendLine("    // Filter out configuration override args before route matching");
    sb.AppendLine("    // Config overrides: --key=value, --Section:Key=value, /key=value, /Section:Key=value");
    sb.AppendLine("    // Original args are still passed to AddCommandLine() for configuration");
    sb.AppendLine("    static bool IsConfigArg(string arg)");
    sb.AppendLine("    {");
    sb.AppendLine("      if (arg.StartsWith(\"--\", global::System.StringComparison.Ordinal))");
    sb.AppendLine("      {");
    sb.AppendLine("        int eqIdx = arg.IndexOf('=');");
    sb.AppendLine("        int colonIdx = arg.IndexOf(':');");
    sb.AppendLine("        return (eqIdx > 2) || (colonIdx > 2);");
    sb.AppendLine("      }");
    sb.AppendLine("      if (arg.StartsWith(\"/\", global::System.StringComparison.Ordinal) && arg.Length > 1 && char.IsLetter(arg[1]))");
    sb.AppendLine("      {");
    sb.AppendLine("        int eqIdx = arg.IndexOf('=');");
    sb.AppendLine("        int colonIdx = arg.IndexOf(':');");
    sb.AppendLine("        return (eqIdx > 1) || (colonIdx > 1);");
    sb.AppendLine("      }");
    sb.AppendLine("      return false;");
    sb.AppendLine("    }");
    sb.AppendLine("    string[] routeArgs = [.. args.Where(arg => !IsConfigArg(arg))];");
    sb.AppendLine();
  }

  /// <summary>
  /// Emits the no-match fallback code.
  /// </summary>
  private static void EmitNoMatch(StringBuilder sb)
  {
    sb.AppendLine("    // No route matched");
    sb.AppendLine("    await app.Terminal.WriteErrorLineAsync(\"Unknown command. Use --help for usage.\").ConfigureAwait(false);");
    sb.AppendLine("    return 1;");
  }

  /// <summary>
  /// Emits the closing of the class and helper methods.
  /// </summary>
  private static void EmitClassEnd(StringBuilder sb, GeneratorModel model, Compilation compilation)
  {
    sb.AppendLine();

    // Emit per-app helper methods (PrintHelp, PrintCapabilities, REPL support)
    // Each app gets its own helpers with only its routes
    for (int appIndex = 0; appIndex < model.Apps.Length; appIndex++)
    {
      AppModel app = model.Apps[appIndex];
      string methodSuffix = model.Apps.Length > 1 ? $"_{appIndex}" : "";

      // Enrich app with version metadata from GeneratorModel
      AppModel enrichedApp = app with
      {
        Version = model.Version,
        CommitHash = model.CommitHash,
        CommitDate = model.CommitDate
      };

      HelpEmitter.Emit(sb, enrichedApp, methodSuffix);
      sb.AppendLine();
      CapabilitiesEmitter.Emit(sb, enrichedApp, methodSuffix);
      sb.AppendLine();

      // REPL support (opt-in via AddRepl())
      if (app.HasRepl)
      {
        ReplEmitter.Emit(sb, enrichedApp, methodSuffix, model.Endpoints, compilation);
        sb.AppendLine();
      }

      // Shell completion support (opt-in via EnableCompletion() or implicitly via AddRepl())
      if (app.HasCompletion || app.HasRepl)
      {
        CompletionEmitter.Emit(sb, enrichedApp, methodSuffix, model.Endpoints, compilation);
        sb.AppendLine();
      }
    }

    // Version is shared (assembly-level, same for all apps)
    // Use first app for metadata, enriched with version info
    AppModel firstAppForVersion = model.Apps[0] with
    {
      Version = model.Version,
      CommitHash = model.CommitHash,
      CommitDate = model.CommitDate
    };
    VersionEmitter.Emit(sb, firstAppForVersion);

    // CheckUpdates is shared (checks same GitHub repo)
    if (model.HasCheckUpdatesRoute)
    {
      sb.AppendLine();
      CheckUpdatesEmitter.Emit(sb, firstAppForVersion);
    }

    sb.AppendLine("}"); // Close class
    sb.AppendLine("}"); // Close namespace
  }
}
