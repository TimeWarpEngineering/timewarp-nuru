namespace TimeWarp.Nuru;

/// <summary>
/// Endpoint builder for routes within a group.
/// Returns to the parent GroupBuilder via Done().
/// </summary>
/// <typeparam name="TGroupParent">The grandparent type that the GroupBuilder returns to.</typeparam>
/// <remarks>
/// <para>
/// This builder provides the same fluent configuration as EndpointBuilder,
/// but returns to a GroupBuilder instead of the app builder:
/// </para>
/// <code>
/// .WithGroupPrefix("admin")
///   .Map("status")              // Returns GroupEndpointBuilder
///     .WithHandler(() => "ok")
///     .WithDescription("Check admin status")
///     .AsQuery()
///     .Done()                   // Returns to GroupBuilder
///   .Done()                     // Returns to parent (TGroupParent)
/// </code>
/// </remarks>
public sealed class GroupEndpointBuilder<TGroupParent> : INestedBuilder<GroupBuilder<TGroupParent>>
  where TGroupParent : class
{
  private readonly GroupBuilder<TGroupParent> ParentBuilder;

  internal GroupEndpointBuilder(GroupBuilder<TGroupParent> parent)
  {
    ParentBuilder = parent;
  }

  /// <summary>
  /// Returns to the parent GroupBuilder.
  /// </summary>
  public GroupBuilder<TGroupParent> Done() => ParentBuilder;

  /// <summary>
  /// Sets the handler delegate for this endpoint.
  /// </summary>
  /// <param name="handler">The delegate to invoke when this route is matched.</param>
  /// <returns>This builder for further configuration.</returns>
  public GroupEndpointBuilder<TGroupParent> WithHandler(Delegate handler)
  {
    // Source generator extracts handler at compile time
    _ = handler;
    return this;
  }

  /// <summary>
  /// Sets the description for this endpoint (shown in help text).
  /// </summary>
  /// <param name="description">The description to display.</param>
  /// <returns>This builder for further configuration.</returns>
  public GroupEndpointBuilder<TGroupParent> WithDescription(string description)
  {
    // Source generator extracts description at compile time
    _ = description;
    return this;
  }

  /// <summary>
  /// Marks this route as a query operation (no state change - safe to run and retry freely).
  /// </summary>
  /// <returns>This builder for further configuration.</returns>
  public GroupEndpointBuilder<TGroupParent> AsQuery()
  {
    // Source generator sets MessageType at compile time
    return this;
  }

  /// <summary>
  /// Marks this route as a command operation (state change, not repeatable - confirm before running).
  /// </summary>
  /// <returns>This builder for further configuration.</returns>
  public GroupEndpointBuilder<TGroupParent> AsCommand()
  {
    // Source generator sets MessageType at compile time
    return this;
  }

  /// <summary>
  /// Marks this route as an idempotent command (state change but repeatable - safe to retry).
  /// </summary>
  /// <returns>This builder for further configuration.</returns>
  public GroupEndpointBuilder<TGroupParent> AsIdempotentCommand()
  {
    // Source generator sets MessageType at compile time
    return this;
  }
}
