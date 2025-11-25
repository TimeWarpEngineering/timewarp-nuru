namespace TimeWarp.Nuru.Repl.Input;

/// <summary>
/// Provides advanced console input handling for REPL mode with tab completion and history navigation.
/// </summary>
public sealed class ReplConsoleReader
{
  private readonly List<string> History;
  private readonly EndpointCollection Endpoints;
  private readonly ReplOptions ReplOptions;
  private readonly SyntaxHighlighter SyntaxHighlighter;
  private readonly ILogger<ReplConsoleReader> Logger;
  private readonly ILoggerFactory? LoggerFactory;
  private readonly ITerminal Terminal;
  private readonly TabCompletionHandler CompletionHandler;
  private readonly Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Func<bool>> KeyBindings;
  private string UserInput = string.Empty;
  private int CursorPosition;
  private int HistoryIndex = -1;
  private string? PrefixSearchString;  // Stores the prefix for F8 prefix search

  /// <summary>
  /// Creates a new REPL console reader.
  /// </summary>
  /// <param name="history">The command history list.</param>
  /// <param name="completionProvider">The completion provider for tab completion.</param>
  /// <param name="endpoints">The endpoint collection for completion.</param>
  /// <param name="replOptions">The REPL configuration options.</param>
  /// <param name="loggerFactory">The logger factory for creating loggers.</param>
  /// <param name="terminal">The terminal I/O provider.</param>
  public ReplConsoleReader
  (
    IEnumerable<string> history,
    CompletionProvider completionProvider,
    EndpointCollection endpoints,
    ReplOptions replOptions,
    ILoggerFactory loggerFactory,
    ITerminal terminal
  )
  {
    ArgumentNullException.ThrowIfNull(history);
    ArgumentNullException.ThrowIfNull(completionProvider);
    ArgumentNullException.ThrowIfNull(endpoints);
    ArgumentNullException.ThrowIfNull(replOptions);
    ArgumentNullException.ThrowIfNull(loggerFactory);
    ArgumentNullException.ThrowIfNull(terminal);

    History = history?.ToList() ?? throw new ArgumentNullException(nameof(history));
    Endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
    ReplOptions = replOptions ?? throw new ArgumentNullException(nameof(replOptions));
    SyntaxHighlighter = new SyntaxHighlighter(endpoints, loggerFactory);
    LoggerFactory = loggerFactory;
    Logger = LoggerFactory?.CreateLogger<ReplConsoleReader>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    Terminal = terminal;
    CompletionHandler = new TabCompletionHandler(completionProvider, endpoints, terminal, replOptions, loggerFactory);
    KeyBindings = InitializeKeyBindings();
  }

  /// <summary>
  /// Initializes the default PSReadLine-compatible keybindings.
  /// </summary>
  /// <returns>Dictionary mapping key combinations to handler functions.</returns>
  private Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Func<bool>> InitializeKeyBindings()
  {
    // Func<bool> returns true if the key was handled and should continue the loop,
    // false if it should return (e.g., Enter, Ctrl+D)
    return new Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Func<bool>>
    {
      // === Enter/Submit ===
      [(ConsoleKey.Enter, ConsoleModifiers.None)] = () => { HandleEnter(); return false; },

      // === Tab Completion ===
      [(ConsoleKey.Tab, ConsoleModifiers.None)] = () => { HandleTabCompletion(reverse: false); return true; },
      [(ConsoleKey.Tab, ConsoleModifiers.Shift)] = () => { HandleTabCompletion(reverse: true); return true; },
      [(ConsoleKey.Oem7, ConsoleModifiers.Alt)] = () => { HandlePossibleCompletions(); return true; }, // Alt+= (Oem7 is the = key)

      // === Character Movement (PSReadLine: BackwardChar, ForwardChar) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.None)] = () => { HandleBackwardChar(); return true; },
      [(ConsoleKey.B, ConsoleModifiers.Control)] = () => { HandleBackwardChar(); return true; },
      [(ConsoleKey.RightArrow, ConsoleModifiers.None)] = () => { HandleForwardChar(); return true; },
      [(ConsoleKey.F, ConsoleModifiers.Control)] = () => { HandleForwardChar(); return true; },

      // === Word Movement (PSReadLine: BackwardWord, ForwardWord) ===
      [(ConsoleKey.LeftArrow, ConsoleModifiers.Control)] = () => { HandleBackwardWord(); return true; },
      [(ConsoleKey.B, ConsoleModifiers.Alt)] = () => { HandleBackwardWord(); return true; },
      [(ConsoleKey.RightArrow, ConsoleModifiers.Control)] = () => { HandleForwardWord(); return true; },
      [(ConsoleKey.F, ConsoleModifiers.Alt)] = () => { HandleForwardWord(); return true; },

      // === Line Position (PSReadLine: BeginningOfLine, EndOfLine) ===
      [(ConsoleKey.Home, ConsoleModifiers.None)] = () => { HandleBeginningOfLine(); return true; },
      [(ConsoleKey.A, ConsoleModifiers.Control)] = () => { HandleBeginningOfLine(); return true; },
      [(ConsoleKey.End, ConsoleModifiers.None)] = () => { HandleEndOfLine(); return true; },
      [(ConsoleKey.E, ConsoleModifiers.Control)] = () => { HandleEndOfLine(); return true; },

      // === History Navigation (PSReadLine: PreviousHistory, NextHistory) ===
      [(ConsoleKey.UpArrow, ConsoleModifiers.None)] = () => { HandlePreviousHistory(); return true; },
      [(ConsoleKey.P, ConsoleModifiers.Control)] = () => { HandlePreviousHistory(); return true; },
      [(ConsoleKey.DownArrow, ConsoleModifiers.None)] = () => { HandleNextHistory(); return true; },
      [(ConsoleKey.N, ConsoleModifiers.Control)] = () => { HandleNextHistory(); return true; },

      // === History Position (PSReadLine: BeginningOfHistory, EndOfHistory) ===
      [(ConsoleKey.OemComma, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = () => { HandleBeginningOfHistory(); return true; }, // Alt+<
      [(ConsoleKey.OemPeriod, ConsoleModifiers.Alt | ConsoleModifiers.Shift)] = () => { HandleEndOfHistory(); return true; }, // Alt+>

      // === History Prefix Search (PSReadLine: HistorySearchBackward, HistorySearchForward) ===
      [(ConsoleKey.F8, ConsoleModifiers.None)] = () => { HandleHistorySearchBackward(); return true; },
      [(ConsoleKey.F8, ConsoleModifiers.Shift)] = () => { HandleHistorySearchForward(); return true; },

      // === Deletion (PSReadLine: BackwardDeleteChar, DeleteChar) ===
      [(ConsoleKey.Backspace, ConsoleModifiers.None)] = () => { HandleBackwardDeleteChar(); return true; },
      [(ConsoleKey.Delete, ConsoleModifiers.None)] = () => { HandleDeleteChar(); return true; },

      // === Special Keys ===
      [(ConsoleKey.Escape, ConsoleModifiers.None)] = () => { HandleEscape(); return true; },
      [(ConsoleKey.D, ConsoleModifiers.Control)] = () => { Terminal.WriteLine(); return false; }, // EOF
    };
  }

  /// <summary>
  /// Reads a line of input with advanced editing capabilities.
  /// </summary>
  /// <param name="prompt">The prompt to display.</param>
  /// <returns>The input line from user, or null if EOF (Ctrl+D) is received.</returns>
  public string? ReadLine(string prompt)
  {
    ArgumentException.ThrowIfNullOrEmpty(prompt);

    ReplLoggerMessages.ReadLineStarted(Logger, prompt, History.Count, null);

    string formattedPrompt = PromptFormatter.Format(prompt, ReplOptions.EnableColors, ReplOptions.PromptColor);
    Terminal.Write(formattedPrompt);

    UserInput = string.Empty;  // Store only user input, not prompt
    CursorPosition = 0;        // Position relative to user input only
    HistoryIndex = History.Count;
    CompletionHandler.Reset();

    while (true)
    {
      ConsoleKeyInfo keyInfo = Terminal.ReadKey(true);

      ReplLoggerMessages.KeyPressed(Logger, keyInfo.Key.ToString(), CursorPosition, null);

      // Normalize modifiers to only include Ctrl, Alt, Shift (ignore other flags)
      ConsoleModifiers normalizedMods = keyInfo.Modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt | ConsoleModifiers.Shift);
      (ConsoleKey Key, ConsoleModifiers Modifiers) keyBinding = (keyInfo.Key, normalizedMods);

      if (KeyBindings.TryGetValue(keyBinding, out Func<bool>? handler))
      {
        bool continueLoop = handler();
        if (!continueLoop)
        {
          // Handler indicated we should return (Enter returns UserInput, Ctrl+D returns null)
          return keyInfo.Key == ConsoleKey.Enter ? UserInput : null;
        }
      }
      else if (!char.IsControl(keyInfo.KeyChar))
      {
        // No binding found, treat as character input
        HandleCharacter(keyInfo.KeyChar);
      }
    }
  }

  private void HandleEnter()
  {
    Terminal.WriteLine();

    // Add to history if not empty and not duplicate of last entry
    if (!string.IsNullOrWhiteSpace(UserInput))
    {
      if (History.Count == 0 || History[^1] != UserInput)
      {
        History.Add(UserInput);
      }
    }
  }

  private void HandleTabCompletion(bool reverse)
  {
    (UserInput, CursorPosition) = CompletionHandler.HandleTab(UserInput, CursorPosition, reverse);
    RedrawLine();
  }

  /// <summary>
  /// PSReadLine: PossibleCompletions - Display possible completions without modifying the input.
  /// Similar to ShowCompletionCandidates but triggered by Alt+= instead of Tab.
  /// </summary>
  private void HandlePossibleCompletions()
  {
    CompletionHandler.ShowPossibleCompletions(UserInput, CursorPosition);
  }

  // ============================================================================
  // PSReadLine-compatible handler methods
  // ============================================================================
  // PSReadLine-compatible handler methods
  // ============================================================================

  /// <summary>
  /// PSReadLine: BackwardDeleteChar - Delete the character before the cursor.
  /// </summary>
  private void HandleBackwardDeleteChar()
  {
    if (CursorPosition > 0)
    {
      ReplLoggerMessages.BackspacePressed(Logger, CursorPosition, null);

      UserInput = UserInput[..(CursorPosition - 1)] + UserInput[CursorPosition..];
      CursorPosition--;
      CompletionHandler.Reset();  // Clear completion cycling when user deletes

      ReplLoggerMessages.UserInputChanged(Logger, UserInput, CursorPosition, null);
      RedrawLine();
    }
  }

  /// <summary>
  /// PSReadLine: DeleteChar - Delete the character under the cursor.
  /// </summary>
  private void HandleDeleteChar()
  {
    if (CursorPosition < UserInput.Length)
    {
      ReplLoggerMessages.DeletePressed(Logger, CursorPosition, null);

      UserInput = UserInput[..CursorPosition] + UserInput[(CursorPosition + 1)..];
      CompletionHandler.Reset();  // Clear completion cycling when user deletes

      ReplLoggerMessages.UserInputChanged(Logger, UserInput, CursorPosition, null);
      RedrawLine();
    }
  }

  /// <summary>
  /// PSReadLine: BackwardChar - Move the cursor back one character.
  /// </summary>
  private void HandleBackwardChar()
  {
    if (CursorPosition > 0)
      CursorPosition--;

    UpdateCursorPosition();
  }

  /// <summary>
  /// PSReadLine: ForwardChar - Move the cursor forward one character.
  /// </summary>
  private void HandleForwardChar()
  {
    if (CursorPosition < UserInput.Length)
      CursorPosition++;

    UpdateCursorPosition();
  }

  /// <summary>
  /// PSReadLine: BackwardWord - Move the cursor to the beginning of the current or previous word.
  /// </summary>
  private void HandleBackwardWord()
  {
    int newPos = CursorPosition;
    // Skip whitespace behind cursor
    while (newPos > 0 && char.IsWhiteSpace(UserInput[newPos - 1]))
      newPos--;
    // Skip word characters to find start of word
    while (newPos > 0 && !char.IsWhiteSpace(UserInput[newPos - 1]))
      newPos--;
    CursorPosition = newPos;

    UpdateCursorPosition();
  }

  /// <summary>
  /// PSReadLine: ForwardWord - Move the cursor to the end of the current or next word.
  /// Note: PSReadLine moves to END of word, not start of next word.
  /// </summary>
  private void HandleForwardWord()
  {
    int newPos = CursorPosition;
    // Skip whitespace ahead of cursor
    while (newPos < UserInput.Length && char.IsWhiteSpace(UserInput[newPos]))
      newPos++;
    // Move to end of word
    while (newPos < UserInput.Length && !char.IsWhiteSpace(UserInput[newPos]))
      newPos++;
    CursorPosition = newPos;

    UpdateCursorPosition();
  }

  /// <summary>
  /// PSReadLine: BeginningOfLine - Move the cursor to the beginning of the line.
  /// </summary>
  private void HandleBeginningOfLine()
  {
    CursorPosition = 0;
    UpdateCursorPosition();
  }

  /// <summary>
  /// PSReadLine: EndOfLine - Move the cursor to the end of the line.
  /// </summary>
  private void HandleEndOfLine()
  {
    CursorPosition = UserInput.Length;
    UpdateCursorPosition();
  }

  /// <summary>
  /// PSReadLine: PreviousHistory - Replace the input with the previous item in the history.
  /// </summary>
  private void HandlePreviousHistory()
  {
    if (HistoryIndex > 0)
    {
      HistoryIndex--;
      UserInput = History[HistoryIndex];
      CursorPosition = UserInput.Length;
      PrefixSearchString = null;  // Clear prefix search when using normal history nav
      RedrawLine();
    }
  }

  /// <summary>
  /// PSReadLine: NextHistory - Replace the input with the next item in the history.
  /// </summary>
  private void HandleNextHistory()
  {
    if (HistoryIndex < History.Count - 1)
    {
      HistoryIndex++;
      UserInput = History[HistoryIndex];
      CursorPosition = UserInput.Length;
      PrefixSearchString = null;  // Clear prefix search when using normal history nav
      RedrawLine();
    }
    else if (HistoryIndex == History.Count - 1)
    {
      HistoryIndex = History.Count;
      UserInput = string.Empty;
      CursorPosition = 0;
      PrefixSearchString = null;  // Clear prefix search
      RedrawLine();
    }
  }

  /// <summary>
  /// PSReadLine: BeginningOfHistory - Move to the first item in the history.
  /// </summary>
  private void HandleBeginningOfHistory()
  {
    if (History.Count > 0)
    {
      HistoryIndex = 0;
      UserInput = History[HistoryIndex];
      CursorPosition = UserInput.Length;
      PrefixSearchString = null;  // Clear prefix search
      RedrawLine();
    }
  }

  /// <summary>
  /// PSReadLine: EndOfHistory - Move to the last item (current input) in the history.
  /// </summary>
  private void HandleEndOfHistory()
  {
    HistoryIndex = History.Count;
    UserInput = string.Empty;
    CursorPosition = 0;
    PrefixSearchString = null;  // Clear prefix search
    RedrawLine();
  }

  /// <summary>
  /// PSReadLine: HistorySearchBackward - Search backward through history for entries starting with current input prefix.
  /// </summary>
  private void HandleHistorySearchBackward()
  {
    // If no prefix search active, use current input as the prefix
    if (PrefixSearchString is null)
    {
      PrefixSearchString = UserInput;
    }

    // Search backward from current position
    int searchIndex = HistoryIndex - 1;
    while (searchIndex >= 0)
    {
      if (History[searchIndex].StartsWith(PrefixSearchString, StringComparison.OrdinalIgnoreCase))
      {
        HistoryIndex = searchIndex;
        UserInput = History[HistoryIndex];
        CursorPosition = UserInput.Length;
        RedrawLine();
        return;
      }

      searchIndex--;
    }

    // No match found - do nothing (keep current state)
  }

  /// <summary>
  /// PSReadLine: HistorySearchForward - Search forward through history for entries starting with current input prefix.
  /// </summary>
  private void HandleHistorySearchForward()
  {
    // If no prefix search active, use current input as the prefix
    if (PrefixSearchString is null)
    {
      PrefixSearchString = UserInput;
    }

    // Search forward from current position
    int searchIndex = HistoryIndex + 1;
    while (searchIndex < History.Count)
    {
      if (History[searchIndex].StartsWith(PrefixSearchString, StringComparison.OrdinalIgnoreCase))
      {
        HistoryIndex = searchIndex;
        UserInput = History[HistoryIndex];
        CursorPosition = UserInput.Length;
        RedrawLine();
        return;
      }

      searchIndex++;
    }

    // No match found - do nothing (keep current state)
  }

  /// <summary>
  /// PSReadLine: RevertLine - Clear the entire input line (like Escape in PowerShell).
  /// Clears all user input and resets cursor to the beginning.
  /// </summary>
  private void HandleEscape()
  {
    // Clear completion state
    CompletionHandler.Reset();

    // Clear the entire input line
    UserInput = string.Empty;
    CursorPosition = 0;

    // Clear any prefix search state
    PrefixSearchString = null;

    // Redraw the empty line
    RedrawLine();
  }

  private void HandleCharacter(char charToInsert)
  {
    ReplLoggerMessages.CharacterInserted(Logger, charToInsert, CursorPosition, null);

    UserInput = UserInput[..CursorPosition] + charToInsert + UserInput[CursorPosition..];
    CursorPosition++;
    PrefixSearchString = null;  // Clear prefix search when user types
    CompletionHandler.Reset();  // Clear completion cycling when user types

    ReplLoggerMessages.UserInputChanged(Logger, UserInput, CursorPosition, null);
    RedrawLine();
  }

  private void RedrawLine()
  {
    ReplLoggerMessages.LineRedrawn(Logger, UserInput, null);

    // Move cursor to beginning of line
    (int _, int top) = Terminal.GetCursorPosition();
    Terminal.SetCursorPosition(0, top);

    // Clear line
    Terminal.Write(new string(' ', Terminal.WindowWidth));

    // Move back to beginning
    Terminal.SetCursorPosition(0, top);

    // Redraw the prompt and current line
    Terminal.Write(PromptFormatter.Format(ReplOptions));

    if (ReplOptions.EnableColors && Endpoints is not null)
    {
      string highlightedText = SyntaxHighlighter.Highlight(UserInput);
      Terminal.Write(highlightedText);
    }
    else
    {
      Terminal.Write(UserInput);
    }

    // Update cursor position
    UpdateCursorPosition();
  }

  private void UpdateCursorPosition()
  {
    // Calculate desired cursor position (after prompt)
    int promptLength = ReplOptions.Prompt.Length; // Use actual prompt length
    int desiredLeft = promptLength + CursorPosition;

    ReplLoggerMessages.CursorPositionUpdated(Logger, promptLength, CursorPosition, null);

    // Set cursor position if within bounds
    if (desiredLeft < Terminal.WindowWidth)
    {
      (int _, int top) = Terminal.GetCursorPosition();
      Terminal.SetCursorPosition(desiredLeft, top);
    }
  }
}
