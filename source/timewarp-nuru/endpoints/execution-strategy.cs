#region Purpose
// Enum naming how an endpoint executes (command/handler pattern versus delegate).
#endregion

#region Open Questions
// Q1 (2026-11-14, claude): ExecutionStrategy has no references anywhere in source, tests or
//     generators, yet it is a public enum. Is it a leftover from the pre-generator endpoint model
//     that can be removed, or is it reserved public API with a planned consumer?
#endregion

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
