namespace TimeWarp.Nuru.Repl.Input;

/// <summary>
/// Provides advanced console input handling for REPL mode with tab completion and history navigation.
/// </summary>
public sealed class ReplConsoleReader
{
  private readonly List<string> History;
  private readonly CompletionProvider CompletionProvider;
  private readonly EndpointCollection Endpoints;
  private readonly ReplOptions ReplOptions;
  private readonly SyntaxHighlighter SyntaxHighlighter;
  private readonly ILogger<ReplConsoleReader> Logger;
  private readonly ILoggerFactory? LoggerFactory;
  private readonly ITerminal Terminal;
  private readonly Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Func<bool>> KeyBindings;
  private string UserInput = string.Empty;
  private int CursorPosition;
  private int HistoryIndex = -1;
  private List<string> CompletionCandidates = [];
  private int CompletionIndex = -1;
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
    CompletionProvider = completionProvider ?? throw new ArgumentNullException(nameof(completionProvider));
    Endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
    ReplOptions = replOptions ?? throw new ArgumentNullException(nameof(replOptions));
    SyntaxHighlighter = new SyntaxHighlighter(endpoints, loggerFactory);
    LoggerFactory = loggerFactory;
    Logger = LoggerFactory?.CreateLogger<ReplConsoleReader>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    Terminal = terminal;
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
    CompletionCandidates.Clear();
    CompletionIndex = -1;

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
    // Parse current line into arguments for completion context
    string inputUpToCursor = UserInput[..CursorPosition];
    string[] args = CommandLineParser.Parse(inputUpToCursor);

    // Detect if input ends with whitespace (user wants to complete the NEXT word)
    bool hasTrailingSpace = inputUpToCursor.Length > 0 && char.IsWhiteSpace(inputUpToCursor[^1]);

    ReplLoggerMessages.TabCompletionTriggered(Logger, UserInput, CursorPosition, args, null);

    // Build completion context
    // CursorPosition in CompletionContext is the word index (not character position)
    var context = new CompletionContext(
      Args: args,
      CursorPosition: args.Length,
      Endpoints: Endpoints,
      HasTrailingSpace: hasTrailingSpace
    );

    ReplLoggerMessages.CompletionContextCreated(Logger, args.Length, null);

    // Get completion candidates
    List<CompletionCandidate> candidates = [.. CompletionProvider.GetCompletions(context, Endpoints)];

    ReplLoggerMessages.CompletionCandidatesGenerated(Logger, candidates.Count, null);

    if (candidates.Count == 0)
      return;

    if (candidates.Count == 1)
    {
      // Single completion - apply it
      ApplyCompletion(candidates[0]);
    }
    else
    {
      // Multiple completions - cycle through them or show all
      if (CompletionCandidates.Count != candidates.Count ||
          !candidates.Select(c => c.Value).SequenceEqual(CompletionCandidates))
      {
        // New completion set - show all candidates
        CompletionCandidates = candidates.ConvertAll(c => c.Value);
        ShowCompletionCandidates(candidates);
      }
      else
      {
        // Same completion set - cycle through them
        CompletionIndex = reverse
          ? (CompletionIndex - 1 + candidates.Count) % candidates.Count
          : (CompletionIndex + 1) % candidates.Count;

        ReplLoggerMessages.CompletionCycling(Logger, CompletionIndex, candidates.Count, null);
        ApplyCompletion(candidates[CompletionIndex]);
      }
    }
  }

  private void ApplyCompletion(CompletionCandidate candidate)
  {
    // Find the start position of the word to complete
    int wordStart = FindWordStart(UserInput, CursorPosition);

    ReplLoggerMessages.CompletionApplied(Logger, candidate.Value, wordStart, null);

    // Replace the word with the completion
    UserInput = UserInput[..wordStart] + candidate.Value + UserInput[CursorPosition..];
    CursorPosition = wordStart + candidate.Value.Length;

    ReplLoggerMessages.UserInputChanged(Logger, UserInput, CursorPosition, null);
    // Redraw the line
    RedrawLine();
  }

  private static int FindWordStart(string line, int position)
  {
    // Find the start of the current word (simplified - looks for space before position)
    for (int i = position - 1; i >= 0; i--)
    {
      if (char.IsWhiteSpace(line[i]))
        return i + 1;
    }

    return 0;
  }

  private void ShowCompletionCandidates(List<CompletionCandidate> candidates)
  {
    ReplLoggerMessages.ShowCompletionCandidatesStarted(Logger, UserInput, null);

    Terminal.WriteLine();
    if (ReplOptions.EnableColors)
    {
      Terminal.WriteLine(AnsiColors.Gray + "Available completions:" + AnsiColors.Reset);
    }
    else
    {
      Terminal.WriteLine("Available completions:");
    }

    // Display nicely in columns
    int maxLen = candidates.Max(c => c.Value.Length) + 2;
    int columns = Math.Max(1, Terminal.WindowWidth / maxLen);

    for (int i = 0; i < candidates.Count; i++)
    {
      CompletionCandidate candidate = candidates[i];
      string padded = candidate.Value.PadRight(maxLen);

      ReplLoggerMessages.CompletionCandidateDetails(Logger, candidate.Value, null);

      Terminal.Write(padded);

      if ((i + 1) % columns == 0)
        Terminal.WriteLine();
    }

    if (candidates.Count % columns != 0)
      Terminal.WriteLine();

    // Redraw the prompt and current line
    Terminal.Write(PromptFormatter.Format(ReplOptions));
    Terminal.Write(UserInput);
  }

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

  private void HandleEscape()
  {
    // Clear completion state
    CompletionCandidates.Clear();
    CompletionIndex = -1;
  }

  private void HandleCharacter(char charToInsert)
  {
    ReplLoggerMessages.CharacterInserted(Logger, charToInsert, CursorPosition, null);

    UserInput = UserInput[..CursorPosition] + charToInsert + UserInput[CursorPosition..];
    CursorPosition++;
    PrefixSearchString = null;  // Clear prefix search when user types

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
