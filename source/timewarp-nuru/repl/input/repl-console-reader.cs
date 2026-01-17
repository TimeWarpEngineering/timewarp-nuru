namespace TimeWarp.Nuru;

/// <summary>
/// Provides advanced console input handling for REPL mode with tab completion and history navigation.
/// </summary>
/// <remarks>
/// This class is split into partial classes for maintainability:
/// - repl-console-reader.cs: Core class, fields, constructor, and main ReadLine loop
/// - repl-console-reader.cursor-movement.cs: Cursor movement handlers
/// - repl-console-reader.history.cs: History navigation handlers
/// - repl-console-reader.editing.cs: Text editing handlers
/// - repl-console-reader.search.cs: Interactive search mode (Ctrl+R/Ctrl+S)
/// - repl-console-reader.kill-ring.cs: Kill ring (cut/paste) handlers
/// - repl-console-reader.undo.cs: Undo/redo handlers
/// - repl-console-reader.selection.cs: Text selection handlers
/// - repl-console-reader.clipboard.cs: Platform-specific clipboard operations
/// - repl-console-reader.word-operations.cs: Word case conversion and transposition handlers
/// </remarks>
public sealed partial class ReplConsoleReader
{
  private readonly List<string> History;
  private readonly IReplRouteProvider RouteProvider;
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

  // Kill ring state fields
  private readonly KillRing KillRing = new();
  private bool LastCommandWasKill;  // For consecutive kill appending
  private bool LastCommandWasYank;  // For YankPop to work
  private int LastYankStart;        // Start position of last yanked text
  private int LastYankLength;       // Length of last yanked text

  // Undo/redo state fields
  private readonly UndoStack UndoManager = new();

  // Selection state fields
  private readonly Selection SelectionState = new();

  // Exit signal for DeleteCharOrExit
  private bool ShouldExitRepl;

  /// <summary>
  /// Creates a new REPL console reader.
  /// </summary>
  /// <param name="history">The command history list.</param>
  /// <param name="routeProvider">The route provider for completion and highlighting.</param>
  /// <param name="replOptions">The REPL configuration options.</param>
  /// <param name="loggerFactory">The logger factory for creating loggers.</param>
  /// <param name="terminal">The terminal I/O provider.</param>
  public ReplConsoleReader
  (
    IEnumerable<string> history,
    IReplRouteProvider routeProvider,
    ReplOptions replOptions,
    ILoggerFactory? loggerFactory,
    ITerminal terminal
  )
  {
    ArgumentNullException.ThrowIfNull(history);
    ArgumentNullException.ThrowIfNull(terminal);

    History = history?.ToList() ?? throw new ArgumentNullException(nameof(history));
    RouteProvider = routeProvider ?? EmptyReplRouteProvider.Instance;
    ReplOptions = replOptions ?? new ReplOptions();
    SyntaxHighlighter = new SyntaxHighlighter(RouteProvider, loggerFactory);
    LoggerFactory = loggerFactory;
    Logger = LoggerFactory?.CreateLogger<ReplConsoleReader>() ?? NullLogger<ReplConsoleReader>.Instance;
    Terminal = terminal;
    CompletionHandler = new TabCompletionHandler(RouteProvider, terminal, ReplOptions, loggerFactory);

    // Initialize key bindings from the configured profile
    // Check for custom profile instance first, then fall back to profile name
    IKeyBindingProfile profile = ReplOptions.KeyBindingProfile is IKeyBindingProfile customProfile
      ? customProfile
      : KeyBindingProfileFactory.GetProfile(ReplOptions.KeyBindingProfileName);
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
    UndoManager.Clear();
    UndoManager.SetInitialState(string.Empty, 0);
    MultilineInput.Clear();    // Reset multiline buffer for new input

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

        // Check if handler signaled exit (e.g., DeleteCharOrExit on empty line)
        if (ShouldExitRepl)
        {
          ShouldExitRepl = false;  // Reset for next call
          return null;
        }

        // Check if this key should exit the read loop (accept line)
        if (ExitKeys.Contains(keyBinding))
        {
          // All exit keys that are "accept line" should return UserInput
          // (Enter, Ctrl+M, Ctrl+J are accept line; others would be EOF but
          // those are now handled by ShouldExitRepl flag)
          return UserInput;
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
    // If in multiline mode, move cursor to end of last line first
    if (IsMultilineMode)
    {
      // Sync to get final UserInput value
      SyncFromMultilineBuffer();
    }

    Terminal.WriteLine();

    // Add to history if not empty and not duplicate of last entry
    if (!string.IsNullOrWhiteSpace(UserInput))
    {
      if (History.Count == 0 || History[^1] != UserInput)
      {
        History.Add(UserInput);
      }
    }

    // Clear multiline buffer for next input
    MultilineInput.Clear();
  }

  internal void HandleTabCompletion(bool reverse)
  {
    (UserInput, CursorPosition) = CompletionHandler.HandleTab(UserInput, CursorPosition, reverse);
    RedrawLine();
  }

  /// <summary>
  /// Writes text to the terminal output. This method is available for custom key binding actions
  /// that need to write output (e.g., for playing the bell character with Ctrl+G).
  /// </summary>
  /// <param name="text">The text to write.</param>
  public void Write(string text) => Terminal.Write(text);

  /// <summary>
  /// PSReadLine: PossibleCompletions - Display possible completions without modifying the input.
  /// Similar to ShowCompletionCandidates but triggered by Alt+= instead of Tab.
  /// </summary>
  internal void HandlePossibleCompletions()
  {
    CompletionHandler.ShowPossibleCompletions(UserInput, CursorPosition);
  }

  private void HandleCharacter(char charToInsert)
  {
    ReplLoggerMessages.CharacterInserted(Logger, charToInsert, CursorPosition, null);

    SaveUndoState(isCharacterInput: true);  // Save state before edit (grouped for consecutive chars)

    // If there's a selection, replace it with the typed character
    if (SelectionState.IsActive)
    {
      int start = SelectionState.Start;
      int end = SelectionState.End;
      UserInput = UserInput[..start] + charToInsert + UserInput[end..];
      CursorPosition = start + 1;
      SelectionState.Clear();
    }
    else if (IsOverwriteMode && CursorPosition < UserInput.Length)
    {
      // Overwrite mode: replace character at cursor
      UserInput = UserInput[..CursorPosition] + charToInsert + UserInput[(CursorPosition + 1)..];
      CursorPosition++;
    }
    else
    {
      // Insert mode (default): insert at cursor
      UserInput = UserInput[..CursorPosition] + charToInsert + UserInput[CursorPosition..];
      CursorPosition++;
    }

    PrefixSearchString = null;  // Clear prefix search when user types
    CompletionHandler.Reset();  // Clear completion cycling when user types
    ResetKillTracking();        // Clear kill ring tracking when user types

    ReplLoggerMessages.UserInputChanged(Logger, UserInput, CursorPosition, null);
    RedrawLine();
  }

  private void RedrawLine()
  {
    // Check if UserInput contains newlines (multiline content)
    // This can happen when recalling multiline history
    if (UserInput.Contains('\n', StringComparison.Ordinal) || UserInput.Contains('\r', StringComparison.Ordinal))
    {
      SyncToMultilineBuffer();
      RedrawMultiline();
      return;
    }

    // If already in multiline mode, delegate to multiline rendering
    if (IsMultilineMode)
    {
      SyncToMultilineBuffer();
      RedrawMultiline();
      return;
    }

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

    if (ReplOptions.EnableColors)
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
