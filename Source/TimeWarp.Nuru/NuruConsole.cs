namespace TimeWarp.Nuru;

/// <summary>
/// Provides abstraction over console output for testing and customization.
/// </summary>
public static class NuruConsole
{
  /// <summary>
  /// Gets or sets the action used to write a line to standard output.
  /// </summary>
  public static Action<string?> WriteLine { get; set; } = System.Console.WriteLine;
  /// <summary>
  /// Gets or sets the action used to write a line to standard error.
  /// </summary>
  public static Action<string?> WriteErrorLine { get; set; } = System.Console.Error.WriteLine;
  /// <summary>
  /// Gets or sets the async function used to write a line to standard error.
  /// </summary>
  public static Func<string?, Task> WriteErrorLineAsync { get; set; } = System.Console.Error.WriteLineAsync;
}