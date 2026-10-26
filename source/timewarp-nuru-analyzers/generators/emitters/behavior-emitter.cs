// Emits pipeline behavior code for cross-cutting concerns.
// Generates behavior instance fields and nested lambda pipeline.

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits code for pipeline behaviors using the HandleAsync(context, next) pattern.
/// Generates:
/// - Static Lazy&lt;T&gt; fields for behavior instances (Singleton pattern)
/// - Nested lambda chain for pipeline execution
/// - BehaviorContext creation with command instance
/// </summary>
internal static class BehaviorEmitter
{
  /// <summary>
  /// Emits static Lazy fields for behavior instances.
  /// Called from InterceptorEmitter after service fields.
  /// </summary>
  /// <param name="sb">StringBuilder to append to.</param>
  /// <param name="behaviors">Behavior definitions to emit.</param>
  /// <param name="services">Service definitions for dependency resolution.</param>
  /// <param name="loggerFactoryFieldName">Name of the logger factory field, or null if no logging configured.</param>
  public static void EmitBehaviorFields(
    StringBuilder sb,
    ImmutableArray<BehaviorDefinition> behaviors,
    ImmutableArray<ServiceDefinition> services,
    string? loggerFactoryFieldName)
  {
    if (behaviors.Length == 0)
      return;

    sb.AppendLine("  // Static behavior fields (Singleton pattern with thread-safe lazy initialization)");

    foreach (BehaviorDefinition behavior in behaviors)
    {
      string fieldName = GetBehaviorFieldName(behavior);
      string constructorArgs = BuildConstructorArgs(behavior, services, loggerFactoryFieldName);

      // A behavior that takes a logger is rebuilt for each app (EmitLoggingBehaviorResets), so it is not readonly.
      string modifiers = HasLoggingDependency(behavior) ? "private static" : "private static readonly";
      sb.AppendLine(
        $"  {modifiers} global::System.Lazy<{behavior.FullTypeName}> {fieldName} = new(() => new {behavior.FullTypeName}({constructorArgs}));");
    }

    sb.AppendLine();
  }

  /// <summary>
  /// Emits, inside EnsureServicesInitialized, a fresh Lazy for each behavior that takes a logger, so a
  /// new app's behavior logs through that app's factory rather than the first app's (task 544).
  /// Behaviors without a logger keep one process-wide instance.
  /// </summary>
  /// <param name="sb">StringBuilder to append to.</param>
  /// <param name="behaviors">Behavior definitions from every app.</param>
  /// <param name="services">Service definitions for dependency resolution.</param>
  /// <param name="loggerFactoryFieldName">The current-app factory field, or null if no app configures logging.</param>
  public static void EmitLoggingBehaviorResets(
    StringBuilder sb,
    ImmutableArray<BehaviorDefinition> behaviors,
    ImmutableArray<ServiceDefinition> services,
    string? loggerFactoryFieldName)
  {
    // Without a logging source every app's behaviors get the same NullLogger, so nothing to rebuild.
    if (loggerFactoryFieldName is null || behaviors.IsDefaultOrEmpty)
      return;

    foreach (BehaviorDefinition behavior in behaviors.Where(HasLoggingDependency))
    {
      string constructorArgs = BuildConstructorArgs(behavior, services, loggerFactoryFieldName);
      sb.AppendLine($"    {GetBehaviorFieldName(behavior)} = new(() => new {behavior.FullTypeName}({constructorArgs}));");
    }
  }

  private static bool HasLoggingDependency(BehaviorDefinition behavior) =>
    behavior.ConstructorDependencies.Any(static dep => LoggerExpressions.IsLoggingServiceType(dep.ParameterTypeName));

  /// <summary>
  /// Emits the pipeline wrapper around handler invocation.
  /// Uses nested lambdas for HandleAsync(context, next) pattern.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="route">The route being handled.</param>
  /// <param name="routeIndex">Index of the route for unique naming.</param>
  /// <param name="behaviors">Behaviors to apply (in registration order, first = outermost).</param>
  /// <param name="services">Registered services.</param>
  /// <param name="indent">Base indentation level.</param>
  /// <param name="emitHandler">Action that emits the handler invocation code.</param>
  /// <param name="methodSuffix">The app's method suffix, part of the generated command class name.</param>
  /// <param name="jsonArgsPath">True when the route is bound from --json-args or TryBind.</param>
  public static void EmitPipelineWrapper(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    ImmutableArray<BehaviorDefinition> behaviors,
    ImmutableArray<ServiceDefinition> services,
    int indent,
    Action emitHandler,
    string methodSuffix,
    bool jsonArgsPath = false)
  {
    if (behaviors.Length == 0)
    {
      // No behaviors - just emit the handler
      emitHandler();
      return;
    }

    string ind = new(' ', indent);

    // Filter behaviors that apply to this route
    ImmutableArray<BehaviorDefinition> applicableBehaviors = FilterBehaviorsForRoute(behaviors, route);

    if (applicableBehaviors.Length == 0)
    {
      // No applicable behaviors - but we still need to create __command for endpoint routes
      // because HandlerInvokerEmitter.Emit() expects it to exist for Command handlers
      if (route.Handler.HandlerKind == HandlerKind.Command)
      {
        EmitCommandCreation(sb, route, routeIndex, methodSuffix, ind, jsonArgsPath);
      }

      emitHandler();
      return;
    }

    // Create command instance (for both delegate and endpoint routes)
    EmitCommandCreation(sb, route, routeIndex, methodSuffix, ind, jsonArgsPath);

    // Create the BehaviorContext
    EmitContextCreation(sb, route, routeIndex, methodSuffix, ind);

    // Build the nested lambda chain
    // Outermost behavior wraps the next, which wraps the next, ..., which wraps the handler
    EmitNestedBehaviorChain(sb, applicableBehaviors, route, routeIndex, indent, emitHandler);
  }

  /// <summary>
  /// Filters behaviors to only those that apply to the given route.
  /// Global behaviors (FilterTypeName is null) always apply.
  /// Filtered behaviors only apply if the route implements the filter interface.
  /// </summary>
  private static ImmutableArray<BehaviorDefinition> FilterBehaviorsForRoute(
    ImmutableArray<BehaviorDefinition> behaviors,
    RouteDefinition route)
  {
    // Get all interface type names implemented by this route
    HashSet<string> routeInterfaces = [.. route.ImplementedInterfaces];

    // For endpoints, we'd also check the command class interfaces
    // This is handled separately in the endpoint extractor

    ImmutableArray<BehaviorDefinition>.Builder applicable = ImmutableArray.CreateBuilder<BehaviorDefinition>();

    foreach (BehaviorDefinition behavior in behaviors)
    {
      if (!behavior.IsFiltered)
      {
        // Global behavior - always applies
        applicable.Add(behavior);
      }
      else if (routeInterfaces.Contains(behavior.FilterTypeName!))
      {
        // Filtered behavior - route implements the filter interface
        applicable.Add(behavior);
      }
      // else: filtered behavior but route doesn't implement interface - skip
    }

    return applicable.ToImmutable();
  }

  /// <summary>
  /// Emits command instance creation for the behavior context.
  /// For delegate routes: creates __NuruCommand_{N} instance.
  /// For endpoints: creates the command class instance.
  /// </summary>
  private static void EmitCommandCreation(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    string methodSuffix,
    string indent,
    bool jsonArgsPath)
  {
    if (route.Handler.HandlerKind == HandlerKind.Delegate)
    {
      // Delegate routes: create generated command class
      string className = CommandClassEmitter.GetCommandClassName(routeIndex, methodSuffix);

      sb.AppendLine($"{indent}// Create command instance for behaviors");
      sb.AppendLine($"{indent}var __command = new {className}");
      sb.AppendLine($"{indent}{{");

      // Set route parameter properties
      foreach (ParameterDefinition param in route.Parameters)
      {
        string propertyName = ToPascalCase(param.Name);
        // Variable names must match what route-matcher-emitter generates
        // Catch-all uses unique name; others need keyword escaping
        string varName = param.IsCatchAll
          ? $"__{param.CamelCaseName}_{routeIndex}"
          : CSharpIdentifierUtils.EscapeIfKeyword(param.CamelCaseName);

        sb.AppendLine($"{indent}  {propertyName} = {varName},");
      }

      // Set option properties
      foreach (OptionDefinition option in route.Options)
      {
        // Use LongForm as property name
        string propertyName = ToPascalCase(option.LongForm ?? option.ParameterName ?? "option");
        // Variable names must match what route-matcher-emitter generates
        string varName = option.IsFlag
          ? CSharpIdentifierUtils.EscapeIfKeyword(ToCamelCase(option.LongForm ?? "flag"))
          : CSharpIdentifierUtils.EscapeIfKeyword(option.ParameterName?.ToLowerInvariant() ?? "value");

        sb.AppendLine($"{indent}  {propertyName} = {varName},");
      }

      sb.AppendLine($"{indent}}};");
      sb.AppendLine();
    }
    else if (route.Handler.HandlerKind == HandlerKind.Command)
    {
      CommandConstructionEmitter.Emit(sb, route, routeIndex, indent, jsonArgsPath);
    }
  }

  /// <summary>
  /// Emits BehaviorContext creation with command instance.
  /// </summary>
  private static void EmitContextCreation(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    string methodSuffix,
    string indent)
  {
    string commandName = EmitterStringUtils.EscapeForStringLiteral(route.FullPattern);
    string commandTypeName = route.Handler.HandlerKind == HandlerKind.Delegate
      ? CommandClassEmitter.GetCommandClassName(routeIndex, methodSuffix)
      : (route.Handler.FullTypeName ?? $"Route_{routeIndex}");

    sb.AppendLine($"{indent}// Create behavior context");
    sb.AppendLine($"{indent}var __behaviorContext = new global::TimeWarp.Nuru.BehaviorContext");
    sb.AppendLine($"{indent}{{");
    sb.AppendLine($"{indent}  CommandName = \"{commandName}\",");
    sb.AppendLine($"{indent}  CommandTypeName = \"{commandTypeName}\",");
    // cancellationToken is ExecuteRouteAsync's parameter (REPL per-command token, 454-017)
    sb.AppendLine($"{indent}  CancellationToken = cancellationToken,");
    sb.AppendLine($"{indent}  Command = __command");
    sb.AppendLine($"{indent}}};");
    sb.AppendLine();
  }

  /// <summary>
  /// Emits the nested behavior chain using HandleAsync(context, next) pattern.
  /// </summary>
  private static void EmitNestedBehaviorChain(
    StringBuilder sb,
    ImmutableArray<BehaviorDefinition> behaviors,
    RouteDefinition route,
    int routeIndex,
    int indent,
    Action emitHandler)
  {
    string ind = new(' ', indent);

    // Build from inside out: handler -> innermost behavior -> ... -> outermost behavior
    // We emit from outermost to innermost, using nested lambdas

    sb.AppendLine($"{ind}// Execute pipeline: behaviors wrap handler");

    // Start with outermost behavior
    EmitBehaviorCall(sb, behaviors, route, 0, routeIndex, indent, emitHandler);
  }

  /// <summary>
  /// Recursively emits behavior calls, nesting each one.
  /// </summary>
  private static void EmitBehaviorCall(
    StringBuilder sb,
    ImmutableArray<BehaviorDefinition> behaviors,
    RouteDefinition route,
    int behaviorIndex,
    int routeIndex,
    int indent,
    Action emitHandler)
  {
    string ind = new(' ', indent);

    if (behaviorIndex >= behaviors.Length)
    {
      // Base case: emit the actual handler
      emitHandler();
      return;
    }

    BehaviorDefinition behavior = behaviors[behaviorIndex];
    string fieldName = GetBehaviorFieldName(behavior);

    if (behavior.IsFiltered)
    {
      // Filtered behavior - create typed context
      string filterType = behavior.FilterTypeName!;

      sb.AppendLine($"{ind}// Create typed context for filtered behavior");
      sb.AppendLine($"{ind}var __typedContext_{behaviorIndex} = new global::TimeWarp.Nuru.BehaviorContext<{filterType}>");
      sb.AppendLine($"{ind}{{");
      sb.AppendLine($"{ind}  CommandName = __behaviorContext.CommandName,");
      sb.AppendLine($"{ind}  CommandTypeName = __behaviorContext.CommandTypeName,");
      sb.AppendLine($"{ind}  CancellationToken = __behaviorContext.CancellationToken,");
      sb.AppendLine($"{ind}  Command = ({filterType})__command");
      sb.AppendLine($"{ind}}};");

      sb.AppendLine($"{ind}await {fieldName}.Value.HandleAsync(__typedContext_{behaviorIndex}, async () =>");
    }
    else
    {
      // Global behavior - use base context
      sb.AppendLine($"{ind}await {fieldName}.Value.HandleAsync(__behaviorContext, async () =>");
    }

    sb.AppendLine($"{ind}{{");

    // Recursively emit the next behavior or handler
    EmitBehaviorCall(sb, behaviors, route, behaviorIndex + 1, routeIndex, indent + 2, emitHandler);

    sb.AppendLine($"{ind}}}).ConfigureAwait(false);");
  }

  /// <summary>
  /// Builds constructor arguments for a behavior instance.
  /// </summary>
  /// <param name="behavior">The behavior definition.</param>
  /// <param name="services">Service definitions for dependency resolution.</param>
  /// <param name="loggerFactoryFieldName">Name of the logger factory field, or null if no logging configured.</param>
  private static string BuildConstructorArgs(
    BehaviorDefinition behavior,
    ImmutableArray<ServiceDefinition> services,
    string? loggerFactoryFieldName)
  {
    if (behavior.ConstructorDependencies.Length == 0)
      return string.Empty;

    List<string> args = [];

    foreach (ParameterBinding dep in behavior.ConstructorDependencies)
    {
      string resolution = ResolveServiceForBehavior(dep.ParameterTypeName, services, loggerFactoryFieldName);
      args.Add(resolution);
    }

    return string.Join(", ", args);
  }

  /// <summary>
  /// Resolves a service type to its instantiation expression for behavior constructors.
  /// </summary>
  /// <param name="serviceTypeName">The type name of the service to resolve.</param>
  /// <param name="services">Registered service definitions.</param>
  /// <param name="loggerFactoryFieldName">Name of the logger factory field, or null if no logging configured.</param>
  private static string ResolveServiceForBehavior(
    string? serviceTypeName,
    ImmutableArray<ServiceDefinition> services,
    string? loggerFactoryFieldName)
  {
    if (string.IsNullOrEmpty(serviceTypeName))
      return "default!";

    // Well-known services
    // Behaviors are static Lazy singletons, built on first use after EnsureServicesInitialized:
    // the routing terminal follows each call's terminal.
    if (serviceTypeName is "global::TimeWarp.Terminal.ITerminal" or "TimeWarp.Terminal.ITerminal")
      return "__fw_ITerminal!";

    if (serviceTypeName.Contains("IConfiguration", StringComparison.Ordinal))
      return "configuration";

    if (serviceTypeName is "global::TimeWarp.Nuru.NuruApp" or "TimeWarp.Nuru.NuruApp" or "NuruApp")
      return "app";

    // ILogger<T>, ILogger, ILoggerFactory - the shared logger path (task 541)
    if (LoggerExpressions.IsLoggingServiceType(serviceTypeName))
      return LoggerExpressions.Resolve(serviceTypeName, loggerFactoryFieldName);

    if (FrameworkServices.IsMediatorServiceType(serviceTypeName))
      return FrameworkServices.MediatorExpression;

    // Find matching service registration
    ServiceDefinition? service = services.FirstOrDefault(s =>
      s.ServiceTypeName == serviceTypeName ||
      NormalizeTypeName(s.ServiceTypeName) == NormalizeTypeName(serviceTypeName));

    if (service is not null)
    {
      if (service.Lifetime == ServiceLifetime.Transient)
        return ServiceScopeExpressions.Construct(service, services, ServiceResolveMode.Command);

      return ServiceScopeExpressions.Read(service, services, ServiceResolveMode.Command);
    }

    // Fallback: emit error
    return $"default! /* ERROR: Service {serviceTypeName} not registered */";
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
  /// Gets the field name for a behavior's Lazy instance.
  /// </summary>
  internal static string GetBehaviorFieldName(BehaviorDefinition behavior)
  {
    return $"__behavior_{behavior.SafeIdentifierName}";
  }

  /// <summary>
  /// Converts a string to PascalCase.
  /// </summary>
  private static string ToPascalCase(string value)
  {
    if (string.IsNullOrEmpty(value))
      return value;

    return char.ToUpperInvariant(value[0]) + value[1..];
  }

  private static string ToCamelCase(string value) => CSharpIdentifierUtils.ToCamelCase(value);
}
