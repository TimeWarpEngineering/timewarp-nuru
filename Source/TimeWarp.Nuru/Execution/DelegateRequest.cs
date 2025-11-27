namespace TimeWarp.Nuru;

/// <summary>
/// Wraps a delegate invocation as a message that can flow through pipeline behaviors,
/// enabling cross-cutting concerns to apply uniformly to delegate routes.
/// </summary>
/// <remarks>
/// This type implements <see cref="IMessage"/> to satisfy pipeline behavior constraints,
/// but delegates are NOT executed through IMediator.Send(). Instead, a custom pipeline
/// executor manually invokes registered pipeline behaviors before executing the delegate.
///
/// This approach is necessary because martinothamar/Mediator uses source generation
/// and doesn't support handlers defined in external libraries.
/// </remarks>
public sealed class DelegateRequest : IMessage
{
  /// <summary>
  /// Gets the matched route pattern (e.g., "deploy {env} --dry-run").
  /// </summary>
  public required string RoutePattern { get; init; }

  /// <summary>
  /// Gets the arguments bound from route parameters.
  /// </summary>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays",
    Justification = "Array is intentional for delegate invocation - represents bound arguments")]
  public required object?[] BoundArguments { get; init; }

  /// <summary>
  /// Gets the function that invokes the delegate with bound arguments.
  /// Returns the result boxed as object (or null for void delegates).
  /// </summary>
  public required Func<object?[], Task<object?>> Invoker { get; init; }

  /// <summary>
  /// Gets the endpoint metadata for logging/tracing.
  /// </summary>
  public required Endpoint Endpoint { get; init; }
}

/// <summary>
/// Response from a delegate execution, containing the boxed result.
/// </summary>
public sealed class DelegateResponse
{
  /// <summary>
  /// Gets the result of the delegate execution (boxed, or null for void).
  /// </summary>
  public object? Result { get; init; }

  /// <summary>
  /// Gets the exit code for the command (0 for success, non-zero for failure).
  /// </summary>
  public int ExitCode { get; init; }

  /// <summary>
  /// Creates a successful response with the given result.
  /// </summary>
  public static DelegateResponse Success(object? result) => new()
  {
    Result = result,
    ExitCode = result is int code ? code : 0
  };

  /// <summary>
  /// Creates a failure response with the given exit code.
  /// </summary>
  public static DelegateResponse Failure(int exitCode = 1) => new()
  {
    Result = null,
    ExitCode = exitCode
  };
}
