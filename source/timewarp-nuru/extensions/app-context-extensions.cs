namespace TimeWarp.Nuru;

/// <summary>
/// Extension methods for AppContext to access entry point file information.
/// This provides runtime-set data for file-based apps (dotnet run file.cs).
/// Available in .NET 10+ file-based apps, but NOT set during publish/pack.
/// </summary>
/// <remarks>
/// See: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-run-script
/// RuntimeHostConfigurationOptions set EntryPointFilePath and EntryPointFileDirectoryPath
/// during dotnet run, but these are virtual only (not preserved after converting to project).
/// </remarks>
public static class AppContextExtensions
{
  extension(AppContext)
  {
    /// <summary>
    /// Gets the entry point file path from AppContext data.
    /// Returns null if not running as a file-based app via dotnet run.
    /// </summary>
    /// <returns>Full path to the entry point source file, or null if not available.</returns>
    public static string? EntryPointFilePath() =>
      AppContext.GetData("EntryPointFilePath") as string;

    /// <summary>
    /// Gets the entry point file directory path from AppContext data.
    /// Returns null if not running as a file-based app via dotnet run.
    /// </summary>
    /// <returns>Directory path containing the entry point source file, or null if not available.</returns>
    public static string? EntryPointFileDirectoryPath() =>
      AppContext.GetData("EntryPointFileDirectoryPath") as string;
  }
}
