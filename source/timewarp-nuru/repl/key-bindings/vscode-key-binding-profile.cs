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

      // === Character Movement (Arrow Keys) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.None)] = reader.HandleBackwardCharAsync,
      [(ConsoleKey.RightArrow, ConsoleModifiers.None)] = reader.HandleForwardCharAsync,

      // === Word Movement (Ctrl+Arrow) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Control)] = reader.HandleBackwardWordAsync,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Control)] = reader.HandleForwardWordAsync,

      // === Line Position (Home/End) ===
      [(ConsoleKey.Home, ConsoleModifiers.None)] = reader.HandleBeginningOfLineAsync,
      [(ConsoleKey.End, ConsoleModifiers.None)] = reader.HandleEndOfLineAsync,

      // === History Position (Ctrl+Home/End) ===
      [(ConsoleKey.Home, ConsoleModifiers.Control)] = reader.HandleBeginningOfHistoryAsync,
      [(ConsoleKey.End, ConsoleModifiers.Control)] = reader.HandleEndOfHistoryAsync,

      // === History Navigation (Arrow Keys) ===
      [(ConsoleKey.UpArrow, ConsoleModifiers.None)] = reader.HandlePreviousHistoryAsync,
      [(ConsoleKey.DownArrow, ConsoleModifiers.None)] = reader.HandleNextHistoryAsync,

      // === Interactive History Search ===
      [(ConsoleKey.R, ConsoleModifiers.Control)] = reader.HandleReverseSearchHistoryAsync,
      [(ConsoleKey.S, ConsoleModifiers.Control)] = reader.HandleForwardSearchHistoryAsync,

      // === Deletion ===
      [(ConsoleKey.Backspace, ConsoleModifiers.None)] = reader.HandleBackwardDeleteCharAsync,
      [(ConsoleKey.Delete, ConsoleModifiers.None)] = reader.HandleDeleteCharAsync,

      // === Kill Operations ===
      [(ConsoleKey.K, ConsoleModifiers.Control)] = reader.HandleKillLineAsync,
      [(ConsoleKey.Backspace, ConsoleModifiers.Control)] = reader.HandleBackwardKillWordAsync,
      [(ConsoleKey.U, ConsoleModifiers.Control)] = reader.HandleBackwardKillInputAsync,

      // === Yank Operations (Cut/Paste from kill ring) ===
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

      // === Undo/Redo Operations (VSCode standard: Ctrl+Z, Ctrl+Shift+Z) ===
      [(ConsoleKey.Z, ConsoleModifiers.Control)] = reader.HandleUndoAsync,
      [(ConsoleKey.Z, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleRedoAsync,

      // === Character Selection (Shift+Arrow) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Shift)] = reader.HandleSelectBackwardCharAsync,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Shift)] = reader.HandleSelectForwardCharAsync,

      // === Word Selection (Ctrl+Shift+Arrow) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleSelectBackwardWordAsync,
      [(ConsoleKey.RightArrow, ConsoleModifiers.Control | ConsoleModifiers.Shift)] = reader.HandleSelectNextWordAsync,

      // === Line Selection (Shift+Home/End) ===
      [(ConsoleKey.Home, ConsoleModifiers.Shift)] = reader.HandleSelectBackwardsLineAsync,
      [(ConsoleKey.End, ConsoleModifiers.Shift)] = reader.HandleSelectLineAsync,
      [(ConsoleKey.A, ConsoleModifiers.Control)] = reader.HandleSelectAllAsync,  // VSCode uses Ctrl+A for Select All

      // === Clipboard Operations (VSCode standard: Ctrl+C, Ctrl+X, Ctrl+V) ===
      [(ConsoleKey.C, ConsoleModifiers.Control)] = reader.HandleCopyOrCancelLineAsync,
      [(ConsoleKey.X, ConsoleModifiers.Control)] = reader.HandleCutAsync,
      [(ConsoleKey.V, ConsoleModifiers.Control)] = reader.HandlePasteAsync,

      // === Screen Operations ===
      [(ConsoleKey.L, ConsoleModifiers.Control)] = reader.HandleClearScreenAsync,

      // === Insert Mode Toggle ===
      [(ConsoleKey.Insert, ConsoleModifiers.None)] = reader.HandleToggleInsertModeAsync,

      // === Special Keys ===
      [(ConsoleKey.Escape, ConsoleModifiers.None)] = reader.HandleEscapeAsync,
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
