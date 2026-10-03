#region Purpose
// Lists and formats built-in REPL key bindings the way Get-PSReadLineKeyHandler does.
#endregion

#region Design
// Built-in profiles store handlers as delegates, so a chord cannot be named from the
// delegate alone. This catalog is the display table: profile, chord, and action name.
// KeyBindingActionRegistry owns the action names. A parity test checks that every
// catalog chord exists on the profile and that method-group handlers match the registry.
// Lambdas (tab completion and digit arguments) are compared by key only.
// Letters render lowercase (Ctrl+a). Named keys keep their ConsoleKey name (LeftArrow).
// Punctuation renders as the character (Alt+., Alt+<, Ctrl+-).
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// Lists the key bindings of the built-in REPL profiles.
/// </summary>
public static partial class KeyBindingCatalog
{
  private const string BasicEditing = "Basic editing functions";
  private const string CursorMovement = "Cursor movement functions";
  private const string History = "History functions";
  private const string Completion = "Completion functions";
  private const string Selection = "Selection functions";
  private const string Miscellaneous = "Miscellaneous functions";

  private static readonly string[] CategoryOrder =
  [
    BasicEditing,
    CursorMovement,
    History,
    Completion,
    Selection,
    Miscellaneous
  ];

  private static readonly Dictionary<ConsoleKey, (string Plain, string Shifted)> OemGlyphs = new()
  {
    [ConsoleKey.OemPeriod] = (".", ">"),
    [ConsoleKey.OemComma] = (",", "<"),
    [ConsoleKey.OemMinus] = ("-", "_"),
    [ConsoleKey.OemPlus] = ("=", "+"),
    [ConsoleKey.Oem1] = (";", ":"),
    [ConsoleKey.Oem2] = ("/", "?"),
    [ConsoleKey.Oem3] = ("`", "~"),
    [ConsoleKey.Oem4] = ("[", "{"),
    [ConsoleKey.Oem5] = ("\\", "|"),
    [ConsoleKey.Oem6] = ("]", "}"),
    [ConsoleKey.Oem7] = ("'", "\"")
  };

  private static readonly Dictionary<ConsoleKey, string> ShiftedDigits = new()
  {
    [ConsoleKey.D1] = "!",
    [ConsoleKey.D2] = "@",
    [ConsoleKey.D3] = "#",
    [ConsoleKey.D4] = "$",
    [ConsoleKey.D5] = "%",
    [ConsoleKey.D6] = "^",
    [ConsoleKey.D7] = "&",
    [ConsoleKey.D8] = "*",
    [ConsoleKey.D9] = "(",
    [ConsoleKey.D0] = ")"
  };

  /// <summary>
  /// Built-in profile names, in display order.
  /// </summary>
  public static IReadOnlyList<string> ProfileNames { get; } = ["Default", "Emacs", "Vi", "VSCode"];

  /// <summary>
  /// Returns whether <paramref name="profileName"/> is a built-in profile.
  /// </summary>
  /// <param name="profileName">The profile name.</param>
  /// <returns><c>true</c> when the catalog can list that profile.</returns>
  public static bool IsKnownProfile(string profileName) =>
    !string.IsNullOrWhiteSpace(profileName)
    && ProfileNames.Contains(profileName, StringComparer.Ordinal);

  /// <summary>
  /// Formats a chord the way the listing prints it.
  /// </summary>
  /// <param name="key">The key.</param>
  /// <param name="modifiers">The modifiers.</param>
  /// <returns>The chord, for example <c>Ctrl+a</c> or <c>Shift+LeftArrow</c>.</returns>
  public static string FormatChord(ConsoleKey key, ConsoleModifiers modifiers)
  {
    string keyText = FormatKey(key, ref modifiers);
    List<string> parts = [];
    if ((modifiers & ConsoleModifiers.Control) != 0)
    {
      parts.Add("Ctrl");
    }

    if ((modifiers & ConsoleModifiers.Alt) != 0)
    {
      parts.Add("Alt");
    }

    if ((modifiers & ConsoleModifiers.Shift) != 0)
    {
      parts.Add("Shift");
    }

    parts.Add(keyText);
    return string.Join('+', parts);
  }

  /// <summary>
  /// Lists the bindings for a built-in profile.
  /// </summary>
  /// <param name="profileName">Default, Emacs, Vi, or VSCode. The match is case-sensitive.</param>
  /// <param name="key">
  /// Optional chord filter. A parseable chord matches that combination only.
  /// Anything else is a case-insensitive substring of the printed chord.
  /// </param>
  /// <param name="function">Optional case-insensitive substring of the function name.</param>
  /// <returns>The matching rows, in profile order.</returns>
  /// <exception cref="ArgumentException">
  /// Thrown when <paramref name="profileName"/> is null, empty, or not a built-in profile.
  /// </exception>
  public static IReadOnlyList<KeyBindingRow> List
  (
    string profileName,
    string? key = null,
    string? function = null
  )
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(profileName);
    EnsureTable();

    if (!IsKnownProfile(profileName))
    {
      // No ParamName: ArgumentException appends it to Message, and this text is shown to users.
      throw new ArgumentException
      (
        $"Unknown key binding profile: '{profileName}'. Valid profiles are: {string.Join(", ", ProfileNames)}."
      );
    }

    string? keyFilter = string.IsNullOrWhiteSpace(key) ? null : key.Trim();
    string? functionFilter = string.IsNullOrWhiteSpace(function) ? null : function.Trim();
    bool keyIsExact = false;
    (ConsoleKey Key, ConsoleModifiers Modifiers) chord = default;
    if (keyFilter is not null)
    {
      try
      {
        chord = KeyBindingComboParser.Parse(keyFilter);
        keyIsExact = true;
      }
      catch (KeyBindingConfigException)
      {
        keyIsExact = false;
      }
    }

    List<KeyBindingRow> rows = [];
    foreach (Assignment assignment in Assignments)
    {
      if (!string.Equals(assignment.Profile, profileName, StringComparison.Ordinal))
      {
        continue;
      }

      string display = FormatChord(assignment.Key, assignment.Modifiers);
      if (keyFilter is not null)
      {
        bool matchesKey = keyIsExact
          ? assignment.Key == chord.Key && assignment.Modifiers == chord.Modifiers
          : display.Contains(keyFilter, StringComparison.OrdinalIgnoreCase);
        if (!matchesKey)
        {
          continue;
        }
      }

      if (functionFilter is not null
        && !assignment.Function.Contains(functionFilter, StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      FunctionInfo info = Functions[assignment.Function];
      rows.Add(new KeyBindingRow(display, assignment.Function, info.Description, info.Category));
    }

    return rows;
  }

  /// <summary>
  /// Renders a profile listing as text.
  /// </summary>
  /// <param name="profileName">The built-in profile name.</param>
  /// <param name="key">Optional chord filter. See <see cref="List"/>.</param>
  /// <param name="function">Optional function filter. See <see cref="List"/>.</param>
  /// <param name="detailed">
  /// <c>true</c> for one block per binding. <c>false</c> for the three-column table.
  /// </param>
  /// <returns>The listing, including the profile header.</returns>
  /// <exception cref="ArgumentException">Thrown when the profile name is unknown.</exception>
  public static string Render
  (
    string profileName,
    string? key = null,
    string? function = null,
    bool detailed = false
  )
  {
    IReadOnlyList<KeyBindingRow> rows = List(profileName, key, function);
    return Format(profileName, rows, detailed);
  }

  /// <summary>
  /// Formats rows that were already listed.
  /// </summary>
  /// <param name="profileName">Printed in the header. Not validated.</param>
  /// <param name="rows">The rows to print.</param>
  /// <param name="detailed"><c>true</c> for one block per binding.</param>
  /// <returns>The listing text.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="rows"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="profileName"/> is null or empty.</exception>
  public static string Format(string profileName, IReadOnlyList<KeyBindingRow> rows, bool detailed)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(profileName);
    ArgumentNullException.ThrowIfNull(rows);

    StringBuilder builder = new();
    builder.Append("Profile: ");
    builder.AppendLine(profileName);

    if (rows.Count == 0)
    {
      builder.AppendLine();
      builder.AppendLine("No key bindings match.");
      return builder.ToString().TrimEnd();
    }

    bool firstCategory = true;
    foreach (string category in CategoryOrder)
    {
      List<KeyBindingRow> group = [];
      foreach (KeyBindingRow row in rows)
      {
        if (row.Category == category)
        {
          group.Add(row);
        }
      }

      if (group.Count == 0)
      {
        continue;
      }

      builder.AppendLine();
      if (!firstCategory)
      {
        builder.AppendLine();
      }

      firstCategory = false;
      builder.AppendLine(category);
      builder.AppendLine(new string('=', category.Length));

      if (detailed)
      {
        AppendDetailed(builder, group);
      }
      else
      {
        AppendTable(builder, group);
      }
    }

    return builder.ToString().TrimEnd();
  }

  private static void AppendTable(StringBuilder builder, List<KeyBindingRow> group)
  {
    int keyWidth = "Key".Length;
    int functionWidth = "Function".Length;
    foreach (KeyBindingRow row in group)
    {
      if (row.Key.Length > keyWidth)
      {
        keyWidth = row.Key.Length;
      }

      if (row.Function.Length > functionWidth)
      {
        functionWidth = row.Function.Length;
      }
    }

    builder.Append("Key".PadRight(keyWidth));
    builder.Append("  ");
    builder.Append("Function".PadRight(functionWidth));
    builder.Append("  ");
    builder.AppendLine("Description");

    builder.Append(new string('-', keyWidth));
    builder.Append("  ");
    builder.Append(new string('-', functionWidth));
    builder.Append("  ");
    builder.AppendLine(new string('-', "Description".Length));

    foreach (KeyBindingRow row in group)
    {
      builder.Append(row.Key.PadRight(keyWidth));
      builder.Append("  ");
      builder.Append(row.Function.PadRight(functionWidth));
      builder.Append("  ");
      builder.AppendLine(row.Description);
    }
  }

  private static void AppendDetailed(StringBuilder builder, List<KeyBindingRow> group)
  {
    for (int index = 0; index < group.Count; index++)
    {
      KeyBindingRow row = group[index];
      if (index > 0)
      {
        builder.AppendLine();
      }

      builder.AppendLine(row.Key);
      builder.Append("  Function: ");
      builder.AppendLine(row.Function);
      builder.Append("  Category: ");
      builder.AppendLine(row.Category);
      builder.Append("  Description: ");
      builder.AppendLine(row.Description);
    }
  }

  private static string FormatKey(ConsoleKey key, ref ConsoleModifiers modifiers)
  {
    if (key is >= ConsoleKey.A and <= ConsoleKey.Z)
    {
      return ((char)('a' + (key - ConsoleKey.A))).ToString();
    }

    if (key is >= ConsoleKey.D0 and <= ConsoleKey.D9)
    {
      if ((modifiers & ConsoleModifiers.Shift) != 0 && ShiftedDigits.TryGetValue(key, out string? shifted))
      {
        modifiers &= ~ConsoleModifiers.Shift;
        return shifted;
      }

      return ((char)('0' + (key - ConsoleKey.D0))).ToString();
    }

    if (OemGlyphs.TryGetValue(key, out (string Plain, string Shifted) glyph))
    {
      if ((modifiers & ConsoleModifiers.Shift) != 0)
      {
        modifiers &= ~ConsoleModifiers.Shift;
        return glyph.Shifted;
      }

      return glyph.Plain;
    }

    return key.ToString();
  }
}
