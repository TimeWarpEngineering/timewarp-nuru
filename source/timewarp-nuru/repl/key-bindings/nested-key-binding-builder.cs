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
  private readonly KeyBindingBuilder Inner = new();
  private readonly TParent Parent;
  private readonly Action<KeyBindingResult> OnBuild;

  /// <summary>
  /// Creates a new nested key binding builder.
  /// </summary>
  /// <param name="parent">The parent builder to return to.</param>
  /// <param name="onBuild">Action to invoke with the built result when <see cref="Done"/> is called.</param>
  internal NestedKeyBindingBuilder(TParent parent, Action<KeyBindingResult> onBuild)
  {
    ArgumentNullException.ThrowIfNull(parent);
    ArgumentNullException.ThrowIfNull(onBuild);
    Parent = parent;
    OnBuild = onBuild;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> Bind(ConsoleKey key, Func<Task> action)
  {
    Inner.Bind(key, action);
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> Bind(ConsoleKey key, ConsoleModifiers modifiers, Func<Task> action)
  {
    Inner.Bind(key, modifiers, action);
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> BindExit(ConsoleKey key, Func<Task> action)
  {
    Inner.BindExit(key, action);
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> BindExit(ConsoleKey key, ConsoleModifiers modifiers, Func<Task> action)
  {
    Inner.BindExit(key, modifiers, action);
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> Remove(ConsoleKey key)
  {
    Inner.Remove(key);
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> Remove(ConsoleKey key, ConsoleModifiers modifiers)
  {
    Inner.Remove(key, modifiers);
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> Clear()
  {
    Inner.Clear();
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> LoadFrom(IKeyBindingProfile profile, ReplConsoleReader reader)
  {
    Inner.LoadFrom(profile, reader);
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> MarkAsExit(ConsoleKey key)
  {
    Inner.MarkAsExit(key);
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> MarkAsExit(ConsoleKey key, ConsoleModifiers modifiers)
  {
    Inner.MarkAsExit(key, modifiers);
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> UnmarkAsExit(ConsoleKey key)
  {
    Inner.UnmarkAsExit(key);
    return this;
  }

  /// <inheritdoc />
  public NestedKeyBindingBuilder<TParent> UnmarkAsExit(ConsoleKey key, ConsoleModifiers modifiers)
  {
    Inner.UnmarkAsExit(key, modifiers);
    return this;
  }

  /// <inheritdoc />
  public int BindingCount => Inner.BindingCount;

  /// <inheritdoc />
  public int ExitKeyCount => Inner.ExitKeyCount;

  /// <inheritdoc />
  public bool IsBound(ConsoleKey key) => Inner.IsBound(key);

  /// <inheritdoc />
  public bool IsBound(ConsoleKey key, ConsoleModifiers modifiers) => Inner.IsBound(key, modifiers);

  /// <inheritdoc />
  public bool IsExitKey(ConsoleKey key) => Inner.IsExitKey(key);

  /// <inheritdoc />
  public bool IsExitKey(ConsoleKey key, ConsoleModifiers modifiers) => Inner.IsExitKey(key, modifiers);

  /// <summary>
  /// Completes the key binding configuration and returns to the parent builder.
  /// </summary>
  /// <returns>The parent builder for continued chaining.</returns>
  public TParent Done()
  {
    KeyBindingResult result = Inner.Build();
    OnBuild(result);
    return Parent;
  }
}
