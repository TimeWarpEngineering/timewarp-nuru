namespace TimeWarp.Terminal;

/// <summary>
/// A testable implementation of <see cref="IConsole"/> that captures all output
/// and provides scripted input for deterministic testing.
/// </summary>
/// <remarks>
/// Use this class in unit tests to verify console output without interacting with the real console.
/// <example>
/// <code>
/// using TestConsole console = new("command1\nexit\n");
/// NuruCoreApp app = NuruCoreApp.CreateBuilder()
///     .Map("command1", () => "Hello!")
///     .Build(console: console);
///
/// await app.RunAsync(["command1"]);
///
/// Assert.Contains("Hello!", console.Output);
/// </code>
/// </example>
/// </remarks>
public sealed class TestConsole : IConsole, IDisposable
{
  private readonly StringReader InputReader;
  private readonly StringWriter OutputWriter;
  private readonly StringWriter ErrorWriter;
  private bool Disposed;

  /// <summary>
  /// Initializes a new instance of <see cref="TestConsole"/> with optional scripted input.
  /// </summary>
  /// <param name="input">
  /// The input to provide when <see cref="ReadLine"/> is called.
  /// Multiple lines should be separated by newlines.
  /// </param>
  public TestConsole(string input = "")
  {
    InputReader = new StringReader(input);
    OutputWriter = new StringWriter();
    ErrorWriter = new StringWriter();
  }

  /// <summary>
  /// Gets all standard output written to this console.
  /// </summary>
  public string Output => OutputWriter.ToString();

  /// <summary>
  /// Gets all error output written to this console.
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

  /// <summary>
  /// Clears all captured output.
  /// </summary>
  public void Clear()
  {
    OutputWriter.GetStringBuilder().Clear();
    ErrorWriter.GetStringBuilder().Clear();
  }

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
  /// Gets the error lines as an array.
  /// </summary>
  /// <returns>An array of error lines.</returns>
  public string[] GetErrorLines()
    => ErrorOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

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
}
