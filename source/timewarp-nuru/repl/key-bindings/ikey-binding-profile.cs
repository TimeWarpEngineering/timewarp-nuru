#region Purpose
// Contract a REPL editing style (Default, Emacs, Vi, VSCode) implements to supply its key bindings and
// exit keys.
#endregion

#region Design
// Bindings are returned from GetBindings(reader) rather than stored, because the handlers are instance
// methods of the ReplConsoleReader that consumes them; each reader gets its own delegates. Exit keys
// are separate from bindings: the reader runs a chord's action first, then returns the input line
// if the chord is an exit key. Profiles are instantiable by name only through the internal factory
// (Default, Emacs, Vi, VSCode); custom profiles are passed as instances.
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// Defines a set of key bindings for REPL input handling.
/// </summary>
/// <remarks>
/// Implementations provide different editing styles (Default, Emacs, Vi, VSCode)
/// allowing users to choose their preferred key bindings based on muscle memory
/// from other editors and shells.
/// </remarks>
public interface IKeyBindingProfile
{
  /// <summary>
  /// Gets the name of this key binding profile.
  /// </summary>
  /// <value>
  /// A human-readable name identifying the profile (e.g., "Default", "Emacs", "Vi", "VSCode").
  /// </value>
  string Name { get; }

  /// <summary>
  /// Gets the key bindings for this profile.
  /// </summary>
  /// <param name="reader">The ReplConsoleReader instance for accessing handler methods.</param>
  /// <returns>
  /// Dictionary mapping key combinations (ConsoleKey + ConsoleModifiers) to async actions
  /// that will be invoked when the key combination is pressed.
  /// </returns>
  /// <remarks>
  /// The reader parameter allows profiles to invoke the reader's handler methods
  /// (e.g., HandleBackwardChar, HandleForwardChar) which implement the actual editing operations.
  /// </remarks>
  Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Func<Task>> GetBindings(ReplConsoleReader reader);

  /// <summary>
  /// Gets the keys that should exit the read loop (typically Enter and Ctrl+D).
  /// </summary>
  /// <returns>
  /// Set of key combinations that should cause ReadLine to return control to the caller.
  /// Typically includes Enter (submit command) and Ctrl+D (EOF).
  /// </returns>
  HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> GetExitKeys();
}
