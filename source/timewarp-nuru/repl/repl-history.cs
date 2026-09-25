namespace TimeWarp.Nuru;

/// <summary>
/// Manages command history for REPL sessions including persistence, filtering, and deduplication.
/// </summary>
internal sealed class ReplHistory
{
  // ~/.nuru and ~/.nuru/history are private trust directories. Owner-only on Unix;
  // Windows relies on user-profile ACLs, which already exclude other users.
  private const UnixFileMode OwnerOnlyDirectoryMode =
    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;

  private const UnixFileMode OwnerOnlyFileMode =
    UnixFileMode.UserRead | UnixFileMode.UserWrite;

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

    // Replace, don't append: a second Load must not duplicate the already-loaded entries.
    Items.Clear();

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
      Terminal.WriteLine($"Warning: Could not load history from {historyPath}");
      Terminal.WriteLine($"  Reason: {ex.Message}");
    }
    catch (UnauthorizedAccessException)
    {
      Terminal.WriteLine($"Warning: Permission denied loading history from {historyPath}");
      Terminal.WriteLine("  Set ReplOptions.HistoryFilePath to a writable location or disable with PersistHistory=false");
    }
  }

  /// <summary>
  /// Saves history to persistent storage if configured.
  /// </summary>
  /// <remarks>
  /// On Unix, the history file is written with owner-only mode (0600). On Windows,
  /// the file inherits user-profile ACLs (other users are already excluded).
  /// </remarks>
  public void Save()
  {
    string historyPath = GetHistoryFilePath();

    try
    {
      string? directory = Path.GetDirectoryName(historyPath);
      if (!string.IsNullOrEmpty(directory))
      {
        EnsureOwnerOnlyDirectory(directory);
      }

      // Merge with whatever is on disk now so a concurrent instance's entries are not
      // clobbered by this writer. File entries first (baseline), then our in-memory items
      // not already present; dedup by exact line and cap to MaxHistorySize.
      List<string> merged = [];
      HashSet<string> seen = new(StringComparer.Ordinal);
      if (File.Exists(historyPath))
      {
        foreach (string line in File.ReadAllLines(historyPath))
        {
          if (!string.IsNullOrWhiteSpace(line) && seen.Add(line))
          {
            merged.Add(line);
          }
        }
      }

      foreach (string item in Items)
      {
        if (!string.IsNullOrWhiteSpace(item) && seen.Add(item))
        {
          merged.Add(item);
        }
      }

      if (merged.Count > Options.MaxHistorySize)
      {
        merged.RemoveRange(0, merged.Count - Options.MaxHistorySize);
      }

      WriteOwnerOnlyLines(historyPath, merged);
    }
    catch (IOException ex)
    {
      Terminal.WriteLine($"Warning: Could not save history to {historyPath}");
      Terminal.WriteLine($"  Reason: {ex.Message}");
    }
    catch (UnauthorizedAccessException)
    {
      Terminal.WriteLine($"Warning: Permission denied saving history to {historyPath}");
      Terminal.WriteLine("  Set ReplOptions.HistoryFilePath to a writable location or disable with PersistHistory=false");
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

    // Ensure directory exists with owner-only mode when creating (0700 on Unix).
    EnsureOwnerOnlyDirectory(historyDir);

    // Use consistent app name detection
    string appName = AppNameDetector.GetEffectiveAppName();
    return Path.Combine(historyDir, appName);
  }

  /// <summary>
  /// Creates <paramref name="directory"/> (and missing parents) with owner-only mode on Unix
  /// when they do not already exist. Existing directories are left unchanged.
  /// </summary>
  /// <remarks>
  /// <see cref="Directory.CreateDirectory(string, UnixFileMode)"/> only applies the mode to
  /// the leaf directory; parents created in the same call keep the process umask. Walk
  /// ancestors so each newly created segment (e.g. <c>~/.nuru</c> then <c>history</c>) is 0700.
  /// </remarks>
  private static void EnsureOwnerOnlyDirectory(string directory)
  {
    if (Directory.Exists(directory))
    {
      return;
    }

    if (OperatingSystem.IsWindows())
    {
      Directory.CreateDirectory(directory);
      return;
    }

    // Create missing ancestors first so each new segment receives OwnerOnlyDirectoryMode.
    string? parent = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(directory));
    if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
    {
      EnsureOwnerOnlyDirectory(parent);
    }

    Directory.CreateDirectory(directory, OwnerOnlyDirectoryMode);
  }

  /// <summary>
  /// Writes history lines with owner-only file mode on Unix (0600). On Windows, relies on
  /// user-profile ACLs. Also hardens an existing file that may have been created under a
  /// permissive umask (FileMode.Create truncates in place and keeps the prior mode).
  /// </summary>
  private static void WriteOwnerOnlyLines(string historyPath, IEnumerable<string> lines)
  {
    FileStreamOptions options = new()
    {
      Mode = FileMode.Create,
      Access = FileAccess.Write,
      Share = FileShare.None
    };

    if (!OperatingSystem.IsWindows())
    {
      options.UnixCreateMode = OwnerOnlyFileMode;
    }

    using (FileStream stream = new(historyPath, options))
    using (StreamWriter writer = new(stream))
    {
      foreach (string line in lines)
      {
        writer.WriteLine(line);
      }
    }

    if (!OperatingSystem.IsWindows())
    {
      File.SetUnixFileMode(historyPath, OwnerOnlyFileMode);
    }
  }
}
