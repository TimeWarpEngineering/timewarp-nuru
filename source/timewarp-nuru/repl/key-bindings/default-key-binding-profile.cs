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
  public Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Func<Task>> GetBindings(ReplConsoleReader reader)
  {
    ArgumentNullException.ThrowIfNull(reader);

    return new Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Func<Task>>
    {
      // === Enter/Submit ===
      [(ConsoleKey.Enter, ConsoleModifiers.None)] = reader.HandleEnterAsync,
      [(ConsoleKey.Enter, ConsoleModifiers.Shift)] = reader.HandleAddLineAsync,  // Shift+Enter adds new line without executing

      // === Tab Completion ===
      [(ConsoleKey.Tab, ConsoleModifiers.None)] = () => reader.HandleTabCompletionAsync(reverse: false),
      [(ConsoleKey.Tab, ConsoleModifiers.Shift)] = () => reader.HandleTabCompletionAsync(reverse: true),
      [(ConsoleKey.Oem7, ConsoleModifiers.Alt)] = reader.HandlePossibleCompletionsAsync, // Alt+= (Oem7 is the = key)

      // === Character Movement (PSReadLine: BackwardChar, ForwardChar) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.None)] = reader.HandleBackwardCharAsync,
      [(ConsoleKey.B, ConsoleModifiers.Control)] = reader.HandleBackwardCharAsync,
      [(ConsoleKey.RightArrow, ConsoleModifiers.None)] = reader.HandleForwardCharAsync,
      [(ConsoleKey.F, ConsoleModifiers.Control)] = reader.HandleForwardCharAsync,

      // === Word Movement (PSReadLine: BackwardWord, ForwardWord) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Control)] = reader.HandleBackwardWordAsync,
      [(ConsoleKey.B, ConsoleModifiers.Alt)] = reader.HandleBackwardWordAsync,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Control)] = reader.HandleForwardWordAsync,
      [(ConsoleKey.F, ConsoleModifiers.Alt)] = reader.HandleForwardWordAsync,

      // === Line Position (PSReadLine: BeginningOfLine, EndOfLine) ===
      [(ConsoleKey.Home, ConsoleModifiers.None)] = reader.HandleBeginningOfLineAsync,
      [(ConsoleKey.A, ConsoleModifiers.Control)] = reader.HandleBeginningOfLineAsync,
      [(ConsoleKey.End, ConsoleModifiers.None)] = reader.HandleEndOfLineAsync,
      [(ConsoleKey.E, ConsoleModifiers.Control)] = reader.HandleEndOfLineAsync,

      // === History Navigation (PSReadLine: PreviousHistory, NextHistory) ===
      [(ConsoleKey.UpArrow, ConsoleModifiers.None)] = reader.HandlePreviousHistoryAsync,
      [(ConsoleKey.P, ConsoleModifiers.Control)] = reader.HandlePreviousHistoryAsync,
      [(ConsoleKey.DownArrow, ConsoleModifiers.None)] = reader.HandleNextHistoryAsync,
      [(ConsoleKey.N, ConsoleModifiers.Control)] = reader.HandleNextHistoryAsync,

      // === History Position (PSReadLine: BeginningOfHistory, EndOfHistory) ===
      [(ConsoleKey.OemComma, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = reader.HandleBeginningOfHistoryAsync, // Alt+<
      [(ConsoleKey.OemPeriod, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = reader.HandleEndOfHistoryAsync, // Alt+>

      // === History Prefix Search (PSReadLine: HistorySearchBackward, HistorySearchForward) ===
      [(ConsoleKey.F8, ConsoleModifiers.None)] = reader.HandleHistorySearchBackwardAsync,
      [(ConsoleKey.F8, ConsoleModifiers.Shift)] = reader.HandleHistorySearchForwardAsync,

      // === Interactive History Search (PSReadLine: ReverseSearchHistory, ForwardSearchHistory) ===
      [(ConsoleKey.R, ConsoleModifiers.Control)] = reader.HandleReverseSearchHistoryAsync,
      [(ConsoleKey.S, ConsoleModifiers.Control)] = reader.HandleForwardSearchHistoryAsync,

      // === Deletion (PSReadLine: BackwardDeleteChar, DeleteChar) ===
      [(ConsoleKey.Backspace, ConsoleModifiers.None)] = reader.HandleBackwardDeleteCharAsync,
      [(ConsoleKey.Delete, ConsoleModifiers.None)] = reader.HandleDeleteCharAsync,

      // === Kill Operations (PSReadLine: KillLine, BackwardKillInput, UnixWordRubout, KillWord, BackwardKillWord) ===
      [(ConsoleKey.K, ConsoleModifiers.Control)] = reader.HandleKillLineAsync,
      [(ConsoleKey.U, ConsoleModifiers.Control)] = reader.HandleBackwardKillInputAsync,
      [(ConsoleKey.W, ConsoleModifiers.Control)] = reader.HandleUnixWordRuboutAsync,
      [(ConsoleKey.D, ConsoleModifiers.Alt)] = reader.HandleKillWordAsync,
      [(ConsoleKey.Backspace, ConsoleModifiers.Alt)] = reader.HandleBackwardKillWordAsync,

      // === Yank Operations (PSReadLine: Yank, YankPop) ===
      [(ConsoleKey.Y, ConsoleModifiers.Control)] = reader.HandleYankAsync,
      [(ConsoleKey.Y, ConsoleModifiers.Alt)] = reader.HandleYankPopAsync,

      // === Yank Argument Operations (PSReadLine: YankLastArg, YankNthArg) ===
      [(ConsoleKey.OemPeriod, ConsoleModifiers.Alt)] = reader.HandleYankLastArgAsync,  // Alt+.
      [(ConsoleKey.OemMinus, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = reader.HandleYankLastArgAsync,  // Alt+_ (underscore)
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

      // === Word Operations (PSReadLine: UpcaseWord, DowncaseWord, CapitalizeWord) ===
      [(ConsoleKey.U, ConsoleModifiers.Alt)] = reader.HandleUpcaseWordAsync,
      [(ConsoleKey.L, ConsoleModifiers.Alt)] = reader.HandleDowncaseWordAsync,
      [(ConsoleKey.C, ConsoleModifiers.Alt)] = reader.HandleCapitalizeWordAsync,

      // === Character Operations (PSReadLine: SwapCharacters) ===
      [(ConsoleKey.T, ConsoleModifiers.Control)] = reader.HandleSwapCharactersAsync,

      // === Word Deletion (PSReadLine: BackwardDeleteWord) ===
      [(ConsoleKey.Backspace, ConsoleModifiers.Control)] = reader.HandleBackwardDeleteWordAsync,

      // === Undo/Redo Operations (PSReadLine: Undo, Redo, RevertLine) ===
      [(ConsoleKey.Z, ConsoleModifiers.Control)] = reader.HandleUndoAsync,
      [(ConsoleKey.Z, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleRedoAsync,
      [(ConsoleKey.OemMinus, ConsoleModifiers.Control)] = reader.HandleUndoAsync,  // Ctrl+_ (underscore)
      [(ConsoleKey.R, ConsoleModifiers.Alt)] = reader.HandleRevertLineAsync,

      // === Character Selection (PSReadLine: SelectBackwardChar, SelectForwardChar) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Shift)] = reader.HandleSelectBackwardCharAsync,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Shift)] = reader.HandleSelectForwardCharAsync,

      // === Word Selection (PSReadLine: SelectBackwardWord, SelectNextWord) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleSelectBackwardWordAsync,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleSelectNextWordAsync,
      [(ConsoleKey.B, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = reader.HandleSelectBackwardWordAsync,
      [(ConsoleKey.F, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = reader.HandleSelectNextWordAsync,

      // === Line Selection (PSReadLine: SelectBackwardsLine, SelectLine, SelectAll) ===
      [(ConsoleKey.Home, ConsoleModifiers.Shift)] = reader.HandleSelectBackwardsLineAsync,
      [(ConsoleKey.End, ConsoleModifiers.Shift)] = reader.HandleSelectLineAsync,
      [(ConsoleKey.A, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleSelectAllAsync,

      // === Clipboard Operations (PSReadLine: CopyOrCancelLine, Cut, Paste) ===
      [(ConsoleKey.C, ConsoleModifiers.Control)] = reader.HandleCopyOrCancelLineAsync,
      [(ConsoleKey.X, ConsoleModifiers.Control)] = reader.HandleCutAsync,
      [(ConsoleKey.V, ConsoleModifiers.Control)] = reader.HandlePasteAsync,

      // === Basic Editing Enhancement (PSReadLine: DeleteCharOrExit, ClearScreen) ===
      [(ConsoleKey.D, ConsoleModifiers.Control)] = reader.HandleDeleteCharOrExitAsync,
      [(ConsoleKey.L, ConsoleModifiers.Control)] = reader.HandleClearScreenAsync,
      [(ConsoleKey.H, ConsoleModifiers.Control)] = reader.HandleBackwardDeleteCharAsync,  // Alternative backspace
      [(ConsoleKey.Insert, ConsoleModifiers.None)] = reader.HandleToggleInsertModeAsync,

      // === Alternative AcceptLine bindings ===
      [(ConsoleKey.M, ConsoleModifiers.Control)] = reader.HandleEnterAsync,  // Ctrl+M = Enter
      [(ConsoleKey.J, ConsoleModifiers.Control)] = reader.HandleEnterAsync,  // Ctrl+J = Newline/Enter

      // === Special Keys ===
      [(ConsoleKey.Escape, ConsoleModifiers.None)] = reader.HandleEscapeAsync,
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
