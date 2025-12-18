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

/// <summary>
/// Fluent builder for creating custom key binding configurations.
/// </summary>
/// <remarks>
/// <para>
/// KeyBindingBuilder allows you to create key binding configurations from scratch
/// or start from an existing profile and modify it. The builder produces the raw
/// bindings dictionary and exit keys set that can be used with CustomKeyBindingProfile.
/// </para>
/// <para>
/// All methods return <c>this</c> to enable fluent chaining.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Build from scratch
/// KeyBindingBuilder builder = new KeyBindingBuilder()
///   .Bind(ConsoleKey.LeftArrow, () => MoveLeft())
///   .Bind(ConsoleKey.RightArrow, () => MoveRight())
///   .BindExit(ConsoleKey.Enter, () => Submit());
///
/// (Dictionary&lt;(ConsoleKey, ConsoleModifiers), Action&gt; bindings, HashSet&lt;ConsoleKey&gt; exitKeys) = builder.Build();
///
/// // Start from existing profile and modify
/// KeyBindingBuilder customBuilder = new KeyBindingBuilder()
///   .LoadFrom(new DefaultKeyBindingProfile(), reader)
///   .Remove(ConsoleKey.D, ConsoleModifiers.Control)
///   .Bind(ConsoleKey.Q, ConsoleModifiers.Control, () => Quit());
/// </code>
/// </example>
public sealed class KeyBindingBuilder : IBuilder<KeyBindingResult>
{
  private readonly Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Action> Bindings = [];
  private readonly HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> ExitKeys = [];

  /// <summary>
  /// Binds a key (without modifiers) to an action.
  /// </summary>
  /// <param name="key">The console key to bind.</param>
  /// <param name="action">The action to execute when the key is pressed.</param>
  /// <returns>This builder for fluent chaining.</returns>
  /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
  public KeyBindingBuilder Bind(ConsoleKey key, Action action)
  {
    ArgumentNullException.ThrowIfNull(action);
    Bindings[(key, ConsoleModifiers.None)] = action;
    return this;
  }

  /// <summary>
  /// Binds a key with modifiers to an action.
  /// </summary>
  /// <param name="key">The console key to bind.</param>
  /// <param name="modifiers">The modifier keys (Ctrl, Alt, Shift) required.</param>
  /// <param name="action">The action to execute when the key combination is pressed.</param>
  /// <returns>This builder for fluent chaining.</returns>
  /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
  public KeyBindingBuilder Bind(ConsoleKey key, ConsoleModifiers modifiers, Action action)
  {
    ArgumentNullException.ThrowIfNull(action);
    Bindings[(key, modifiers)] = action;
    return this;
  }

  /// <summary>
  /// Binds an exit key (without modifiers) that will terminate the read loop.
  /// </summary>
  /// <param name="key">The console key to bind as an exit key.</param>
  /// <param name="action">The action to execute before exiting (e.g., HandleEnter).</param>
  /// <returns>This builder for fluent chaining.</returns>
  /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
  /// <remarks>
  /// Exit keys cause ReadLine to return control to the caller after executing the action.
  /// Typical exit keys are Enter (submit) and Ctrl+D (EOF).
  /// </remarks>
  public KeyBindingBuilder BindExit(ConsoleKey key, Action action)
  {
    ArgumentNullException.ThrowIfNull(action);
    (ConsoleKey Key, ConsoleModifiers Modifiers) keyBinding = (key, ConsoleModifiers.None);
    Bindings[keyBinding] = action;
    ExitKeys.Add(keyBinding);
    return this;
  }

  /// <summary>
  /// Binds an exit key with modifiers that will terminate the read loop.
  /// </summary>
  /// <param name="key">The console key to bind as an exit key.</param>
  /// <param name="modifiers">The modifier keys (Ctrl, Alt, Shift) required.</param>
  /// <param name="action">The action to execute before exiting.</param>
  /// <returns>This builder for fluent chaining.</returns>
  /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
  public KeyBindingBuilder BindExit(ConsoleKey key, ConsoleModifiers modifiers, Action action)
  {
    ArgumentNullException.ThrowIfNull(action);
    (ConsoleKey Key, ConsoleModifiers Modifiers) keyBinding = (key, modifiers);
    Bindings[keyBinding] = action;
    ExitKeys.Add(keyBinding);
    return this;
  }

  /// <summary>
  /// Removes a key binding (without modifiers).
  /// </summary>
  /// <param name="key">The console key to unbind.</param>
  /// <returns>This builder for fluent chaining.</returns>
  /// <remarks>
  /// If the key was an exit key, it is also removed from the exit keys set.
  /// No error is thrown if the key was not bound.
  /// </remarks>
  public KeyBindingBuilder Remove(ConsoleKey key)
  {
    (ConsoleKey Key, ConsoleModifiers Modifiers) keyBinding = (key, ConsoleModifiers.None);
    Bindings.Remove(keyBinding);
    ExitKeys.Remove(keyBinding);
    return this;
  }

  /// <summary>
  /// Removes a key binding with modifiers.
  /// </summary>
  /// <param name="key">The console key to unbind.</param>
  /// <param name="modifiers">The modifier keys of the binding to remove.</param>
  /// <returns>This builder for fluent chaining.</returns>
  /// <remarks>
  /// If the key combination was an exit key, it is also removed from the exit keys set.
  /// No error is thrown if the key combination was not bound.
  /// </remarks>
  public KeyBindingBuilder Remove(ConsoleKey key, ConsoleModifiers modifiers)
  {
    (ConsoleKey Key, ConsoleModifiers Modifiers) keyBinding = (key, modifiers);
    Bindings.Remove(keyBinding);
    ExitKeys.Remove(keyBinding);
    return this;
  }

  /// <summary>
  /// Clears all bindings and exit keys.
  /// </summary>
  /// <returns>This builder for fluent chaining.</returns>
  /// <remarks>
  /// Use this to start fresh after loading from a profile, or to reset the builder.
  /// </remarks>
  public KeyBindingBuilder Clear()
  {
    Bindings.Clear();
    ExitKeys.Clear();
    return this;
  }

  /// <summary>
  /// Loads bindings from an existing profile.
  /// </summary>
  /// <param name="profile">The profile to load bindings from.</param>
  /// <param name="reader">The ReplConsoleReader instance needed to create the bindings.</param>
  /// <returns>This builder for fluent chaining.</returns>
  /// <exception cref="ArgumentNullException">Thrown when profile or reader is null.</exception>
  /// <remarks>
  /// <para>
  /// This replaces any existing bindings with those from the specified profile.
  /// To add to existing bindings rather than replace, use Bind() methods after LoadFrom().
  /// </para>
  /// <para>
  /// The reader parameter is required because profile bindings reference the reader's
  /// handler methods (HandleBackwardChar, HandleForwardChar, etc.).
  /// </para>
  /// </remarks>
  public KeyBindingBuilder LoadFrom(IKeyBindingProfile profile, ReplConsoleReader reader)
  {
    ArgumentNullException.ThrowIfNull(profile);
    ArgumentNullException.ThrowIfNull(reader);

    Bindings.Clear();
    ExitKeys.Clear();

    foreach (KeyValuePair<(ConsoleKey Key, ConsoleModifiers Modifiers), Action> binding in profile.GetBindings(reader))
    {
      Bindings[binding.Key] = binding.Value;
    }

    foreach ((ConsoleKey Key, ConsoleModifiers Modifiers) exitKey in profile.GetExitKeys())
    {
      ExitKeys.Add(exitKey);
    }

    return this;
  }

  /// <summary>
  /// Marks an existing binding as an exit key.
  /// </summary>
  /// <param name="key">The console key to mark as exit.</param>
  /// <returns>This builder for fluent chaining.</returns>
  /// <remarks>
  /// The key must already be bound. Use this when you want to make an existing
  /// binding also terminate the read loop without changing its action.
  /// </remarks>
  /// <exception cref="InvalidOperationException">Thrown when the key is not bound.</exception>
  public KeyBindingBuilder MarkAsExit(ConsoleKey key)
  {
    (ConsoleKey Key, ConsoleModifiers Modifiers) keyBinding = (key, ConsoleModifiers.None);
    if (!Bindings.ContainsKey(keyBinding))
    {
      throw new InvalidOperationException($"Cannot mark unbound key {key} as exit key. Bind it first.");
    }

    ExitKeys.Add(keyBinding);
    return this;
  }

  /// <summary>
  /// Marks an existing binding with modifiers as an exit key.
  /// </summary>
  /// <param name="key">The console key to mark as exit.</param>
  /// <param name="modifiers">The modifier keys of the binding.</param>
  /// <returns>This builder for fluent chaining.</returns>
  /// <exception cref="InvalidOperationException">Thrown when the key combination is not bound.</exception>
  public KeyBindingBuilder MarkAsExit(ConsoleKey key, ConsoleModifiers modifiers)
  {
    (ConsoleKey Key, ConsoleModifiers Modifiers) keyBinding = (key, modifiers);
    if (!Bindings.ContainsKey(keyBinding))
    {
      throw new InvalidOperationException($"Cannot mark unbound key {key}+{modifiers} as exit key. Bind it first.");
    }

    ExitKeys.Add(keyBinding);
    return this;
  }

  /// <summary>
  /// Unmarks an exit key, keeping the binding but preventing it from terminating the read loop.
  /// </summary>
  /// <param name="key">The console key to unmark.</param>
  /// <returns>This builder for fluent chaining.</returns>
  public KeyBindingBuilder UnmarkAsExit(ConsoleKey key)
  {
    ExitKeys.Remove((key, ConsoleModifiers.None));
    return this;
  }

  /// <summary>
  /// Unmarks an exit key with modifiers.
  /// </summary>
  /// <param name="key">The console key to unmark.</param>
  /// <param name="modifiers">The modifier keys of the binding.</param>
  /// <returns>This builder for fluent chaining.</returns>
  public KeyBindingBuilder UnmarkAsExit(ConsoleKey key, ConsoleModifiers modifiers)
  {
    ExitKeys.Remove((key, modifiers));
    return this;
  }

  /// <summary>
  /// Builds the final key bindings configuration.
  /// </summary>
  /// <returns>
  /// A <see cref="KeyBindingResult"/> containing the bindings dictionary and exit keys set.
  /// Supports tuple deconstruction for backward compatibility.
  /// </returns>
  /// <remarks>
  /// The returned collections are copies; further modifications to the builder
  /// will not affect the built result.
  /// </remarks>
  public KeyBindingResult Build() => new()
  {
    Bindings = new Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Action>(Bindings),
    ExitKeys = [.. ExitKeys]
  };

  /// <summary>
  /// Gets the current number of bindings.
  /// </summary>
  public int BindingCount => Bindings.Count;

  /// <summary>
  /// Gets the current number of exit keys.
  /// </summary>
  public int ExitKeyCount => ExitKeys.Count;

  /// <summary>
  /// Checks if a key (without modifiers) is currently bound.
  /// </summary>
  /// <param name="key">The console key to check.</param>
  /// <returns>True if the key is bound; otherwise, false.</returns>
  public bool IsBound(ConsoleKey key) => Bindings.ContainsKey((key, ConsoleModifiers.None));

  /// <summary>
  /// Checks if a key with modifiers is currently bound.
  /// </summary>
  /// <param name="key">The console key to check.</param>
  /// <param name="modifiers">The modifier keys to check.</param>
  /// <returns>True if the key combination is bound; otherwise, false.</returns>
  public bool IsBound(ConsoleKey key, ConsoleModifiers modifiers) => Bindings.ContainsKey((key, modifiers));

  /// <summary>
  /// Checks if a key (without modifiers) is an exit key.
  /// </summary>
  /// <param name="key">The console key to check.</param>
  /// <returns>True if the key is an exit key; otherwise, false.</returns>
  public bool IsExitKey(ConsoleKey key) => ExitKeys.Contains((key, ConsoleModifiers.None));

  /// <summary>
  /// Checks if a key with modifiers is an exit key.
  /// </summary>
  /// <param name="key">The console key to check.</param>
  /// <param name="modifiers">The modifier keys to check.</param>
  /// <returns>True if the key combination is an exit key; otherwise, false.</returns>
  public bool IsExitKey(ConsoleKey key, ConsoleModifiers modifiers) => ExitKeys.Contains((key, modifiers));
}

/// <summary>
/// Nested builder for key bindings that returns to a parent context via <see cref="Done"/>.
/// </summary>
/// <typeparam name="TParent">The parent builder type to return to.</typeparam>
/// <remarks>
/// <para>
/// This builder wraps <see cref="KeyBindingBuilder"/> and enables fluent configuration
/// of key bindings nested within a parent builder (e.g., REPL options).
/// </para>
/// <para>
/// Use <see cref="Done"/> to build the key bindings and return to the parent.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// app.ConfigureRepl(options => options
///     .WithKeyBindings(kb => kb
///         .Bind(ConsoleKey.F1, ShowHelp)
///         .BindExit(ConsoleKey.Enter, Submit)
///         .Done())
///     .WithPrompt("nuru> "));
/// </code>
/// </example>
public sealed class NestedKeyBindingBuilder<TParent> : INestedBuilder<TParent>
  where TParent : class
{
  private readonly KeyBindingBuilder _inner = new();
  private readonly TParent _parent;
  private readonly Action<KeyBindingResult> _onBuild;

  /// <summary>
  /// Creates a new nested key binding builder.
  /// </summary>
  /// <param name="parent">The parent builder to return to.</param>
  /// <param name="onBuild">Action to invoke with the built result when <see cref="Done"/> is called.</param>
  internal NestedKeyBindingBuilder(TParent parent, Action<KeyBindingResult> onBuild)
  {
    ArgumentNullException.ThrowIfNull(parent);
    ArgumentNullException.ThrowIfNull(onBuild);
    _parent = parent;
    _onBuild = onBuild;
  }

  /// <summary>
  /// Binds a key (without modifiers) to an action.
  /// </summary>
  /// <param name="key">The console key to bind.</param>
  /// <param name="action">The action to execute when the key is pressed.</param>
  /// <returns>This builder for fluent chaining.</returns>
  public NestedKeyBindingBuilder<TParent> Bind(ConsoleKey key, Action action)
  {
    _inner.Bind(key, action);
    return this;
  }

  /// <summary>
  /// Binds a key with modifiers to an action.
  /// </summary>
  /// <param name="key">The console key to bind.</param>
  /// <param name="modifiers">The modifier keys (Ctrl, Alt, Shift) required.</param>
  /// <param name="action">The action to execute when the key combination is pressed.</param>
  /// <returns>This builder for fluent chaining.</returns>
  public NestedKeyBindingBuilder<TParent> Bind(ConsoleKey key, ConsoleModifiers modifiers, Action action)
  {
    _inner.Bind(key, modifiers, action);
    return this;
  }

  /// <summary>
  /// Binds an exit key (without modifiers) that will terminate the read loop.
  /// </summary>
  /// <param name="key">The console key to bind as an exit key.</param>
  /// <param name="action">The action to execute before exiting.</param>
  /// <returns>This builder for fluent chaining.</returns>
  public NestedKeyBindingBuilder<TParent> BindExit(ConsoleKey key, Action action)
  {
    _inner.BindExit(key, action);
    return this;
  }

  /// <summary>
  /// Binds an exit key with modifiers that will terminate the read loop.
  /// </summary>
  /// <param name="key">The console key to bind as an exit key.</param>
  /// <param name="modifiers">The modifier keys (Ctrl, Alt, Shift) required.</param>
  /// <param name="action">The action to execute before exiting.</param>
  /// <returns>This builder for fluent chaining.</returns>
  public NestedKeyBindingBuilder<TParent> BindExit(ConsoleKey key, ConsoleModifiers modifiers, Action action)
  {
    _inner.BindExit(key, modifiers, action);
    return this;
  }

  /// <summary>
  /// Removes a key binding (without modifiers).
  /// </summary>
  /// <param name="key">The console key to unbind.</param>
  /// <returns>This builder for fluent chaining.</returns>
  public NestedKeyBindingBuilder<TParent> Remove(ConsoleKey key)
  {
    _inner.Remove(key);
    return this;
  }

  /// <summary>
  /// Removes a key binding with modifiers.
  /// </summary>
  /// <param name="key">The console key to unbind.</param>
  /// <param name="modifiers">The modifier keys of the binding to remove.</param>
  /// <returns>This builder for fluent chaining.</returns>
  public NestedKeyBindingBuilder<TParent> Remove(ConsoleKey key, ConsoleModifiers modifiers)
  {
    _inner.Remove(key, modifiers);
    return this;
  }

  /// <summary>
  /// Clears all bindings and exit keys.
  /// </summary>
  /// <returns>This builder for fluent chaining.</returns>
  public NestedKeyBindingBuilder<TParent> Clear()
  {
    _inner.Clear();
    return this;
  }

  /// <summary>
  /// Loads bindings from an existing profile.
  /// </summary>
  /// <param name="profile">The profile to load bindings from.</param>
  /// <param name="reader">The ReplConsoleReader instance needed to create the bindings.</param>
  /// <returns>This builder for fluent chaining.</returns>
  public NestedKeyBindingBuilder<TParent> LoadFrom(IKeyBindingProfile profile, ReplConsoleReader reader)
  {
    _inner.LoadFrom(profile, reader);
    return this;
  }

  /// <summary>
  /// Marks an existing binding as an exit key.
  /// </summary>
  /// <param name="key">The console key to mark as exit.</param>
  /// <returns>This builder for fluent chaining.</returns>
  public NestedKeyBindingBuilder<TParent> MarkAsExit(ConsoleKey key)
  {
    _inner.MarkAsExit(key);
    return this;
  }

  /// <summary>
  /// Marks an existing binding with modifiers as an exit key.
  /// </summary>
  /// <param name="key">The console key to mark as exit.</param>
  /// <param name="modifiers">The modifier keys of the binding.</param>
  /// <returns>This builder for fluent chaining.</returns>
  public NestedKeyBindingBuilder<TParent> MarkAsExit(ConsoleKey key, ConsoleModifiers modifiers)
  {
    _inner.MarkAsExit(key, modifiers);
    return this;
  }

  /// <summary>
  /// Unmarks an exit key, keeping the binding but preventing it from terminating the read loop.
  /// </summary>
  /// <param name="key">The console key to unmark.</param>
  /// <returns>This builder for fluent chaining.</returns>
  public NestedKeyBindingBuilder<TParent> UnmarkAsExit(ConsoleKey key)
  {
    _inner.UnmarkAsExit(key);
    return this;
  }

  /// <summary>
  /// Unmarks an exit key with modifiers.
  /// </summary>
  /// <param name="key">The console key to unmark.</param>
  /// <param name="modifiers">The modifier keys of the binding.</param>
  /// <returns>This builder for fluent chaining.</returns>
  public NestedKeyBindingBuilder<TParent> UnmarkAsExit(ConsoleKey key, ConsoleModifiers modifiers)
  {
    _inner.UnmarkAsExit(key, modifiers);
    return this;
  }

  /// <summary>
  /// Gets the current number of bindings.
  /// </summary>
  public int BindingCount => _inner.BindingCount;

  /// <summary>
  /// Gets the current number of exit keys.
  /// </summary>
  public int ExitKeyCount => _inner.ExitKeyCount;

  /// <summary>
  /// Checks if a key (without modifiers) is currently bound.
  /// </summary>
  /// <param name="key">The console key to check.</param>
  /// <returns>True if the key is bound; otherwise, false.</returns>
  public bool IsBound(ConsoleKey key) => _inner.IsBound(key);

  /// <summary>
  /// Checks if a key with modifiers is currently bound.
  /// </summary>
  /// <param name="key">The console key to check.</param>
  /// <param name="modifiers">The modifier keys to check.</param>
  /// <returns>True if the key combination is bound; otherwise, false.</returns>
  public bool IsBound(ConsoleKey key, ConsoleModifiers modifiers) => _inner.IsBound(key, modifiers);

  /// <summary>
  /// Checks if a key (without modifiers) is an exit key.
  /// </summary>
  /// <param name="key">The console key to check.</param>
  /// <returns>True if the key is an exit key; otherwise, false.</returns>
  public bool IsExitKey(ConsoleKey key) => _inner.IsExitKey(key);

  /// <summary>
  /// Checks if a key with modifiers is an exit key.
  /// </summary>
  /// <param name="key">The console key to check.</param>
  /// <param name="modifiers">The modifier keys to check.</param>
  /// <returns>True if the key combination is an exit key; otherwise, false.</returns>
  public bool IsExitKey(ConsoleKey key, ConsoleModifiers modifiers) => _inner.IsExitKey(key, modifiers);

  /// <summary>
  /// Completes the key binding configuration and returns to the parent builder.
  /// </summary>
  /// <returns>The parent builder for continued chaining.</returns>
  public TParent Done()
  {
    KeyBindingResult result = _inner.Build();
    _onBuild(result);
    return _parent;
  }
}
