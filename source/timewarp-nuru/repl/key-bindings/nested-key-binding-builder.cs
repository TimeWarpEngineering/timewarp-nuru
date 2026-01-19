namespace TimeWarp.Nuru;

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
/// <para>
/// This class is part of the key binding builder family:
/// <list type="bullet">
/// <item><description>key-binding-builder.cs: Main builder class</description></item>
/// <item><description>key-binding-result.cs: Result container with tuple deconstruction</description></item>
/// <item><description>nested-key-binding-builder.cs: Nested builder for parent-child patterns (this file)</description></item>
/// <item><description>ikey-binding-builder.cs: Common interface for fluent operations</description></item>
/// </list>
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
public sealed class NestedKeyBindingBuilder<TParent> : IKeyBindingBuilder<NestedKeyBindingBuilder<TParent>>, INestedBuilder<TParent>
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

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> Bind(ConsoleKey key, Action action)
  {
    _inner.Bind(key, action);
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> Bind(ConsoleKey key, ConsoleModifiers modifiers, Action action)
  {
    _inner.Bind(key, modifiers, action);
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> BindExit(ConsoleKey key, Action action)
  {
    _inner.BindExit(key, action);
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> BindExit(ConsoleKey key, ConsoleModifiers modifiers, Action action)
  {
    _inner.BindExit(key, modifiers, action);
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> Remove(ConsoleKey key)
  {
    _inner.Remove(key);
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> Remove(ConsoleKey key, ConsoleModifiers modifiers)
  {
    _inner.Remove(key, modifiers);
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> Clear()
  {
    _inner.Clear();
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> LoadFrom(IKeyBindingProfile profile, ReplConsoleReader reader)
  {
    _inner.LoadFrom(profile, reader);
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> MarkAsExit(ConsoleKey key)
  {
    _inner.MarkAsExit(key);
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> MarkAsExit(ConsoleKey key, ConsoleModifiers modifiers)
  {
    _inner.MarkAsExit(key, modifiers);
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> UnmarkAsExit(ConsoleKey key)
  {
    _inner.UnmarkAsExit(key);
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> UnmarkAsExit(ConsoleKey key, ConsoleModifiers modifiers)
  {
    _inner.UnmarkAsExit(key, modifiers);
    return this;
  }

  /// <inheritdoc />
  public int BindingCount => _inner.BindingCount;

  /// <inheritdoc />
  public int ExitKeyCount => _inner.ExitKeyCount;

  /// <inheritdoc />
  public bool IsBound(ConsoleKey key) => _inner.IsBound(key);

  /// <inheritdoc />
  public bool IsBound(ConsoleKey key, ConsoleModifiers modifiers) => _inner.IsBound(key, modifiers);

  /// <inheritdoc />
  public bool IsExitKey(ConsoleKey key) => _inner.IsExitKey(key);

  /// <inheritdoc />
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
