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

      // === Character Movement ===
      [(ConsoleKey.B, ConsoleModifiers.Control)] = reader.HandleBackwardChar,
      [(ConsoleKey.F, ConsoleModifiers.Control)] = reader.HandleForwardChar,
      [(ConsoleKey.LeftArrow, ConsoleModifiers.None)] = reader.HandleBackwardChar,
      [(ConsoleKey.RightArrow, ConsoleModifiers.None)] = reader.HandleForwardChar,

      // === Word Movement ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Control)] = reader.HandleBackwardWord,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Control)] = reader.HandleForwardWord,

      // === Line Position ===
      [(ConsoleKey.A, ConsoleModifiers.Control)] = reader.HandleBeginningOfLine,
      [(ConsoleKey.E, ConsoleModifiers.Control)] = reader.HandleEndOfLine,
      [(ConsoleKey.Home, ConsoleModifiers.None)] = reader.HandleBeginningOfLine,
      [(ConsoleKey.End, ConsoleModifiers.None)] = reader.HandleEndOfLine,

      // === History Navigation ===
      [(ConsoleKey.P, ConsoleModifiers.Control)] = reader.HandlePreviousHistory,
      [(ConsoleKey.N, ConsoleModifiers.Control)] = reader.HandleNextHistory,
      [(ConsoleKey.UpArrow, ConsoleModifiers.None)] = reader.HandlePreviousHistory,
      [(ConsoleKey.DownArrow, ConsoleModifiers.None)] = reader.HandleNextHistory,

      // === Interactive History Search ===
      [(ConsoleKey.R, ConsoleModifiers.Control)] = reader.HandleReverseSearchHistory,
      [(ConsoleKey.S, ConsoleModifiers.Control)] = reader.HandleForwardSearchHistory,

      // === Deletion ===
      [(ConsoleKey.Backspace, ConsoleModifiers.None)] = reader.HandleBackwardDeleteChar,
      [(ConsoleKey.Delete, ConsoleModifiers.None)] = reader.HandleDeleteChar,
      [(ConsoleKey.D, ConsoleModifiers.Control)] = reader.HandleDeleteChar, // Also EOF when handled by ExitKeys

      // === Vi-style Kill Operations ===
      [(ConsoleKey.W, ConsoleModifiers.Control)] = reader.HandleUnixWordRubout,
      [(ConsoleKey.U, ConsoleModifiers.Control)] = reader.HandleBackwardKillInput,
      [(ConsoleKey.K, ConsoleModifiers.Control)] = reader.HandleKillLine,
      [(ConsoleKey.D, ConsoleModifiers.Alt)] = reader.HandleKillWord,
      [(ConsoleKey.Backspace, ConsoleModifiers.Alt)] = reader.HandleBackwardKillWord,

      // === Yank Operations ===
      [(ConsoleKey.Y, ConsoleModifiers.Control)] = reader.HandleYank,
      [(ConsoleKey.Y, ConsoleModifiers.Alt)] = reader.HandleYankPop,

      // === Undo/Redo Operations ===
      [(ConsoleKey.Z, ConsoleModifiers.Control)] = reader.HandleUndo,
      [(ConsoleKey.Z, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleRedo,

      // === Selection (Shift+Arrow) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Shift)] = reader.HandleSelectBackwardChar,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Shift)] = reader.HandleSelectForwardChar,
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleSelectBackwardWord,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleSelectNextWord,
      [(ConsoleKey.Home, ConsoleModifiers.Shift)] = reader.HandleSelectBackwardsLine,
      [(ConsoleKey.End, ConsoleModifiers.Shift)] = reader.HandleSelectLine,

      // === Clipboard Operations ===
      [(ConsoleKey.C, ConsoleModifiers.Control)] = reader.HandleCopyOrCancelLine,
      [(ConsoleKey.X, ConsoleModifiers.Control)] = reader.HandleCut,
      [(ConsoleKey.V, ConsoleModifiers.Control)] = reader.HandlePaste,

      // === Clear/Escape ===
      [(ConsoleKey.Escape, ConsoleModifiers.None)] = reader.HandleEscape,
      // Note: Ctrl+[ is Escape in ASCII, but ConsoleKey doesn't distinguish this easily
    };
  }

  /// <inheritdoc/>
  public HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> GetExitKeys() =>
  [
    (ConsoleKey.Enter, ConsoleModifiers.None),  // Submit command
    (ConsoleKey.D, ConsoleModifiers.Control)    // EOF (Vi-style Ctrl+D)
  ];
}
