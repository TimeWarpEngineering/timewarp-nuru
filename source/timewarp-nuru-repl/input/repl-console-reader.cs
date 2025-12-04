namespace TimeWarp.Nuru;

/// <summary>
/// Defines the editing mode for the REPL console reader.
/// </summary>
/// <remarks>
/// The mode determines how key presses are interpreted:
/// - Normal: Standard line editing
/// - Search: Interactive history search (Ctrl+R/Ctrl+S)
/// - MenuComplete: Menu-style completion selection (future)
/// </remarks>
internal enum EditMode
{
  /// <summary>Standard line editing mode.</summary>
  Normal,

  /// <summary>Interactive incremental history search mode (Ctrl+R/Ctrl+S).</summary>
  Search,

  /// <summary>Menu completion mode for Ctrl+Space (future).</summary>
  MenuComplete
}

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
  private readonly Dictionary<(ConsoleKey Key, ConsoleModifiers Modifiers), Action> KeyBindings;
  private readonly HashSet<(ConsoleKey Key, ConsoleModifiers Modifiers)> ExitKeys;
  private string UserInput = string.Empty;
  private int CursorPosition;
  private int HistoryIndex = -1;
  private string? PrefixSearchString;  // Stores the prefix for F8 prefix search

  // EditMode state machine fields
  private EditMode CurrentMode = EditMode.Normal;
  private string SearchPattern = string.Empty;
  private int SearchMatchIndex = -1;  // Index in history of current match (-1 = no match)
  private string SavedInputBeforeSearch = string.Empty;  // Original input to restore on cancel
  private int SavedCursorBeforeSearch;  // Original cursor position to restore on cancel
  private bool SearchDirectionIsReverse = true;  // true = Ctrl+R (backward), false = Ctrl+S (forward)

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

    // Initialize key bindings from the configured profile
    // Check for custom profile instance first, then fall back to profile name
    IKeyBindingProfile profile = replOptions.KeyBindingProfile is IKeyBindingProfile customProfile
      ? customProfile
      : KeyBindingProfileFactory.GetProfile(replOptions.KeyBindingProfileName);
    KeyBindings = profile.GetBindings(this);
    ExitKeys = profile.GetExitKeys();
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

      // Route to mode-specific handler
      if (CurrentMode == EditMode.Search)
      {
        string? result = HandleSearchModeKey(keyInfo);
        if (result is not null)
          return result;
        continue;
      }

      // Normal mode: use key bindings
      // Normalize modifiers to only include Ctrl, Alt, Shift (ignore other flags)
      ConsoleModifiers normalizedMods = keyInfo.Modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt | ConsoleModifiers.Shift);
      (ConsoleKey Key, ConsoleModifiers Modifiers) keyBinding = (keyInfo.Key, normalizedMods);

      if (KeyBindings.TryGetValue(keyBinding, out Action? handler))
      {
        handler();

        // Check if this key should exit the read loop
        if (ExitKeys.Contains(keyBinding))
        {
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

  internal void HandleEnter()
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

  internal void HandleTabCompletion(bool reverse)
  {
    (UserInput, CursorPosition) = CompletionHandler.HandleTab(UserInput, CursorPosition, reverse);
    RedrawLine();
  }

  /// <summary>
  /// PSReadLine: PossibleCompletions - Display possible completions without modifying the input.
  /// Similar to ShowCompletionCandidates but triggered by Alt+= instead of Tab.
  /// </summary>
  internal void HandlePossibleCompletions()
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
  internal void HandleBackwardDeleteChar()
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
  internal void HandleDeleteChar()
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
  internal void HandleBackwardChar()
  {
    if (CursorPosition > 0)
      CursorPosition--;

    UpdateCursorPosition();
  }

  /// <summary>
  /// PSReadLine: ForwardChar - Move the cursor forward one character.
  /// </summary>
  internal void HandleForwardChar()
  {
    if (CursorPosition < UserInput.Length)
      CursorPosition++;

    UpdateCursorPosition();
  }

  /// <summary>
  /// PSReadLine: BackwardWord - Move the cursor to the beginning of the current or previous word.
  /// </summary>
  internal void HandleBackwardWord()
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
  internal void HandleForwardWord()
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
  internal void HandleBeginningOfLine()
  {
    CursorPosition = 0;
    UpdateCursorPosition();
  }

  /// <summary>
  /// PSReadLine: EndOfLine - Move the cursor to the end of the line.
  /// </summary>
  internal void HandleEndOfLine()
  {
    CursorPosition = UserInput.Length;
    UpdateCursorPosition();
  }

  /// <summary>
  /// PSReadLine: PreviousHistory - Replace the input with the previous item in the history.
  /// </summary>
  internal void HandlePreviousHistory()
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
  internal void HandleNextHistory()
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
  internal void HandleBeginningOfHistory()
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
  internal void HandleEndOfHistory()
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
  internal void HandleHistorySearchBackward()
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
  internal void HandleHistorySearchForward()
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

  // ============================================================================
  // Interactive History Search (Ctrl+R / Ctrl+S)
  // ============================================================================

  /// <summary>
  /// PSReadLine: ReverseSearchHistory - Enter interactive reverse incremental search mode.
  /// </summary>
  /// <remarks>
  /// In search mode, the user types a search pattern and sees matching history entries
  /// in real-time. Pressing Ctrl+R again cycles to the next (older) match.
  /// </remarks>
  internal void HandleReverseSearchHistory()
  {
    if (CurrentMode == EditMode.Normal)
    {
      EnterSearchMode(reverse: true);
    }
    else if (CurrentMode == EditMode.Search)
    {
      // Already in search mode - find next match in reverse direction
      SearchDirectionIsReverse = true;
      FindNextMatch(reverse: true);
    }
  }

  /// <summary>
  /// PSReadLine: ForwardSearchHistory - Enter interactive forward incremental search mode.
  /// </summary>
  /// <remarks>
  /// In search mode, the user types a search pattern and sees matching history entries
  /// in real-time. Pressing Ctrl+S again cycles to the next (newer) match.
  /// </remarks>
  internal void HandleForwardSearchHistory()
  {
    if (CurrentMode == EditMode.Normal)
    {
      EnterSearchMode(reverse: false);
    }
    else if (CurrentMode == EditMode.Search)
    {
      // Already in search mode - find next match in forward direction
      SearchDirectionIsReverse = false;
      FindNextMatch(reverse: false);
    }
  }

  private void EnterSearchMode(bool reverse)
  {
    // Save current state to restore on cancel
    SavedInputBeforeSearch = UserInput;
    SavedCursorBeforeSearch = CursorPosition;

    // Initialize search state
    CurrentMode = EditMode.Search;
    SearchPattern = string.Empty;
    SearchMatchIndex = reverse ? History.Count : -1;  // Start from end (reverse) or beginning (forward)
    SearchDirectionIsReverse = reverse;

    // Display search prompt
    RedrawSearchLine();
  }

  private void ExitSearchMode(bool acceptMatch)
  {
    CurrentMode = EditMode.Normal;

    if (acceptMatch && SearchMatchIndex >= 0 && SearchMatchIndex < History.Count)
    {
      // Accept: keep the matched history entry as user input
      UserInput = History[SearchMatchIndex];
      CursorPosition = UserInput.Length;
      HistoryIndex = SearchMatchIndex;
    }
    else
    {
      // Cancel: restore original input
      UserInput = SavedInputBeforeSearch;
      CursorPosition = SavedCursorBeforeSearch;
    }

    // Clear search state
    SearchPattern = string.Empty;
    SearchMatchIndex = -1;

    // Redraw in normal mode
    RedrawLine();
  }

  /// <summary>
  /// Handles a key press while in search mode.
  /// </summary>
  /// <returns>
  /// The result string if the read loop should exit (e.g., Enter was pressed),
  /// or null to continue the loop.
  /// </returns>
  private string? HandleSearchModeKey(ConsoleKeyInfo keyInfo)
  {
    ConsoleModifiers mods = keyInfo.Modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt | ConsoleModifiers.Shift);

    // Ctrl+R: find next older match
    if (keyInfo.Key == ConsoleKey.R && mods == ConsoleModifiers.Control)
    {
      SearchDirectionIsReverse = true;
      FindNextMatch(reverse: true);
      return null;
    }

    // Ctrl+S: find next newer match
    if (keyInfo.Key == ConsoleKey.S && mods == ConsoleModifiers.Control)
    {
      SearchDirectionIsReverse = false;
      FindNextMatch(reverse: false);
      return null;
    }

    // Enter: accept current match and exit
    if (keyInfo.Key == ConsoleKey.Enter)
    {
      ExitSearchMode(acceptMatch: true);
      HandleEnter();
      return UserInput;
    }

    // Escape: cancel and restore original input
    if (keyInfo.Key == ConsoleKey.Escape)
    {
      ExitSearchMode(acceptMatch: false);
      return null;
    }

    // Backspace: remove last character from search pattern
    if (keyInfo.Key == ConsoleKey.Backspace)
    {
      if (SearchPattern.Length > 0)
      {
        SearchPattern = SearchPattern[..^1];
        // Re-search from beginning with new pattern
        SearchMatchIndex = SearchDirectionIsReverse ? History.Count : -1;
        FindNextMatch(SearchDirectionIsReverse);
      }

      return null;
    }

    // Ctrl+G: cancel (alternative to Escape, like in Emacs)
    if (keyInfo.Key == ConsoleKey.G && mods == ConsoleModifiers.Control)
    {
      ExitSearchMode(acceptMatch: false);
      return null;
    }

    // Character input: append to search pattern
    if (!char.IsControl(keyInfo.KeyChar))
    {
      SearchPattern += keyInfo.KeyChar;
      // Search with new pattern
      FindNextMatch(SearchDirectionIsReverse);
      return null;
    }

    // Any other key: accept current match, exit search mode, and process the key
    ExitSearchMode(acceptMatch: true);

    // Now process the key in normal mode
    ConsoleModifiers normalizedMods = keyInfo.Modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt | ConsoleModifiers.Shift);
    (ConsoleKey Key, ConsoleModifiers Modifiers) keyBinding = (keyInfo.Key, normalizedMods);

    if (KeyBindings.TryGetValue(keyBinding, out Action? handler))
    {
      handler();
      if (ExitKeys.Contains(keyBinding))
      {
        return keyInfo.Key == ConsoleKey.Enter ? UserInput : null;
      }
    }

    return null;
  }

  private void FindNextMatch(bool reverse)
  {
    if (History.Count == 0)
    {
      RedrawSearchLine();
      return;
    }

    int startIndex = SearchMatchIndex;

    if (reverse)
    {
      // Search backward (older entries)
      int searchIndex = startIndex == History.Count ? History.Count - 1 : startIndex - 1;
      while (searchIndex >= 0)
      {
        if (string.IsNullOrEmpty(SearchPattern) ||
            History[searchIndex].Contains(SearchPattern, StringComparison.OrdinalIgnoreCase))
        {
          SearchMatchIndex = searchIndex;
          RedrawSearchLine();
          return;
        }

        searchIndex--;
      }
    }
    else
    {
      // Search forward (newer entries)
      int searchIndex = startIndex < 0 ? 0 : startIndex + 1;
      while (searchIndex < History.Count)
      {
        if (string.IsNullOrEmpty(SearchPattern) ||
            History[searchIndex].Contains(SearchPattern, StringComparison.OrdinalIgnoreCase))
        {
          SearchMatchIndex = searchIndex;
          RedrawSearchLine();
          return;
        }

        searchIndex++;
      }
    }

    // No match found - keep current state but indicate "failing"
    // For now, just redraw (future: could show "failing" in prompt)
    RedrawSearchLine();
  }

  private void RedrawSearchLine()
  {
    // Move cursor to beginning of line
    (int _, int top) = Terminal.GetCursorPosition();
    Terminal.SetCursorPosition(0, top);

    // Clear line
    Terminal.Write(new string(' ', Terminal.WindowWidth));

    // Move back to beginning
    Terminal.SetCursorPosition(0, top);

    // Build search prompt: (reverse-i-search)`pattern': matched_text
    string direction = SearchDirectionIsReverse ? "reverse" : "forward";
    string searchPrompt = $"({direction}-i-search)`{SearchPattern}': ";

    Terminal.Write(searchPrompt);

    // Show matched history entry (if any)
    if (SearchMatchIndex >= 0 && SearchMatchIndex < History.Count)
    {
      string matchedEntry = History[SearchMatchIndex];
      Terminal.Write(matchedEntry);
    }
  }

  /// <summary>
  /// PSReadLine: RevertLine - Clear the entire input line (like Escape in PowerShell).
  /// Clears all user input and resets cursor to the beginning.
  /// </summary>
  internal void HandleEscape()
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

  /// <summary>
  /// PSReadLine: KillLine - Delete from the cursor position to the end of the line.
  /// The deleted text is removed (kill ring not implemented).
  /// </summary>
  internal void HandleKillLine()
  {
    if (CursorPosition < UserInput.Length)
    {
      // Delete everything from cursor to end of line
      UserInput = UserInput[..CursorPosition];
      CompletionHandler.Reset();
      RedrawLine();
    }
  }

  /// <summary>
  /// PSReadLine: BackwardKillWord - Delete the word before the cursor.
  /// Deletes from cursor back to the start of the current or previous word.
  /// </summary>
  internal void HandleDeleteWordBackward()
  {
    if (CursorPosition > 0)
    {
      int newPos = CursorPosition;

      // Skip whitespace behind cursor
      while (newPos > 0 && char.IsWhiteSpace(UserInput[newPos - 1]))
        newPos--;

      // Skip word characters to find start of word
      while (newPos > 0 && !char.IsWhiteSpace(UserInput[newPos - 1]))
        newPos--;

      // Delete from newPos to CursorPosition
      UserInput = UserInput[..newPos] + UserInput[CursorPosition..];
      CursorPosition = newPos;
      CompletionHandler.Reset();
      RedrawLine();
    }
  }

  /// <summary>
  /// PSReadLine: BackwardKillLine - Delete from the beginning of the line to the cursor.
  /// The deleted text is removed (kill ring not implemented).
  /// </summary>
  internal void HandleDeleteToLineStart()
  {
    if (CursorPosition > 0)
    {
      // Delete everything from start of line to cursor
      UserInput = UserInput[CursorPosition..];
      CursorPosition = 0;
      CompletionHandler.Reset();
      RedrawLine();
    }
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
