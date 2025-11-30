namespace TimeWarp.Nuru;

/// <summary>
/// Provides route execution context that can be shared across pipeline behaviors.
/// Registered as a scoped service to maintain context within a single request execution.
/// </summary>
/// <remarks>
/// This follows Jimmy Bogard's recommendation for sharing context in MediatR pipelines.
/// See: https://www.jimmybogard.com/sharing-context-in-mediatr-pipelines/
///
/// Pipeline behaviors can inject this service to access route metadata:
/// - RoutePattern: The matched route pattern (e.g., "deploy {env}")
/// - RouteName: Optional route name for identification
/// - Parameters: Extracted parameter values from the route
/// - StartedAt: When execution began (for timing/logging)
/// - Items: Arbitrary key-value storage for behavior-to-behavior communication
/// </remarks>
public sealed class RouteExecutionContext
{
  /// <summary>
  /// Gets or sets the matched route pattern (e.g., "deploy {env} --dry-run").
  /// </summary>
  public string RoutePattern { get; set; } = "";

  /// <summary>
  /// Gets or sets the optional route name for identification in logs and metrics.
  /// </summary>
  public string? RouteName { get; set; }

  /// <summary>
  /// Gets or sets the extracted parameter values from the route.
  /// Keys are parameter names, values are the extracted string values.
  /// </summary>
  public IReadOnlyDictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

  /// <summary>
  /// Gets or sets when the route execution started.
  /// Useful for timing and performance monitoring behaviors.
  /// </summary>
  public DateTimeOffset StartedAt { get; set; }

  /// <summary>
  /// Gets a dictionary for storing arbitrary data during pipeline execution.
  /// Behaviors can use this to communicate with each other.
  /// </summary>
  public IDictionary<string, object> Items { get; } = new Dictionary<string, object>();

  /// <summary>
  /// Gets the execution strategy used for this route (Delegate or Mediator).
  /// </summary>
  public ExecutionStrategy Strategy { get; set; } = ExecutionStrategy.Delegate;

  /// <summary>
  /// Gets or sets whether this is a delegate route being wrapped for pipeline execution.
  /// When true, indicates the original route was a delegate that is being executed
  /// through the Mediator pipeline for uniform behavior application.
  /// </summary>
  public bool IsWrappedDelegate { get; set; }
}
