namespace TimeWarp.Nuru.Repl;

/// <summary>
/// Provides static access to current REPL mode instance.
/// </summary>
internal static class ReplContext
{
  /// <summary>
  /// Gets or sets current REPL mode instance.
  /// </summary>
  public static ReplMode? ReplMode { get; set; }
}
