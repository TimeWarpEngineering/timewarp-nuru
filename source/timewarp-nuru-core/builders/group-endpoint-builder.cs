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
  private readonly GroupBuilder<TGroupParent> _parent;
  private readonly Endpoint _endpoint;

  internal GroupEndpointBuilder(GroupBuilder<TGroupParent> parent, Endpoint endpoint)
  {
    _parent = parent;
    _endpoint = endpoint;
  }

  /// <summary>
  /// Returns to the parent GroupBuilder.
  /// </summary>
  public GroupBuilder<TGroupParent> Done() => _parent;

  /// <summary>
  /// Sets the handler delegate for this endpoint.
  /// </summary>
  /// <param name="handler">The delegate to invoke when this route is matched.</param>
  /// <returns>This builder for further configuration.</returns>
  public GroupEndpointBuilder<TGroupParent> WithHandler(Delegate handler)
  {
    ArgumentNullException.ThrowIfNull(handler);
    _endpoint.Handler = handler;
    _endpoint.Method = handler.Method;
    return this;
  }

  /// <summary>
  /// Sets the description for this endpoint (shown in help text).
  /// </summary>
  /// <param name="description">The description to display.</param>
  /// <returns>This builder for further configuration.</returns>
  public GroupEndpointBuilder<TGroupParent> WithDescription(string description)
  {
    _endpoint.Description = description;
    return this;
  }

  /// <summary>
  /// Marks this route as a query operation (no state change - safe to run and retry freely).
  /// </summary>
  /// <returns>This builder for further configuration.</returns>
  public GroupEndpointBuilder<TGroupParent> AsQuery()
  {
    _endpoint.MessageType = MessageType.Query;
    _endpoint.CompiledRoute.MessageType = MessageType.Query;
    return this;
  }

  /// <summary>
  /// Marks this route as a command operation (state change, not repeatable - confirm before running).
  /// </summary>
  /// <returns>This builder for further configuration.</returns>
  public GroupEndpointBuilder<TGroupParent> AsCommand()
  {
    _endpoint.MessageType = MessageType.Command;
    _endpoint.CompiledRoute.MessageType = MessageType.Command;
    return this;
  }

  /// <summary>
  /// Marks this route as an idempotent command (state change but repeatable - safe to retry).
  /// </summary>
  /// <returns>This builder for further configuration.</returns>
  public GroupEndpointBuilder<TGroupParent> AsIdempotentCommand()
  {
    _endpoint.MessageType = MessageType.IdempotentCommand;
    _endpoint.CompiledRoute.MessageType = MessageType.IdempotentCommand;
    return this;
  }
}
