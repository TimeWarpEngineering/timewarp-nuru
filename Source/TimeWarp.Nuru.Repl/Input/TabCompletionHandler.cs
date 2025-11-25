namespace TimeWarp.Nuru.Repl.Input;

/// <summary>
/// Handles tab completion logic for REPL input, including cycling and candidate display.
/// </summary>
internal sealed class TabCompletionHandler
{
  private readonly CompletionProvider _completionProvider;
  private readonly EndpointCollection _endpoints;
  private readonly ITerminal _terminal;
  private readonly ReplOptions _replOptions;
  private readonly ILogger<TabCompletionHandler> _logger;
  private readonly CompletionState _state = new();

  /// <summary>
  /// Creates a new tab completion handler.
  /// </summary>
  /// <param name="completionProvider">The completion provider for generating candidates.</param>
  /// <param name="endpoints">The endpoint collection for completion context.</param>
  /// <param name="terminal">The terminal I/O provider.</param>
  /// <param name="replOptions">The REPL configuration options.</param>
  /// <param name="loggerFactory">The logger factory for creating loggers.</param>
  public TabCompletionHandler
  (
    CompletionProvider completionProvider,
    EndpointCollection endpoints,
    ITerminal terminal,
    ReplOptions replOptions,
    ILoggerFactory loggerFactory
  )
  {
    ArgumentNullException.ThrowIfNull(completionProvider);
    ArgumentNullException.ThrowIfNull(endpoints);
    ArgumentNullException.ThrowIfNull(terminal);
    ArgumentNullException.ThrowIfNull(replOptions);
    ArgumentNullException.ThrowIfNull(loggerFactory);

    _completionProvider = completionProvider;
    _endpoints = endpoints;
    _terminal = terminal;
    _replOptions = replOptions;
    _logger = loggerFactory.CreateLogger<TabCompletionHandler>();
  }

  /// <summary>
  /// Handles tab completion and returns the modified input state.
  /// </summary>
  /// <param name="currentInput">The current user input.</param>
  /// <param name="currentCursor">The current cursor position.</param>
  /// <param name="reverse">True to cycle backward through completions, false to cycle forward.</param>
  /// <returns>A tuple containing the new input string and new cursor position.</returns>
  public (string NewInput, int NewCursor) HandleTab(
    string currentInput,
    int currentCursor,
    bool reverse)
  {
    // If we're cycling, restore the original input before getting new completions
    if (_state.IsActive)
    {
      currentInput = _state.OriginalInput!;
      currentCursor = _state.OriginalCursor;
    }

    List<CompletionCandidate> candidates = GetCandidates(currentInput, currentCursor);

    if (candidates.Count == 0)
      return (currentInput, currentCursor);

    if (candidates.Count == 1)
      return ApplySingleCompletion(currentInput, currentCursor, candidates[0]);

    return HandleMultipleCompletions(currentInput, currentCursor, candidates, reverse);
  }

  /// <summary>
  /// Shows all possible completions without modifying input.
  /// </summary>
  /// <param name="currentInput">The current user input.</param>
  /// <param name="currentCursor">The current cursor position.</param>
  public void ShowPossibleCompletions(string currentInput, int currentCursor)
  {
    List<CompletionCandidate> candidates = GetCandidates(currentInput, currentCursor);
    if (candidates.Count > 0)
      DisplayCandidates(candidates, currentInput);
  }

  /// <summary>
  /// Resets completion state (call when user types/deletes/escapes).
  /// </summary>
  public void Reset() => _state.Reset();

  private List<CompletionCandidate> GetCandidates(string input, int cursor)
  {
    // Parse current line into arguments for completion context
    string inputUpToCursor = input[..cursor];
    string[] args = CommandLineParser.Parse(inputUpToCursor);

    // Detect if input ends with whitespace (user wants to complete the NEXT word)
    bool hasTrailingSpace = inputUpToCursor.Length > 0 && char.IsWhiteSpace(inputUpToCursor[^1]);

    ReplLoggerMessages.TabCompletionTriggered(_logger, input, cursor, args, null);

    // Build completion context
    // CursorPosition in CompletionContext is the word index (not character position)
    var context = new CompletionContext(
      Args: args,
      CursorPosition: args.Length,
      Endpoints: _endpoints,
      HasTrailingSpace: hasTrailingSpace
    );

    ReplLoggerMessages.CompletionContextCreated(_logger, args.Length, null);

    // Get completion candidates
    List<CompletionCandidate> candidates = [.. _completionProvider.GetCompletions(context, _endpoints)];

    ReplLoggerMessages.CompletionCandidatesGenerated(_logger, candidates.Count, null);

    return candidates;
  }

  private (string, int) ApplySingleCompletion(
    string input,
    int cursor,
    CompletionCandidate candidate)
  {
    // Single completion - apply it and clear cycling state
    _state.Reset();

    // Find the start position of the word to complete
    int wordStart = FindWordStart(input, cursor);

    ReplLoggerMessages.CompletionApplied(_logger, candidate.Value, wordStart, null);

    // Replace the word with the completion
    string newInput = input[..wordStart] + candidate.Value + input[cursor..];
    int newCursor = wordStart + candidate.Value.Length;

    ReplLoggerMessages.UserInputChanged(_logger, newInput, newCursor, null);

    return (newInput, newCursor);
  }

  private (string, int) HandleMultipleCompletions(
    string input,
    int cursor,
    List<CompletionCandidate> candidates,
    bool reverse)
  {
    List<string> candidateValues = candidates.ConvertAll(c => c.Value);

    // New completion set - save original input and show all candidates
    if (!_state.HasCandidates ||
        !candidateValues.SequenceEqual(_state.Candidates))
    {
      _state.BeginCycle(input, cursor);
      _state.Candidates.Clear();
      _state.Candidates.AddRange(candidateValues);
      _state.Index = -1;
      DisplayCandidates(candidates, input);
      return (input, cursor);
    }

    // Same completion set - cycle through them
    _state.Index = reverse
      ? (_state.Index - 1 + candidates.Count) % candidates.Count
      : (_state.Index + 1) % candidates.Count;

    ReplLoggerMessages.CompletionCycling(_logger, _state.Index, candidates.Count, null);

    return ApplySingleCompletion(input, cursor, candidates[_state.Index]);
  }

  private void DisplayCandidates(
    List<CompletionCandidate> candidates,
    string currentInput)
  {
    ReplLoggerMessages.ShowCompletionCandidatesStarted(_logger, currentInput, null);

    _terminal.WriteLine();
    if (_replOptions.EnableColors)
    {
      _terminal.WriteLine(AnsiColors.Gray + "Available completions:" + AnsiColors.Reset);
    }
    else
    {
      _terminal.WriteLine("Available completions:");
    }

    // Display nicely in columns
    int maxLen = candidates.Max(c => c.Value.Length) + 2;
    int columns = Math.Max(1, _terminal.WindowWidth / maxLen);

    for (int i = 0; i < candidates.Count; i++)
    {
      CompletionCandidate candidate = candidates[i];
      string padded = candidate.Value.PadRight(maxLen);

      ReplLoggerMessages.CompletionCandidateDetails(_logger, candidate.Value, null);

      _terminal.Write(padded);

      if ((i + 1) % columns == 0)
        _terminal.WriteLine();
    }

    if (candidates.Count % columns != 0)
      _terminal.WriteLine();

    // Redraw the prompt and current line
    _terminal.Write(PromptFormatter.Format(_replOptions));
    _terminal.Write(currentInput);
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
}
