namespace TimeWarp.Nuru;

/// <summary>
/// Emacs-style key binding profile using GNU Readline/bash conventions.
/// </summary>
/// <remarks>
/// <para>
/// This profile implements standard Emacs/readline keybindings familiar to bash and
/// GNU Readline users. It focuses on Ctrl-based navigation without relying on arrow keys.
/// </para>
/// <para><strong>Key Bindings:</strong></para>
/// <list type="table">
/// <listheader>
///   <term>Key</term>
///   <description>Action</description>
/// </listheader>
/// <item>
///   <term>Ctrl+A</term>
///   <description>Move to beginning of line</description>
/// </item>
/// <item>
///   <term>Ctrl+E</term>
///   <description>Move to end of line</description>
/// </item>
/// <item>
///   <term>Ctrl+F</term>
///   <description>Move forward one character</description>
/// </item>
/// <item>
///   <term>Ctrl+B</term>
///   <description>Move backward one character</description>
/// </item>
/// <item>
///   <term>Alt+F</term>
///   <description>Move forward one word</description>
/// </item>
/// <item>
///   <term>Alt+B</term>
///   <description>Move backward one word</description>
/// </item>
/// <item>
///   <term>Ctrl+K</term>
///   <description>Kill (delete) to end of line</description>
/// </item>
/// <item>
///   <term>Ctrl+U</term>
///   <description>Kill (delete) from start of line to cursor</description>
/// </item>
/// <item>
///   <term>Ctrl+W</term>
///   <description>Kill previous whitespace-delimited word</description>
/// </item>
/// <item>
///   <term>Alt+D</term>
///   <description>Kill from cursor to end of word</description>
/// </item>
/// <item>
///   <term>Alt+Backspace</term>
///   <description>Kill from start of word to cursor</description>
/// </item>
/// <item>
///   <term>Ctrl+Y</term>
///   <description>Yank (paste) most recent kill</description>
/// </item>
/// <item>
///   <term>Alt+Y</term>
///   <description>Yank-pop: cycle through kill ring after yank</description>
/// </item>
/// <item>
///   <term>Ctrl+D</term>
///   <description>Delete character under cursor (or EOF if line empty)</description>
/// </item>
/// <item>
///   <term>Ctrl+P</term>
///   <description>Previous history entry</description>
/// </item>
/// <item>
///   <term>Ctrl+N</term>
///   <description>Next history entry</description>
/// </item>
/// <item>
///   <term>Alt+&lt;</term>
///   <description>Move to beginning of history</description>
/// </item>
/// <item>
///   <term>Alt+&gt;</term>
///   <description>Move to end of history</description>
/// </item>
/// <item>
///   <term>Tab</term>
///   <description>Complete current token</description>
/// </item>
/// <item>
///   <term>Alt+=</term>
///   <description>Show possible completions</description>
/// </item>
/// <item>
///   <term>Backspace</term>
///   <description>Delete character before cursor</description>
/// </item>
/// <item>
///   <term>Enter</term>
///   <description>Submit command</description>
/// </item>
/// </list>
/// <para>
/// Note: Arrow keys are intentionally omitted to maintain pure Emacs/readline conventions,
/// though users can create a custom profile if they want hybrid bindings.
/// </para>
/// </remarks>
public sealed class EmacsKeyBindingProfile : IKeyBindingProfile
{
  /// <inheritdoc/>
  public string Name => "Emacs";

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
      [(ConsoleKey.Oem7, ConsoleModifiers.Alt)] = reader.HandlePossibleCompletions, // Alt+=

      // === Character Movement (Emacs: forward-char, backward-char) ===
      [(ConsoleKey.F, ConsoleModifiers.Control)] = reader.HandleForwardChar,
      [(ConsoleKey.B, ConsoleModifiers.Control)] = reader.HandleBackwardChar,

      // === Word Movement (Emacs: forward-word, backward-word) ===
      [(ConsoleKey.F, ConsoleModifiers.Alt)] = reader.HandleForwardWord,
      [(ConsoleKey.B, ConsoleModifiers.Alt)] = reader.HandleBackwardWord,

      // === Line Position (Emacs: beginning-of-line, end-of-line) ===
      [(ConsoleKey.A, ConsoleModifiers.Control)] = reader.HandleBeginningOfLine,
      [(ConsoleKey.E, ConsoleModifiers.Control)] = reader.HandleEndOfLine,

      // === History Navigation (Emacs: previous-history, next-history) ===
      [(ConsoleKey.P, ConsoleModifiers.Control)] = reader.HandlePreviousHistory,
      [(ConsoleKey.N, ConsoleModifiers.Control)] = reader.HandleNextHistory,

      // === History Position (Emacs: beginning-of-history, end-of-history) ===
      [(ConsoleKey.OemComma, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = reader.HandleBeginningOfHistory, // Alt+<
      [(ConsoleKey.OemPeriod, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = reader.HandleEndOfHistory, // Alt+>

      // === Interactive History Search (Emacs: reverse-search-history, forward-search-history) ===
      [(ConsoleKey.R, ConsoleModifiers.Control)] = reader.HandleReverseSearchHistory,
      [(ConsoleKey.S, ConsoleModifiers.Control)] = reader.HandleForwardSearchHistory,

      // === Deletion (Emacs: backward-delete-char, delete-char, delete-char-or-eof) ===
      [(ConsoleKey.Backspace, ConsoleModifiers.None)] = reader.HandleBackwardDeleteChar,
      [(ConsoleKey.H, ConsoleModifiers.Control)] = reader.HandleBackwardDeleteChar,  // Ctrl+H = Backspace
      [(ConsoleKey.Delete, ConsoleModifiers.None)] = reader.HandleDeleteChar,
      [(ConsoleKey.D, ConsoleModifiers.Control)] = reader.HandleDeleteCharOrExit,  // Delete char or EOF if empty

      // === Kill Operations (Emacs: kill-line, backward-kill-input, unix-word-rubout, kill-word, backward-kill-word) ===
      [(ConsoleKey.K, ConsoleModifiers.Control)] = reader.HandleKillLine,
      [(ConsoleKey.U, ConsoleModifiers.Control)] = reader.HandleBackwardKillInput,
      [(ConsoleKey.W, ConsoleModifiers.Control)] = reader.HandleUnixWordRubout,
      [(ConsoleKey.D, ConsoleModifiers.Alt)] = reader.HandleKillWord,
      [(ConsoleKey.Backspace, ConsoleModifiers.Alt)] = reader.HandleBackwardKillWord,

      // === Yank Operations (Emacs: yank, yank-pop) ===
      [(ConsoleKey.Y, ConsoleModifiers.Control)] = reader.HandleYank,
      [(ConsoleKey.Y, ConsoleModifiers.Alt)] = reader.HandleYankPop,

      // === Yank Argument Operations (Emacs: yank-last-arg, yank-nth-arg) ===
      [(ConsoleKey.OemPeriod, ConsoleModifiers.Alt)] = reader.HandleYankLastArg,  // Alt+. (insert-last-argument)
      [(ConsoleKey.OemMinus, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = reader.HandleYankLastArg,  // Alt+_
      [(ConsoleKey.Y, ConsoleModifiers.Alt | ConsoleModifiers.Control)] = reader.HandleYankNthArg,  // Alt+Ctrl+Y

      // === Digit Arguments for YankNthArg (Alt+0 through Alt+9) ===
      [(ConsoleKey.D0, ConsoleModifiers.Alt)] = () => reader.HandleDigitArgument(0),
      [(ConsoleKey.D1, ConsoleModifiers.Alt)] = () => reader.HandleDigitArgument(1),
      [(ConsoleKey.D2, ConsoleModifiers.Alt)] = () => reader.HandleDigitArgument(2),
      [(ConsoleKey.D3, ConsoleModifiers.Alt)] = () => reader.HandleDigitArgument(3),
      [(ConsoleKey.D4, ConsoleModifiers.Alt)] = () => reader.HandleDigitArgument(4),
      [(ConsoleKey.D5, ConsoleModifiers.Alt)] = () => reader.HandleDigitArgument(5),
      [(ConsoleKey.D6, ConsoleModifiers.Alt)] = () => reader.HandleDigitArgument(6),
      [(ConsoleKey.D7, ConsoleModifiers.Alt)] = () => reader.HandleDigitArgument(7),
      [(ConsoleKey.D8, ConsoleModifiers.Alt)] = () => reader.HandleDigitArgument(8),
      [(ConsoleKey.D9, ConsoleModifiers.Alt)] = () => reader.HandleDigitArgument(9),

      // === Word Case Operations (Emacs: upcase-word, downcase-word, capitalize-word) ===
      [(ConsoleKey.U, ConsoleModifiers.Alt)] = reader.HandleUpcaseWord,
      [(ConsoleKey.L, ConsoleModifiers.Alt)] = reader.HandleDowncaseWord,
      [(ConsoleKey.C, ConsoleModifiers.Alt)] = reader.HandleCapitalizeWord,

      // === Character Transposition (Emacs: transpose-chars) ===
      [(ConsoleKey.T, ConsoleModifiers.Control)] = reader.HandleSwapCharacters,

      // === Word Deletion (Emacs: backward-kill-word with Ctrl+Backspace) ===
      [(ConsoleKey.Backspace, ConsoleModifiers.Control)] = reader.HandleBackwardDeleteWord,

      // === Undo/Redo Operations (Emacs: undo, redo, revert-line) ===
      [(ConsoleKey.OemMinus, ConsoleModifiers.Control)] = reader.HandleUndo,  // Ctrl+_ (underscore) - canonical Emacs
      [(ConsoleKey.Z, ConsoleModifiers.Control)] = reader.HandleUndo,         // Ctrl+Z - alternative
      [(ConsoleKey.Z, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleRedo,
      [(ConsoleKey.R, ConsoleModifiers.Alt)] = reader.HandleRevertLine,

      // === Word Selection (Emacs: uses Shift+movement) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Shift)] = reader.HandleSelectBackwardChar,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Shift)] = reader.HandleSelectForwardChar,
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleSelectBackwardWord,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleSelectNextWord,
      [(ConsoleKey.B, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = reader.HandleSelectBackwardWord,
      [(ConsoleKey.F, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = reader.HandleSelectNextWord,
      [(ConsoleKey.Home, ConsoleModifiers.Shift)] = reader.HandleSelectBackwardsLine,
      [(ConsoleKey.End, ConsoleModifiers.Shift)] = reader.HandleSelectLine,
      [(ConsoleKey.A, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleSelectAll,

      // === Clipboard Operations ===
      [(ConsoleKey.C, ConsoleModifiers.Control)] = reader.HandleCopyOrCancelLine,
      [(ConsoleKey.X, ConsoleModifiers.Control)] = reader.HandleCut,
      [(ConsoleKey.V, ConsoleModifiers.Control)] = reader.HandlePaste,

      // === Screen Operations (Emacs: clear-screen) ===
      [(ConsoleKey.L, ConsoleModifiers.Control)] = reader.HandleClearScreen,

      // === Insert Mode Toggle ===
      [(ConsoleKey.Insert, ConsoleModifiers.None)] = reader.HandleToggleInsertMode,

      // === Alternative AcceptLine bindings (Emacs: accept-line) ===
      [(ConsoleKey.M, ConsoleModifiers.Control)] = reader.HandleEnter,  // Ctrl+M = carriage return
      [(ConsoleKey.J, ConsoleModifiers.Control)] = reader.HandleEnter,  // Ctrl+J = newline

      // === Special Keys ===
      [(ConsoleKey.Escape, ConsoleModifiers.None)] = reader.HandleEscape,
    };
  }

  /// <inheritdoc/>
  public HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> GetExitKeys() =>
  [
    (ConsoleKey.Enter, ConsoleModifiers.None),   // Submit command
    (ConsoleKey.M, ConsoleModifiers.Control),    // Ctrl+M = Enter
    (ConsoleKey.J, ConsoleModifiers.Control),    // Ctrl+J = Newline
  ];
}
