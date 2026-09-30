namespace TimeWarp.Nuru;

/// <summary>
/// Thrown when a key binding JSON config is missing, unreadable, or invalid.
/// </summary>
/// <remarks>
/// Messages for parse and validation failures include a 1-based line number when the
/// source text can be located.
/// </remarks>
[SuppressMessage(
  "Design",
  "CA1032:Implement standard exception constructors",
  Justification = "Callers always supply a message. A parameterless constructor would hide the config error.")]
public sealed class KeyBindingConfigException : Exception
{
  /// <summary>
  /// Creates an exception with the given message.
  /// </summary>
  /// <param name="message">The error message.</param>
  public KeyBindingConfigException(string message)
    : base(message)
  {
  }

  /// <summary>
  /// Creates an exception with the given message and inner exception.
  /// </summary>
  /// <param name="message">The error message.</param>
  /// <param name="innerException">The exception that caused this failure.</param>
  public KeyBindingConfigException(string message, Exception innerException)
    : base(message, innerException)
  {
  }
}

/// <summary>
/// JSON document for a global key binding profile.
/// </summary>
/// <remarks>
/// <para>
/// Property names are camelCase: <c>name</c>, <c>baseProfile</c>, <c>overrides</c>,
/// <c>additions</c>, <c>removals</c>, and <c>exitKeys</c>.
/// </para>
/// <para>
/// <c>baseProfile</c> is one of <c>Default</c>, <c>Emacs</c>, <c>Vi</c>, or <c>VSCode</c>.
/// Key strings use the form <c>Ctrl+Shift+Left</c>. Action names come from
/// <see cref="KeyBindingActionRegistry.GetAvailableActions"/>.
/// </para>
/// </remarks>
[SuppressMessage(
  "Design",
  "CA2227:Collection properties should be read only",
  Justification = "System.Text.Json assigns these collections while reading a config file.")]
[SuppressMessage(
  "Design",
  "CA1002:Do not expose generic lists",
  Justification = "The JSON document shape is a list of key combination strings.")]
public sealed class KeyBindingConfig
{
  /// <summary>
  /// Display name of the profile. When omitted, the base profile name is used.
  /// </summary>
  public string? Name { get; set; }

  /// <summary>
  /// Optional built-in profile to start from: Default, Emacs, Vi, or VSCode.
  /// </summary>
  public string? BaseProfile { get; set; }

  /// <summary>
  /// Bindings that replace the same key on the base profile.
  /// </summary>
  public Dictionary<string, string>? Overrides { get; set; }

  /// <summary>
  /// Bindings applied after overrides. An addition wins when the same key is also overridden.
  /// </summary>
  public Dictionary<string, string>? Additions { get; set; }

  /// <summary>
  /// Key combinations to remove from the base profile, including their exit-key status.
  /// </summary>
  public List<string>? Removals { get; set; }

  /// <summary>
  /// Key combinations that should end the read loop after their action runs.
  /// The key still needs a binding (from the base profile, overrides, or additions).
  /// </summary>
  public List<string>? ExitKeys { get; set; }
}

/// <summary>
/// Source-generated JSON metadata for <see cref="KeyBindingConfig"/>.
/// </summary>
[JsonSerializable(typeof(KeyBindingConfig))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(List<string>))]
[JsonSourceGenerationOptions(
  PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
  PropertyNameCaseInsensitive = true,
  AllowTrailingCommas = true,
  ReadCommentHandling = JsonCommentHandling.Skip,
  UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
internal partial class KeyBindingConfigJsonSerializerContext : JsonSerializerContext;
