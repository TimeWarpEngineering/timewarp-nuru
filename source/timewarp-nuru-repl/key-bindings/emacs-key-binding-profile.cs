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

      // === Deletion (Emacs: backward-delete-char, delete-char) ===
      [(ConsoleKey.Backspace, ConsoleModifiers.None)] = reader.HandleBackwardDeleteChar,
      [(ConsoleKey.Delete, ConsoleModifiers.None)] = reader.HandleDeleteChar,
      [(ConsoleKey.D, ConsoleModifiers.Control)] = reader.HandleDeleteChar, // Also EOF when handled by ExitKeys

      // === Kill Operations (Emacs: kill-line, backward-kill-line) ===
      [(ConsoleKey.K, ConsoleModifiers.Control)] = reader.HandleKillLine,
      [(ConsoleKey.U, ConsoleModifiers.Control)] = reader.HandleDeleteToLineStart,

      // === Special Keys ===
      [(ConsoleKey.Escape, ConsoleModifiers.None)] = reader.HandleEscape,
    };
  }

  /// <inheritdoc/>
  public HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> GetExitKeys() =>
  [
    (ConsoleKey.Enter, ConsoleModifiers.None),  // Submit command
    (ConsoleKey.D, ConsoleModifiers.Control)    // EOF
  ];
}
