// Base context for pipeline behaviors.
// See kanban task #315 for design decisions.

namespace TimeWarp.Nuru;

/// <summary>
/// Context passed to behavior <see cref="INuruBehavior.HandleAsync"/> methods.
/// Contains metadata about the current request and the command instance.
/// A new context instance is created for each request.
/// </summary>
/// <example>
/// <code>
/// public class AuthorizationBehavior : INuruBehavior
/// {
///   public async ValueTask HandleAsync(BehaviorContext context, Func&lt;ValueTask&gt; next)
///   {
///     // Check command interface
///     if (context.Command is IRequireAuthorization auth)
///     {
///       if (!HasPermission(auth.RequiredPermission))
///         throw new UnauthorizedAccessException();
///     }
///
///     // Use correlation ID for logging
///     Console.WriteLine($"[{context.CorrelationId[..8]}] Processing {context.CommandName}");
///
///     await next();
///   }
/// }
/// </code>
/// </example>
public class BehaviorContext
{
  /// <summary>
  /// The route pattern being executed (e.g., "ping", "greet {name}", "user add").
  /// </summary>
  public required string CommandName { get; init; }

  /// <summary>
  /// The type name of the command being executed.
  /// For attributed routes: the user-defined command class name (e.g., "DeployCommand").
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
  /// For attributed routes: the user-defined command class instance with bound parameters.
  /// </para>
  /// <para>
  /// For delegate routes: a generated command instance. If <c>.Implements&lt;T&gt;()</c> was used,
  /// the generated class implements that interface with the configured property values.
  /// </para>
  /// <para>
  /// May be <c>null</c> for delegate routes that don't use <c>.Implements&lt;T&gt;()</c>.
  /// </para>
  /// </remarks>
  public object? Command { get; init; }
}
