namespace TimeWarp.Nuru;

/// <summary>
/// Nested fluent builder for constructing <see cref="CompiledRoute"/> instances within a fluent chain.
/// </summary>
/// <typeparam name="TParent">The parent builder type to return to.</typeparam>
/// <remarks>
/// <para>
/// This builder wraps <see cref="CompiledRouteBuilder"/> and implements <see cref="INestedBuilder{TParent}"/>
/// to enable fluent API patterns where route configuration returns to the parent context.
/// </para>
/// <para>
/// For standalone route building, use <see cref="CompiledRouteBuilder"/> directly.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Nested usage in fluent chain
/// app.Map(r => r
///     .WithLiteral("deploy")
///     .WithParameter("env")
///     .WithOption("force", "f")
///     .Done())                    // Builds route, returns EndpointBuilder
///     .WithHandler(handler)
///     .Done();                    // Returns to app builder
/// </code>
/// </example>
public sealed class NestedCompiledRouteBuilder<TParent> : INestedBuilder<TParent>
  where TParent : class
{
  private readonly CompiledRouteBuilder _inner = new();
  private readonly TParent _parent;
  private readonly Action<CompiledRoute> _onBuild;

  /// <summary>
  /// Initializes a new instance of the <see cref="NestedCompiledRouteBuilder{TParent}"/> class.
  /// </summary>
  /// <param name="parent">The parent builder to return to when <see cref="Done"/> is called.</param>
  /// <param name="onBuild">Callback invoked with the built route when <see cref="Done"/> is called.</param>
  internal NestedCompiledRouteBuilder(TParent parent, Action<CompiledRoute> onBuild)
  {
    _parent = parent;
    _onBuild = onBuild;
  }

  /// <summary>
  /// Adds a literal segment (e.g., "git", "commit").
  /// </summary>
  /// <param name="value">The literal value that must be matched exactly.</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedCompiledRouteBuilder<TParent> WithLiteral(string value)
  {
    _inner.WithLiteral(value);
    return this;
  }

  /// <summary>
  /// Adds a positional parameter.
  /// </summary>
  /// <param name="name">The parameter name (e.g., "name" for {name} or {name?}).</param>
  /// <param name="type">Optional type constraint (e.g., "int" for {id:int}).</param>
  /// <param name="description">Optional description for help text.</param>
  /// <param name="isOptional">True for optional parameters ({name?}), false for required ({name}).</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedCompiledRouteBuilder<TParent> WithParameter(
    string name,
    string? type = null,
    string? description = null,
    bool isOptional = false)
  {
    _inner.WithParameter(name, type, description, isOptional);
    return this;
  }

  /// <summary>
  /// Adds an option (flag or option with value).
  /// </summary>
  /// <param name="longForm">Long form without dashes (e.g., "force" for --force).</param>
  /// <param name="shortForm">Optional short form without dash (e.g., "f" for -f).</param>
  /// <param name="parameterName">Parameter name for the option value (null for boolean flags).</param>
  /// <param name="expectsValue">True if the option expects a value argument.</param>
  /// <param name="parameterType">Type constraint for the value (e.g., "int" for --port {port:int}).</param>
  /// <param name="parameterIsOptional">True if the option value is optional (e.g., --config {file?}).</param>
  /// <param name="description">Optional description for help text.</param>
  /// <param name="isOptionalFlag">True if the option flag itself is optional (affects specificity scoring only).</param>
  /// <param name="isRepeated">True if the option can be specified multiple times.</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedCompiledRouteBuilder<TParent> WithOption(
    string longForm,
    string? shortForm = null,
    string? parameterName = null,
    bool expectsValue = false,
    string? parameterType = null,
    bool parameterIsOptional = false,
    string? description = null,
    bool isOptionalFlag = false,
    bool isRepeated = false)
  {
    _inner.WithOption(longForm, shortForm, parameterName, expectsValue, parameterType, parameterIsOptional, description, isOptionalFlag, isRepeated);
    return this;
  }

  /// <summary>
  /// Adds a catch-all parameter that captures all remaining arguments.
  /// </summary>
  /// <param name="name">The parameter name (e.g., "args" for {*args}).</param>
  /// <param name="type">Optional type constraint (e.g., "string[]" for {*args:string[]}).</param>
  /// <param name="description">Optional description for help text.</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedCompiledRouteBuilder<TParent> WithCatchAll(
    string name,
    string? type = null,
    string? description = null)
  {
    _inner.WithCatchAll(name, type, description);
    return this;
  }

  /// <summary>
  /// Sets the message type for this route.
  /// </summary>
  /// <param name="messageType">The message type indicating query, command, or idempotent command behavior.</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedCompiledRouteBuilder<TParent> WithMessageType(MessageType messageType)
  {
    _inner.WithMessageType(messageType);
    return this;
  }

  /// <summary>
  /// Builds the route, passes it to the parent via callback, and returns to the parent builder.
  /// </summary>
  /// <returns>The parent builder for continued chaining.</returns>
  public TParent Done()
  {
    CompiledRoute route = _inner.Build();
    _onBuild(route);
    return _parent;
  }
}
