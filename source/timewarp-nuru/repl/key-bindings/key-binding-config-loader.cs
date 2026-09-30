namespace TimeWarp.Nuru;

/// <summary>
/// Locations the loader searches for <c>keybindings.json</c>.
/// </summary>
/// <param name="EnvironmentVariablePath">
/// Explicit file from <c>NURU_KEYBINDINGS</c>. Highest precedence. Missing files are skipped.
/// </param>
/// <param name="ProjectDirectory">
/// Directory whose <c>.nuru/keybindings.json</c> is the project file. Null skips this location.
/// </param>
/// <param name="UserProfileDirectory">
/// Directory whose <c>.nuru/keybindings.json</c> is the user file. Null skips this location.
/// </param>
public readonly record struct KeyBindingConfigSearch
(
  string? EnvironmentVariablePath,
  string? ProjectDirectory,
  string? UserProfileDirectory
);

/// <summary>
/// Loads a <see cref="CustomKeyBindingProfile"/> from JSON key binding config.
/// </summary>
/// <remarks>
/// <para>
/// Search order for <see cref="TryLoadDefault"/> is:
/// </para>
/// <list type="number">
/// <item><description><c>NURU_KEYBINDINGS</c> environment variable (explicit path)</description></item>
/// <item><description><c>./.nuru/keybindings.json</c> under the current directory</description></item>
/// <item><description><c>~/.nuru/keybindings.json</c></description></item>
/// </list>
/// <para>
/// Missing files are skipped. The first file that exists is loaded. Invalid JSON or an unknown
/// action fails with <see cref="KeyBindingConfigException"/> and does not fall through to a later file.
/// </para>
/// <para>
/// The REPL calls <see cref="ResolveStartupProfile"/> only when no profile instance is set and
/// <c>KeyBindingProfileName</c> is still <c>Default</c>.
/// </para>
/// </remarks>
public static class KeyBindingConfigLoader
{
  /// <summary>
  /// Environment variable that holds an explicit config file path.
  /// </summary>
  public const string EnvironmentVariableName = "NURU_KEYBINDINGS";

  /// <summary>
  /// Directory name, under a project or the user profile, that contains the config file.
  /// </summary>
  public const string ConfigDirectoryName = ".nuru";

  /// <summary>
  /// Config file name inside <see cref="ConfigDirectoryName"/>.
  /// </summary>
  public const string ConfigFileName = "keybindings.json";

  /// <summary>
  /// Loads a profile from a JSON file.
  /// </summary>
  /// <param name="path">Path to the config file.</param>
  /// <returns>The profile described by the file.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
  /// <exception cref="KeyBindingConfigException">Thrown when the file is missing, unreadable, or invalid.</exception>
  public static CustomKeyBindingProfile LoadFromFile(string path)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(path);

    if (!File.Exists(path))
    {
      throw new KeyBindingConfigException($"Key binding config file was not found: '{path}'.");
    }

    string json;
    try
    {
      json = File.ReadAllText(path);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      throw new KeyBindingConfigException
      (
        $"Could not read key binding config '{path}': {exception.Message}",
        exception
      );
    }

    try
    {
      return LoadFromJson(json);
    }
    catch (KeyBindingConfigException exception)
    {
      throw new KeyBindingConfigException
      (
        $"Key binding config '{path}': {exception.Message}",
        exception
      );
    }
  }

  /// <summary>
  /// Loads a profile from a JSON string.
  /// </summary>
  /// <param name="json">The config document.</param>
  /// <returns>The profile described by the document.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
  /// <exception cref="KeyBindingConfigException">Thrown when the document is invalid.</exception>
  public static CustomKeyBindingProfile LoadFromJson(string json)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(json);
    KeyBindingConfig config = Deserialize(json);
    return Build(config, json);
  }

  /// <summary>
  /// Loads the first config file found in the default search path.
  /// </summary>
  /// <returns>The loaded profile.</returns>
  /// <exception cref="KeyBindingConfigException">
  /// Thrown when no config file exists, or the first existing file is invalid.
  /// </exception>
  public static CustomKeyBindingProfile LoadDefault() => Load(CreateDefaultSearch());

  /// <summary>
  /// Loads the first config file found for <paramref name="search"/>.
  /// </summary>
  /// <param name="search">The locations to search, in precedence order.</param>
  /// <returns>The loaded profile.</returns>
  /// <exception cref="KeyBindingConfigException">
  /// Thrown when no config file exists, or the first existing file is invalid.
  /// </exception>
  public static CustomKeyBindingProfile Load(KeyBindingConfigSearch search)
  {
    if (TryLoad(search, out IKeyBindingProfile? profile) && profile is CustomKeyBindingProfile custom)
    {
      return custom;
    }

    throw new KeyBindingConfigException
    (
      "No key binding config file was found. Set NURU_KEYBINDINGS, or create ./.nuru/keybindings.json, or ~/.nuru/keybindings.json."
    );
  }

  /// <summary>
  /// Tries to load the first config file in the default search path.
  /// </summary>
  /// <param name="profile">The loaded profile, or null when no file exists.</param>
  /// <returns><c>true</c> when a file was found and loaded.</returns>
  /// <exception cref="KeyBindingConfigException">Thrown when the first existing file is invalid.</exception>
  public static bool TryLoadDefault(out IKeyBindingProfile? profile) =>
    TryLoad(CreateDefaultSearch(), out profile);

  /// <summary>
  /// Tries to load the first config file in the default search path and logs the path at Debug.
  /// </summary>
  /// <param name="logger">The logger that receives the loaded-path message.</param>
  /// <param name="profile">The loaded profile, or null when no file exists.</param>
  /// <returns><c>true</c> when a file was found and loaded.</returns>
  /// <exception cref="KeyBindingConfigException">Thrown when the first existing file is invalid.</exception>
  public static bool TryLoadDefault(ILogger logger, out IKeyBindingProfile? profile) =>
    TryLoad(CreateDefaultSearch(), logger, out profile);

  /// <summary>
  /// Tries to load the first existing file in <paramref name="search"/>.
  /// </summary>
  /// <param name="search">The locations to search, in precedence order.</param>
  /// <param name="profile">The loaded profile, or null when no file exists.</param>
  /// <returns><c>true</c> when a file was found and loaded.</returns>
  /// <exception cref="KeyBindingConfigException">Thrown when the first existing file is invalid.</exception>
  public static bool TryLoad(KeyBindingConfigSearch search, out IKeyBindingProfile? profile) =>
    TryLoad(search, logger: null, out profile);

  /// <summary>
  /// Tries to load the first existing file in <paramref name="search"/>.
  /// </summary>
  /// <param name="search">The locations to search, in precedence order.</param>
  /// <param name="logger">Optional logger. A loaded file is reported at Debug.</param>
  /// <param name="profile">The loaded profile, or null when no file exists.</param>
  /// <returns><c>true</c> when a file was found and loaded.</returns>
  /// <exception cref="KeyBindingConfigException">Thrown when the first existing file is invalid.</exception>
  public static bool TryLoad
  (
    KeyBindingConfigSearch search,
    ILogger? logger,
    out IKeyBindingProfile? profile
  )
  {
    foreach (string path in GetCandidatePaths(search))
    {
      if (!File.Exists(path))
      {
        continue;
      }

      profile = LoadFromFile(path);
      if (logger is not null)
      {
        ReplLoggerMessages.KeyBindingConfigLoaded(logger, path, null);
      }

      return true;
    }

    profile = null;
    return false;
  }

  /// <summary>
  /// Builds the default search from the process environment, current directory, and user profile.
  /// </summary>
  /// <returns>The search locations, with empty user-profile paths omitted.</returns>
  public static KeyBindingConfigSearch CreateDefaultSearch()
  {
    string? userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    if (string.IsNullOrWhiteSpace(userProfile))
    {
      userProfile = null;
    }

    return new KeyBindingConfigSearch
    (
      Environment.GetEnvironmentVariable(EnvironmentVariableName),
      Directory.GetCurrentDirectory(),
      userProfile
    );
  }

  /// <summary>
  /// Returns candidate config paths in precedence order.
  /// </summary>
  /// <param name="search">The locations to expand.</param>
  /// <returns>Paths that should be tested with <see cref="File.Exists(string)"/>.</returns>
  /// <exception cref="KeyBindingConfigException">
  /// Thrown when <c>NURU_KEYBINDINGS</c> points at a directory.
  /// </exception>
  public static IReadOnlyList<string> GetCandidatePaths(KeyBindingConfigSearch search)
  {
    List<string> paths = [];

    if (!string.IsNullOrWhiteSpace(search.EnvironmentVariablePath))
    {
      string environmentPath = search.EnvironmentVariablePath.Trim();
      if (Directory.Exists(environmentPath))
      {
        throw new KeyBindingConfigException
        (
          $"NURU_KEYBINDINGS points at a directory, not a file: '{environmentPath}'."
        );
      }

      paths.Add(environmentPath);
    }

    if (!string.IsNullOrWhiteSpace(search.ProjectDirectory))
    {
      paths.Add(Path.Combine(search.ProjectDirectory, ConfigDirectoryName, ConfigFileName));
    }

    if (!string.IsNullOrWhiteSpace(search.UserProfileDirectory))
    {
      paths.Add(Path.Combine(search.UserProfileDirectory, ConfigDirectoryName, ConfigFileName));
    }

    return paths;
  }

  /// <summary>
  /// Chooses the profile the REPL should start with.
  /// </summary>
  /// <param name="configuredProfile">
  /// <see cref="ReplOptions.KeyBindingProfile"/>. An <see cref="IKeyBindingProfile"/> wins over everything else.
  /// </param>
  /// <param name="profileName">
  /// <see cref="ReplOptions.KeyBindingProfileName"/>. A value other than <c>Default</c> skips config files.
  /// </param>
  /// <param name="search">Config file locations. Used only for the default profile name.</param>
  /// <param name="logger">Optional logger for the loaded config path.</param>
  /// <returns>The profile to install on the reader.</returns>
  /// <exception cref="KeyBindingConfigException">Thrown when the discovered config file is invalid.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="profileName"/> is not a known profile.</exception>
  public static IKeyBindingProfile ResolveStartupProfile
  (
    object? configuredProfile,
    string? profileName,
    KeyBindingConfigSearch search,
    ILogger? logger = null
  )
  {
    if (configuredProfile is IKeyBindingProfile explicitProfile)
    {
      return explicitProfile;
    }

    string name = string.IsNullOrWhiteSpace(profileName) ? "Default" : profileName;
    if (!string.Equals(name, "Default", StringComparison.Ordinal))
    {
      return KeyBindingProfileFactory.GetProfile(name);
    }

    if (TryLoad(search, logger, out IKeyBindingProfile? discovered) && discovered is not null)
    {
      return discovered;
    }

    return KeyBindingProfileFactory.GetProfile(name);
  }

  private static KeyBindingConfig Deserialize(string json)
  {
    try
    {
      KeyBindingConfig? config = JsonSerializer.Deserialize
      (
        json,
        KeyBindingConfigJsonSerializerContext.Default.KeyBindingConfig
      );

      if (config is null)
      {
        throw new KeyBindingConfigException("Key binding config JSON must be an object.");
      }

      return config;
    }
    catch (JsonException exception)
    {
      long line = (exception.LineNumber ?? 0) + 1;
      long column = (exception.BytePositionInLine ?? 0) + 1;
      throw new KeyBindingConfigException
      (
        $"Line {line}, column {column}: Invalid key binding JSON. {exception.Message}",
        exception
      );
    }
  }

  private static CustomKeyBindingProfile Build(KeyBindingConfig config, string json)
  {
    IKeyBindingProfile? baseProfile = ResolveBaseProfile(config.BaseProfile, json);
    CustomKeyBindingProfile profile = baseProfile is null
      ? new CustomKeyBindingProfile()
      : new CustomKeyBindingProfile(baseProfile);

    string name = config.Name?.Trim() ?? string.Empty;
    if (name.Length > 0)
    {
      profile.WithName(name);
    }
    else if (baseProfile is not null)
    {
      profile.WithName(baseProfile.Name);
    }

    foreach (string removal in config.Removals ?? [])
    {
      (ConsoleKey key, ConsoleModifiers modifiers) = ParseCombo(removal, json);
      profile.Remove(key, modifiers);
    }

    ApplyMap(profile, config.Overrides, json);
    ApplyMap(profile, config.Additions, json);

    foreach (string exitKey in config.ExitKeys ?? [])
    {
      (ConsoleKey key, ConsoleModifiers modifiers) = ParseCombo(exitKey, json);
      profile.MarkExitKey(key, modifiers);
    }

    return profile;
  }

  private static void ApplyMap
  (
    CustomKeyBindingProfile profile,
    Dictionary<string, string>? map,
    string json
  )
  {
    if (map is null)
    {
      return;
    }

    foreach (KeyValuePair<string, string> pair in map)
    {
      if (string.IsNullOrWhiteSpace(pair.Key))
      {
        throw new KeyBindingConfigException("A key combination string is missing.");
      }

      if (string.IsNullOrWhiteSpace(pair.Value))
      {
        int line = FindLineNumber(json, pair.Key);
        throw new KeyBindingConfigException
        (
          $"Line {line}: Key '{pair.Key}' is missing an action name."
        );
      }

      (ConsoleKey key, ConsoleModifiers modifiers) = ParseCombo(pair.Key, json);
      string actionName = pair.Value.Trim();
      EnsureKnownAction(actionName, json);
      string capturedAction = actionName;
      profile.Override
      (
        key,
        modifiers,
        reader => KeyBindingActionRegistry.GetAction(capturedAction, reader)
      );
    }
  }

  private static IKeyBindingProfile? ResolveBaseProfile(string? baseProfile, string json)
  {
    if (string.IsNullOrWhiteSpace(baseProfile))
    {
      return null;
    }

    string trimmed = baseProfile.Trim();
    try
    {
      return KeyBindingProfileFactory.GetProfile(trimmed);
    }
    catch (ArgumentException exception)
    {
      int line = FindLineNumber(json, trimmed);
      throw new KeyBindingConfigException($"Line {line}: {exception.Message}", exception);
    }
  }

  private static void EnsureKnownAction(string actionName, string json)
  {
    if (KeyBindingActionRegistry.IsKnownAction(actionName))
    {
      return;
    }

    int line = FindLineNumber(json, actionName);
    throw new KeyBindingConfigException
    (
      $"Line {line}: Unknown key binding action '{actionName}'."
    );
  }

  private static (ConsoleKey Key, ConsoleModifiers Modifiers) ParseCombo(string combination, string json)
  {
    if (string.IsNullOrWhiteSpace(combination))
    {
      throw new KeyBindingConfigException("A key combination string is empty.");
    }

    try
    {
      return KeyBindingComboParser.Parse(combination);
    }
    catch (KeyBindingConfigException exception)
    {
      int line = FindLineNumber(json, combination.Trim());
      throw new KeyBindingConfigException($"Line {line}: {exception.Message}", exception);
    }
  }

  private static int FindLineNumber(string json, string token)
  {
    string quoted = "\"" + token + "\"";
    int index = json.IndexOf(quoted, StringComparison.Ordinal);
    if (index < 0)
    {
      index = json.IndexOf(token, StringComparison.Ordinal);
    }

    if (index < 0)
    {
      return 1;
    }

    int line = 1;
    for (int i = 0; i < index; i++)
    {
      if (json[i] == '\n')
      {
        line++;
      }
    }

    return line;
  }
}
