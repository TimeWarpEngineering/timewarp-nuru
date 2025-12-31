// Emits pipeline behavior code for cross-cutting concerns.
// Generates behavior instance fields, state classes, and pipeline wrapping.

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits code for pipeline behaviors.
/// Generates:
/// - Static Lazy&lt;T&gt; fields for behavior instances (Singleton pattern)
/// - Generated State classes for behaviors without custom state
/// - Context/state creation code per route
/// - Pipeline wrapping (OnBefore → Handler → OnAfter, with OnError in catch)
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
  /// Emits generated State classes for behaviors that don't define their own.
  /// Called from InterceptorEmitter in the class body.
  /// </summary>
  public static void EmitGeneratedStateClasses(StringBuilder sb, ImmutableArray<BehaviorDefinition> behaviors)
  {
    // Find behaviors without custom state - materialize to array to avoid multiple enumeration
    BehaviorDefinition[] behaviorsWithoutState = [.. behaviors.Where(b => !b.HasCustomState)];

    if (behaviorsWithoutState.Length == 0)
      return;

    sb.AppendLine("  // Generated State classes for behaviors without custom state");

    foreach (BehaviorDefinition behavior in behaviorsWithoutState)
    {
      sb.AppendLine($"  private sealed class {behavior.ShortTypeName}_GeneratedState : global::TimeWarp.Nuru.BehaviorContext {{ }}");
    }

    sb.AppendLine();
  }

  /// <summary>
  /// Emits the pipeline wrapper around handler invocation.
  /// This wraps the handler with OnBefore/OnAfter/OnError calls.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="route">The route being handled.</param>
  /// <param name="routeIndex">Index of the route for unique naming.</param>
  /// <param name="behaviors">Behaviors to apply (already filtered by scope if needed).</param>
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
    string ind2 = new(' ', indent + 2);

    // Create context/state instances
    EmitContextCreation(sb, route, routeIndex, behaviors, ind);

    // OnBefore calls (in registration order)
    sb.AppendLine($"{ind}// OnBefore (in registration order)");
    foreach (BehaviorDefinition behavior in behaviors)
    {
      string fieldName = GetBehaviorFieldName(behavior);
      string stateName = GetStateVariableName(behavior, routeIndex);
      sb.AppendLine($"{ind}await {fieldName}.Value.OnBeforeAsync({stateName}).ConfigureAwait(false);");
    }

    sb.AppendLine();
    sb.AppendLine($"{ind}try");
    sb.AppendLine($"{ind}{{");

    // Handler invocation (indented inside try)
    emitHandler();

    sb.AppendLine();

    // OnAfter calls (reverse order, only on success)
    sb.AppendLine($"{ind2}// OnAfter (reverse order, only on success)");
    foreach (BehaviorDefinition behavior in behaviors.Reverse())
    {
      string fieldName = GetBehaviorFieldName(behavior);
      string stateName = GetStateVariableName(behavior, routeIndex);
      sb.AppendLine($"{ind2}await {fieldName}.Value.OnAfterAsync({stateName}).ConfigureAwait(false);");
    }

    sb.AppendLine($"{ind}}}");
    sb.AppendLine($"{ind}catch (global::System.Exception __behaviorException)");
    sb.AppendLine($"{ind}{{");

    // OnError calls (reverse order)
    sb.AppendLine($"{ind2}// OnError (reverse order)");
    foreach (BehaviorDefinition behavior in behaviors.Reverse())
    {
      string fieldName = GetBehaviorFieldName(behavior);
      string stateName = GetStateVariableName(behavior, routeIndex);
      sb.AppendLine($"{ind2}await {fieldName}.Value.OnErrorAsync({stateName}, __behaviorException).ConfigureAwait(false);");
    }

    sb.AppendLine($"{ind2}throw;");
    sb.AppendLine($"{ind}}}");
  }

  /// <summary>
  /// Emits context/state instance creation for each behavior.
  /// </summary>
  private static void EmitContextCreation(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    ImmutableArray<BehaviorDefinition> behaviors,
    string indent)
  {
    sb.AppendLine($"{indent}// Create behavior context/state instances");

    foreach (BehaviorDefinition behavior in behaviors)
    {
      string stateName = GetStateVariableName(behavior, routeIndex);
      string stateTypeName = behavior.HasCustomState
        ? behavior.StateTypeName!
        : $"{behavior.ShortTypeName}_GeneratedState";

      // Escape the route pattern for use as CommandName
      string commandName = EscapeString(route.FullPattern);
      string commandTypeName = route.Handler.FullTypeName ?? $"Route_{routeIndex}";

      sb.AppendLine($"{indent}var {stateName} = new {stateTypeName}");
      sb.AppendLine($"{indent}{{");
      sb.AppendLine($"{indent}  CommandName = \"{commandName}\",");
      sb.AppendLine($"{indent}  CommandTypeName = \"{commandTypeName}\",");
      sb.AppendLine($"{indent}  CancellationToken = global::System.Threading.CancellationToken.None");
      sb.AppendLine($"{indent}}};");
    }

    sb.AppendLine();
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
  /// Gets the variable name for a behavior's state instance.
  /// </summary>
  private static string GetStateVariableName(BehaviorDefinition behavior, int routeIndex)
  {
    return $"__state_{behavior.ShortTypeName}_{routeIndex}";
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
