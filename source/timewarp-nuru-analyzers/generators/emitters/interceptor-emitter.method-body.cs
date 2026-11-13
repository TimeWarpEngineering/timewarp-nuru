#region Purpose
// InterceptorEmitter partial: route dispatch method body, exclusive gates and endpoint filtering.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

internal static partial class InterceptorEmitter
{
  private static void EmitMethodBody(StringBuilder sb, AppModel app, int appIndex, GeneratorModel model, string? loggerFactoryFieldName)
  {
    // Full sources when AddConfiguration() ran or a handler takes configuration.
    // A blank root still satisfies EnsureServicesInitialized.
    if (app.HasConfiguration)
    {
      ConfigurationEmitter.Emit(sb);
    }
    else
    {
      sb.AppendLine("    // Minimal configuration for service initialization");
      sb.AppendLine("    global::Microsoft.Extensions.Configuration.IConfigurationRoot configuration =");
      sb.AppendLine("      new global::Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();");
    }

    // Peel --json-args before the command scope so the REPL host check sees routeArgs
    // (config args stripped), including AutoStartWhenEmpty on an empty route.
    // A call in a NuruCallContext (daemon, MCP host) is isolated: its own __NuruCall, scoped like a REPL
    // command (Scoped per call), and its session and command instances disposed when it returns.
    sb.AppendLine("    bool __isolatedCall = !fromRepl && global::TimeWarp.Nuru.NuruCallContext.Current is not null;");
    sb.AppendLine("    if (__isolatedCall)");
    sb.AppendLine("      __nuruCall.Value = new __NuruCall();");
    sb.AppendLine("    __replCommandActive = fromRepl || __isolatedCall;");
    JsonArgsEmitter.EmitPrepare(sb, app);

    // The REPL host must not open a command scope. Nested commands open their own.
    // await using disposes that scope on every later return, including early exits.
    string enteringRepl = EnteringReplExpression(app);
    sb.AppendLine("    // Initialize services with app and configuration");
    sb.AppendLine($"    bool __enteringRepl = __invocation is null && ({enteringRepl});");
    string scopeSuffix = model.Apps.Length > 1 ? $"_{appIndex}" : "";
    string ensureArgs = model.Apps.Any(HasLoggerFactorySource)
      ? $"app, configuration, {loggerFactoryFieldName ?? "null"}"
      : "app, configuration";
    if (app.UseMicrosoftDependencyInjection)
    {
      sb.AppendLine($"    EnsureServicesInitialized({ensureArgs});");
      sb.AppendLine($"    await using global::TimeWarp.Nuru.NuruCommandLease __nuruLease = global::TimeWarp.Nuru.NuruCommandLease.Enter(GetServiceProvider{scopeSuffix}(app), fromRepl, static scope => __commandScope{scopeSuffix} = scope, hostRepl: __enteringRepl, isolatedCall: __isolatedCall);");
    }
    else
    {
      // Services are built when a handler or behavior first reads them, not here.
      sb.AppendLine($"    EnsureServicesInitialized({ensureArgs});");
      sb.AppendLine("    await using global::TimeWarp.Nuru.NuruAsyncLease __nuruLease = __enteringRepl");
      sb.AppendLine("      ? global::TimeWarp.Nuru.NuruAsyncLease.Idle()");
      sb.AppendLine("      : global::TimeWarp.Nuru.NuruAsyncLease.For(fromRepl, EndReplCommandServicesAsync, EndSingleRunServicesAsync);");
    }

    sb.AppendLine();

    // --interactive / -i must be checked BEFORE user routes so catch-all routes don't intercept it
    string methodSuffix = model.Apps.Length > 1 ? $"_{appIndex}" : "";

    // Route matching - emit this app's routes in specificity order (highest first)
    // User routes are emitted BEFORE built-ins so users can override --help, --version, etc.
    // Filter endpoints based on app's discovery mode (DiscoverEndpoints or Map<T> calls)
    ImmutableArray<RouteDefinition> endpointsForApp = FilterEndpointsForApp(app, model.Endpoints);
    IEnumerable<RouteDefinition> allRoutes = app.RoutesBySpecificity.Concat(endpointsForApp);

    // Build a lookup from route to its original index for command class naming
    // IMPORTANT: Use only this app's routes (plus filtered endpoints), not all routes from all apps
    // Using model.AllRoutes would cause route index collisions between different apps
    List<RouteDefinition> allRoutesOrdered = RoutesInIndexOrder(app, model);

    // A request bound by NuruApp.TryBind runs here, after services and the command scope.
    JsonArgsEmitter.EmitInvocationDispatch(sb, allRoutesOrdered, app, methodSuffix, loggerFactoryFieldName, model.HasGeneratedMediator);

    // JSON selection returns. Calls without --json-args leave __jsonDoc null and fall through.
    JsonArgsEmitter.EmitDispatch(sb, allRoutesOrdered, app, methodSuffix, loggerFactoryFieldName, model.HasGeneratedMediator, forwardRoutes: app.ServeRoute is not null);

    // Route matchers record an option-shaped token no route declares; the no-match fallback names it.
    sb.AppendLine("    string? __unknownOption = null;");
    sb.AppendLine("    global::System.Collections.Generic.HashSet<string> __unknownOptionForms = [];");
    sb.AppendLine("    if (!__jsonBuiltInOnly)");
    sb.AppendLine("    {");

    EmitInteractiveFlag(sb, app, methodSuffix);

    // Shared-prefix help prints every route whose leading literals equal the typed words.
    // It runs before group-summary help so those routes win when the group prefix is the
    // whole literal prefix (an empty subcommand together with "{env}").
    HashSet<RouteDefinition> sharedPrefixHelpRoutes =
      RouteHelpEmitter.EmitSharedPrefixHelpChecks(sb, allRoutesOrdered, 4);

    // Group-level help checks (before user routes so groups get priority over a single route)
    RouteHelpEmitter.EmitGroupHelpChecks(sb, allRoutesOrdered, 4);

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

      RouteMatcherEmitter.Emit(sb, route, routeIndex, app.Services, app.Behaviors, app.CustomConverters, loggerFactoryFieldName, app.UseMicrosoftDependencyInjection, methodSuffix, app.HttpClientConfigurations, sharedPrefixHelpRoutes, app.UseDirectInvoke, model.HasGeneratedMediator, forwardRoutes: app.ServeRoute is not null, verbatimRendererOutput: app.VerbatimRendererOutput);
    }

    sb.AppendLine("    }");
    sb.AppendLine();

    // Built-in flags: --help, --version, --capabilities
    // Emitted AFTER user routes so users can override default behavior
    EmitBuiltInFlags(sb, app, methodSuffix);

    // No match fallback
    EmitNoMatch(sb);
  }

  /// <summary>
  /// For an <c>[Exclusive]</c> / <c>.Exclusive()</c> route, takes the app's exclusive gate before the
  /// behaviors and handler; the lease releases it when the dispatch block exits. Without a call context
  /// (a plain CLI run) the lease is a no-op, since nothing runs concurrently there.
  /// </summary>
  /// <remarks>
  /// With <paramref name="openBlock"/>, the lease opens a block that <see cref="EmitExclusiveGateEnd"/>
  /// closes: argv matching jumps past the dispatch with <c>goto</c>, which C# forbids across a using
  /// declaration in the same block (CS8648).
  /// </remarks>
  internal static void EmitExclusiveGate(StringBuilder sb, RouteDefinition route, string indent, bool openBlock = false)
  {
    if (!route.IsExclusive)
      return;

    if (openBlock)
      sb.AppendLine($"{indent}{{");
    sb.AppendLine($"{indent}await using global::TimeWarp.Nuru.NuruExclusiveLease __exclusive = await global::TimeWarp.Nuru.NuruExclusiveLease.EnterAsync(app, cancellationToken).ConfigureAwait(false);");
  }

  /// <summary>Closes the block <see cref="EmitExclusiveGate"/> opened with <c>openBlock</c>.</summary>
  internal static void EmitExclusiveGateEnd(StringBuilder sb, RouteDefinition route, string indent)
  {
    if (route.IsExclusive)
      sb.AppendLine($"{indent}}}");
  }

  /// <summary>
  /// Fails the matched route when its handler was not visible at build time, or when a visible
  /// handler that is not nested has no generated mediator to send through.
  /// Built-ins and other routes run first; one missing handler does not exit the process.
  /// </summary>
  /// <remarks>
  /// TimeWarp.Mediator 15 dispatches only the handlers in its compile-time graph and registers
  /// concrete handler types, not handler interfaces, so there is nothing to probe at run time.
  /// Its graph is this compilation plus public types in referenced assemblies, which is what
  /// Nuru's visibility scan covers, so a handler Nuru cannot see cannot be dispatched either.
  /// </remarks>
  internal static void EmitMissingHandlerOnDispatch(
    StringBuilder sb,
    RouteDefinition route,
    bool useRuntimeDI,
    bool hasGeneratedMediator,
    string indent)
  {
    // A visible handler that is not nested has nowhere to send when this compilation
    // has no generated mediator. Exit 1 here, before any INuruBehavior lambda opens.
    if (route.Handler.HandlerKind == HandlerKind.Command
        && route.Handler.HandlerVisible
        && route.Handler.NestedHandlerTypeName is null
        && !useRuntimeDI
        && !hasGeneratedMediator)
    {
      sb.AppendLine($"{indent}app.Terminal.WriteErrorLine(\"The TimeWarp.Mediator generated mediator is not available in this compilation.\");");
      sb.AppendLine($"{indent}return 1;");
      return;
    }

    if (!IsMissingHandler(route.Handler))
      return;

    string typeName = route.Handler.FullTypeName ?? route.OriginalPattern;
    sb.AppendLine($"{indent}// {typeName} had no visible handler at build time.");
    sb.AppendLine($"{indent}app.Terminal.WriteErrorLine(\"No handler is registered for {typeName}.\");");
    sb.AppendLine($"{indent}return 1;");
  }

  /// <summary>
  /// True when the route is a mediator message whose handler was not visible at build time.
  /// <see cref="EmitMissingHandlerOnDispatch"/> has already exited 1 for it, so nothing dispatches.
  /// </summary>
  internal static bool IsMissingHandler(HandlerDefinition handler) =>
    !handler.HandlerVisible && !string.IsNullOrEmpty(handler.HandlerInterfaceName);

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
    // Priority 1: FilterGroupTypeNames (DiscoverEndpoints(typeof(...)))
    // Must be checked BEFORE DiscoverEndpoints because both set DiscoverEndpoints=true
    if (!app.FilterGroupTypeNames.IsDefaultOrEmpty && app.FilterGroupTypeNames.Length > 0)
    {
      return
      [
        .. allEndpoints
          .Where(e => !e.IsExplicitOnly && MatchesFilterGroupType(e, app.FilterGroupTypeNames))
          .Select(e => StripGroupPrefixAboveMatchedType(e, app.FilterGroupTypeNames))
      ];
    }

    // Priority 2: Explicit Map<T>() calls
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

    // Priority 3: DiscoverEndpoints() without filters - include all endpoints
    // A referenced endpoint found only through Map<T>() belongs to the app that maps it.
    if (app.DiscoverEndpoints)
      return [.. allEndpoints.Where(static e => !e.IsExplicitOnly)];

    // Default: no endpoints (test isolation)
    return [];
  }

  /// <summary>
  /// Checks if an endpoint's group type hierarchy matches any of the filter types.
  /// </summary>
  private static bool MatchesFilterGroupType
  (
    RouteDefinition endpoint,
    ImmutableArray<string> filterTypeNames
  )
  {
    // Ungrouped endpoints don't match group filters
    if (endpoint.GroupTypeHierarchy.IsDefaultOrEmpty)
      return false;

    return endpoint.GroupTypeHierarchy.Any(typeName =>
      filterTypeNames.Any(filterType =>
        string.Equals(filterType, typeName, StringComparison.OrdinalIgnoreCase)));
  }

  /// <summary>
  /// Computes the stripped group prefix for an endpoint.
  /// Strips prefixes from types above the matched filter type.
  /// </summary>
  private static RouteDefinition StripGroupPrefixAboveMatchedType
  (
    RouteDefinition endpoint,
    ImmutableArray<string> filterTypeNames
  )
  {
    if (endpoint.GroupTypeHierarchy.IsDefaultOrEmpty || string.IsNullOrEmpty(endpoint.GroupPrefix))
    {
      // No group info - no changes needed
      return endpoint;
    }

    // Find the matched index in the type hierarchy
    // Type hierarchy is in root-to-leaf order (most general to most specific)
    int? matchedIndex = null;
    for (int i = 0; i < endpoint.GroupTypeHierarchy.Length; i++)
    {
      string typeName = endpoint.GroupTypeHierarchy[i];
      if (filterTypeNames.Any(filterType =>
        string.Equals(filterType, typeName, StringComparison.OrdinalIgnoreCase)))
      {
        matchedIndex = i;
        break;
      }
    }

    if (matchedIndex is null)
    {
      // No match - shouldn't happen since we already filtered, but be safe
      return endpoint;
    }

    // Split the current prefix into individual components
    // Prefix is built from root-to-leaf order, so we can slice by index
    string[] allPrefixes = endpoint.GroupPrefix.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    // The number of non-empty prefixes in the hierarchy should match the length
    // We keep prefixes from matched index onwards
    if (matchedIndex.Value >= allPrefixes.Length)
    {
      // Matched type has no prefix, use empty prefix
      return endpoint with { GroupPrefix = null };
    }

    // For root groups (matchedIndex == 0), also strip the first prefix segment
    // This enables creating subset editions from root groups with the root prefix stripped
    int startIndex = matchedIndex.Value == 0
      ? matchedIndex.Value + 1 // Strip root prefix
      : matchedIndex.Value; // Keep matched type's prefix
    string[] effectivePrefixes = allPrefixes[startIndex..];
    string? effectivePrefix = effectivePrefixes.Length > 0
      ? string.Join(" ", effectivePrefixes)
      : null;

    return endpoint with { GroupPrefix = effectivePrefix };
  }
}
