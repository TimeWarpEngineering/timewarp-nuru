namespace TimeWarp.Nuru;

/// <summary>
/// Parses key combination strings such as <c>Ctrl+Shift+Left</c> and <c>Alt+.</c>.
/// </summary>
/// <remarks>
/// Modifier names are <c>Ctrl</c> (or <c>Control</c>), <c>Alt</c>, and <c>Shift</c>.
/// The final segment is the key: a letter, digit, punctuation character, arrow alias
/// (<c>Left</c>, <c>Right</c>, <c>Up</c>, <c>Down</c>), or a <see cref="ConsoleKey"/> name.
/// Characters that require Shift on a US keyboard (<c>+</c>, <c>&lt;</c>, <c>_</c>, and similar)
/// include <see cref="ConsoleModifiers.Shift"/> in the result.
/// </remarks>
public static class KeyBindingComboParser
{
  private static readonly Dictionary<string, (ConsoleKey Key, ConsoleModifiers Modifiers)> NamedKeys =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["Left"] = (ConsoleKey.LeftArrow, ConsoleModifiers.None),
      ["LeftArrow"] = (ConsoleKey.LeftArrow, ConsoleModifiers.None),
      ["Right"] = (ConsoleKey.RightArrow, ConsoleModifiers.None),
      ["RightArrow"] = (ConsoleKey.RightArrow, ConsoleModifiers.None),
      ["Up"] = (ConsoleKey.UpArrow, ConsoleModifiers.None),
      ["UpArrow"] = (ConsoleKey.UpArrow, ConsoleModifiers.None),
      ["Down"] = (ConsoleKey.DownArrow, ConsoleModifiers.None),
      ["DownArrow"] = (ConsoleKey.DownArrow, ConsoleModifiers.None),
      ["Enter"] = (ConsoleKey.Enter, ConsoleModifiers.None),
      ["Return"] = (ConsoleKey.Enter, ConsoleModifiers.None),
      ["Escape"] = (ConsoleKey.Escape, ConsoleModifiers.None),
      ["Esc"] = (ConsoleKey.Escape, ConsoleModifiers.None),
      ["Tab"] = (ConsoleKey.Tab, ConsoleModifiers.None),
      ["Backspace"] = (ConsoleKey.Backspace, ConsoleModifiers.None),
      ["Bksp"] = (ConsoleKey.Backspace, ConsoleModifiers.None),
      ["Delete"] = (ConsoleKey.Delete, ConsoleModifiers.None),
      ["Del"] = (ConsoleKey.Delete, ConsoleModifiers.None),
      ["Insert"] = (ConsoleKey.Insert, ConsoleModifiers.None),
      ["Ins"] = (ConsoleKey.Insert, ConsoleModifiers.None),
      ["Home"] = (ConsoleKey.Home, ConsoleModifiers.None),
      ["End"] = (ConsoleKey.End, ConsoleModifiers.None),
      ["PageUp"] = (ConsoleKey.PageUp, ConsoleModifiers.None),
      ["PgUp"] = (ConsoleKey.PageUp, ConsoleModifiers.None),
      ["PageDown"] = (ConsoleKey.PageDown, ConsoleModifiers.None),
      ["PgDn"] = (ConsoleKey.PageDown, ConsoleModifiers.None),
      ["Space"] = (ConsoleKey.Spacebar, ConsoleModifiers.None),
      ["Spacebar"] = (ConsoleKey.Spacebar, ConsoleModifiers.None),
      ["Plus"] = (ConsoleKey.OemPlus, ConsoleModifiers.Shift),
      ["Equals"] = (ConsoleKey.OemPlus, ConsoleModifiers.None),
      ["Equal"] = (ConsoleKey.OemPlus, ConsoleModifiers.None),
      ["Minus"] = (ConsoleKey.OemMinus, ConsoleModifiers.None),
      ["Underscore"] = (ConsoleKey.OemMinus, ConsoleModifiers.Shift),
      ["Comma"] = (ConsoleKey.OemComma, ConsoleModifiers.None),
      ["Period"] = (ConsoleKey.OemPeriod, ConsoleModifiers.None),
      ["Dot"] = (ConsoleKey.OemPeriod, ConsoleModifiers.None),
    };

  private static readonly Dictionary<char, (ConsoleKey Key, ConsoleModifiers Modifiers)> Punctuation = new()
  {
    ['.'] = (ConsoleKey.OemPeriod, ConsoleModifiers.None),
    [','] = (ConsoleKey.OemComma, ConsoleModifiers.None),
    ['-'] = (ConsoleKey.OemMinus, ConsoleModifiers.None),
    ['='] = (ConsoleKey.OemPlus, ConsoleModifiers.None),
    ['+'] = (ConsoleKey.OemPlus, ConsoleModifiers.Shift),
    ['_'] = (ConsoleKey.OemMinus, ConsoleModifiers.Shift),
    ['<'] = (ConsoleKey.OemComma, ConsoleModifiers.Shift),
    ['>'] = (ConsoleKey.OemPeriod, ConsoleModifiers.Shift),
    ['/'] = (ConsoleKey.Oem2, ConsoleModifiers.None),
    ['?'] = (ConsoleKey.Oem2, ConsoleModifiers.Shift),
    [';'] = (ConsoleKey.Oem1, ConsoleModifiers.None),
    [':'] = (ConsoleKey.Oem1, ConsoleModifiers.Shift),
    ['\''] = (ConsoleKey.Oem7, ConsoleModifiers.None),
    ['"'] = (ConsoleKey.Oem7, ConsoleModifiers.Shift),
    ['['] = (ConsoleKey.Oem4, ConsoleModifiers.None),
    ['{'] = (ConsoleKey.Oem4, ConsoleModifiers.Shift),
    [']'] = (ConsoleKey.Oem6, ConsoleModifiers.None),
    ['}'] = (ConsoleKey.Oem6, ConsoleModifiers.Shift),
    ['\\'] = (ConsoleKey.Oem5, ConsoleModifiers.None),
    ['|'] = (ConsoleKey.Oem5, ConsoleModifiers.Shift),
    ['`'] = (ConsoleKey.Oem3, ConsoleModifiers.None),
    ['~'] = (ConsoleKey.Oem3, ConsoleModifiers.Shift),
    ['!'] = (ConsoleKey.D1, ConsoleModifiers.Shift),
    ['@'] = (ConsoleKey.D2, ConsoleModifiers.Shift),
    ['#'] = (ConsoleKey.D3, ConsoleModifiers.Shift),
    ['$'] = (ConsoleKey.D4, ConsoleModifiers.Shift),
    ['%'] = (ConsoleKey.D5, ConsoleModifiers.Shift),
    ['^'] = (ConsoleKey.D6, ConsoleModifiers.Shift),
    ['&'] = (ConsoleKey.D7, ConsoleModifiers.Shift),
    ['*'] = (ConsoleKey.D8, ConsoleModifiers.Shift),
    ['('] = (ConsoleKey.D9, ConsoleModifiers.Shift),
    [')'] = (ConsoleKey.D0, ConsoleModifiers.Shift),
    [' '] = (ConsoleKey.Spacebar, ConsoleModifiers.None),
  };

  /// <summary>
  /// Parses a key combination string.
  /// </summary>
  /// <param name="combination">The combination, for example <c>Ctrl+K</c> or <c>Alt+.</c>.</param>
  /// <returns>The console key and modifiers.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="combination"/> is null or empty.</exception>
  /// <exception cref="KeyBindingConfigException">Thrown when the combination cannot be parsed.</exception>
  public static (ConsoleKey Key, ConsoleModifiers Modifiers) Parse(string combination)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(combination);

    string text = combination.Trim();
    ConsoleModifiers modifiers = ConsoleModifiers.None;

    while (true)
    {
      if (text == "+")
      {
        (ConsoleKey plusKey, ConsoleModifiers plusModifiers) = ParseCharacter('+');
        return (plusKey, modifiers | plusModifiers);
      }

      int separator = text.IndexOf('+', StringComparison.Ordinal);
      if (separator < 0)
      {
        if (TryParseModifier(text, out _))
        {
          throw new KeyBindingConfigException
          (
            $"Key combination '{combination}' is missing a key."
          );
        }

        (ConsoleKey key, ConsoleModifiers extra) = ParseKeyToken(text, combination);
        return (key, modifiers | extra);
      }

      if (separator == 0)
      {
        throw new KeyBindingConfigException
        (
          $"Key combination '{combination}' has an empty modifier."
        );
      }

      string head = text[..separator].Trim();
      string tail = text[(separator + 1)..].Trim();
      if (tail.Length == 0)
      {
        throw new KeyBindingConfigException
        (
          $"Key combination '{combination}' is missing a key."
        );
      }

      if (!TryParseModifier(head, out ConsoleModifiers modifier))
      {
        throw new KeyBindingConfigException
        (
          $"Unknown modifier '{head}' in key combination '{combination}'."
        );
      }

      if ((modifiers & modifier) != ConsoleModifiers.None)
      {
        throw new KeyBindingConfigException
        (
          $"Modifier '{head}' is repeated in key combination '{combination}'."
        );
      }

      modifiers |= modifier;
      text = tail;
    }
  }

  private static (ConsoleKey Key, ConsoleModifiers Modifiers) ParseKeyToken
  (
    string token,
    string combination
  )
  {
    if (token.Length == 1)
    {
      return ParseCharacter(token[0], combination);
    }

    if (NamedKeys.TryGetValue(token, out (ConsoleKey Key, ConsoleModifiers Modifiers) named))
    {
      return named;
    }

    if (Enum.TryParse(token, ignoreCase: true, out ConsoleKey key) && key != ConsoleKey.None)
    {
      return (key, ConsoleModifiers.None);
    }

    throw new KeyBindingConfigException
    (
      $"Unknown key '{token}' in key combination '{combination}'."
    );
  }

  private static (ConsoleKey Key, ConsoleModifiers Modifiers) ParseCharacter
  (
    char character,
    string? combination = null
  )
  {
    if (character is >= 'a' and <= 'z')
    {
      return ((ConsoleKey)(ConsoleKey.A + (character - 'a')), ConsoleModifiers.None);
    }

    if (character is >= 'A' and <= 'Z')
    {
      return ((ConsoleKey)(ConsoleKey.A + (character - 'A')), ConsoleModifiers.None);
    }

    if (character is >= '0' and <= '9')
    {
      return ((ConsoleKey)(ConsoleKey.D0 + (character - '0')), ConsoleModifiers.None);
    }

    if (Punctuation.TryGetValue(character, out (ConsoleKey Key, ConsoleModifiers Modifiers) mapped))
    {
      return mapped;
    }

    string source = combination ?? character.ToString();
    throw new KeyBindingConfigException
    (
      $"Unknown key '{character}' in key combination '{source}'."
    );
  }

  private static bool TryParseModifier(string token, out ConsoleModifiers modifier)
  {
    if (token.Equals("Ctrl", StringComparison.OrdinalIgnoreCase)
      || token.Equals("Control", StringComparison.OrdinalIgnoreCase))
    {
      modifier = ConsoleModifiers.Control;
      return true;
    }

    if (token.Equals("Alt", StringComparison.OrdinalIgnoreCase))
    {
      modifier = ConsoleModifiers.Alt;
      return true;
    }

    if (token.Equals("Shift", StringComparison.OrdinalIgnoreCase))
    {
      modifier = ConsoleModifiers.Shift;
      return true;
    }

    modifier = ConsoleModifiers.None;
    return false;
  }
}
