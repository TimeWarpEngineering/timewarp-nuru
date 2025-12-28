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
  private readonly TParent ParentBuilder;

  internal GroupBuilder(TParent parent)
  {
    ParentBuilder = parent;
  }

  // ============================================================================
  // DEAD CODE - To be removed in #293-006
  // Old constructor with unused parameters for backward compatibility
  // ============================================================================

#pragma warning disable IDE0060 // Remove unused parameter

  /// <summary>
  /// Backward-compatible constructor for incremental migration.
  /// Will be removed in #293-006.
  /// </summary>
  [EditorBrowsable(EditorBrowsableState.Never)]
  internal GroupBuilder(
    TParent parent,
    string prefix,
    Action<Endpoint> registerEndpoint,
    ILoggerFactory? loggerFactory = null) : this(parent)
  {
    _ = prefix;
    _ = registerEndpoint;
    _ = loggerFactory;
  }

#pragma warning restore IDE0060

  /// <summary>
  /// Returns to the parent builder.
  /// </summary>
  public TParent Done() => ParentBuilder;

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
    // Source generator extracts grouped pattern at compile time
    _ = pattern;
    return new GroupEndpointBuilder<TParent>(this);
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
    // Source generator handles nested prefix at compile time
    _ = prefix;
    return new GroupBuilder<GroupBuilder<TParent>>(this);
  }
}
