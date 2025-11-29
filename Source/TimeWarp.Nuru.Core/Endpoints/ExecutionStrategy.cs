namespace TimeWarp.Nuru;

/// <summary>
/// Defines how an endpoint should be executed.
/// </summary>
public enum ExecutionStrategy
{
  /// <summary>
  /// Execute through Mediator pattern (requires dependency injection).
  /// </summary>
  Mediator,
  /// <summary>
  /// Execute as a direct delegate.
  /// </summary>
  Delegate,
  /// <summary>
  /// Invalid configuration (neither CommandType nor Handler is set).
  /// </summary>
  Invalid
}
