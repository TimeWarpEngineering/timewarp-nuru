namespace TimeWarp.Nuru;

/// <summary>
/// Internal abstraction for framework console output.
/// User handlers should write to Console directly or return values for framework display.
/// </summary>
internal static class NuruConsole
{
  private static Action<string?> WriteLineAction = System.Console.WriteLine;
  private static Action<string?> WriteErrorLineAction = System.Console.Error.WriteLine;
  private static Func<string?, Task> WriteErrorLineAsyncFunc = System.Console.Error.WriteLineAsync;

  /// <summary>
  /// Gets or sets the action used to write a line to standard output.
  /// </summary>
  internal static Action<string?> WriteLine
  {
    get => WriteLineAction;
    set => WriteLineAction = value ?? throw new ArgumentNullException(nameof(value));
  }

  /// <summary>
  /// Gets or sets the action used to write a line to standard error.
  /// </summary>
  internal static Action<string?> WriteErrorLine
  {
    get => WriteErrorLineAction;
    set => WriteErrorLineAction = value ?? throw new ArgumentNullException(nameof(value));
  }

  /// <summary>
  /// Gets or sets the async function used to write a line to standard error.
  /// </summary>
  internal static Func<string?, Task> WriteErrorLineAsync
  {
    get => WriteErrorLineAsyncFunc;
    set => WriteErrorLineAsyncFunc = value ?? throw new ArgumentNullException(nameof(value));
  }

  /// <summary>
  /// Asynchronously writes a line to standard output.
  /// </summary>
  internal static Task WriteLineAsync(string? message)
    => System.Console.Out.WriteLineAsync(message);
}
