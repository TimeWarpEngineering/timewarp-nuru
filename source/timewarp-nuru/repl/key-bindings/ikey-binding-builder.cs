namespace TimeWarp.Nuru;

/// <summary>
/// Interface for key binding builders providing fluent configuration of key bindings.
/// </summary>
/// <typeparam name="TSelf">The concrete builder type for fluent chaining.</typeparam>
/// <remarks>
/// <para>
/// This interface defines the common key binding operations shared by both
/// <see cref="KeyBindingBuilder"/> (standalone) and <see cref="NestedKeyBindingBuilder{TParent}"/> (nested).
/// </para>
/// <para>
/// The generic <typeparamref name="TSelf"/> parameter enables fluent method chaining
/// by returning the concrete builder type from each method.
/// </para>
/// </remarks>
public interface IKeyBindingBuilder<TSelf> where TSelf : IKeyBindingBuilder<TSelf>
{
  /// <summary>
  /// Binds a key (without modifiers) to an action.
  /// </summary>
  /// <param name="key">The console key to bind.</param>
  /// <param name="action">The action to execute when the key is pressed.</param>
  /// <returns>This builder for fluent chaining.</returns>
  TSelf Bind(ConsoleKey key, Action action);

  /// <summary>
  /// Binds a key with modifiers to an action.
  /// </summary>
  /// <param name="key">The console key to bind.</param>
  /// <param name="modifiers">The modifier keys (Ctrl, Alt, Shift) required.</param>
  /// <param name="action">The action to execute when the key combination is pressed.</param>
  /// <returns>This builder for fluent chaining.</returns>
  TSelf Bind(ConsoleKey key, ConsoleModifiers modifiers, Action action);

  /// <summary>
  /// Binds an exit key (without modifiers) that will terminate the read loop.
  /// </summary>
  /// <param name="key">The console key to bind as an exit key.</param>
  /// <param name="action">The action to execute before exiting.</param>
  /// <returns>This builder for fluent chaining.</returns>
  TSelf BindExit(ConsoleKey key, Action action);

  /// <summary>
  /// Binds an exit key with modifiers that will terminate the read loop.
  /// </summary>
  /// <param name="key">The console key to bind as an exit key.</param>
  /// <param name="modifiers">The modifier keys (Ctrl, Alt, Shift) required.</param>
  /// <param name="action">The action to execute before exiting.</param>
  /// <returns>This builder for fluent chaining.</returns>
  TSelf BindExit(ConsoleKey key, ConsoleModifiers modifiers, Action action);

  /// <summary>
  /// Removes a key binding (without modifiers).
  /// </summary>
  /// <param name="key">The console key to unbind.</param>
  /// <returns>This builder for fluent chaining.</returns>
  TSelf Remove(ConsoleKey key);

  /// <summary>
  /// Removes a key binding with modifiers.
  /// </summary>
  /// <param name="key">The console key to unbind.</param>
  /// <param name="modifiers">The modifier keys of the binding to remove.</param>
  /// <returns>This builder for fluent chaining.</returns>
  TSelf Remove(ConsoleKey key, ConsoleModifiers modifiers);

  /// <summary>
  /// Clears all bindings and exit keys.
  /// </summary>
  /// <returns>This builder for fluent chaining.</returns>
  TSelf Clear();

  /// <summary>
  /// Loads bindings from an existing profile.
  /// </summary>
  /// <param name="profile">The profile to load bindings from.</param>
  /// <param name="reader">The ReplConsoleReader instance needed to create the bindings.</param>
  /// <returns>This builder for fluent chaining.</returns>
  TSelf LoadFrom(IKeyBindingProfile profile, ReplConsoleReader reader);

  /// <summary>
  /// Marks an existing binding as an exit key.
  /// </summary>
  /// <param name="key">The console key to mark as exit.</param>
  /// <returns>This builder for fluent chaining.</returns>
  TSelf MarkAsExit(ConsoleKey key);

  /// <summary>
  /// Marks an existing binding with modifiers as an exit key.
  /// </summary>
  /// <param name="key">The console key to mark as exit.</param>
  /// <param name="modifiers">The modifier keys of the binding.</param>
  /// <returns>This builder for fluent chaining.</returns>
  TSelf MarkAsExit(ConsoleKey key, ConsoleModifiers modifiers);

  /// <summary>
  /// Unmarks an exit key, keeping the binding but preventing it from terminating the read loop.
  /// </summary>
  /// <param name="key">The console key to unmark.</param>
  /// <returns>This builder for fluent chaining.</returns>
  TSelf UnmarkAsExit(ConsoleKey key);

  /// <summary>
  /// Unmarks an exit key with modifiers.
  /// </summary>
  /// <param name="key">The console key to unmark.</param>
  /// <param name="modifiers">The modifier keys of the binding.</param>
  /// <returns>This builder for fluent chaining.</returns>
  TSelf UnmarkAsExit(ConsoleKey key, ConsoleModifiers modifiers);

  /// <summary>
  /// Gets the current number of bindings.
  /// </summary>
  int BindingCount { get; }

  /// <summary>
  /// Gets the current number of exit keys.
  /// </summary>
  int ExitKeyCount { get; }

  /// <summary>
  /// Checks if a key (without modifiers) is currently bound.
  /// </summary>
  /// <param name="key">The console key to check.</param>
  /// <returns>True if the key is bound; otherwise, false.</returns>
  bool IsBound(ConsoleKey key);

  /// <summary>
  /// Checks if a key with modifiers is currently bound.
  /// </summary>
  /// <param name="key">The console key to check.</param>
  /// <param name="modifiers">The modifier keys to check.</param>
  /// <returns>True if the key combination is bound; otherwise, false.</returns>
  bool IsBound(ConsoleKey key, ConsoleModifiers modifiers);

  /// <summary>
  /// Checks if a key (without modifiers) is an exit key.
  /// </summary>
  /// <param name="key">The console key to check.</param>
  /// <returns>True if the key is an exit key; otherwise, false.</returns>
  bool IsExitKey(ConsoleKey key);

  /// <summary>
  /// Checks if a key with modifiers is an exit key.
  /// </summary>
  /// <param name="key">The console key to check.</param>
  /// <param name="modifiers">The modifier keys to check.</param>
  /// <returns>True if the key combination is an exit key; otherwise, false.</returns>
  bool IsExitKey(ConsoleKey key, ConsoleModifiers modifiers);
}
