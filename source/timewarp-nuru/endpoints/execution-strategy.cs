namespace TimeWarp.Nuru;

/// <summary>
/// Defines how an endpoint should be executed.
/// </summary>
public enum ExecutionStrategy
{
  /// <summary>
  /// Execute through command/handler pattern with nested Handler class.
  /// </summary>
  Command,
  /// <summary>
  /// Execute as a direct delegate.
  /// </summary>
  Delegate,
  /// <summary>
  /// Invalid configuration (neither CommandType nor Handler is set).
  /// </summary>
  Invalid
}
