namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Design-time representation of a route handler.
/// This captures all information needed to generate invocation code.
/// </summary>
/// <param name="HandlerKind">The kind of handler (Delegate, Command, Method)</param>
/// <param name="FullTypeName">Fully qualified type name for command/method handlers</param>
/// <param name="MethodName">Method name for method-based handlers</param>
/// <param name="LambdaBodySource">The lambda body source text for delegate handlers (expression or block)</param>
/// <param name="IsExpressionBody">True if lambda uses expression body syntax, false for block body</param>
/// <param name="Parameters">Parameters that need to be bound from the route (become command properties)</param>
/// <param name="ReturnType">Information about the return type</param>
/// <param name="IsAsync">Whether the handler is async (returns Task/ValueTask)</param>
/// <param name="RequiresCancellationToken">Whether the handler accepts a CancellationToken</param>
/// <param name="RequiresServiceProvider">Whether the handler requires IServiceProvider injection</param>
/// <param name="NestedHandlerTypeName">For Command handlers: fully qualified nested Handler class type name</param>
/// <param name="ConstructorDependencies">For Command handlers: constructor parameters of the nested Handler class</param>
public sealed record HandlerDefinition(
  HandlerKind HandlerKind,
  string? FullTypeName,
  string? MethodName,
  string? LambdaBodySource,
  bool IsExpressionBody,
  ImmutableArray<ParameterBinding> Parameters,
  HandlerReturnType ReturnType,
  bool IsAsync,
  bool RequiresCancellationToken,
  bool RequiresServiceProvider,
  string? NestedHandlerTypeName = null,
  ImmutableArray<ParameterBinding> ConstructorDependencies = default)
{
  /// <summary>
  /// Creates a handler definition for a delegate-based handler.
  /// </summary>
  /// <param name="parameters">Parameters that need to be bound from the route.</param>
  /// <param name="returnType">Information about the return type.</param>
  /// <param name="isAsync">Whether the handler is async.</param>
  /// <param name="lambdaBodySource">The lambda body source text (expression or block).</param>
  /// <param name="isExpressionBody">True for expression body, false for block body.</param>
  /// <param name="requiresCancellationToken">Whether the handler accepts a CancellationToken.</param>
  public static HandlerDefinition ForDelegate(
    ImmutableArray<ParameterBinding> parameters,
    HandlerReturnType returnType,
    bool isAsync,
    string? lambdaBodySource = null,
    bool isExpressionBody = true,
    bool requiresCancellationToken = false)
  {
    return new HandlerDefinition(
      HandlerKind: HandlerKind.Delegate,
      FullTypeName: null,
      MethodName: null,
      LambdaBodySource: lambdaBodySource,
      IsExpressionBody: isExpressionBody,
      Parameters: parameters,
      ReturnType: returnType,
      IsAsync: isAsync,
      RequiresCancellationToken: requiresCancellationToken,
      RequiresServiceProvider: false);
  }

  /// <summary>
  /// Creates a handler definition for a command/query handler with nested Handler class.
  /// </summary>
  /// <param name="commandTypeName">Fully qualified command type name (e.g., "global::MyApp.GreetQuery").</param>
  /// <param name="nestedHandlerTypeName">Fully qualified nested Handler type name (e.g., "global::MyApp.GreetQuery.Handler").</param>
  /// <param name="propertyBindings">Parameters that bind to command properties.</param>
  /// <param name="constructorDependencies">Handler constructor dependencies (services).</param>
  /// <param name="returnType">Information about the return type.</param>
  public static HandlerDefinition ForCommand(
    string commandTypeName,
    string nestedHandlerTypeName,
    ImmutableArray<ParameterBinding> propertyBindings,
    ImmutableArray<ParameterBinding> constructorDependencies,
    HandlerReturnType returnType)
  {
    return new HandlerDefinition(
      HandlerKind: HandlerKind.Command,
      FullTypeName: commandTypeName,
      MethodName: null,
      LambdaBodySource: null,
      IsExpressionBody: false,
      Parameters: propertyBindings,
      ReturnType: returnType,
      IsAsync: true, // Handler.Handle is always async
      RequiresCancellationToken: true,
      RequiresServiceProvider: constructorDependencies.Length > 0,
      NestedHandlerTypeName: nestedHandlerTypeName,
      ConstructorDependencies: constructorDependencies);
  }

  /// <summary>
  /// Creates a handler definition for a method-based handler.
  /// </summary>
  public static HandlerDefinition ForMethod(
    string fullTypeName,
    string methodName,
    ImmutableArray<ParameterBinding> parameters,
    HandlerReturnType returnType,
    bool isAsync,
    bool requiresCancellationToken = false,
    bool requiresServiceProvider = false)
  {
    return new HandlerDefinition(
      HandlerKind: HandlerKind.Method,
      FullTypeName: fullTypeName,
      MethodName: methodName,
      LambdaBodySource: null,
      IsExpressionBody: false,
      Parameters: parameters,
      ReturnType: returnType,
      IsAsync: isAsync,
      RequiresCancellationToken: requiresCancellationToken,
      RequiresServiceProvider: requiresServiceProvider);
  }

  /// <summary>
  /// Gets whether this handler has any parameters to bind.
  /// </summary>
  public bool HasParameters => Parameters.Length > 0;

  /// <summary>
  /// Gets parameters that come from route segments (not injected).
  /// </summary>
  public IEnumerable<ParameterBinding> RouteParameters =>
    Parameters.Where(p => p.Source != BindingSource.Service);

  /// <summary>
  /// Gets parameters that need to be resolved from the service provider.
  /// </summary>
  public IEnumerable<ParameterBinding> ServiceParameters =>
    Parameters.Where(p => p.Source == BindingSource.Service);
}

/// <summary>
/// Specifies the kind of handler.
/// </summary>
public enum HandlerKind
{
  /// <summary>
  /// A delegate passed directly to Map().
  /// </summary>
  Delegate,

  /// <summary>
  /// A command/query with nested Handler class pattern.
  /// </summary>
  Command,

  /// <summary>
  /// A method on a class that will be invoked.
  /// </summary>
  Method
}

/// <summary>
/// Information about a handler's return type.
/// </summary>
/// <param name="FullTypeName">Fully qualified type name</param>
/// <param name="ShortTypeName">Short type name for display</param>
/// <param name="IsVoid">Whether the return type is void</param>
/// <param name="IsTask">Whether it returns Task/ValueTask</param>
/// <param name="UnwrappedTypeName">For Task&lt;T&gt;, the T type name; otherwise same as FullTypeName</param>
public sealed record HandlerReturnType(
  string FullTypeName,
  string ShortTypeName,
  bool IsVoid,
  bool IsTask,
  string? UnwrappedTypeName)
{
  /// <summary>
  /// The void return type.
  /// </summary>
  public static readonly HandlerReturnType Void = new(
    FullTypeName: "void",
    ShortTypeName: "void",
    IsVoid: true,
    IsTask: false,
    UnwrappedTypeName: null);

  /// <summary>
  /// The Task return type (async void).
  /// </summary>
  public static readonly HandlerReturnType Task = new(
    FullTypeName: "global::System.Threading.Tasks.Task",
    ShortTypeName: "Task",
    IsVoid: false,
    IsTask: true,
    UnwrappedTypeName: null);

  /// <summary>
  /// Creates a Task&lt;T&gt; return type.
  /// </summary>
  public static HandlerReturnType TaskOf(string innerTypeName, string shortInnerTypeName) => new(
    FullTypeName: $"global::System.Threading.Tasks.Task<{innerTypeName}>",
    ShortTypeName: $"Task<{shortInnerTypeName}>",
    IsVoid: false,
    IsTask: true,
    UnwrappedTypeName: innerTypeName);

  /// <summary>
  /// Creates a non-async return type.
  /// </summary>
  public static HandlerReturnType Of(string fullTypeName, string shortTypeName) => new(
    FullTypeName: fullTypeName,
    ShortTypeName: shortTypeName,
    IsVoid: false,
    IsTask: false,
    UnwrappedTypeName: null);

  /// <summary>
  /// Gets whether this returns a value (not void or Task without result).
  /// </summary>
  public bool HasValue => !IsVoid && (!IsTask || UnwrappedTypeName is not null);
}
