namespace TimeWarp.Terminal;

/// <summary>
/// A testable implementation of <see cref="ITerminal"/> that captures all output
/// and provides scripted input including key sequences for REPL testing.
/// </summary>
/// <remarks>
/// Use this class to test interactive terminal features like:
/// <list type="bullet">
///   <item><description>Arrow key navigation</description></item>
///   <item><description>Tab completion</description></item>
///   <item><description>Command history</description></item>
///   <item><description>Line editing</description></item>
/// </list>
/// <example>
/// <code>
/// using TestTerminal terminal = new();
/// terminal.QueueKeys("hello");
/// terminal.QueueKey(ConsoleKey.Tab);  // Trigger completion
/// terminal.QueueKey(ConsoleKey.Enter);
/// terminal.QueueLine("exit");
///
/// await app.RunReplAsync(terminal);
///
/// Assert.Contains("hello", terminal.Output);
/// </code>
/// </example>
/// </remarks>
public sealed class TestTerminal : ITerminal, IDisposable
{
  private readonly StringReader InputReader;
  private readonly StringWriter OutputWriter;
  private readonly StringWriter ErrorWriter;
  private readonly Queue<ConsoleKeyInfo> KeyQueue;
  private int CursorLeft;
  private int CursorTop;
  private bool Disposed;

  /// <summary>
  /// Initializes a new instance of <see cref="TestTerminal"/> with optional scripted line input.
  /// </summary>
  /// <param name="input">
  /// The input to provide when <see cref="ReadLine"/> is called.
  /// Multiple lines should be separated by newlines.
  /// </param>
  public TestTerminal(string input = "")
  {
    InputReader = new StringReader(input);
    OutputWriter = new StringWriter();
    ErrorWriter = new StringWriter();
    KeyQueue = new Queue<ConsoleKeyInfo>();
    WindowWidth = 80;
    IsInteractive = false; // Testing is non-interactive by default
    SupportsColor = true;

    // Suppress unused field warnings - these fields will be used when REPL is updated to use ITerminal
    _ = CursorLeft;
    _ = CursorTop;
  }

  /// <summary>
  /// Gets all standard output written to this terminal.
  /// </summary>
  public string Output => OutputWriter.ToString();

  /// <summary>
  /// Gets all error output written to this terminal.
  /// </summary>
  public string ErrorOutput => ErrorWriter.ToString();

  /// <summary>
  /// Gets all output (both standard and error) combined.
  /// </summary>
  public string AllOutput => Output + ErrorOutput;
  /// <inheritdoc />
  public void Write(string message)
    => OutputWriter.Write(message);

  /// <inheritdoc />
  public void WriteLine(string? message = null)
    => OutputWriter.WriteLine(message ?? string.Empty);

  /// <inheritdoc />
  public async Task WriteLineAsync(string? message = null)
    => await OutputWriter.WriteLineAsync(message ?? string.Empty).ConfigureAwait(false);

  /// <inheritdoc />
  public void WriteErrorLine(string? message = null)
    => ErrorWriter.WriteLine(message ?? string.Empty);

  /// <inheritdoc />
  public async Task WriteErrorLineAsync(string? message = null)
    => await ErrorWriter.WriteLineAsync(message ?? string.Empty).ConfigureAwait(false);

  /// <inheritdoc />
  public string? ReadLine()
    => InputReader.ReadLine();

  /// <inheritdoc />
  public ConsoleKeyInfo ReadKey(bool intercept)
  {
    if (KeyQueue.Count > 0)
      return KeyQueue.Dequeue();

    // If no keys queued, try to read from input as a line
    string? line = InputReader.ReadLine();
    if (line is null)
    {
      // EOF - simulate Ctrl+D
      return new ConsoleKeyInfo('\u0004', ConsoleKey.D, false, false, true);
    }

    // Convert line characters to keys and queue them
    foreach (char c in line)
    {
      ConsoleKey key = CharToConsoleKey(c);
      KeyQueue.Enqueue(new ConsoleKeyInfo(c, key, false, false, false));
    }

    // Add Enter at end of line
    KeyQueue.Enqueue(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

    return KeyQueue.Count > 0
      ? KeyQueue.Dequeue()
      : new ConsoleKeyInfo('\0', ConsoleKey.NoName, false, false, false);
  }

  /// <inheritdoc />
  public void SetCursorPosition(int left, int top)
  {
    CursorLeft = left;
    CursorTop = top;
  }

  /// <inheritdoc />
  public (int Left, int Top) GetCursorPosition()
    => (CursorLeft, CursorTop);

  /// <inheritdoc />
  public int WindowWidth { get; set; }

  /// <inheritdoc />
  public bool IsInteractive { get; set; }

  /// <inheritdoc />
  public bool SupportsColor { get; set; }

  /// <inheritdoc />
  public bool SupportsHyperlinks { get; set; }

  /// <inheritdoc />
  public void Clear()
    => OutputWriter.WriteLine("[CLEAR]");

  // ========== Test Helper Methods ==========

  /// <summary>
  /// Queues a single key press for the next <see cref="ReadKey"/> call.
  /// </summary>
  /// <param name="key">The console key to queue.</param>
  /// <param name="shift">Whether Shift is pressed.</param>
  /// <param name="alt">Whether Alt is pressed.</param>
  /// <param name="ctrl">Whether Ctrl is pressed.</param>
  public void QueueKey(ConsoleKey key, bool shift = false, bool alt = false, bool ctrl = false)
  {
    char keyChar = key switch
    {
      ConsoleKey.Tab => '\t',
      ConsoleKey.Enter => '\r',
      ConsoleKey.Escape => '\u001b',
      ConsoleKey.Backspace => '\b',
      ConsoleKey.Spacebar => ' ',
      >= ConsoleKey.A and <= ConsoleKey.Z => (char)('a' + (key - ConsoleKey.A)),
      >= ConsoleKey.D0 and <= ConsoleKey.D9 => (char)('0' + (key - ConsoleKey.D0)),
      _ => '\0'
    };

    KeyQueue.Enqueue(new ConsoleKeyInfo(keyChar, key, shift, alt, ctrl));
  }

  /// <summary>
  /// Queues a <see cref="ConsoleKeyInfo"/> directly.
  /// </summary>
  /// <param name="keyInfo">The key info to queue.</param>
  public void QueueKeyInfo(ConsoleKeyInfo keyInfo)
    => KeyQueue.Enqueue(keyInfo);

  /// <summary>
  /// Queues a string of characters as individual key presses.
  /// </summary>
  /// <param name="text">The text to queue as key presses.</param>
  public void QueueKeys(string text)
  {
    ArgumentNullException.ThrowIfNull(text);
    foreach (char c in text)
    {
      ConsoleKey key = CharToConsoleKey(c);
      KeyQueue.Enqueue(new ConsoleKeyInfo(c, key, false, false, false));
    }
  }

  /// <summary>
  /// Queues a string followed by Enter.
  /// </summary>
  /// <param name="text">The text to queue as a complete line.</param>
  public void QueueLine(string text)
  {
    QueueKeys(text);
    QueueKey(ConsoleKey.Enter);
  }

  /// <summary>
  /// Queues an arrow key press.
  /// </summary>
  /// <param name="direction">The arrow direction (UpArrow, DownArrow, LeftArrow, RightArrow).</param>
  public void QueueArrow(ConsoleKey direction)
  {
    if (direction is ConsoleKey.UpArrow or ConsoleKey.DownArrow
        or ConsoleKey.LeftArrow or ConsoleKey.RightArrow)
    {
      QueueKey(direction);
    }
  }

  /// <summary>
  /// Clears all captured output.
  /// </summary>
  public void ClearOutput()
  {
    OutputWriter.GetStringBuilder().Clear();
    ErrorWriter.GetStringBuilder().Clear();
  }

  /// <summary>
  /// Clears all queued keys.
  /// </summary>
  public void ClearKeys()
    => KeyQueue.Clear();

  /// <summary>
  /// Checks if the standard output contains the specified text.
  /// </summary>
  /// <param name="text">The text to search for.</param>
  /// <returns><c>true</c> if the output contains the text; otherwise, <c>false</c>.</returns>
  public bool OutputContains(string text)
    => Output.Contains(text, StringComparison.Ordinal);

  /// <summary>
  /// Checks if the error output contains the specified text.
  /// </summary>
  /// <param name="text">The text to search for.</param>
  /// <returns><c>true</c> if the error output contains the text; otherwise, <c>false</c>.</returns>
  public bool ErrorContains(string text)
    => ErrorOutput.Contains(text, StringComparison.Ordinal);

  /// <summary>
  /// Gets the output lines as an array.
  /// </summary>
  /// <returns>An array of output lines.</returns>
  public string[] GetOutputLines()
    => Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

  /// <summary>
  /// Gets the number of keys currently in the queue.
  /// </summary>
  public int KeysInQueue => KeyQueue.Count;

  /// <summary>
  /// Disposes the resources used by this instance.
  /// </summary>
  public void Dispose()
  {
    if (Disposed)
      return;

    InputReader.Dispose();
    OutputWriter.Dispose();
    ErrorWriter.Dispose();
    Disposed = true;
  }

  private static ConsoleKey CharToConsoleKey(char c) => c switch
  {
    >= 'a' and <= 'z' => ConsoleKey.A + (c - 'a'),
    >= 'A' and <= 'Z' => ConsoleKey.A + (c - 'A'),
    >= '0' and <= '9' => ConsoleKey.D0 + (c - '0'),
    ' ' => ConsoleKey.Spacebar,
    '\t' => ConsoleKey.Tab,
    '\r' or '\n' => ConsoleKey.Enter,
    '\b' => ConsoleKey.Backspace,
    '-' => ConsoleKey.OemMinus,
    '=' => ConsoleKey.OemPlus,
    '/' => ConsoleKey.Oem2,
    '.' => ConsoleKey.OemPeriod,
    ',' => ConsoleKey.OemComma,
    _ => ConsoleKey.NoName
  };
}
