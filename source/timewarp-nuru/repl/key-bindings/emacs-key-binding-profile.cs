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
  public Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Func<Task>> GetBindings(ReplConsoleReader reader)
  {
    ArgumentNullException.ThrowIfNull(reader);

    return new Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Func<Task>>
    {
      // === Enter/Submit ===
      [(ConsoleKey.Enter, ConsoleModifiers.None)] = reader.HandleEnterAsync,

      // === Tab Completion ===
      [(ConsoleKey.Tab, ConsoleModifiers.None)] = () => reader.HandleTabCompletionAsync(reverse: false),
      [(ConsoleKey.Tab, ConsoleModifiers.Shift)] = () => reader.HandleTabCompletionAsync(reverse: true),
      [(ConsoleKey.Oem7, ConsoleModifiers.Alt)] = reader.HandlePossibleCompletionsAsync, // Alt+=

      // === Character Movement (Emacs: forward-char, backward-char) ===
      [(ConsoleKey.F, ConsoleModifiers.Control)] = reader.HandleForwardCharAsync,
      [(ConsoleKey.B, ConsoleModifiers.Control)] = reader.HandleBackwardCharAsync,

      // === Word Movement (Emacs: forward-word, backward-word) ===
      [(ConsoleKey.F, ConsoleModifiers.Alt)] = reader.HandleForwardWordAsync,
      [(ConsoleKey.B, ConsoleModifiers.Alt)] = reader.HandleBackwardWordAsync,

      // === Line Position (Emacs: beginning-of-line, end-of-line) ===
      [(ConsoleKey.A, ConsoleModifiers.Control)] = reader.HandleBeginningOfLineAsync,
      [(ConsoleKey.E, ConsoleModifiers.Control)] = reader.HandleEndOfLineAsync,

      // === History Navigation (Emacs: previous-history, next-history) ===
      [(ConsoleKey.P, ConsoleModifiers.Control)] = reader.HandlePreviousHistoryAsync,
      [(ConsoleKey.N, ConsoleModifiers.Control)] = reader.HandleNextHistoryAsync,

      // === History Position (Emacs: beginning-of-history, end-of-history) ===
      [(ConsoleKey.OemComma, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = reader.HandleBeginningOfHistoryAsync, // Alt+<
      [(ConsoleKey.OemPeriod, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = reader.HandleEndOfHistoryAsync, // Alt+>

      // === Interactive History Search (Emacs: reverse-search-history, forward-search-history) ===
      [(ConsoleKey.R, ConsoleModifiers.Control)] = reader.HandleReverseSearchHistoryAsync,
      [(ConsoleKey.S, ConsoleModifiers.Control)] = reader.HandleForwardSearchHistoryAsync,

      // === Deletion (Emacs: backward-delete-char, delete-char, delete-char-or-eof) ===
      [(ConsoleKey.Backspace, ConsoleModifiers.None)] = reader.HandleBackwardDeleteCharAsync,
      [(ConsoleKey.H, ConsoleModifiers.Control)] = reader.HandleBackwardDeleteCharAsync,  // Ctrl+H = Backspace
      [(ConsoleKey.Delete, ConsoleModifiers.None)] = reader.HandleDeleteCharAsync,
      [(ConsoleKey.D, ConsoleModifiers.Control)] = reader.HandleDeleteCharOrExitAsync,  // Delete char or EOF if empty

      // === Kill Operations (Emacs: kill-line, backward-kill-input, unix-word-rubout, kill-word, backward-kill-word) ===
      [(ConsoleKey.K, ConsoleModifiers.Control)] = reader.HandleKillLineAsync,
      [(ConsoleKey.U, ConsoleModifiers.Control)] = reader.HandleBackwardKillInputAsync,
      [(ConsoleKey.W, ConsoleModifiers.Control)] = reader.HandleUnixWordRuboutAsync,
      [(ConsoleKey.D, ConsoleModifiers.Alt)] = reader.HandleKillWordAsync,
      [(ConsoleKey.Backspace, ConsoleModifiers.Alt)] = reader.HandleBackwardKillWordAsync,

      // === Yank Operations (Emacs: yank, yank-pop) ===
      [(ConsoleKey.Y, ConsoleModifiers.Control)] = reader.HandleYankAsync,
      [(ConsoleKey.Y, ConsoleModifiers.Alt)] = reader.HandleYankPopAsync,

      // === Yank Argument Operations (Emacs: yank-last-arg, yank-nth-arg) ===
      [(ConsoleKey.OemPeriod, ConsoleModifiers.Alt)] = reader.HandleYankLastArgAsync,  // Alt+. (insert-last-argument)
      [(ConsoleKey.OemMinus, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = reader.HandleYankLastArgAsync,  // Alt+_
      [(ConsoleKey.Y, ConsoleModifiers.Alt | ConsoleModifiers.Control)] = reader.HandleYankNthArgAsync,  // Alt+Ctrl+Y

      // === Digit Arguments for YankNthArg (Alt+0 through Alt+9) ===
      [(ConsoleKey.D0, ConsoleModifiers.Alt)] = () => { reader.HandleDigitArgument(0); return Task.CompletedTask; },
      [(ConsoleKey.D1, ConsoleModifiers.Alt)] = () => { reader.HandleDigitArgument(1); return Task.CompletedTask; },
      [(ConsoleKey.D2, ConsoleModifiers.Alt)] = () => { reader.HandleDigitArgument(2); return Task.CompletedTask; },
      [(ConsoleKey.D3, ConsoleModifiers.Alt)] = () => { reader.HandleDigitArgument(3); return Task.CompletedTask; },
      [(ConsoleKey.D4, ConsoleModifiers.Alt)] = () => { reader.HandleDigitArgument(4); return Task.CompletedTask; },
      [(ConsoleKey.D5, ConsoleModifiers.Alt)] = () => { reader.HandleDigitArgument(5); return Task.CompletedTask; },
      [(ConsoleKey.D6, ConsoleModifiers.Alt)] = () => { reader.HandleDigitArgument(6); return Task.CompletedTask; },
      [(ConsoleKey.D7, ConsoleModifiers.Alt)] = () => { reader.HandleDigitArgument(7); return Task.CompletedTask; },
      [(ConsoleKey.D8, ConsoleModifiers.Alt)] = () => { reader.HandleDigitArgument(8); return Task.CompletedTask; },
      [(ConsoleKey.D9, ConsoleModifiers.Alt)] = () => { reader.HandleDigitArgument(9); return Task.CompletedTask; },

      // === Word Case Operations (Emacs: upcase-word, downcase-word, capitalize-word) ===
      [(ConsoleKey.U, ConsoleModifiers.Alt)] = reader.HandleUpcaseWordAsync,
      [(ConsoleKey.L, ConsoleModifiers.Alt)] = reader.HandleDowncaseWordAsync,
      [(ConsoleKey.C, ConsoleModifiers.Alt)] = reader.HandleCapitalizeWordAsync,

      // === Character Transposition (Emacs: transpose-chars) ===
      [(ConsoleKey.T, ConsoleModifiers.Control)] = reader.HandleSwapCharactersAsync,

      // === Word Deletion (Emacs: backward-kill-word with Ctrl+Backspace) ===
      [(ConsoleKey.Backspace, ConsoleModifiers.Control)] = reader.HandleBackwardDeleteWordAsync,

      // === Undo/Redo Operations (Emacs: undo, redo, revert-line) ===
      [(ConsoleKey.OemMinus, ConsoleModifiers.Control)] = reader.HandleUndoAsync,  // Ctrl+_ (underscore) - canonical Emacs
      [(ConsoleKey.Z, ConsoleModifiers.Control)] = reader.HandleUndoAsync,         // Ctrl+Z - alternative
      [(ConsoleKey.Z, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleRedoAsync,
      [(ConsoleKey.R, ConsoleModifiers.Alt)] = reader.HandleRevertLineAsync,

      // === Word Selection (Emacs: uses Shift+movement) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Shift)] = reader.HandleSelectBackwardCharAsync,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Shift)] = reader.HandleSelectForwardCharAsync,
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleSelectBackwardWordAsync,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleSelectNextWordAsync,
      [(ConsoleKey.B, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = reader.HandleSelectBackwardWordAsync,
      [(ConsoleKey.F, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = reader.HandleSelectNextWordAsync,
      [(ConsoleKey.Home, ConsoleModifiers.Shift)] = reader.HandleSelectBackwardsLineAsync,
      [(ConsoleKey.End, ConsoleModifiers.Shift)] = reader.HandleSelectLineAsync,
      [(ConsoleKey.A, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleSelectAllAsync,

      // === Clipboard Operations ===
      [(ConsoleKey.C, ConsoleModifiers.Control)] = reader.HandleCopyOrCancelLineAsync,
      [(ConsoleKey.X, ConsoleModifiers.Control)] = reader.HandleCutAsync,
      [(ConsoleKey.V, ConsoleModifiers.Control)] = reader.HandlePasteAsync,

      // === Screen Operations (Emacs: clear-screen) ===
      [(ConsoleKey.L, ConsoleModifiers.Control)] = reader.HandleClearScreenAsync,

      // === Insert Mode Toggle ===
      [(ConsoleKey.Insert, ConsoleModifiers.None)] = reader.HandleToggleInsertModeAsync,

      // === Alternative AcceptLine bindings (Emacs: accept-line) ===
      [(ConsoleKey.M, ConsoleModifiers.Control)] = reader.HandleEnterAsync,  // Ctrl+M = carriage return
      [(ConsoleKey.J, ConsoleModifiers.Control)] = reader.HandleEnterAsync,  // Ctrl+J = newline

      // === Special Keys ===
      [(ConsoleKey.Escape, ConsoleModifiers.None)] = reader.HandleEscapeAsync,
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
