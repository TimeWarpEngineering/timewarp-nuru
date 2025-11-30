namespace TimeWarp.Nuru.Repl;

/// <summary>
/// A customizable key binding profile that can extend or modify existing profiles.
/// </summary>
/// <remarks>
/// <para>
/// CustomKeyBindingProfile allows users to create personalized key bindings by:
/// </para>
/// <list type="bullet">
/// <item><description>Starting from an existing profile (Default, Emacs, Vi, VSCode) and modifying it</description></item>
/// <item><description>Building a completely custom set of bindings from scratch</description></item>
/// <item><description>Overriding, adding, or removing specific key bindings</description></item>
/// </list>
/// <para>
/// All modification methods return <c>this</c> to enable fluent chaining.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Start from Emacs profile and customize
/// CustomKeyBindingProfile customProfile = new CustomKeyBindingProfile(new EmacsKeyBindingProfile())
///   .WithName("MyCustomProfile")
///   .Override(ConsoleKey.K, ConsoleModifiers.Control, reader => reader.HandleDeleteToEnd)
///   .Remove(ConsoleKey.D, ConsoleModifiers.Control);
///
/// ReplOptions replOptions = new() { KeyBindingProfile = customProfile };
/// </code>
/// </example>
public sealed class CustomKeyBindingProfile : IKeyBindingProfile
{
  private readonly IKeyBindingProfile? BaseProfile;
  private readonly List<BindingModification> Modifications = [];
  private readonly HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> RemovedBindings = [];
  private readonly HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> AddedExitKeys = [];
  private readonly HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> RemovedExitKeys = [];
  private string ProfileName = "Custom";

  /// <summary>
  /// Creates a custom profile starting from scratch (no base bindings).
  /// </summary>
  public CustomKeyBindingProfile()
  {
    BaseProfile = null;
  }

  /// <summary>
  /// Creates a custom profile based on an existing profile.
  /// </summary>
  /// <param name="baseProfile">The profile to use as a starting point.</param>
  /// <exception cref="ArgumentNullException">Thrown when baseProfile is null.</exception>
  public CustomKeyBindingProfile(IKeyBindingProfile baseProfile)
  {
    ArgumentNullException.ThrowIfNull(baseProfile);
    BaseProfile = baseProfile;
  }

  /// <inheritdoc/>
  public string Name => ProfileName;

  /// <summary>
  /// Sets a custom name for this profile.
  /// </summary>
  /// <param name="name">The name to use for this profile.</param>
  /// <returns>This profile for fluent chaining.</returns>
  /// <exception cref="ArgumentException">Thrown when name is null or empty.</exception>
  public CustomKeyBindingProfile WithName(string name)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    ProfileName = name;
    return this;
  }

  /// <summary>
  /// Overrides or adds a key binding (without modifiers).
  /// </summary>
  /// <param name="key">The console key to bind.</param>
  /// <param name="actionFactory">Factory that creates the action given a ReplConsoleReader.</param>
  /// <returns>This profile for fluent chaining.</returns>
  /// <exception cref="ArgumentNullException">Thrown when actionFactory is null.</exception>
  /// <remarks>
  /// If the key is already bound (in base profile or previous modifications), this replaces it.
  /// If the key was previously removed, this re-adds it with the new action.
  /// </remarks>
  public CustomKeyBindingProfile Override
  (
    ConsoleKey key,
    Func<ReplConsoleReader, Action> actionFactory
  )
  {
    ArgumentNullException.ThrowIfNull(actionFactory);
    RemovedBindings.Remove((key, ConsoleModifiers.None));
    Modifications.Add(new BindingModification(key, ConsoleModifiers.None, actionFactory));
    return this;
  }

  /// <summary>
  /// Overrides or adds a key binding with modifiers.
  /// </summary>
  /// <param name="key">The console key to bind.</param>
  /// <param name="modifiers">The modifier keys (Ctrl, Alt, Shift) required.</param>
  /// <param name="actionFactory">Factory that creates the action given a ReplConsoleReader.</param>
  /// <returns>This profile for fluent chaining.</returns>
  /// <exception cref="ArgumentNullException">Thrown when actionFactory is null.</exception>
  public CustomKeyBindingProfile Override
  (
    ConsoleKey key,
    ConsoleModifiers modifiers,
    Func<ReplConsoleReader, Action> actionFactory
  )
  {
    ArgumentNullException.ThrowIfNull(actionFactory);
    RemovedBindings.Remove((key, modifiers));
    Modifications.Add(new BindingModification(key, modifiers, actionFactory));
    return this;
  }

  /// <summary>
  /// Adds a new key binding (without modifiers). Alias for Override.
  /// </summary>
  /// <param name="key">The console key to bind.</param>
  /// <param name="actionFactory">Factory that creates the action given a ReplConsoleReader.</param>
  /// <returns>This profile for fluent chaining.</returns>
  /// <exception cref="ArgumentNullException">Thrown when actionFactory is null.</exception>
  public CustomKeyBindingProfile Add
  (
    ConsoleKey key,
    Func<ReplConsoleReader, Action> actionFactory
  ) => Override(key, actionFactory);

  /// <summary>
  /// Adds a new key binding with modifiers. Alias for Override.
  /// </summary>
  /// <param name="key">The console key to bind.</param>
  /// <param name="modifiers">The modifier keys (Ctrl, Alt, Shift) required.</param>
  /// <param name="actionFactory">Factory that creates the action given a ReplConsoleReader.</param>
  /// <returns>This profile for fluent chaining.</returns>
  /// <exception cref="ArgumentNullException">Thrown when actionFactory is null.</exception>
  public CustomKeyBindingProfile Add
  (
    ConsoleKey key,
    ConsoleModifiers modifiers,
    Func<ReplConsoleReader, Action> actionFactory
  ) => Override(key, modifiers, actionFactory);

  /// <summary>
  /// Removes a key binding (without modifiers).
  /// </summary>
  /// <param name="key">The console key to unbind.</param>
  /// <returns>This profile for fluent chaining.</returns>
  /// <remarks>
  /// The binding is removed from both the base profile and any previous modifications.
  /// Also removes the key from exit keys if it was one.
  /// </remarks>
  public CustomKeyBindingProfile Remove(ConsoleKey key)
  {
    RemovedBindings.Add((key, ConsoleModifiers.None));
    RemovedExitKeys.Add((key, ConsoleModifiers.None));
    return this;
  }

  /// <summary>
  /// Removes a key binding with modifiers.
  /// </summary>
  /// <param name="key">The console key to unbind.</param>
  /// <param name="modifiers">The modifier keys of the binding to remove.</param>
  /// <returns>This profile for fluent chaining.</returns>
  public CustomKeyBindingProfile Remove(ConsoleKey key, ConsoleModifiers modifiers)
  {
    RemovedBindings.Add((key, modifiers));
    RemovedExitKeys.Add((key, modifiers));
    return this;
  }

  /// <summary>
  /// Adds an exit key binding (without modifiers).
  /// </summary>
  /// <param name="key">The console key to make an exit key.</param>
  /// <param name="actionFactory">Factory that creates the action given a ReplConsoleReader.</param>
  /// <returns>This profile for fluent chaining.</returns>
  /// <exception cref="ArgumentNullException">Thrown when actionFactory is null.</exception>
  /// <remarks>
  /// Exit keys cause ReadLine to return control to the caller after executing the action.
  /// </remarks>
  public CustomKeyBindingProfile AddExitKey
  (
    ConsoleKey key,
    Func<ReplConsoleReader, Action> actionFactory
  )
  {
    ArgumentNullException.ThrowIfNull(actionFactory);
    (ConsoleKey, ConsoleModifiers) keyBinding = (key, ConsoleModifiers.None);
    RemovedBindings.Remove(keyBinding);
    RemovedExitKeys.Remove(keyBinding);
    Modifications.Add(new BindingModification(key, ConsoleModifiers.None, actionFactory));
    AddedExitKeys.Add(keyBinding);
    return this;
  }

  /// <summary>
  /// Adds an exit key binding with modifiers.
  /// </summary>
  /// <param name="key">The console key to make an exit key.</param>
  /// <param name="modifiers">The modifier keys (Ctrl, Alt, Shift) required.</param>
  /// <param name="actionFactory">Factory that creates the action given a ReplConsoleReader.</param>
  /// <returns>This profile for fluent chaining.</returns>
  /// <exception cref="ArgumentNullException">Thrown when actionFactory is null.</exception>
  public CustomKeyBindingProfile AddExitKey
  (
    ConsoleKey key,
    ConsoleModifiers modifiers,
    Func<ReplConsoleReader, Action> actionFactory
  )
  {
    ArgumentNullException.ThrowIfNull(actionFactory);
    (ConsoleKey, ConsoleModifiers) keyBinding = (key, modifiers);
    RemovedBindings.Remove(keyBinding);
    RemovedExitKeys.Remove(keyBinding);
    Modifications.Add(new BindingModification(key, modifiers, actionFactory));
    AddedExitKeys.Add(keyBinding);
    return this;
  }

  /// <summary>
  /// Removes an exit key status from a binding (keeps the binding, just not as exit).
  /// </summary>
  /// <param name="key">The console key to remove from exit keys.</param>
  /// <returns>This profile for fluent chaining.</returns>
  public CustomKeyBindingProfile RemoveExitKey(ConsoleKey key)
  {
    RemovedExitKeys.Add((key, ConsoleModifiers.None));
    AddedExitKeys.Remove((key, ConsoleModifiers.None));
    return this;
  }

  /// <summary>
  /// Removes an exit key status from a binding with modifiers.
  /// </summary>
  /// <param name="key">The console key to remove from exit keys.</param>
  /// <param name="modifiers">The modifier keys of the exit key to remove.</param>
  /// <returns>This profile for fluent chaining.</returns>
  public CustomKeyBindingProfile RemoveExitKey(ConsoleKey key, ConsoleModifiers modifiers)
  {
    RemovedExitKeys.Add((key, modifiers));
    AddedExitKeys.Remove((key, modifiers));
    return this;
  }

  /// <inheritdoc/>
  public Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Action> GetBindings(ReplConsoleReader reader)
  {
    ArgumentNullException.ThrowIfNull(reader);

    // Start with base profile bindings if we have one
    Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Action> bindings = BaseProfile is not null
      ? new Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Action>(BaseProfile.GetBindings(reader))
      : [];

    // Remove any bindings marked for removal
    foreach ((ConsoleKey Key, ConsoleModifiers Modifiers) key in RemovedBindings)
    {
      bindings.Remove(key);
    }

    // Apply modifications (overrides and additions)
    foreach (BindingModification modification in Modifications)
    {
      (ConsoleKey, ConsoleModifiers) key = (modification.Key, modification.Modifiers);
      if (!RemovedBindings.Contains(key))
      {
        bindings[key] = modification.ActionFactory(reader);
      }
    }

    return bindings;
  }

  /// <inheritdoc/>
  public HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> GetExitKeys()
  {
    // Start with base profile exit keys if we have one
    HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> exitKeys = BaseProfile is not null
      ? [.. BaseProfile.GetExitKeys()]
      : [];

    // Remove any exit keys marked for removal
    foreach ((ConsoleKey Key, ConsoleModifiers Modifiers) key in RemovedExitKeys)
    {
      exitKeys.Remove(key);
    }

    // Add any new exit keys
    foreach ((ConsoleKey Key, ConsoleModifiers Modifiers) key in AddedExitKeys)
    {
      exitKeys.Add(key);
    }

    return exitKeys;
  }

  /// <summary>
  /// Represents a binding modification (override or addition).
  /// </summary>
  private sealed record BindingModification
  (
    ConsoleKey Key,
    ConsoleModifiers Modifiers,
    Func<ReplConsoleReader, Action> ActionFactory
  );
}
