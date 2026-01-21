// Base context for pipeline behaviors.
// See kanban task #315 for design decisions.
// See kanban task #316 for typed context design (BehaviorContext<TFilter>).

namespace TimeWarp.Nuru;

/// <summary>
/// Context passed to behavior <see cref="INuruBehavior.HandleAsync"/> methods.
/// Contains metadata about the current request and the command instance.
/// A new context instance is created for each request.
/// </summary>
/// <example>
/// <code>
/// // Global behavior using base BehaviorContext
/// public class LoggingBehavior : INuruBehavior
/// {
///   public async ValueTask HandleAsync(BehaviorContext context, Func&lt;ValueTask&gt; proceed)
///   {
///     Console.WriteLine($"[{context.CorrelationId[..8]}] Processing {context.CommandName}");
///     await proceed();
///   }
/// }
/// </code>
/// </example>
/// <seealso cref="BehaviorContext{TFilter}"/>
public class BehaviorContext
{
  /// <summary>
  /// The route pattern being executed (e.g., "ping", "greet {name}", "user add").
  /// </summary>
  public required string CommandName { get; init; }

  /// <summary>
  /// The type name of the command being executed.
  /// For endpoints: the user-defined command class name (e.g., "DeployCommand").
  /// For delegate routes: a generated name (e.g., "Route_0").
  /// </summary>
  public required string CommandTypeName { get; init; }

  /// <summary>
  /// Cancellation token for the request.
  /// </summary>
  public required CancellationToken CancellationToken { get; init; }

  /// <summary>
  /// Unique identifier for this request.
  /// Useful for correlating log entries across behaviors and handlers.
  /// Format: GUID (e.g., "550e8400-e29b-41d4-a716-446655440000").
  /// </summary>
  public string CorrelationId { get; } = Guid.NewGuid().ToString();

  /// <summary>
  /// The command instance for this request.
  /// Can be cast to check or use interface implementations (e.g., <c>IRequireAuthorization</c>, <c>IRetryable</c>).
  /// </summary>
  /// <remarks>
  /// <para>
  /// For endpoints: the user-defined command class instance with bound parameters.
  /// </para>
  /// <para>
  /// For delegate routes: a generated command instance. If <c>.Implements&lt;T&gt;()</c> was used,
  /// the generated class implements that interface with the configured property values.
  /// </para>
  /// </remarks>
  public object? Command { get; init; }
}

/// <summary>
/// Typed context passed to filtered behaviors implementing <see cref="INuruBehavior{TFilter}"/>.
/// Provides a strongly-typed <see cref="Command"/> property that is guaranteed to implement <typeparamref name="TFilter"/>.
/// </summary>
/// <typeparam name="TFilter">The filter interface type that the command implements.</typeparam>
/// <example>
/// <code>
/// // Filtered behavior with typed context - no casting needed!
/// public class AuthorizationBehavior : INuruBehavior&lt;IRequireAuthorization&gt;
/// {
///   public async ValueTask HandleAsync(BehaviorContext&lt;IRequireAuthorization&gt; context, Func&lt;ValueTask&gt; proceed)
///   {
///     // context.Command is already IRequireAuthorization
///     string permission = context.Command.RequiredPermission;
///
///     if (!HasPermission(permission))
///       throw new UnauthorizedAccessException($"Required: {permission}");
///
///     await proceed();
///   }
/// }
/// </code>
/// </example>
public class BehaviorContext<TFilter> : BehaviorContext where TFilter : class
{
  /// <summary>
  /// The strongly-typed command instance for this request.
  /// Guaranteed to implement <typeparamref name="TFilter"/>.
  /// </summary>
  public new required TFilter Command { get; init; }
}
