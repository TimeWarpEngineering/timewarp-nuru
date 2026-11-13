#region Purpose
// Main emitter that generates the RunAsync interceptor methods; the entry point coordinating all other emitters.
#endregion
#region Design
// Key design: Each app instance gets its own interceptor method with isolated routes.
// This ensures route patterns don't leak between different NuruApp instances.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits the complete generated interceptor source file.
/// This is the main entry point for the emission phase.
/// </summary>
internal static partial class InterceptorEmitter
{
  /// <summary>
  /// Generates the complete source code for the interceptor.
  /// </summary>
  /// <param name="model">The generator model containing all apps and routes.</param>
  /// <param name="enumInfo">Precomputed enum member names for enum completions (replaces the live Compilation).</param>
  /// <returns>The generated C# source code.</returns>
  public static string Emit(GeneratorModel model, EquatableArray<EnumInfo> enumInfo)
  {
    StringBuilder sb = new();

    IReadOnlyDictionary<string, ImmutableArray<string>> enumValues = EnumInfoExtractor.BuildLookup(enumInfo);

    EmitHeader(sb);
    EmitInterceptsLocationAttribute(sb);
    EmitNamespaceAndUsings(sb, model);
    EmitClassStart(sb);

    // Emit shared infrastructure (command classes, service fields, behaviors, logging, telemetry)
    EmitCommandClasses(sb, model);

    // Emit service infrastructure for both source-gen DI and runtime DI apps
    // Apps can mix strategies - some may use UseMicrosoftDependencyInjection(), others may not
    IEnumerable<ServiceDefinition> sourceGenServices = model.Apps
      .Where(a => !a.UseMicrosoftDependencyInjection)
      .SelectMany(a => a.Services)
      .Where(s => !s.IsInternalType)
      .DistinctBy(s => s.ImplementationTypeName);
    IEnumerable<ServiceDefinition> runtimeDIServices = model.Apps
      .Where(a => a.UseMicrosoftDependencyInjection)
      .SelectMany(a => a.Services)
      .DistinctBy(s => s.ImplementationTypeName);

    // Emit static Lazy<T> fields for source-gen DI apps
    ImmutableArray<HttpClientConfiguration> sourceGenHttpClients =
    [
      .. model.Apps
        .Where(app => !app.UseMicrosoftDependencyInjection && app.HasHttpClients)
        .SelectMany(app => app.HttpClientConfigurations)
        .DistinctBy(config => config.ServiceTypeName)
    ];
    // Shared static state (service framework fields, behavior instances, the mediator container) is
    // built for whichever app runs, so it reads the running app's factory, set by EnsureServicesInitialized.
    string? currentAppLoggerFactory = model.Apps.Any(HasLoggerFactorySource) ? LoggerExpressions.CurrentAppFactory : null;
    ImmutableArray<BehaviorDefinition> allBehaviors = [.. model.AllBehaviors];
    ImmutableArray<ServiceDefinition> allServices = [.. model.AllServices];
    // Kanban 573: nested handlers get explicit constructor factories in every container that
    // holds the generated mediator, so CA1812 sees them instantiated.
    ImmutableArray<HandlerDefinition> handlerFactories = model.GeneratedMediatorNamespace is null
      ? []
      : CollectHandlerFactories(model);
    bool emitsMediatorContainer = model.SourceGenDIRequiresMediator || model.UsesMicrosoftDependencyInjection;
    if (handlerFactories.Length > 0 && emitsMediatorContainer)
      EmitHandlerFactoryHelper(sb);

    EmitServiceFields(sb, sourceGenServices, model.SourceGenDIRequiresMediator, model.GeneratedMediatorNamespace, currentAppLoggerFactory, sourceGenHttpClients, allBehaviors, allServices, handlerFactories);

    // Emit runtime DI infrastructure for apps that use UseMicrosoftDependencyInjection()
    // Each app gets its own __ConfigureServices and GetServiceProvider methods
    if (model.UsesMicrosoftDependencyInjection)
    {
      EmitRuntimeDIInfrastructure(sb, model.Apps, model.GeneratedMediatorNamespace, handlerFactories);
    }

    EmitLoggingFactoryFields(sb, model);
    EmitHttpClientFields(sb, model);

    // Emit telemetry infrastructure if any app has telemetry enabled
    if (model.HasTelemetry)
    {
      bool enableTracing = model.Apps.Any(a => a.HasTelemetry && (a.TelemetryOptions?.EnableTracing ?? true));
      bool enableMetrics = model.Apps.Any(a => a.HasTelemetry && (a.TelemetryOptions?.EnableMetrics ?? true));
      TelemetryEmitter.EmitTelemetryFields(sb, enableTracing, enableMetrics);
    }

    BehaviorEmitter.EmitBehaviorFields(sb, allBehaviors, allServices, currentAppLoggerFactory);

    // Emit per-app interceptor methods
    // A Build() site is emitted once even when two app models share it.
    HashSet<string> emittedBuildSites = new(StringComparer.Ordinal);
    for (int appIndex = 0; appIndex < model.Apps.Length; appIndex++)
    {
      AppModel app = model.Apps[appIndex];
      EmitBuildInterceptorMethod(sb, app, appIndex, model, emittedBuildSites);
      // Each app's handlers log through its own factory, not the first app's (task 544)
      EmitAppInterceptorMethod(sb, app, appIndex, model, GetLoggerFactorySource(model, appIndex));
      EmitRunReplAsyncInterceptorMethod(sb, app, appIndex, model);
    }

    EmitClassEnd(sb, model, enumValues);

    return sb.ToString();
  }

  /// <summary>
  /// Emits the Build() interceptor for one app. It calls the original Build() and attaches the
  /// app's generated capabilities catalog, so NuruApp.Capabilities works before RunAsync().
  /// </summary>
  private static void EmitBuildInterceptorMethod(StringBuilder sb, AppModel app, int appIndex, GeneratorModel model, HashSet<string> emittedBuildSites)
  {
    if (app.BuildSite is not BuildInterceptSite buildSite || !emittedBuildSites.Add(buildSite.Site.AttributeSyntax))
      return;

    string methodSuffix = model.Apps.Length > 1 ? $"_{appIndex}" : "";

    sb.AppendLine($"  {buildSite.Site.AttributeSyntax}");
    sb.AppendLine($"  public static global::TimeWarp.Nuru.NuruApp Build_Intercepted{methodSuffix}_{emittedBuildSites.Count}(this {buildSite.ReceiverTypeName} builder)");
    sb.AppendLine("  {");
    sb.AppendLine("    global::TimeWarp.Nuru.NuruApp app = builder.Build();");
    sb.AppendLine($"    app.CapabilitiesProvider = GetCapabilities{methodSuffix};");
    if (EmitsExecuteRoute(app, model))
    {
      sb.AppendLine($"    app.JsonBinder = {JsonArgsEmitter.TryBindMethodName(methodSuffix)};");
      sb.AppendLine($"    app.InvocationExecutor = __InvokeJson{methodSuffix};");
      sb.AppendLine($"    app.RouteExecutor = __RunArgs{methodSuffix};");
      EmitProblemRendererAttach(sb, app, model, methodSuffix, fallback: false);
      if (app.ServeRoute is not null)
      {
        sb.AppendLine($"    app.RouteForwarder = new global::TimeWarp.Nuru.Daemon.NuruDaemonClient(__DaemonOptions{methodSuffix}());");
      }
    }

    sb.AppendLine("    return app;");
    sb.AppendLine("  }");
    sb.AppendLine();
  }

  /// <summary>
  /// Emits a single interceptor method for one app.
  /// Each app gets its own method with its own [InterceptsLocation] attributes and routes.
  /// </summary>
  private static void EmitAppInterceptorMethod(StringBuilder sb, AppModel app, int appIndex, GeneratorModel model, string? loggerFactoryFieldName)
  {
    string methodSuffix = model.Apps.Length > 1 ? $"_{appIndex}" : "";

    // Get RunAsync intercept sites from the dictionary
    bool hasRunAsyncSites = app.InterceptSitesByMethod.TryGetSites("RunAsync", out EquatableArray<InterceptSiteModel> runAsyncSites);

    if (EmitsExecuteRoute(app, model))
    {
      EmitExecuteRouteAsyncMethod(sb, app, appIndex, model, methodSuffix, loggerFactoryFieldName);
    }

    // Only emit RunAsync_Intercepted when there are actual intercept sites
    if (!hasRunAsyncSites)
      return;

    // Emit [InterceptsLocation] attributes for this app's RunAsync calls
    foreach (InterceptSiteModel site in runAsyncSites)
    {
      sb.AppendLine($"  {site.AttributeSyntax}");
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
      TelemetryEmitter.EmitTelemetrySetup(sb, app.TelemetryOptions ?? TelemetryModel.Default);
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
  /// True when ExecuteRouteAsync is emitted for <paramref name="app"/>: it has routes or endpoints
  /// (route matching, and NuruApp.InvokeAsync on a Build()-only app), a REPL (the REPL calls
  /// ExecuteRouteAsync), or RunAsync intercept sites (RunAsync_Intercepted calls it). The public bind
  /// and invoke entry points exist exactly when it does.
  /// </summary>
  private static bool EmitsExecuteRoute(AppModel app, GeneratorModel model) =>
    app.HasRoutes
    || app.HasRepl
    || app.InterceptSitesByMethod.TryGetSites("RunAsync", out _)
    || FilterEndpointsForApp(app, model.Endpoints).Length > 0;

  /// <summary>
  /// Routes in route-index order: the app's routes, then its endpoints. Route indices name the
  /// generated command classes and the JSON bind methods.
  /// </summary>
  private static List<RouteDefinition> RoutesInIndexOrder(AppModel app, GeneratorModel model) =>
    [.. app.Routes.Concat(FilterEndpointsForApp(app, model.Endpoints))];

  /// <summary>
  /// Emits the core route execution method that handles all route matching.
  /// This is called by both RunAsync_Intercepted and the REPL command executor.
  /// </summary>
  private static void EmitExecuteRouteAsyncMethod(StringBuilder sb, AppModel app, int appIndex, GeneratorModel model, string methodSuffix, string? loggerFactoryFieldName)
  {
    sb.AppendLine($"  private static async Task<int> ExecuteRouteAsync{methodSuffix}");
    sb.AppendLine("  (");
    sb.AppendLine("    NuruApp app,");
    sb.AppendLine("    string[] args,");
    // The token flows from the REPL's per-command linked CTS (Ctrl+C cancellation, 454-017)
    // and doubles as the declaration that handler CancellationToken parameters bind to.
    // fromRepl is true only for the REPL command callback. --json-args - is rejected there.
    sb.AppendLine("    global::System.Threading.CancellationToken cancellationToken = default,");
    sb.AppendLine("    bool fromRepl = false,");
    // Set only by NuruApp.InvokeAsync / RenderAsync: run (or render) a request bound by TryBind.
    sb.AppendLine("    global::TimeWarp.Nuru.NuruInvocation? __invocation = null");
    sb.AppendLine("  )");
    sb.AppendLine("  {");
    // A previous command may have stored a problem status. This invocation starts clean. A render
    // continues its call: it keeps the exit code the handler set, and problem details overwrite it.
    sb.AppendLine("    if (__invocation?.RenderTerminal is null)");
    sb.AppendLine("      global::TimeWarp.Nuru.NuruCallContext.CurrentExitCode = 0;");
    // Fallback for an app whose Build() call was not intercepted. The forwarder is attached only
    // then, so an app may set RouteForwarder = null after Build() to run every route here.
    if (app.ServeRoute is not null)
    {
      sb.AppendLine("    if (app.CapabilitiesProvider is null)");
      sb.AppendLine($"      app.RouteForwarder ??= new global::TimeWarp.Nuru.Daemon.NuruDaemonClient(__DaemonOptions{methodSuffix}());");
    }

    // Before CapabilitiesProvider: a null provider means Build() was not intercepted.
    EmitProblemRendererAttach(sb, app, model, methodSuffix, fallback: true);
    sb.AppendLine($"    app.CapabilitiesProvider ??= GetCapabilities{methodSuffix};");
    sb.AppendLine($"    app.JsonBinder ??= {JsonArgsEmitter.TryBindMethodName(methodSuffix)};");
    sb.AppendLine($"    app.InvocationExecutor ??= __InvokeJson{methodSuffix};");
    sb.AppendLine($"    app.RouteExecutor ??= __RunArgs{methodSuffix};");

    // --daemon / --no-daemon choose between a running server and this process (AddServeRoute()).
    // They are removed before routing, so they work with every route and never reach the server.
    if (app.ServeRoute is not null)
    {
      sb.AppendLine("    global::TimeWarp.Nuru.NuruForwardMode __forwardMode = global::TimeWarp.Nuru.NuruForwardMode.Auto;");
      sb.AppendLine("    if (__invocation is null && !fromRepl)");
      sb.AppendLine("      args = global::TimeWarp.Nuru.NuruForwarding.PeelFlags(args, out __forwardMode);");
    }

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

    // Public bind and invoke: NuruApp.TryBind / InvokeAsync / RenderAsync.
    JsonArgsEmitter.EmitBindMethods(sb, RoutesInIndexOrder(app, model), app, methodSuffix);
    sb.AppendLine($"  private static global::System.Threading.Tasks.Task<int> __InvokeJson{methodSuffix}(NuruApp app, global::TimeWarp.Nuru.NuruInvocation invocation, global::System.Threading.CancellationToken cancellationToken) =>");
    sb.AppendLine($"    ExecuteRouteAsync{methodSuffix}(app, [], cancellationToken, fromRepl: false, __invocation: invocation);");
    sb.AppendLine();
    // NuruApp.ExecuteAsync: argv routing for code the generator does not intercept (a daemon host).
    sb.AppendLine($"  private static global::System.Threading.Tasks.Task<int> __RunArgs{methodSuffix}(NuruApp app, string[] args, global::System.Threading.CancellationToken cancellationToken) =>");
    sb.AppendLine($"    ExecuteRouteAsync{methodSuffix}(app, args, cancellationToken);");
    sb.AppendLine();

    if (HandlerInvokerEmitter.HasProblemRenderer(app, model.ProblemRendererTypeName))
      HandlerInvokerEmitter.EmitProblemRendererMethod(sb, app, methodSuffix, model.ProblemRendererTypeName);

    if (app.ServeRoute is ServeRouteModel serveRoute)
    {
      EmitDaemonOptionsMethod(sb, serveRoute, methodSuffix);
    }
  }

  /// <summary>
  /// Attaches the app's problem renderer, when it can have one, and its error-output flags (task 508-018). The
  /// Build() interceptor sets them; ExecuteRouteAsync sets them only for an app whose Build() was not intercepted.
  /// </summary>
  private static void EmitProblemRendererAttach(StringBuilder sb, AppModel app, GeneratorModel model, string methodSuffix, bool fallback)
  {
    List<string> assignments = [];
    if (HandlerInvokerEmitter.HasProblemRenderer(app, model.ProblemRendererTypeName))
      assignments.Add($"app.ProblemRenderer = __RenderProblem{methodSuffix};");
    if (app.ProblemRendererForErrors)
      assignments.Add("app.ProblemRendererForErrors = true;");
    if (app.VerbatimRendererOutput)
      assignments.Add("app.VerbatimRendererOutput = true;");
    if (assignments.Count == 0)
      return;

    if (!fallback)
    {
      foreach (string assignment in assignments)
        sb.AppendLine($"    {assignment}");
      return;
    }

    sb.AppendLine("    if (app.CapabilitiesProvider is null)");
    sb.AppendLine("    {");
    foreach (string assignment in assignments)
      sb.AppendLine($"      {assignment}");
    sb.AppendLine("    }");
  }

  /// <summary>
  /// Emits the factory for the TimeWarp.Nuru.Daemon options from the AddServeRoute() arguments.
  /// The serve route and the forwarding client share it, so both use the same socket.
  /// </summary>
  private static void EmitDaemonOptionsMethod(StringBuilder sb, ServeRouteModel serveRoute, string methodSuffix)
  {
    string socketPath = serveRoute.SocketPath is null
      ? "null"
      : $"\"{EmitterStringUtils.EscapeForStringLiteral(serveRoute.SocketPath)}\"";

    sb.AppendLine($"  private static global::TimeWarp.Nuru.Daemon.NuruDaemonOptions __DaemonOptions{methodSuffix}() => new()");
    sb.AppendLine("  {");
    sb.AppendLine($"    SocketPath = {socketPath},");
    sb.AppendLine($"    IdleTimeout = global::System.TimeSpan.FromSeconds({serveRoute.IdleTimeoutSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)}),");
    sb.AppendLine($"    AutoStart = {(serveRoute.AutoStart ? "true" : "false")},");
    sb.AppendLine($"    ExclusiveCalls = {(serveRoute.ExclusiveCalls ? "true" : "false")}");
    sb.AppendLine("  };");
    sb.AppendLine();
  }

  /// <summary>
  /// Emits the RunReplAsync interceptor method for one app.
  /// This intercepts direct RunReplAsync() calls and delegates to the REPL session.
  /// </summary>
  private static void EmitRunReplAsyncInterceptorMethod(StringBuilder sb, AppModel app, int appIndex, GeneratorModel model)
  {
    // Get RunReplAsync intercept sites from the dictionary
    if (!app.InterceptSitesByMethod.TryGetSites("RunReplAsync", out EquatableArray<InterceptSiteModel> replSites))
      return;

    // Only emit if app has REPL enabled
    if (!app.HasRepl)
      return;

    string methodSuffix = model.Apps.Length > 1 ? $"_{appIndex}" : "";

    // Emit [InterceptsLocation] attributes for this app's RunReplAsync calls
    foreach (InterceptSiteModel site in replSites)
    {
      sb.AppendLine($"  {site.AttributeSyntax}");
    }

    // Method signature - matches NuruApp.RunReplAsync signature
    sb.AppendLine($"  public static async global::System.Threading.Tasks.Task<int> RunReplAsync_Intercepted{methodSuffix}");
    sb.AppendLine("  (");
    sb.AppendLine("    this NuruApp app,");
    sb.AppendLine("    global::System.Threading.CancellationToken cancellationToken = default");
    sb.AppendLine("  )");
    sb.AppendLine("  {");
    sb.AppendLine($"    return await RunReplAsync{methodSuffix}(app, cancellationToken).ConfigureAwait(false);");
    sb.AppendLine("  }");
    sb.AppendLine();
  }
}
