namespace TimeWarp.Nuru;

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
/// <para>
/// This class is part of the key binding builder family:
/// <list type="bullet">
/// <item><description>key-binding-builder.cs: Main builder class (this file)</description></item>
/// <item><description>key-binding-result.cs: Result container with tuple deconstruction</description></item>
/// <item><description>nested-key-binding-builder.cs: Nested builder for parent-child patterns</description></item>
/// <item><description>ikey-binding-builder.cs: Common interface for fluent operations</description></item>
/// </list>
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
public sealed class KeyBindingBuilder : IKeyBindingBuilder<KeyBindingBuilder>, IBuilder<KeyBindingResult>
{
  private readonly Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Action> Bindings = [];
  private readonly HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> ExitKeys = [];

  /// <inheritdoc />
  public KeyBindingBuilder Bind(ConsoleKey key, Action action)
  {
    ArgumentNullException.ThrowIfNull(action);
    Bindings[(key, ConsoleModifiers.None)] = action;
    return this;
  }

  /// <inheritdoc />
  public KeyBindingBuilder Bind(ConsoleKey key, ConsoleModifiers modifiers, Action action)
  {
    ArgumentNullException.ThrowIfNull(action);
    Bindings[(key, modifiers)] = action;
    return this;
  }

  /// <inheritdoc />
  public KeyBindingBuilder BindExit(ConsoleKey key, Action action)
  {
    ArgumentNullException.ThrowIfNull(action);
    (ConsoleKey Key, ConsoleModifiers Modifiers) keyBinding = (key, ConsoleModifiers.None);
    Bindings[keyBinding] = action;
    ExitKeys.Add(keyBinding);
    return this;
  }

  /// <inheritdoc />
  public KeyBindingBuilder BindExit(ConsoleKey key, ConsoleModifiers modifiers, Action action)
  {
    ArgumentNullException.ThrowIfNull(action);
    (ConsoleKey Key, ConsoleModifiers Modifiers) keyBinding = (key, modifiers);
    Bindings[keyBinding] = action;
    ExitKeys.Add(keyBinding);
    return this;
  }

  /// <inheritdoc />
  public KeyBindingBuilder Remove(ConsoleKey key)
  {
    (ConsoleKey Key, ConsoleModifiers Modifiers) keyBinding = (key, ConsoleModifiers.None);
    Bindings.Remove(keyBinding);
    ExitKeys.Remove(keyBinding);
    return this;
  }

  /// <inheritdoc />
  public KeyBindingBuilder Remove(ConsoleKey key, ConsoleModifiers modifiers)
  {
    (ConsoleKey Key, ConsoleModifiers Modifiers) keyBinding = (key, modifiers);
    Bindings.Remove(keyBinding);
    ExitKeys.Remove(keyBinding);
    return this;
  }

  /// <inheritdoc />
  public KeyBindingBuilder Clear()
  {
    Bindings.Clear();
    ExitKeys.Clear();
    return this;
  }

  /// <inheritdoc />
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

  /// <inheritdoc />
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

  /// <inheritdoc />
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

  /// <inheritdoc />
  public KeyBindingBuilder UnmarkAsExit(ConsoleKey key)
  {
    ExitKeys.Remove((key, ConsoleModifiers.None));
    return this;
  }

  /// <inheritdoc />
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

  /// <inheritdoc />
  public int BindingCount => Bindings.Count;

  /// <inheritdoc />
  public int ExitKeyCount => ExitKeys.Count;

  /// <inheritdoc />
  public bool IsBound(ConsoleKey key) => Bindings.ContainsKey((key, ConsoleModifiers.None));

  /// <inheritdoc />
  public bool IsBound(ConsoleKey key, ConsoleModifiers modifiers) => Bindings.ContainsKey((key, modifiers));

  /// <inheritdoc />
  public bool IsExitKey(ConsoleKey key) => ExitKeys.Contains((key, ConsoleModifiers.None));

  /// <inheritdoc />
  public bool IsExitKey(ConsoleKey key, ConsoleModifiers modifiers) => ExitKeys.Contains((key, modifiers));
}
