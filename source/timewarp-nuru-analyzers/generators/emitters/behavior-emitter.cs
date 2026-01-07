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
  public static void EmitBehaviorFields(
    StringBuilder sb,
    ImmutableArray<BehaviorDefinition> behaviors,
    ImmutableArray<ServiceDefinition> services)
  {
    if (behaviors.Length == 0)
      return;

    sb.AppendLine("  // Static behavior fields (Singleton pattern with thread-safe lazy initialization)");

    foreach (BehaviorDefinition behavior in behaviors)
    {
      string fieldName = GetBehaviorFieldName(behavior);
      string constructorArgs = BuildConstructorArgs(behavior, services);

      sb.AppendLine(
        $"  private static readonly global::System.Lazy<{behavior.FullTypeName}> {fieldName} = new(() => new {behavior.FullTypeName}({constructorArgs}));");
    }

    sb.AppendLine();
  }

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
  public static void EmitPipelineWrapper(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    ImmutableArray<BehaviorDefinition> behaviors,
    ImmutableArray<ServiceDefinition> services,
    int indent,
    Action emitHandler)
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
      // No applicable behaviors - just emit the handler
      emitHandler();
      return;
    }

    // Create command instance (for delegate routes)
    EmitCommandCreation(sb, route, routeIndex, ind);

    // Create the BehaviorContext
    EmitContextCreation(sb, route, routeIndex, ind);

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

    // For attributed routes, we'd also check the command class interfaces
    // This is handled separately in the attributed route extractor

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
  /// Emits command instance creation for delegate routes.
  /// </summary>
  private static void EmitCommandCreation(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    string indent)
  {
    // Only emit for delegate routes - attributed routes already have __command
    if (route.Handler.HandlerKind != HandlerKind.Delegate)
      return;

    string className = CommandClassEmitter.GetCommandClassName(routeIndex);

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

  /// <summary>
  /// Emits BehaviorContext creation with command instance.
  /// </summary>
  private static void EmitContextCreation(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    string indent)
  {
    string commandName = EscapeString(route.FullPattern);
    string commandTypeName = route.Handler.HandlerKind == HandlerKind.Delegate
      ? CommandClassEmitter.GetCommandClassName(routeIndex)
      : (route.Handler.FullTypeName ?? $"Route_{routeIndex}");

    sb.AppendLine($"{indent}// Create behavior context");
    sb.AppendLine($"{indent}var __behaviorContext = new global::TimeWarp.Nuru.BehaviorContext");
    sb.AppendLine($"{indent}{{");
    sb.AppendLine($"{indent}  CommandName = \"{commandName}\",");
    sb.AppendLine($"{indent}  CommandTypeName = \"{commandTypeName}\",");
    sb.AppendLine($"{indent}  CancellationToken = global::System.Threading.CancellationToken.None,");
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
  private static string BuildConstructorArgs(BehaviorDefinition behavior, ImmutableArray<ServiceDefinition> services)
  {
    if (behavior.ConstructorDependencies.Length == 0)
      return string.Empty;

    List<string> args = [];

    foreach (ParameterBinding dep in behavior.ConstructorDependencies)
    {
      string resolution = ResolveServiceForBehavior(dep.ParameterTypeName, services);
      args.Add(resolution);
    }

    return string.Join(", ", args);
  }

  /// <summary>
  /// Resolves a service type to its instantiation expression for behavior constructors.
  /// </summary>
  private static string ResolveServiceForBehavior(string? serviceTypeName, ImmutableArray<ServiceDefinition> services)
  {
    if (string.IsNullOrEmpty(serviceTypeName))
      return "default!";

    // Well-known services
    if (serviceTypeName is "global::TimeWarp.Terminal.ITerminal" or "TimeWarp.Terminal.ITerminal")
      return "app.Terminal";

    if (serviceTypeName.Contains("IConfiguration", StringComparison.Ordinal))
      return "configuration";

    // ILogger<T> - create a null logger for now (TODO: proper logger resolution)
    if (serviceTypeName.Contains("ILogger", StringComparison.Ordinal))
      return $"global::Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance.CreateLogger<{ExtractLoggerTypeArg(serviceTypeName)}>()";

    // Find matching service registration
    ServiceDefinition? service = services.FirstOrDefault(s =>
      s.ServiceTypeName == serviceTypeName ||
      NormalizeTypeName(s.ServiceTypeName) == NormalizeTypeName(serviceTypeName));

    if (service is not null)
    {
      string fieldName = InterceptorEmitter.GetServiceFieldName(service.ImplementationTypeName);
      return $"{fieldName}.Value";
    }

    // Fallback: emit error
    return $"default! /* ERROR: Service {serviceTypeName} not registered */";
  }

  /// <summary>
  /// Extracts the type argument from ILogger&lt;T&gt;.
  /// </summary>
  private static string ExtractLoggerTypeArg(string loggerTypeName)
  {
    int start = loggerTypeName.IndexOf('<', StringComparison.Ordinal);
    int end = loggerTypeName.LastIndexOf('>');

    if (start >= 0 && end > start)
      return loggerTypeName[(start + 1)..end];

    return "object";
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
  /// Escapes a string for use in generated code.
  /// </summary>
  private static string EscapeString(string value)
  {
    return value
      .Replace("\\", "\\\\", StringComparison.Ordinal)
      .Replace("\"", "\\\"", StringComparison.Ordinal);
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

  /// <summary>
  /// Converts a string to camelCase.
  /// Handles kebab-case (e.g., "no-cache" -> "noCache").
  /// </summary>
  private static string ToCamelCase(string value)
  {
    if (string.IsNullOrEmpty(value))
      return value;

    // Handle kebab-case by converting to PascalCase first, then camelCase
    string[] parts = value.Split('-');
    StringBuilder result = new();

    for (int i = 0; i < parts.Length; i++)
    {
      string part = parts[i];
      if (string.IsNullOrEmpty(part))
        continue;

      if (i == 0)
      {
        result.Append(char.ToLowerInvariant(part[0]));
      }
      else
      {
        result.Append(char.ToUpperInvariant(part[0]));
      }

      if (part.Length > 1)
      {
        result.Append(part[1..]);
      }
    }

    return result.ToString();
  }
}
