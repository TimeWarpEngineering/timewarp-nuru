// Pipeline behavior interfaces and base context for cross-cutting concerns.
// Behaviors wrap handler execution with before/after/error logic.
// See kanban task #315 for design decisions.

namespace TimeWarp.Nuru;

using System.Diagnostics;

/// <summary>
/// Interface for pipeline behaviors that wrap handler execution.
/// Behaviors are instantiated once (Singleton) with services via constructor injection.
/// Per-request state should use a nested <c>State</c> class that inherits from <see cref="BehaviorContext"/>.
/// All methods have default no-op implementations - override only what you need.
/// </summary>
/// <example>
/// <code>
/// // Simple behavior - no per-request state
/// public class LoggingBehavior(ILogger&lt;LoggingBehavior&gt; logger) : INuruBehavior
/// {
///   public ValueTask OnBeforeAsync(BehaviorContext context)
///   {
///     logger.LogInformation("[{CorrelationId}] Handling {Command}",
///       context.CorrelationId, context.CommandName);
///     return ValueTask.CompletedTask;
///   }
/// }
///
/// // Behavior with per-request state
/// public class PerformanceBehavior(ILogger&lt;PerformanceBehavior&gt; logger) : INuruBehavior
/// {
///   public class State : BehaviorContext
///   {
///     public Stopwatch Timer { get; } = new();
///   }
///
///   public ValueTask OnBeforeAsync(State state)
///   {
///     state.Timer.Start();
///     return ValueTask.CompletedTask;
///   }
///
///   public ValueTask OnAfterAsync(State state)
///   {
///     state.Timer.Stop();
///     if (state.Timer.ElapsedMilliseconds > 500)
///       logger.LogWarning("{Command} took {Ms}ms", state.CommandName, state.Timer.ElapsedMilliseconds);
///     return ValueTask.CompletedTask;
///   }
/// }
/// </code>
/// </example>
public interface INuruBehavior
{
  /// <summary>
  /// Called before the handler executes.
  /// </summary>
  /// <param name="context">The behavior context containing request metadata.</param>
  /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
  ValueTask OnBeforeAsync(BehaviorContext context) => ValueTask.CompletedTask;

  /// <summary>
  /// Called after the handler completes successfully.
  /// </summary>
  /// <param name="context">The behavior context containing request metadata.</param>
  /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
  ValueTask OnAfterAsync(BehaviorContext context) => ValueTask.CompletedTask;

  /// <summary>
  /// Called when the handler throws an exception.
  /// The exception will be re-thrown after all OnErrorAsync handlers complete.
  /// </summary>
  /// <param name="context">The behavior context containing request metadata.</param>
  /// <param name="exception">The exception that was thrown.</param>
  /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
  ValueTask OnErrorAsync(BehaviorContext context, Exception exception) => ValueTask.CompletedTask;
}

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
