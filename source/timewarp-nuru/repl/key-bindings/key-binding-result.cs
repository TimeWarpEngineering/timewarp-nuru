namespace TimeWarp.Nuru;

/// <summary>
/// Result of building key bindings via <see cref="KeyBindingBuilder"/>.
/// </summary>
/// <remarks>
/// Supports tuple deconstruction for backward compatibility with existing code.
/// </remarks>
public sealed class KeyBindingResult
{
  /// <summary>
  /// The key bindings dictionary mapping key combinations to actions.
  /// </summary>
  public Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Action> Bindings { get; init; } = [];

  /// <summary>
  /// The set of key combinations that terminate the read loop.
  /// </summary>
  public HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> ExitKeys { get; init; } = [];

  /// <summary>
  /// Deconstructs the result into a tuple for backward compatibility.
  /// </summary>
  /// <param name="bindings">The bindings dictionary.</param>
  /// <param name="exitKeys">The exit keys set.</param>
  /// <example>
  /// <code>
  /// var (bindings, exitKeys) = new KeyBindingBuilder().Bind(...).Build();
  /// </code>
  /// </example>
  public void Deconstruct(
    out Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Action> bindings,
    out HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> exitKeys)
  {
    bindings = Bindings;
    exitKeys = ExitKeys;
  }
}
