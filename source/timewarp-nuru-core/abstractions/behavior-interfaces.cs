// Pipeline behavior interface for cross-cutting concerns.
// See kanban task #315 for design decisions.

namespace TimeWarp.Nuru;

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
