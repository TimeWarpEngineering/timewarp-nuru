namespace TimeWarp.Nuru.IO;

/// <summary>
/// Default implementation of <see cref="ITerminal"/> that wraps <see cref="System.Console"/>
/// with full interactive terminal capabilities.
/// </summary>
/// <remarks>
/// This class provides the production terminal implementation for Nuru applications
/// requiring interactive features such as REPL, tab completion, and arrow key navigation.
/// For testing scenarios, use <see cref="TestTerminal"/> or create a custom implementation.
/// </remarks>
public sealed class NuruTerminal : ITerminal
{
  /// <summary>
  /// Gets the default singleton instance of <see cref="NuruTerminal"/>.
  /// </summary>
  public static NuruTerminal Default { get; } = new();

  /// <inheritdoc />
  public void Write(string message)
    => Console.Write(message);

  /// <inheritdoc />
  public void WriteLine(string? message = null)
    => Console.WriteLine(message ?? string.Empty);

  /// <inheritdoc />
  public Task WriteLineAsync(string? message = null)
    => Console.Out.WriteLineAsync(message);

  /// <inheritdoc />
  public void WriteErrorLine(string? message = null)
    => Console.Error.WriteLine(message ?? string.Empty);

  /// <inheritdoc />
  public Task WriteErrorLineAsync(string? message = null)
    => Console.Error.WriteLineAsync(message);

  /// <inheritdoc />
  public string? ReadLine()
    => Console.ReadLine();

  /// <inheritdoc />
  public ConsoleKeyInfo ReadKey(bool intercept)
    => Console.ReadKey(intercept);

  /// <inheritdoc />
  public void SetCursorPosition(int left, int top)
  {
    try
    {
      Console.SetCursorPosition(left, top);
    }
    catch (ArgumentOutOfRangeException)
    {
      // Silently ignore invalid cursor positions
    }
    catch (IOException)
    {
      // Silently ignore I/O errors (e.g., redirected output)
    }
  }

  /// <inheritdoc />
  public (int Left, int Top) GetCursorPosition()
  {
    try
    {
      return (Console.CursorLeft, Console.CursorTop);
    }
    catch (IOException)
    {
      // Return default if console is redirected
      return (0, 0);
    }
  }

  /// <inheritdoc />
  public int WindowWidth
  {
    get
    {
      try
      {
        return Console.WindowWidth;
      }
      catch (IOException)
      {
        // Return default width if console is redirected
        return 80;
      }
    }
  }

  /// <inheritdoc />
  public bool IsInteractive
    => !Console.IsInputRedirected;

  /// <inheritdoc />
  public bool SupportsColor
    => !Console.IsOutputRedirected && Environment.GetEnvironmentVariable("NO_COLOR") is null;

  /// <inheritdoc />
  public void Clear()
  {
    try
    {
      Console.Clear();
    }
    catch (IOException)
    {
      // Silently ignore if console is redirected
    }
  }
}
