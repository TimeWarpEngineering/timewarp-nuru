// Pipeline behavior interface for cross-cutting concerns.
// See kanban task #315 for design decisions.

namespace TimeWarp.Nuru;

/// <summary>
/// Interface for pipeline behaviors that wrap handler execution.
/// Behaviors are instantiated once (Singleton) with services via constructor injection.
/// Use the <c>next</c> delegate to invoke the next behavior or handler in the pipeline.
/// </summary>
/// <remarks>
/// <para>
/// This pattern provides full control over execution flow, enabling:
/// </para>
/// <list type="bullet">
///   <item>Before/after logic (logging, timing)</item>
///   <item>Exception handling and retry</item>
///   <item>Short-circuiting (authorization)</item>
///   <item>Resource management with <c>using</c> statements</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Simple logging behavior
/// public class LoggingBehavior : INuruBehavior
/// {
///   public async ValueTask HandleAsync(BehaviorContext context, Func&lt;ValueTask&gt; proceed)
///   {
///     Console.WriteLine($"Before: {context.CommandName}");
///     await proceed();
///     Console.WriteLine($"After: {context.CommandName}");
///   }
/// }
///
/// // Performance timing
/// public class PerformanceBehavior : INuruBehavior
/// {
///   public async ValueTask HandleAsync(BehaviorContext context, Func&lt;ValueTask&gt; proceed)
///   {
///     var sw = Stopwatch.StartNew();
///     await proceed();
///     sw.Stop();
///     if (sw.ElapsedMilliseconds > 500)
///       Console.WriteLine($"SLOW: {context.CommandName} took {sw.ElapsedMilliseconds}ms");
///   }
/// }
///
/// // Exception handling
/// public class ExceptionBehavior : INuruBehavior
/// {
///   public async ValueTask HandleAsync(BehaviorContext context, Func&lt;ValueTask&gt; proceed)
///   {
///     try
///     {
///       await proceed();
///     }
///     catch (ValidationException ex)
///     {
///       Console.Error.WriteLine($"Validation error: {ex.Message}");
///       throw;
///     }
///   }
/// }
///
/// // Retry with exponential backoff
/// public class RetryBehavior : INuruBehavior
/// {
///   public async ValueTask HandleAsync(BehaviorContext context, Func&lt;ValueTask&gt; proceed)
///   {
///     if (context.Command is not IRetryable retryable) { await proceed(); return; }
///
///     for (int attempt = 1; attempt &lt;= retryable.MaxRetries + 1; attempt++)
///     {
///       try { await proceed(); return; }
///       catch (Exception ex) when (IsTransient(ex) &amp;&amp; attempt &lt;= retryable.MaxRetries)
///       {
///         await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
///       }
///     }
///   }
/// }
///
/// // Telemetry with using statement
/// public class TelemetryBehavior : INuruBehavior
/// {
///   private static readonly ActivitySource Source = new("MyApp");
///
///   public async ValueTask HandleAsync(BehaviorContext context, Func&lt;ValueTask&gt; proceed)
///   {
///     using var activity = Source.StartActivity(context.CommandName);
///     try
///     {
///       await proceed();
///       activity?.SetStatus(ActivityStatusCode.Ok);
///     }
///     catch (Exception ex)
///     {
///       activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
///       throw;
///     }
///   }
/// }
/// </code>
/// </example>
public interface INuruBehavior
{
  /// <summary>
  /// Handles the request by wrapping the next behavior or handler in the pipeline.
  /// </summary>
  /// <param name="context">The behavior context containing request metadata and command instance.</param>
  /// <param name="proceed">Delegate to invoke the next behavior or handler. Must be called to continue the pipeline.</param>
  /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
  /// <remarks>
  /// <para>
  /// The <paramref name="proceed"/> delegate must be called exactly once to continue the pipeline,
  /// unless you want to short-circuit (e.g., authorization failure).
  /// </para>
  /// <para>
  /// Behaviors execute in registration order. First registered = outermost (called first).
  /// </para>
  /// </remarks>
  ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed);
}
