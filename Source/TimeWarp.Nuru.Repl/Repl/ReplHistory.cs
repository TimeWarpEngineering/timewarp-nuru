namespace TimeWarp.Nuru.Repl;

/// <summary>
/// Manages command history for REPL sessions including persistence, filtering, and deduplication.
/// </summary>
internal sealed class ReplHistory
{
  private readonly List<string> Items = [];
  private readonly ReplOptions Options;
  private readonly ITerminal Terminal;
  private readonly List<Regex> CompiledIgnorePatterns = [];

  /// <summary>
  /// Gets the number of commands in history.
  /// </summary>
  public int Count => Items.Count;

  /// <summary>
  /// Gets a read-only view of the history items.
  /// </summary>
  public IReadOnlyList<string> AsReadOnly => Items.AsReadOnly();

  /// <summary>
  /// Gets a command at the specified index.
  /// </summary>
  /// <param name="index">The zero-based index of the command to get.</param>
  /// <returns>The command at the specified index.</returns>
  public string this[int index] => Items[index];

  /// <summary>
  /// Creates a new history manager.
  /// </summary>
  /// <param name="options">The REPL configuration options.</param>
  /// <param name="terminal">The terminal I/O provider.</param>
  internal ReplHistory(ReplOptions options, ITerminal terminal)
  {
    Options = options ?? throw new ArgumentNullException(nameof(options));
    Terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));

    // Pre-compile regex patterns for history ignore filters
    if (options.HistoryIgnorePatterns is not null)
    {
      foreach (string pattern in options.HistoryIgnorePatterns)
      {
        if (!string.IsNullOrEmpty(pattern))
        {
          // Convert wildcard pattern to regex pattern
          string regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*", StringComparison.Ordinal)  // * matches any characters
            .Replace("\\?", ".", StringComparison.Ordinal)   // ? matches single character
            + "$";

          CompiledIgnorePatterns.Add(
            new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled)
          );
        }
      }
    }
  }

  /// <summary>
  /// Adds a command to the history with deduplication and size limiting.
  /// </summary>
  /// <param name="command">The command to add to history.</param>
  public void Add(string command)
  {
    // Check if command matches any ignore pattern
    if (ShouldIgnore(command)) return;

    // Don't add if same as last command
    if (Items.Count > 0 && Items[^1] == command) return;

    Items.Add(command);

    // Trim history if it exceeds max size
    while (Items.Count > Options.MaxHistorySize)
    {
      Items.RemoveAt(0);
    }
  }

  /// <summary>
  /// Clears all commands from history.
  /// </summary>
  public void Clear()
  {
    Items.Clear();
  }

  /// <summary>
  /// Determines whether a command should be ignored based on configured patterns.
  /// </summary>
  /// <param name="command">The command to check.</param>
  /// <returns>True if the command matches an ignore pattern; otherwise, false.</returns>
  public bool ShouldIgnore(string command)
  {
    foreach (Regex regex in CompiledIgnorePatterns)
    {
      if (regex.IsMatch(command))
        return true;
    }

    return false;
  }

  /// <summary>
  /// Loads history from persistent storage if configured.
  /// </summary>
  public void Load()
  {
    string historyPath = GetHistoryFilePath();
    if (!File.Exists(historyPath))
    {
      return;
    }

    try
    {
      string[] lines = File.ReadAllLines(historyPath);
      foreach (string line in lines.TakeLast(Options.MaxHistorySize))
      {
        if (!string.IsNullOrWhiteSpace(line))
        {
          Items.Add(line);
        }
      }
    }
    catch (IOException ex)
    {
      Terminal.WriteLine($"Warning: Could not load history: {ex.Message}");
    }
    catch (UnauthorizedAccessException ex)
    {
      Terminal.WriteLine($"Warning: Could not load history: {ex.Message}");
    }
  }

  /// <summary>
  /// Saves history to persistent storage if configured.
  /// </summary>
  public void Save()
  {
    string historyPath = GetHistoryFilePath();

    try
    {
      string? directory = Path.GetDirectoryName(historyPath);
      if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

      File.WriteAllLines(historyPath, Items);
    }
    catch (IOException ex)
    {
      Terminal.WriteLine($"Warning: Could not save history: {ex.Message}");
    }
    catch (UnauthorizedAccessException ex)
    {
      Terminal.WriteLine($"Warning: Could not save history: {ex.Message}");
    }
  }

  /// <summary>
  /// Gets the file path for history persistence.
  /// </summary>
  /// <returns>The full path to the history file.</returns>
  private string GetHistoryFilePath()
  {
    if (!string.IsNullOrEmpty(Options.HistoryFilePath))
    {
      return Options.HistoryFilePath;
    }

    // Use per-app history in ~/.nuru/history/ directory
    string nuruDir = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      ".nuru"
    );
    string historyDir = Path.Combine(nuruDir, "history");

    // Ensure directory exists
    Directory.CreateDirectory(historyDir);

    // Use consistent app name detection
    string appName = AppNameDetector.GetEffectiveAppName();
    return Path.Combine(historyDir, appName);
  }
}
