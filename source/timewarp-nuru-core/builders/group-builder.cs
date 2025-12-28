namespace TimeWarp.Nuru;

/// <summary>
/// Fluent builder for nested route groups with shared prefixes.
/// Enables hierarchical route organization like "admin config get {key}".
/// </summary>
/// <typeparam name="TParent">The parent builder type to return to via Done().</typeparam>
/// <remarks>
/// <para>
/// GroupBuilder enables nested route prefixes:
/// </para>
/// <code>
/// builder.WithGroupPrefix("admin")
///   .Map("status")
///     .WithHandler(() => "admin status")
///     .Done()
///   .WithGroupPrefix("config")  // Nested group
///     .Map("get {key}")
///       .WithHandler((string key) => $"value-of-{key}")
///       .Done()
///     .Done()  // End config group
///   .Done()    // End admin group
/// </code>
/// <para>
/// The above creates routes: "admin status" and "admin config get {key}"
/// </para>
/// </remarks>
public sealed class GroupBuilder<TParent> : INestedBuilder<TParent>
  where TParent : class
{
  private readonly TParent _parent;
  private readonly string _prefix;
  private readonly Action<Endpoint> _registerEndpoint;
  private readonly ILoggerFactory? _loggerFactory;

  internal GroupBuilder(
    TParent parent,
    string prefix,
    Action<Endpoint> registerEndpoint,
    ILoggerFactory? loggerFactory = null)
  {
    _parent = parent;
    _prefix = prefix;
    _registerEndpoint = registerEndpoint;
    _loggerFactory = loggerFactory;
  }

  /// <summary>
  /// Simplified constructor for no-op mode.
  /// Temporary backward-compatible constructor for incremental migration.
  /// Will be the only constructor after #293-004.
  /// </summary>
  [EditorBrowsable(EditorBrowsableState.Never)]
  internal GroupBuilder(TParent parent)
  {
    _parent = parent;
    _prefix = string.Empty;
    _registerEndpoint = _ => { };
    _loggerFactory = null;
  }

  /// <summary>
  /// Returns to the parent builder.
  /// </summary>
  public TParent Done() => _parent;

  /// <summary>
  /// Adds a route within this group. Pattern will be prefixed with the group prefix.
  /// </summary>
  /// <param name="pattern">The route pattern to add (e.g., "get {key}").</param>
  /// <returns>A GroupEndpointBuilder for configuring the route.</returns>
  /// <example>
  /// <code>
  /// .WithGroupPrefix("admin")
  ///   .Map("status")
  ///     .WithHandler(() => "admin status")
  ///     .Done()  // Returns to GroupBuilder
  ///   .Done()    // Returns to parent
  /// </code>
  /// </example>
  public GroupEndpointBuilder<TParent> Map(string pattern)
  {
    ArgumentNullException.ThrowIfNull(pattern);

    string fullPattern = $"{_prefix} {pattern}";

    Endpoint endpoint = new()
    {
      RoutePattern = fullPattern,
      CompiledRoute = PatternParser.Parse(fullPattern, _loggerFactory)
    };

    _registerEndpoint(endpoint);
    return new GroupEndpointBuilder<TParent>(this, endpoint);
  }

  /// <summary>
  /// Creates a nested group with an additional prefix.
  /// The nested group's prefix is combined with this group's prefix.
  /// </summary>
  /// <param name="prefix">The additional prefix for the nested group.</param>
  /// <returns>A GroupBuilder for the nested group.</returns>
  /// <example>
  /// <code>
  /// .WithGroupPrefix("admin")           // prefix: "admin"
  ///   .WithGroupPrefix("config")        // prefix: "admin config"
  ///     .Map("get {key}")               // route: "admin config get {key}"
  ///       .WithHandler(...)
  ///       .Done()
  ///     .Done()  // Returns to admin GroupBuilder
  ///   .Done()    // Returns to parent
  /// </code>
  /// </example>
  public GroupBuilder<GroupBuilder<TParent>> WithGroupPrefix(string prefix)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
    string nestedPrefix = $"{_prefix} {prefix}";
    return new GroupBuilder<GroupBuilder<TParent>>(this, nestedPrefix, _registerEndpoint, _loggerFactory);
  }
}
