namespace TimeWarp.Nuru;

/// <summary>
/// Handles tab completion logic for REPL input, including cycling and candidate display.
/// </summary>
internal sealed class TabCompletionHandler
{
  private readonly IReplRouteProvider RouteProvider;
  private readonly ITerminal Terminal;
  private readonly ReplOptions ReplOptions;
  private readonly ILogger<TabCompletionHandler> Logger;
  private readonly CompletionState State = new();

  /// <summary>
  /// Creates a new tab completion handler.
  /// </summary>
  /// <param name="routeProvider">The route provider for generating completions.</param>
  /// <param name="terminal">The terminal I/O provider.</param>
  /// <param name="replOptions">The REPL configuration options.</param>
  /// <param name="loggerFactory">The logger factory for creating loggers.</param>
  public TabCompletionHandler
  (
    IReplRouteProvider routeProvider,
    ITerminal terminal,
    ReplOptions replOptions,
    ILoggerFactory? loggerFactory
  )
  {
    ArgumentNullException.ThrowIfNull(terminal);

    RouteProvider = routeProvider ?? EmptyReplRouteProvider.Instance;
    Terminal = terminal;
    ReplOptions = replOptions ?? new ReplOptions();
    Logger = loggerFactory?.CreateLogger<TabCompletionHandler>() ?? NullLogger<TabCompletionHandler>.Instance;
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
    if (State.IsActive)
    {
      currentInput = State.OriginalInput!;
      currentCursor = State.OriginalCursor;
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
  public void Reset() => State.Reset();

  private List<CompletionCandidate> GetCandidates(string input, int cursor)
  {
    // Parse current line into arguments for completion context
    string inputUpToCursor = input[..cursor];
    string[] args = CommandLineParser.Parse(inputUpToCursor);

    // Detect if input ends with whitespace (user wants to complete the NEXT word)
    bool hasTrailingSpace = inputUpToCursor.Length > 0 && char.IsWhiteSpace(inputUpToCursor[^1]);

    ReplLoggerMessages.TabCompletionTriggered(Logger, input, cursor, args, null);

    // Get completion candidates from route provider
    List<CompletionCandidate> candidates = [.. RouteProvider.GetCompletions(args, hasTrailingSpace)];

    ReplLoggerMessages.CompletionCandidatesGenerated(Logger, candidates.Count, null);

    return candidates;
  }

  private (string, int) ApplySingleCompletion(
    string input,
    int cursor,
    CompletionCandidate candidate,
    bool resetState = true)
  {
    // Only reset state when not cycling through multiple completions
    if (resetState)
      State.Reset();

    // Find the start position of the word to complete
    int wordStart = FindWordStart(input, cursor);

    ReplLoggerMessages.CompletionApplied(Logger, candidate.Value, wordStart, null);

    // Replace the word with the completion
    string newInput = input[..wordStart] + candidate.Value + input[cursor..];
    int newCursor = wordStart + candidate.Value.Length;

    ReplLoggerMessages.UserInputChanged(Logger, newInput, newCursor, null);

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
    if (!State.HasCandidates ||
        !candidateValues.SequenceEqual(State.Candidates))
    {
      State.BeginCycle(input, cursor);
      State.Candidates.Clear();
      State.Candidates.AddRange(candidateValues);
      State.Index = -1;
      DisplayCandidates(candidates, input);
      return (input, cursor);
    }

    // Same completion set - cycle through them
    State.Index = reverse
      ? (State.Index - 1 + candidates.Count) % candidates.Count
      : (State.Index + 1) % candidates.Count;

    ReplLoggerMessages.CompletionCycling(Logger, State.Index, candidates.Count, null);

    return ApplySingleCompletion(input, cursor, candidates[State.Index], resetState: false);
  }

  private void DisplayCandidates(
    List<CompletionCandidate> candidates,
    string currentInput)
  {
    ReplLoggerMessages.ShowCompletionCandidatesStarted(Logger, currentInput, null);

    Terminal.WriteLine();
    string header = "Available completions:";
    Terminal.WriteLine(ReplOptions.EnableColors ? header.Gray() : header);

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
    Terminal.Write(currentInput);
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
