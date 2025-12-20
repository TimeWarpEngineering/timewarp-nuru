namespace TimeWarp.Terminal;

/// <summary>
/// Default implementation of <see cref="IConsole"/> that wraps <see cref="System.Console"/>.
/// </summary>
/// <remarks>
/// This class provides the production console implementation for Nuru applications.
/// For testing scenarios, use <see cref="TestConsole"/> or create a custom implementation.
/// </remarks>
public sealed class NuruConsole : IConsole
{
  /// <summary>
  /// Gets the default singleton instance of <see cref="NuruConsole"/>.
  /// </summary>
  public static NuruConsole Default { get; } = new();

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
}
