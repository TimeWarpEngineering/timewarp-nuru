namespace TimeWarp.Nuru;

/// <summary>
/// VSCode-style key binding profile using modern IDE conventions.
/// </summary>
/// <remarks>
/// <para>
/// This profile implements key bindings familiar to Visual Studio Code users,
/// emphasizing arrow keys with modifiers and standard Windows/modern editor conventions.
/// </para>
/// <para><strong>Key Bindings:</strong></para>
/// <list type="table">
/// <listheader>
///   <term>Key</term>
///   <description>Action</description>
/// </listheader>
/// <item>
///   <term>←</term>
///   <description>Move backward one character</description>
/// </item>
/// <item>
///   <term>→</term>
///   <description>Move forward one character</description>
/// </item>
/// <item>
///   <term>Ctrl+←</term>
///   <description>Move backward one word</description>
/// </item>
/// <item>
///   <term>Ctrl+→</term>
///   <description>Move forward one word</description>
/// </item>
/// <item>
///   <term>Home</term>
///   <description>Move to beginning of line</description>
/// </item>
/// <item>
///   <term>End</term>
///   <description>Move to end of line</description>
/// </item>
/// <item>
///   <term>Ctrl+Home</term>
///   <description>Move to beginning of history</description>
/// </item>
/// <item>
///   <term>Ctrl+End</term>
///   <description>Move to end of history</description>
/// </item>
/// <item>
///   <term>↑</term>
///   <description>Previous history entry</description>
/// </item>
/// <item>
///   <term>↓</term>
///   <description>Next history entry</description>
/// </item>
/// <item>
///   <term>Ctrl+K</term>
///   <description>Delete to end of line</description>
/// </item>
/// <item>
///   <term>Ctrl+Backspace</term>
///   <description>Delete word backward</description>
/// </item>
/// <item>
///   <term>Backspace</term>
///   <description>Delete character before cursor</description>
/// </item>
/// <item>
///   <term>Delete</term>
///   <description>Delete character under cursor</description>
/// </item>
/// <item>
///   <term>Tab</term>
///   <description>Complete current token</description>
/// </item>
/// <item>
///   <term>Shift+Tab</term>
///   <description>Reverse completion (cycle backward)</description>
/// </item>
/// <item>
///   <term>Escape</term>
///   <description>Clear line</description>
/// </item>
/// <item>
///   <term>Enter</term>
///   <description>Submit command</description>
/// </item>
/// </list>
/// <para>
/// Note: Some advanced VSCode features like Ctrl+K (delete-to-end), Ctrl+Backspace (delete-word-backward)
/// require additional handler methods and are noted as future enhancements.
/// </para>
/// </remarks>
public sealed class VSCodeKeyBindingProfile : IKeyBindingProfile
{
  /// <inheritdoc/>
  public string Name => "VSCode";

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

      // === Character Movement (Arrow Keys) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.None)] = reader.HandleBackwardChar,
      [(ConsoleKey.RightArrow, ConsoleModifiers.None)] = reader.HandleForwardChar,

      // === Word Movement (Ctrl+Arrow) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Control)] = reader.HandleBackwardWord,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Control)] = reader.HandleForwardWord,

      // === Line Position (Home/End) ===
      [(ConsoleKey.Home, ConsoleModifiers.None)] = reader.HandleBeginningOfLine,
      [(ConsoleKey.End, ConsoleModifiers.None)] = reader.HandleEndOfLine,

      // === History Position (Ctrl+Home/End) ===
      [(ConsoleKey.Home, ConsoleModifiers.Control)] = reader.HandleBeginningOfHistory,
      [(ConsoleKey.End, ConsoleModifiers.Control)] = reader.HandleEndOfHistory,

      // === History Navigation (Arrow Keys) ===
      [(ConsoleKey.UpArrow, ConsoleModifiers.None)] = reader.HandlePreviousHistory,
      [(ConsoleKey.DownArrow, ConsoleModifiers.None)] = reader.HandleNextHistory,

      // === Interactive History Search ===
      [(ConsoleKey.R, ConsoleModifiers.Control)] = reader.HandleReverseSearchHistory,
      [(ConsoleKey.S, ConsoleModifiers.Control)] = reader.HandleForwardSearchHistory,

      // === Deletion ===
      [(ConsoleKey.Backspace, ConsoleModifiers.None)] = reader.HandleBackwardDeleteChar,
      [(ConsoleKey.Delete, ConsoleModifiers.None)] = reader.HandleDeleteChar,

      // === Kill Operations ===
      [(ConsoleKey.K, ConsoleModifiers.Control)] = reader.HandleKillLine,
      [(ConsoleKey.Backspace, ConsoleModifiers.Control)] = reader.HandleBackwardKillWord,
      [(ConsoleKey.U, ConsoleModifiers.Control)] = reader.HandleBackwardKillInput,

      // === Yank Operations (Cut/Paste from kill ring) ===
      [(ConsoleKey.Y, ConsoleModifiers.Control)] = reader.HandleYank,
      [(ConsoleKey.Y, ConsoleModifiers.Alt)] = reader.HandleYankPop,

      // === Yank Argument Operations (PSReadLine: YankLastArg, YankNthArg) ===
      [(ConsoleKey.OemPeriod, ConsoleModifiers.Alt)] = reader.HandleYankLastArg,  // Alt+.
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

      // === Undo/Redo Operations (VSCode standard: Ctrl+Z, Ctrl+Shift+Z) ===
      [(ConsoleKey.Z, ConsoleModifiers.Control)] = reader.HandleUndo,
      [(ConsoleKey.Z, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleRedo,

      // === Character Selection (Shift+Arrow) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Shift)] = reader.HandleSelectBackwardChar,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Shift)] = reader.HandleSelectForwardChar,

      // === Word Selection (Ctrl+Shift+Arrow) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleSelectBackwardWord,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleSelectNextWord,

      // === Line Selection (Shift+Home/End) ===
      [(ConsoleKey.Home, ConsoleModifiers.Shift)] = reader.HandleSelectBackwardsLine,
      [(ConsoleKey.End, ConsoleModifiers.Shift)] = reader.HandleSelectLine,
      [(ConsoleKey.A, ConsoleModifiers.Control)] = reader.HandleSelectAll,  // VSCode uses Ctrl+A for Select All

      // === Clipboard Operations (VSCode standard: Ctrl+C, Ctrl+X, Ctrl+V) ===
      [(ConsoleKey.C, ConsoleModifiers.Control)] = reader.HandleCopyOrCancelLine,
      [(ConsoleKey.X, ConsoleModifiers.Control)] = reader.HandleCut,
      [(ConsoleKey.V, ConsoleModifiers.Control)] = reader.HandlePaste,

      // === Screen Operations ===
      [(ConsoleKey.L, ConsoleModifiers.Control)] = reader.HandleClearScreen,

      // === Insert Mode Toggle ===
      [(ConsoleKey.Insert, ConsoleModifiers.None)] = reader.HandleToggleInsertMode,

      // === Special Keys ===
      [(ConsoleKey.Escape, ConsoleModifiers.None)] = reader.HandleEscape,
    };
  }

  /// <inheritdoc/>
  public HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> GetExitKeys() =>
  [
    (ConsoleKey.Enter, ConsoleModifiers.None)  // Submit command
    // Note: VSCode doesn't typically use Ctrl+D for EOF in terminal,
    // so it's omitted from this profile
  ];
}
