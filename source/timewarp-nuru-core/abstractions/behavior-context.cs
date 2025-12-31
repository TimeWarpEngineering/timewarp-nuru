// Base context for pipeline behaviors.
// See kanban task #315 for design decisions.

namespace TimeWarp.Nuru;

using System.Diagnostics;

/// <summary>
/// Base context passed to behavior methods.
/// Contains common properties available to all behaviors.
/// Behaviors needing per-request state should define a nested <c>State</c> class that inherits from this.
/// A new context instance is created for each request.
/// </summary>
/// <example>
/// <code>
/// public class MyBehavior : INuruBehavior
/// {
///   // Nested State class for per-request state
///   public class State : BehaviorContext
///   {
///     public Stopwatch Timer { get; } = new();
///     public string? UserId { get; set; }
///   }
///
///   public ValueTask OnBeforeAsync(State state)
///   {
///     // Access base properties
///     Console.WriteLine($"[{state.CorrelationId}] {state.CommandName}");
///
///     // Access custom state
///     state.Timer.Start();
///     state.UserId = "user123";
///
///     return ValueTask.CompletedTask;
///   }
/// }
/// </code>
/// </example>
public class BehaviorContext
{
  /// <summary>
  /// The command/route name being executed (e.g., "ping", "greet", "user add").
  /// </summary>
  public required string CommandName { get; init; }

  /// <summary>
  /// The full type name of the command being executed (e.g., "MyApp.Commands.PingCommand").
  /// For delegate handlers, this will be the generated command type name.
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
  /// Stopwatch that starts when the context is created.
  /// Useful for timing measurements in behaviors.
  /// </summary>
  public Stopwatch Stopwatch { get; } = Stopwatch.StartNew();
}
