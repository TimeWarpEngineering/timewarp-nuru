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

    // Create the BehaviorContext
    EmitContextCreation(sb, route, routeIndex, ind);

    // Build the nested lambda chain
    // Outermost behavior wraps the next, which wraps the next, ..., which wraps the handler
    EmitNestedBehaviorChain(sb, behaviors, routeIndex, indent, emitHandler);
  }

  /// <summary>
  /// Emits BehaviorContext creation.
  /// </summary>
  private static void EmitContextCreation(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    string indent)
  {
    string commandName = EscapeString(route.FullPattern);
    string commandTypeName = route.Handler.FullTypeName ?? $"Route_{routeIndex}";

    sb.AppendLine($"{indent}// Create behavior context");
    sb.AppendLine($"{indent}var __behaviorContext = new global::TimeWarp.Nuru.BehaviorContext");
    sb.AppendLine($"{indent}{{");
    sb.AppendLine($"{indent}  CommandName = \"{commandName}\",");
    sb.AppendLine($"{indent}  CommandTypeName = \"{commandTypeName}\",");
    sb.AppendLine($"{indent}  CancellationToken = global::System.Threading.CancellationToken.None,");
    sb.AppendLine($"{indent}  Command = null");  // TODO: Pass command instance when available (#316)
    sb.AppendLine($"{indent}}};");
    sb.AppendLine();
  }

  /// <summary>
  /// Emits the nested behavior chain using HandleAsync(context, next) pattern.
  /// </summary>
  private static void EmitNestedBehaviorChain(
    StringBuilder sb,
    ImmutableArray<BehaviorDefinition> behaviors,
    int routeIndex,
    int indent,
    Action emitHandler)
  {
    string ind = new(' ', indent);

    // Build from inside out: handler -> innermost behavior -> ... -> outermost behavior
    // We emit from outermost to innermost, using nested lambdas

    sb.AppendLine($"{ind}// Execute pipeline: behaviors wrap handler");

    // Start with outermost behavior
    EmitBehaviorCall(sb, behaviors, 0, routeIndex, indent, emitHandler);
  }

  /// <summary>
  /// Recursively emits behavior calls, nesting each one.
  /// </summary>
  private static void EmitBehaviorCall(
    StringBuilder sb,
    ImmutableArray<BehaviorDefinition> behaviors,
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

    // Emit: await __behavior_X.Value.HandleAsync(__behaviorContext, async () => { ... });
    sb.AppendLine($"{ind}await {fieldName}.Value.HandleAsync(__behaviorContext, async () =>");
    sb.AppendLine($"{ind}{{");

    // Recursively emit the next behavior or handler
    EmitBehaviorCall(sb, behaviors, behaviorIndex + 1, routeIndex, indent + 2, emitHandler);

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
}
