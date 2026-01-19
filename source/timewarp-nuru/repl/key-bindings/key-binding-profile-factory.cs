namespace TimeWarp.Nuru;

/// <summary>
/// Factory for creating key binding profile instances by name.
/// </summary>
internal static class KeyBindingProfileFactory
{
  /// <summary>
  /// Gets a key binding profile by name.
  /// </summary>
  /// <param name="profileName">The name of the profile (Default, Emacs, Vi, VSCode).</param>
  /// <returns>The requested key binding profile.</returns>
  /// <exception cref="ArgumentException">Thrown when an unknown profile name is specified.</exception>
  public static IKeyBindingProfile GetProfile(string profileName)
  {
    return profileName switch
    {
      "Default" => new DefaultKeyBindingProfile(),
      "Emacs" => new EmacsKeyBindingProfile(),
      "Vi" => new ViKeyBindingProfile(),
      "VSCode" => new VSCodeKeyBindingProfile(),
      _ => throw new ArgumentException($"Unknown key binding profile: '{profileName}'. Valid profiles are: Default, Emacs, Vi, VSCode.", nameof(profileName))
    };
  }
}
