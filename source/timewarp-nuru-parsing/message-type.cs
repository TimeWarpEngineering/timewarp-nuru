namespace TimeWarp.Nuru;

/// <summary>
/// Defines the message type for a route, enabling AI agents to make informed decisions
/// about command execution safety based on CQRS terminology.
/// </summary>
/// <remarks>
/// <para>
/// This enum aligns with CQRS (Command Query Responsibility Segregation) patterns
/// and Mediator interfaces (IQuery&lt;T&gt;, ICommand&lt;T&gt;).
/// </para>
/// <para>
/// AI agents use this metadata to determine:
/// <list type="bullet">
///   <item><description>Whether to run a command freely or ask for confirmation</description></item>
///   <item><description>Whether it's safe to retry on failure</description></item>
///   <item><description>Whether the command will modify state</description></item>
/// </list>
/// </para>
/// </remarks>
public enum MessageType
{
  /// <summary>
  /// Query operation. No state change - safe to run and retry freely.
  /// </summary>
  /// <remarks>
  /// AI agents can run these commands without confirmation and retry on failure.
  /// Examples: list, get, status, show, describe
  /// </remarks>
  Query,

  /// <summary>
  /// Command operation. State change, not repeatable - confirm before running.
  /// </summary>
  /// <remarks>
  /// AI agents should ask for confirmation before running and should not auto-retry.
  /// Examples: create, append, send, delete (non-idempotent)
  /// </remarks>
  Command,

  /// <summary>
  /// Idempotent command. State change but repeatable - safe to retry on failure.
  /// </summary>
  /// <remarks>
  /// AI agents may run with less caution than Command and can safely retry on failure.
  /// Examples: set, enable, disable, upsert, update
  /// </remarks>
  IdempotentCommand
}
