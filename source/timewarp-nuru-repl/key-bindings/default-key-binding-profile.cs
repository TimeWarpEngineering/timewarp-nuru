namespace TimeWarp.Nuru;

/// <summary>
/// Default key binding profile providing PSReadLine-compatible keybindings.
/// </summary>
/// <remarks>
/// This profile implements the standard key bindings that ship with TimeWarp.Nuru,
/// combining Emacs-style navigation (Ctrl+A, Ctrl+E, etc.) with arrow keys for
/// a familiar experience across different platforms.
/// </remarks>
public sealed class DefaultKeyBindingProfile : IKeyBindingProfile
{
  /// <inheritdoc/>
  public string Name => "Default";

  /// <inheritdoc/>
  public Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Action> GetBindings(ReplConsoleReader reader)
  {
    ArgumentNullException.ThrowIfNull(reader);

    return new Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Action>
    {
      // === Enter/Submit ===
      [(ConsoleKey.Enter, ConsoleModifiers.None)] = reader.HandleEnter,

      // === Tab Completion ===
      [(ConsoleKey.Tab, ConsoleModifiers.None)] = () => reader.HandleTabCompletion(reverse: false),
      [(ConsoleKey.Tab, ConsoleModifiers.Shift)] = () => reader.HandleTabCompletion(reverse: true),
      [(ConsoleKey.Oem7, ConsoleModifiers.Alt)] = reader.HandlePossibleCompletions, // Alt+= (Oem7 is the = key)

      // === Character Movement (PSReadLine: BackwardChar, ForwardChar) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.None)] = reader.HandleBackwardChar,
      [(ConsoleKey.B, ConsoleModifiers.Control)] = reader.HandleBackwardChar,
      [(ConsoleKey.RightArrow, ConsoleModifiers.None)] = reader.HandleForwardChar,
      [(ConsoleKey.F, ConsoleModifiers.Control)] = reader.HandleForwardChar,

      // === Word Movement (PSReadLine: BackwardWord, ForwardWord) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Control)] = reader.HandleBackwardWord,
      [(ConsoleKey.B, ConsoleModifiers.Alt)] = reader.HandleBackwardWord,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Control)] = reader.HandleForwardWord,
      [(ConsoleKey.F, ConsoleModifiers.Alt)] = reader.HandleForwardWord,

      // === Line Position (PSReadLine: BeginningOfLine, EndOfLine) ===
      [(ConsoleKey.Home, ConsoleModifiers.None)] = reader.HandleBeginningOfLine,
      [(ConsoleKey.A, ConsoleModifiers.Control)] = reader.HandleBeginningOfLine,
      [(ConsoleKey.End, ConsoleModifiers.None)] = reader.HandleEndOfLine,
      [(ConsoleKey.E, ConsoleModifiers.Control)] = reader.HandleEndOfLine,

      // === History Navigation (PSReadLine: PreviousHistory, NextHistory) ===
      [(ConsoleKey.UpArrow, ConsoleModifiers.None)] = reader.HandlePreviousHistory,
      [(ConsoleKey.P, ConsoleModifiers.Control)] = reader.HandlePreviousHistory,
      [(ConsoleKey.DownArrow, ConsoleModifiers.None)] = reader.HandleNextHistory,
      [(ConsoleKey.N, ConsoleModifiers.Control)] = reader.HandleNextHistory,

      // === History Position (PSReadLine: BeginningOfHistory, EndOfHistory) ===
      [(ConsoleKey.OemComma, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = reader.HandleBeginningOfHistory, // Alt+<
      [(ConsoleKey.OemPeriod, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = reader.HandleEndOfHistory, // Alt+>

      // === History Prefix Search (PSReadLine: HistorySearchBackward, HistorySearchForward) ===
      [(ConsoleKey.F8, ConsoleModifiers.None)] = reader.HandleHistorySearchBackward,
      [(ConsoleKey.F8, ConsoleModifiers.Shift)] = reader.HandleHistorySearchForward,

      // === Interactive History Search (PSReadLine: ReverseSearchHistory, ForwardSearchHistory) ===
      [(ConsoleKey.R, ConsoleModifiers.Control)] = reader.HandleReverseSearchHistory,
      [(ConsoleKey.S, ConsoleModifiers.Control)] = reader.HandleForwardSearchHistory,

      // === Deletion (PSReadLine: BackwardDeleteChar, DeleteChar) ===
      [(ConsoleKey.Backspace, ConsoleModifiers.None)] = reader.HandleBackwardDeleteChar,
      [(ConsoleKey.Delete, ConsoleModifiers.None)] = reader.HandleDeleteChar,

      // === Special Keys ===
      [(ConsoleKey.Escape, ConsoleModifiers.None)] = reader.HandleEscape,
      [(ConsoleKey.D, ConsoleModifiers.Control)] = () => { }, // EOF - handled by ExitKeys
    };
  }

  /// <inheritdoc/>
  public HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> GetExitKeys() =>
  [
    (ConsoleKey.Enter, ConsoleModifiers.None),  // Submit command
    (ConsoleKey.D, ConsoleModifiers.Control)    // EOF
  ];
}
