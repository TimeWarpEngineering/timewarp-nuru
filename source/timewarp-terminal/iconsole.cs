namespace TimeWarp.Terminal;

/// <summary>
/// Abstraction for basic console I/O operations.
/// Provides a testable interface for console input and output.
/// </summary>
/// <remarks>
/// Implement this interface for custom console environments such as:
/// <list type="bullet">
///   <item><description>Unit testing with captured output</description></item>
///   <item><description>Web-based terminals</description></item>
///   <item><description>GUI application integration</description></item>
///   <item><description>Remote console access</description></item>
/// </list>
/// </remarks>
public interface IConsole
{
  /// <summary>
  /// Writes the specified string value to the standard output stream.
  /// </summary>
  /// <param name="message">The value to write.</param>
  void Write(string message);

  /// <summary>
  /// Writes the specified string value, followed by the current line terminator,
  /// to the standard output stream.
  /// </summary>
  /// <param name="message">The value to write. If null, only the line terminator is written.</param>
  void WriteLine(string? message = null);

  /// <summary>
  /// Asynchronously writes the specified string value, followed by the current line terminator,
  /// to the standard output stream.
  /// </summary>
  /// <param name="message">The value to write. If null, only the line terminator is written.</param>
  /// <returns>A task that represents the asynchronous write operation.</returns>
  Task WriteLineAsync(string? message = null);

  /// <summary>
  /// Writes the specified string value, followed by the current line terminator,
  /// to the standard error stream.
  /// </summary>
  /// <param name="message">The value to write. If null, only the line terminator is written.</param>
  void WriteErrorLine(string? message = null);

  /// <summary>
  /// Asynchronously writes the specified string value, followed by the current line terminator,
  /// to the standard error stream.
  /// </summary>
  /// <param name="message">The value to write. If null, only the line terminator is written.</param>
  /// <returns>A task that represents the asynchronous write operation.</returns>
  Task WriteErrorLineAsync(string? message = null);

  /// <summary>
  /// Reads the next line of characters from the standard input stream.
  /// </summary>
  /// <returns>
  /// The next line of characters from the input stream, or null if no more lines are available.
  /// </returns>
  string? ReadLine();
}
