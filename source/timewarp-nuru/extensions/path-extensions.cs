namespace TimeWarp.Nuru;

/// <summary>
/// Extension methods for Path to capture entry point file information at compile time.
/// Uses CallerFilePath as fallback when AppContext runtime data is not available
/// (e.g., during publish/pack or when running compiled executables).
/// </summary>
public static class PathExtensions
{
  extension(Path)
  {
    /// <summary>
    /// Gets the full path to the entry point source file using CallerFilePath.
    /// Captures the source file path at compile time.
    /// </summary>
    /// <returns>Full path to the calling source file.</returns>
    public static string EntryPointFilePath() => EntryPointImpl();

    /// <summary>
    /// Gets the directory path containing the entry point source file using CallerFilePath.
    /// Captures the source file directory at compile time.
    /// </summary>
    /// <returns>Directory path containing the calling source file, or empty string if unable to determine.</returns>
    public static string EntryPointFileDirectoryPath() =>
      Path.GetDirectoryName(EntryPointImpl()) ?? "";

    private static string EntryPointImpl([System.Runtime.CompilerServices.CallerFilePath] string filePath = "")
      => filePath;
  }
}
