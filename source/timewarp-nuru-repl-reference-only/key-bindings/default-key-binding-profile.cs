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
      [(ConsoleKey.Enter, ConsoleModifiers.Shift)] = reader.HandleAddLine,  // Shift+Enter adds new line without executing

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

      // === Kill Operations (PSReadLine: KillLine, BackwardKillInput, UnixWordRubout, KillWord, BackwardKillWord) ===
      [(ConsoleKey.K, ConsoleModifiers.Control)] = reader.HandleKillLine,
      [(ConsoleKey.U, ConsoleModifiers.Control)] = reader.HandleBackwardKillInput,
      [(ConsoleKey.W, ConsoleModifiers.Control)] = reader.HandleUnixWordRubout,
      [(ConsoleKey.D, ConsoleModifiers.Alt)] = reader.HandleKillWord,
      [(ConsoleKey.Backspace, ConsoleModifiers.Alt)] = reader.HandleBackwardKillWord,

      // === Yank Operations (PSReadLine: Yank, YankPop) ===
      [(ConsoleKey.Y, ConsoleModifiers.Control)] = reader.HandleYank,
      [(ConsoleKey.Y, ConsoleModifiers.Alt)] = reader.HandleYankPop,

      // === Yank Argument Operations (PSReadLine: YankLastArg, YankNthArg) ===
      [(ConsoleKey.OemPeriod, ConsoleModifiers.Alt)] = reader.HandleYankLastArg,  // Alt+.
      [(ConsoleKey.OemMinus, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = reader.HandleYankLastArg,  // Alt+_ (underscore)
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

      // === Word Operations (PSReadLine: UpcaseWord, DowncaseWord, CapitalizeWord) ===
      [(ConsoleKey.U, ConsoleModifiers.Alt)] = reader.HandleUpcaseWord,
      [(ConsoleKey.L, ConsoleModifiers.Alt)] = reader.HandleDowncaseWord,
      [(ConsoleKey.C, ConsoleModifiers.Alt)] = reader.HandleCapitalizeWord,

      // === Character Operations (PSReadLine: SwapCharacters) ===
      [(ConsoleKey.T, ConsoleModifiers.Control)] = reader.HandleSwapCharacters,

      // === Word Deletion (PSReadLine: BackwardDeleteWord) ===
      [(ConsoleKey.Backspace, ConsoleModifiers.Control)] = reader.HandleBackwardDeleteWord,

      // === Undo/Redo Operations (PSReadLine: Undo, Redo, RevertLine) ===
      [(ConsoleKey.Z, ConsoleModifiers.Control)] = reader.HandleUndo,
      [(ConsoleKey.Z, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleRedo,
      [(ConsoleKey.OemMinus, ConsoleModifiers.Control)] = reader.HandleUndo,  // Ctrl+_ (underscore)
      [(ConsoleKey.R, ConsoleModifiers.Alt)] = reader.HandleRevertLine,

      // === Character Selection (PSReadLine: SelectBackwardChar, SelectForwardChar) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Shift)] = reader.HandleSelectBackwardChar,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Shift)] = reader.HandleSelectForwardChar,

      // === Word Selection (PSReadLine: SelectBackwardWord, SelectNextWord) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleSelectBackwardWord,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleSelectNextWord,
      [(ConsoleKey.B, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = reader.HandleSelectBackwardWord,
      [(ConsoleKey.F, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = reader.HandleSelectNextWord,

      // === Line Selection (PSReadLine: SelectBackwardsLine, SelectLine, SelectAll) ===
      [(ConsoleKey.Home, ConsoleModifiers.Shift)] = reader.HandleSelectBackwardsLine,
      [(ConsoleKey.End, ConsoleModifiers.Shift)] = reader.HandleSelectLine,
      [(ConsoleKey.A, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleSelectAll,

      // === Clipboard Operations (PSReadLine: CopyOrCancelLine, Cut, Paste) ===
      [(ConsoleKey.C, ConsoleModifiers.Control)] = reader.HandleCopyOrCancelLine,
      [(ConsoleKey.X, ConsoleModifiers.Control)] = reader.HandleCut,
      [(ConsoleKey.V, ConsoleModifiers.Control)] = reader.HandlePaste,

      // === Basic Editing Enhancement (PSReadLine: DeleteCharOrExit, ClearScreen) ===
      [(ConsoleKey.D, ConsoleModifiers.Control)] = reader.HandleDeleteCharOrExit,
      [(ConsoleKey.L, ConsoleModifiers.Control)] = reader.HandleClearScreen,
      [(ConsoleKey.H, ConsoleModifiers.Control)] = reader.HandleBackwardDeleteChar,  // Alternative backspace
      [(ConsoleKey.Insert, ConsoleModifiers.None)] = reader.HandleToggleInsertMode,

      // === Alternative AcceptLine bindings ===
      [(ConsoleKey.M, ConsoleModifiers.Control)] = reader.HandleEnter,  // Ctrl+M = Enter
      [(ConsoleKey.J, ConsoleModifiers.Control)] = reader.HandleEnter,  // Ctrl+J = Newline/Enter

      // === Special Keys ===
      [(ConsoleKey.Escape, ConsoleModifiers.None)] = reader.HandleEscape,
    };
  }

  /// <inheritdoc/>
  public HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> GetExitKeys() =>
  [
    (ConsoleKey.Enter, ConsoleModifiers.None),   // Submit command
    (ConsoleKey.M, ConsoleModifiers.Control),    // Ctrl+M = Enter
    (ConsoleKey.J, ConsoleModifiers.Control),    // Ctrl+J = Newline/Enter
  ];
}
