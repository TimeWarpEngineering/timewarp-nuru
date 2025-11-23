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
  private string UserInput = string.Empty;
  private int CursorPosition;
  private int HistoryIndex = -1;
  private List<string> CompletionCandidates = [];
  private int CompletionIndex = -1;

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
  }

  /// <summary>
  /// Reads a line of input with advanced editing capabilities.
  /// </summary>
  /// <param name="prompt">The prompt to display.</param>
  /// <returns>The input line from user.</returns>
  public string ReadLine(string prompt)
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

      switch (keyInfo.Key)
      {
        case ConsoleKey.Enter:
          HandleEnter();
          return UserInput;

        case ConsoleKey.Tab:
          HandleTabCompletion(keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift));
          break;

        case ConsoleKey.Backspace:
          HandleBackspace();
          break;

        case ConsoleKey.Delete:
          HandleDelete();
          break;

        case ConsoleKey.LeftArrow:
          HandleLeftArrow(keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control));
          break;

        case ConsoleKey.RightArrow:
          HandleRightArrow(keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control));
          break;

        case ConsoleKey.Home:
          HandleHome();
          break;

        case ConsoleKey.End:
          HandleEnd();
          break;

        case ConsoleKey.UpArrow:
          HandleUpArrow();
          break;

        case ConsoleKey.DownArrow:
          HandleDownArrow();
          break;

        case ConsoleKey.Escape:
          HandleEscape();
          break;

        default:
          if (!char.IsControl(keyInfo.KeyChar))
          {
            HandleCharacter(keyInfo.KeyChar);
          }

          break;
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
    string[] args = CommandLineParser.Parse(UserInput[..CursorPosition]);

    ReplLoggerMessages.TabCompletionTriggered(Logger, UserInput, CursorPosition, args, null);

    // Build completion context
    var context = new CompletionContext(
      Args: args,
      CursorPosition: CursorPosition,
      Endpoints: Endpoints
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

  private void HandleBackspace()
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

  private void HandleDelete()
  {
    if (CursorPosition < UserInput.Length)
    {
      ReplLoggerMessages.DeletePressed(Logger, CursorPosition, null);

      UserInput = UserInput[..CursorPosition] + UserInput[(CursorPosition + 1)..];

      ReplLoggerMessages.UserInputChanged(Logger, UserInput, CursorPosition, null);
      RedrawLine();
    }
  }

  private void HandleLeftArrow(bool ctrl)
  {
    if (ctrl)
    {
      // Move to previous word
      int newPos = CursorPosition;
      while (newPos > 0 && char.IsWhiteSpace(UserInput[newPos - 1]))
        newPos--;
      while (newPos > 0 && !char.IsWhiteSpace(UserInput[newPos - 1]))
        newPos--;
      CursorPosition = newPos;
    }
    else
    {
      // Move one character left
      if (CursorPosition > 0)
        CursorPosition--;
    }

    UpdateCursorPosition();
  }

  private void HandleRightArrow(bool ctrl)
  {
    if (ctrl)
    {
      // Move to next word
      int newPos = CursorPosition;
      while (newPos < UserInput.Length && !char.IsWhiteSpace(UserInput[newPos]))
        newPos++;
      while (newPos < UserInput.Length && char.IsWhiteSpace(UserInput[newPos]))
        newPos++;
      CursorPosition = newPos;
    }
    else
    {
      // Move one character right
      if (CursorPosition < UserInput.Length)
        CursorPosition++;
    }

    UpdateCursorPosition();
  }

  private void HandleHome()
  {
    CursorPosition = 0;
    UpdateCursorPosition();
  }

  private void HandleEnd()
  {
    CursorPosition = UserInput.Length;
    UpdateCursorPosition();
  }

  private void HandleUpArrow()
  {
    if (HistoryIndex > 0)
    {
      HistoryIndex--;
      UserInput = History[HistoryIndex];
      CursorPosition = UserInput.Length;
      RedrawLine();
    }
  }

  private void HandleDownArrow()
  {
    if (HistoryIndex < History.Count - 1)
    {
      HistoryIndex++;
      UserInput = History[HistoryIndex];
      CursorPosition = UserInput.Length;
      RedrawLine();
    }
    else if (HistoryIndex == History.Count - 1)
    {
      HistoryIndex = History.Count;
      UserInput = string.Empty;
      CursorPosition = 0;
      RedrawLine();
    }
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
