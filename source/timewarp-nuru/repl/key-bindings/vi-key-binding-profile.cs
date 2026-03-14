namespace TimeWarp.Nuru;

/// <summary>
/// Vi-inspired key binding profile (insert mode bindings).
/// </summary>
/// <remarks>
/// <para>
/// This profile provides Vi-inspired keybindings, primarily staying in insert mode
/// for REPL interaction. It combines some Vi conventions with practical defaults
/// for command-line editing.
/// </para>
/// <para>
/// Note: This is NOT a full Vi modal editor implementation. It provides Vi-style
/// navigation using common Vi keys but does not implement normal/visual mode switching.
/// Full modal Vi support may be added in a future enhancement.
/// </para>
/// <para><strong>Key Bindings:</strong></para>
/// <list type="table">
/// <listheader>
///   <term>Key</term>
///   <description>Action</description>
/// </listheader>
/// <item>
///   <term>Ctrl+[</term>
///   <description>Clear line (Vi-inspired Escape alternative)</description>
/// </item>
/// <item>
///   <term>Escape</term>
///   <description>Clear line (future: switch to normal mode)</description>
/// </item>
/// <item>
///   <term>Ctrl+A</term>
///   <description>Move to beginning of line</description>
/// </item>
/// <item>
///   <term>Ctrl+E</term>
///   <description>Move to end of line</description>
/// </item>
/// <item>
///   <term>Ctrl+B</term>
///   <description>Move backward one character</description>
/// </item>
/// <item>
///   <term>Ctrl+F</term>
///   <description>Move forward one character</description>
/// </item>
/// <item>
///   <term>Ctrl+W</term>
///   <description>Delete word backward (Vi-style)</description>
/// </item>
/// <item>
///   <term>Ctrl+U</term>
///   <description>Delete to beginning of line (Vi-style)</description>
/// </item>
/// <item>
///   <term>Ctrl+K</term>
///   <description>Delete to end of line (Vi-style)</description>
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
///   <term>↑ / ↓</term>
///   <description>Navigate history (practical addition)</description>
/// </item>
/// <item>
///   <term>Tab</term>
///   <description>Complete current token</description>
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
/// Future enhancements could include full modal editing where Escape switches to
/// normal mode with bindings like: h (left), l (right), w (forward-word), b (backward-word),
/// 0 (line-start), $ (line-end), k (previous-history), j (next-history), i (insert-mode).
/// </para>
/// </remarks>
public sealed class ViKeyBindingProfile : IKeyBindingProfile
{
  /// <inheritdoc/>
  public string Name => "Vi";

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

      // === Character Movement ===
      [(ConsoleKey.B, ConsoleModifiers.Control)] = reader.HandleBackwardCharAsync,
      [(ConsoleKey.F, ConsoleModifiers.Control)] = reader.HandleForwardCharAsync,
      [(ConsoleKey.LeftArrow, ConsoleModifiers.None)] = reader.HandleBackwardCharAsync,
      [(ConsoleKey.RightArrow, ConsoleModifiers.None)] = reader.HandleForwardCharAsync,

      // === Word Movement ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Control)] = reader.HandleBackwardWordAsync,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Control)] = reader.HandleForwardWordAsync,

      // === Line Position ===
      [(ConsoleKey.A, ConsoleModifiers.Control)] = reader.HandleBeginningOfLineAsync,
      [(ConsoleKey.E, ConsoleModifiers.Control)] = reader.HandleEndOfLineAsync,
      [(ConsoleKey.Home, ConsoleModifiers.None)] = reader.HandleBeginningOfLineAsync,
      [(ConsoleKey.End, ConsoleModifiers.None)] = reader.HandleEndOfLineAsync,

      // === History Navigation ===
      [(ConsoleKey.P, ConsoleModifiers.Control)] = reader.HandlePreviousHistoryAsync,
      [(ConsoleKey.N, ConsoleModifiers.Control)] = reader.HandleNextHistoryAsync,
      [(ConsoleKey.UpArrow, ConsoleModifiers.None)] = reader.HandlePreviousHistoryAsync,
      [(ConsoleKey.DownArrow, ConsoleModifiers.None)] = reader.HandleNextHistoryAsync,

      // === Interactive History Search ===
      [(ConsoleKey.R, ConsoleModifiers.Control)] = reader.HandleReverseSearchHistoryAsync,
      [(ConsoleKey.S, ConsoleModifiers.Control)] = reader.HandleForwardSearchHistoryAsync,

      // === Deletion ===
      [(ConsoleKey.Backspace, ConsoleModifiers.None)] = reader.HandleBackwardDeleteCharAsync,
      [(ConsoleKey.Delete, ConsoleModifiers.None)] = reader.HandleDeleteCharAsync,
      [(ConsoleKey.D, ConsoleModifiers.Control)] = reader.HandleDeleteCharOrExitAsync,  // Delete char or EOF if empty

      // === Vi-style Kill Operations ===
      [(ConsoleKey.W, ConsoleModifiers.Control)] = reader.HandleUnixWordRuboutAsync,
      [(ConsoleKey.U, ConsoleModifiers.Control)] = reader.HandleBackwardKillInputAsync,
      [(ConsoleKey.K, ConsoleModifiers.Control)] = reader.HandleKillLineAsync,
      [(ConsoleKey.D, ConsoleModifiers.Alt)] = reader.HandleKillWordAsync,
      [(ConsoleKey.Backspace, ConsoleModifiers.Alt)] = reader.HandleBackwardKillWordAsync,

      // === Yank Operations ===
      [(ConsoleKey.Y, ConsoleModifiers.Control)] = reader.HandleYankAsync,
      [(ConsoleKey.Y, ConsoleModifiers.Alt)] = reader.HandleYankPopAsync,

      // === Yank Argument Operations (PSReadLine: YankLastArg, YankNthArg) ===
      [(ConsoleKey.OemPeriod, ConsoleModifiers.Alt)] = reader.HandleYankLastArgAsync,  // Alt+.
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

      // === Undo/Redo Operations ===
      [(ConsoleKey.Z, ConsoleModifiers.Control)] = reader.HandleUndoAsync,
      [(ConsoleKey.Z, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleRedoAsync,

      // === Selection (Shift+Arrow) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Shift)] = reader.HandleSelectBackwardCharAsync,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Shift)] = reader.HandleSelectForwardCharAsync,
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleSelectBackwardWordAsync,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleSelectNextWordAsync,
      [(ConsoleKey.Home, ConsoleModifiers.Shift)] = reader.HandleSelectBackwardsLineAsync,
      [(ConsoleKey.End, ConsoleModifiers.Shift)] = reader.HandleSelectLineAsync,

      // === Clipboard Operations ===
      [(ConsoleKey.C, ConsoleModifiers.Control)] = reader.HandleCopyOrCancelLineAsync,
      [(ConsoleKey.X, ConsoleModifiers.Control)] = reader.HandleCutAsync,
      [(ConsoleKey.V, ConsoleModifiers.Control)] = reader.HandlePasteAsync,

      // === Screen Operations ===
      [(ConsoleKey.L, ConsoleModifiers.Control)] = reader.HandleClearScreenAsync,

      // === Clear/Escape ===
      [(ConsoleKey.Escape, ConsoleModifiers.None)] = reader.HandleEscapeAsync,
      // Note: Ctrl+[ is Escape in ASCII, but ConsoleKey doesn't distinguish this easily
    };
  }

  /// <inheritdoc/>
  public HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> GetExitKeys() =>
  [
    (ConsoleKey.Enter, ConsoleModifiers.None),  // Submit command
    // Note: Ctrl+D now handled by HandleDeleteCharOrExit which signals exit via ShouldExitRepl
  ];
}
