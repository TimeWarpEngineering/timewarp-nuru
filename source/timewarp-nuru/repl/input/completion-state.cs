namespace TimeWarp.Nuru;

/// <summary>
/// Encapsulates the state of tab completion cycling.
/// </summary>
internal sealed class CompletionState
{
  /// <summary>
  /// Gets the list of completion candidate values.
  /// </summary>
  public List<string> Candidates { get; } = [];

  /// <summary>
  /// Gets or sets the current index in the completion candidates list.
  /// A value of -1 indicates no candidate is currently selected.
  /// </summary>
  public int Index { get; set; } = -1;

  /// <summary>
  /// Gets or sets the original user input before completion cycling started.
  /// Null when not actively cycling completions.
  /// </summary>
  public string? OriginalInput { get; set; }

  /// <summary>
  /// Gets or sets the original cursor position before completion cycling started.
  /// </summary>
  public int OriginalCursor { get; set; }

  /// <summary>
  /// Gets a value indicating whether completion cycling is active.
  /// </summary>
  public bool IsActive => OriginalInput is not null;

  /// <summary>
  /// Gets a value indicating whether there are completion candidates available.
  /// </summary>
  public bool HasCandidates => Candidates.Count > 0;

  /// <summary>
  /// Resets all completion state to initial values.
  /// </summary>
  public void Reset()
  {
    Candidates.Clear();
    Index = -1;
    OriginalInput = null;
    OriginalCursor = 0;
  }

  /// <summary>
  /// Begins a new completion cycle by saving the current input state.
  /// </summary>
  /// <param name="input">The current user input to save.</param>
  /// <param name="cursor">The current cursor position to save.</param>
  public void BeginCycle(string input, int cursor)
  {
    OriginalInput = input;
    OriginalCursor = cursor;
  }
}
